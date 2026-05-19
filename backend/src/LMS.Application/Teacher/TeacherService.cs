using LMS.Application.Common.Interfaces;
using LMS.Application.Teacher.Dtos;
using LMS.Domain.Enrollments;
using LMS.Domain.Progress;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Teacher;

/// <summary>
/// EF Core implementation of <see cref="ITeacherService"/>.
///
/// Access control:
///   Every public method calls <see cref="ResolveTeacherUserId"/> to get the calling
///   teacher's <c>User.Id</c> from JWT claims.  Per-student access is protected by
///   <see cref="AssertAssignedStudentAsync"/> which throws <see cref="TeacherAccessDeniedException"/>
///   (→ HTTP 403) when the <c>TeacherStudentAssignment</c> table has no matching row.
///
/// Query strategy (N+1 avoidance):
///   Aggregate counts are computed in dedicated grouped queries returning dictionaries,
///   then merged in memory.  This is the same pattern used by StudentService and ParentService.
/// </summary>
public sealed class TeacherService : ITeacherService
{
    private readonly ILmsDbContext       _db;
    private readonly ICurrentUserService _currentUser;

    public TeacherService(ILmsDbContext db, ICurrentUserService currentUser)
    {
        _db          = db;
        _currentUser = currentUser;
    }

    // ── Access helpers ────────────────────────────────────────────────────────

    private Guid ResolveTeacherUserId()
    {
        var userId = _currentUser.UserId;
        if (userId is null || userId == Guid.Empty)
            throw new TeacherAccessDeniedException();
        return userId.Value;
    }

    private async Task AssertAssignedStudentAsync(
        Guid teacherUserId,
        Guid studentId,
        CancellationToken ct)
    {
        var assigned = await _db.TeacherStudentAssignments
            .AnyAsync(a => a.TeacherUserId == teacherUserId && a.StudentProfileId == studentId, ct);

        if (!assigned)
            throw new TeacherAccessDeniedException(studentId);
    }

    // ── Dashboard ─────────────────────────────────────────────────────────────

    public async Task<TeacherSummaryDto> GetSummaryAsync(CancellationToken ct = default)
    {
        var teacherUserId = ResolveTeacherUserId();

        var studentIds = await _db.TeacherStudentAssignments
            .AsNoTracking()
            .Where(a => a.TeacherUserId == teacherUserId)
            .Select(a => a.StudentProfileId)
            .ToListAsync(ct);

        if (studentIds.Count == 0)
            return new TeacherSummaryDto(0, 0, 0);

        var activeEnrollments = await _db.Enrollments
            .AsNoTracking()
            .CountAsync(e => studentIds.Contains(e.StudentId)
                          && e.Status == EnrollmentStatus.Active, ct);

        var weekStart = DateTime.UtcNow.AddDays(-7);
        var completionsThisWeek = await _db.LessonProgress
            .AsNoTracking()
            .CountAsync(lp => studentIds.Contains(lp.StudentId)
                           && lp.Status == ProgressStatus.Completed
                           && lp.CompletedAt >= weekStart, ct);

        return new TeacherSummaryDto(studentIds.Count, activeEnrollments, completionsThisWeek);
    }

    // ── Student list ──────────────────────────────────────────────────────────

    public async Task<List<AssignedStudentSummaryDto>> GetMyStudentsAsync(
        CancellationToken ct = default)
    {
        var teacherUserId = ResolveTeacherUserId();

        var assignedIds = await _db.TeacherStudentAssignments
            .AsNoTracking()
            .Where(a => a.TeacherUserId == teacherUserId)
            .Select(a => a.StudentProfileId)
            .ToListAsync(ct);

        var students = await _db.StudentProfiles
            .AsNoTracking()
            .Where(s => assignedIds.Contains(s.Id))
            .OrderBy(s => s.User.LastName)
            .ThenBy(s => s.User.FirstName)
            .Select(s => new
            {
                s.Id,
                s.User.FirstName,
                s.User.LastName,
                s.User.Email,
                GradeName = s.Grade != null ? s.Grade.Name : (string?)null,
            })
            .ToListAsync(ct);

        if (students.Count == 0)
            return [];

        var studentIds = students.Select(s => s.Id).ToList();

        // Active enrollment counts per student
        var enrollmentCounts = await _db.Enrollments
            .AsNoTracking()
            .Where(e => studentIds.Contains(e.StudentId)
                     && e.Status == EnrollmentStatus.Active)
            .GroupBy(e => e.StudentId)
            .Select(g => new { StudentId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.StudentId, x => x.Count, ct);

        // Completed lesson counts per student
        var completedCounts = await _db.LessonProgress
            .AsNoTracking()
            .Where(lp => studentIds.Contains(lp.StudentId)
                      && lp.Status == ProgressStatus.Completed)
            .GroupBy(lp => lp.StudentId)
            .Select(g => new { StudentId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.StudentId, x => x.Count, ct);

        return students.Select(s => new AssignedStudentSummaryDto(
            StudentId:             s.Id,
            FullName:              $"{s.FirstName} {s.LastName}".Trim(),
            Email:                 s.Email,
            GradeName:             s.GradeName,
            ActiveEnrollmentCount: enrollmentCounts.GetValueOrDefault(s.Id, 0),
            TotalLessonsCompleted: completedCounts.GetValueOrDefault(s.Id, 0)
        )).ToList();
    }

    // ── Student detail (enrollments) ──────────────────────────────────────────

    public async Task<TeacherStudentDetailDto> GetStudentDetailAsync(
        Guid studentId,
        CancellationToken ct = default)
    {
        var teacherUserId = ResolveTeacherUserId();
        await AssertAssignedStudentAsync(teacherUserId, studentId, ct);

        var student = await _db.StudentProfiles
            .AsNoTracking()
            .Include(s => s.User)
            .Include(s => s.Grade)
            .FirstAsync(s => s.Id == studentId, ct);

        var enrollments = await _db.Enrollments
            .AsNoTracking()
            .Include(e => e.Subject)
            .Where(e => e.StudentId == studentId)
            .OrderByDescending(e => e.EnrolledAt)
            .ToListAsync(ct);

        var fullName = $"{student.User.FirstName} {student.User.LastName}".Trim();

        if (enrollments.Count == 0)
            return new TeacherStudentDetailDto(studentId, fullName, student.User.Email,
                student.Grade?.Name, []);

        var subjectIds = enrollments.Select(e => e.SubjectId).ToList();

        // Total lesson count per subject
        var totalLessonsPerSubject = await _db.Lessons
            .AsNoTracking()
            .Include(l => l.Chapter)
            .Where(l => subjectIds.Contains(l.Chapter.SubjectId))
            .GroupBy(l => l.Chapter.SubjectId)
            .Select(g => new { SubjectId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.SubjectId, x => x.Count, ct);

        // All progress rows for this student (across all their enrollments)
        var progressRows = await _db.LessonProgress
            .AsNoTracking()
            .Include(lp => lp.Lesson).ThenInclude(l => l.Chapter)
            .Where(lp => lp.StudentId == studentId)
            .Select(lp => new
            {
                SubjectId   = lp.Lesson.Chapter.SubjectId,
                lp.Status,
                lp.CompletedAt,
                lp.StartedAt,
            })
            .ToListAsync(ct);

        var progressBySubject = progressRows
            .GroupBy(p => p.SubjectId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var enrollmentDtos = enrollments.Select(e =>
        {
            var rows       = progressBySubject.GetValueOrDefault(e.SubjectId, []);
            var completed  = rows.Count(p => p.Status == ProgressStatus.Completed);
            var inProgress = rows.Count(p => p.Status == ProgressStatus.InProgress);

            var dates = rows
                .SelectMany(p => new[] { p.CompletedAt, p.StartedAt })
                .Where(d => d.HasValue)
                .Select(d => d!.Value)
                .ToList();
            DateTime? lastActivityAt = dates.Count > 0 ? dates.Max() : null;

            return new TeacherEnrollmentItemDto(
                EnrollmentId:     e.Id,
                SubjectId:        e.SubjectId,
                SubjectName:      e.Subject.Name,
                Status:           e.Status,
                EnrolledAt:       e.EnrolledAt,
                LessonsTotal:     totalLessonsPerSubject.GetValueOrDefault(e.SubjectId, 0),
                LessonsCompleted: completed,
                LessonsInProgress: inProgress,
                LastActivityAt:   lastActivityAt
            );
        }).ToList();

        return new TeacherStudentDetailDto(studentId, fullName, student.User.Email,
            student.Grade?.Name, enrollmentDtos);
    }

    // ── Student progress detail ───────────────────────────────────────────────

    public async Task<TeacherStudentProgressDto> GetStudentProgressAsync(
        Guid studentId,
        CancellationToken ct = default)
    {
        var teacherUserId = ResolveTeacherUserId();
        await AssertAssignedStudentAsync(teacherUserId, studentId, ct);

        var student = await _db.StudentProfiles
            .AsNoTracking()
            .Include(s => s.User)
            .FirstAsync(s => s.Id == studentId, ct);

        var fullName = $"{student.User.FirstName} {student.User.LastName}".Trim();

        // Scope to active enrollments only
        var activeSubjectIds = await _db.Enrollments
            .AsNoTracking()
            .Where(e => e.StudentId == studentId && e.Status == EnrollmentStatus.Active)
            .Select(e => e.SubjectId)
            .ToListAsync(ct);

        if (activeSubjectIds.Count == 0)
            return new TeacherStudentProgressDto(studentId, fullName, []);

        // All lessons in active enrolled subjects with chapter/subject metadata
        var lessonData = await _db.Lessons
            .AsNoTracking()
            .Include(l => l.Chapter)
            .Where(l => activeSubjectIds.Contains(l.Chapter.SubjectId))
            .OrderBy(l => l.Chapter.SubjectId)
            .ThenBy(l => l.Chapter.Order)
            .ThenBy(l => l.Order)
            .Select(l => new
            {
                l.Id,
                l.Title,
                SubjectId    = l.Chapter.SubjectId,
                ChapterTitle = l.Chapter.Title,
            })
            .ToListAsync(ct);

        // Subject names in one query
        var subjectNames = await _db.Subjects
            .AsNoTracking()
            .Where(s => activeSubjectIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.Name, ct);

        // Progress for this student — one query, merged in memory
        var progressMap = await _db.LessonProgress
            .AsNoTracking()
            .Where(lp => lp.StudentId == studentId)
            .ToDictionaryAsync(lp => lp.LessonId, lp => lp, ct);

        var items = lessonData.Select(l =>
        {
            progressMap.TryGetValue(l.Id, out var p);
            return new TeacherLessonProgressItemDto(
                LessonId:     l.Id,
                LessonTitle:  l.Title,
                SubjectId:    l.SubjectId,
                SubjectName:  subjectNames.GetValueOrDefault(l.SubjectId, string.Empty),
                ChapterTitle: l.ChapterTitle,
                Status:       p?.Status ?? ProgressStatus.NotStarted,
                ScorePercent: p?.ScorePercent,
                StartedAt:    p?.StartedAt,
                CompletedAt:  p?.CompletedAt
            );
        }).ToList();

        return new TeacherStudentProgressDto(studentId, fullName, items);
    }
}
