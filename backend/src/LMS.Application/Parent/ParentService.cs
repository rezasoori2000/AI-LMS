using LMS.Application.Common.Interfaces;
using LMS.Application.Parent.Dtos;
using LMS.Domain.Progress;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Parent;

/// <summary>
/// Read-only service for parent-facing queries.
///
/// Access control:
///   Every public method resolves the calling parent's <c>ParentProfile.Id</c> from
///   <see cref="ICurrentUserService.UserId"/> and uses it as the hard ownership gate.
///   A parent can only see students whose <c>StudentProfile.ParentId</c> matches
///   their own profile.
///
/// Empty-state policy:
///   When a parent has no linked students — valid in Phase 1 because linkage is
///   admin-controlled — <see cref="GetMyChildrenAsync"/> returns an empty list rather
///   than throwing.  The frontend is expected to render a friendly empty state.
///
/// N+1 avoidance:
///   List queries aggregate lesson progress counts in a single DB round-trip using
///   subquery projections.  The detail view issues three targeted queries (student,
///   enrollments, progress) which is acceptable for Phase 1 cardinality (1–5 enrollments
///   per student, ~10 lessons per enrollment).
/// </summary>
public sealed class ParentService : IParentService
{
    private readonly ILmsDbContext       _db;
    private readonly ICurrentUserService _currentUser;

    public ParentService(ILmsDbContext db, ICurrentUserService currentUser)
    {
        _db          = db;
        _currentUser = currentUser;
    }

    // ── Queries ───────────────────────────────────────────────────────────────

    public async Task<List<ChildSummaryDto>> GetMyChildrenAsync(
        CancellationToken ct = default)
    {
        var parentProfileId = await ResolveParentProfileIdAsync(ct);

        // parentProfileId is null when no ParentProfile exists for this user
        // (e.g. a parent was registered before the Option B flow was in place).
        // Return empty list defensively rather than throwing.
        if (parentProfileId is null)
            return [];

        var students = await _db.StudentProfiles
            .AsNoTracking()
            .Where(s => s.ParentId == parentProfileId)
            .Include(s => s.User)
            .Include(s => s.Grade)
            .ToListAsync(ct);

        if (students.Count == 0)
            return [];

        var studentIds = students.Select(s => s.Id).ToList();

        // Count active enrollments per student in one query
        var activeEnrollmentCounts = await _db.Enrollments
            .AsNoTracking()
            .Where(e => studentIds.Contains(e.StudentId)
                     && e.Status == Domain.Enrollments.EnrollmentStatus.Active)
            .GroupBy(e => e.StudentId)
            .Select(g => new { StudentId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.StudentId, x => x.Count, ct);

        // Count completed lessons per student in one query
        var completedLessonCounts = await _db.LessonProgress
            .AsNoTracking()
            .Where(p => studentIds.Contains(p.StudentId)
                     && p.Status == ProgressStatus.Completed)
            .GroupBy(p => p.StudentId)
            .Select(g => new { StudentId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.StudentId, x => x.Count, ct);

        // Last activity per student (most recent CompletedAt or StartedAt)
        var lastActivity = await _db.LessonProgress
            .AsNoTracking()
            .Where(p => studentIds.Contains(p.StudentId))
            .GroupBy(p => p.StudentId)
            .Select(g => new
            {
                StudentId    = g.Key,
                LastActivity = g.Max(p => p.CompletedAt ?? p.StartedAt),
            })
            .ToDictionaryAsync(x => x.StudentId, x => x.LastActivity, ct);

        return students.Select(s => new ChildSummaryDto(
            StudentId:         s.Id,
            FullName:          $"{s.User.FirstName} {s.User.LastName}".Trim(),
            GradeName:         s.Grade?.Name,
            ActiveEnrollments: activeEnrollmentCounts.GetValueOrDefault(s.Id, 0),
            LessonsCompleted:  completedLessonCounts.GetValueOrDefault(s.Id, 0),
            LastActivityAt:    lastActivity.GetValueOrDefault(s.Id)
        )).ToList();
    }

    public async Task<ChildDetailDto> GetChildDetailAsync(
        Guid studentId,
        CancellationToken ct = default)
    {
        var parentProfileId = await ResolveParentProfileIdAsync(ct);

        // Verify ownership — student must be linked to this parent.
        // Null parentProfileId means no profile exists; treat as denied.
        var student = await _db.StudentProfiles
            .AsNoTracking()
            .Include(s => s.User)
            .Include(s => s.Grade)
            .FirstOrDefaultAsync(s => s.Id == studentId, ct);

        if (student is null || student.ParentId != parentProfileId)
            throw new ParentAccessDeniedException(studentId);

        // Build summary (re-use the same aggregation logic)
        var activeEnrollmentCount = await _db.Enrollments
            .AsNoTracking()
            .CountAsync(e => e.StudentId == studentId
                          && e.Status == Domain.Enrollments.EnrollmentStatus.Active, ct);

        var completedLessonCount = await _db.LessonProgress
            .AsNoTracking()
            .CountAsync(p => p.StudentId == studentId
                          && p.Status == ProgressStatus.Completed, ct);

        var lastActivity = await _db.LessonProgress
            .AsNoTracking()
            .Where(p => p.StudentId == studentId)
            .MaxAsync(p => (DateTime?)(p.CompletedAt ?? p.StartedAt), ct);

        var summary = new ChildSummaryDto(
            StudentId:         student.Id,
            FullName:          $"{student.User.FirstName} {student.User.LastName}".Trim(),
            GradeName:         student.Grade?.Name,
            ActiveEnrollments: activeEnrollmentCount,
            LessonsCompleted:  completedLessonCount,
            LastActivityAt:    lastActivity);

        // Load enrollments with subject names
        var enrollments = await _db.Enrollments
            .AsNoTracking()
            .Include(e => e.Subject)
            .Where(e => e.StudentId == studentId)
            .OrderBy(e => e.EnrolledAt)
            .ToListAsync(ct);

        if (enrollments.Count == 0)
            return new ChildDetailDto(summary, []);

        // Load all lesson progress for this student in one query,
        // including Lesson → Chapter so we can filter by SubjectId
        var allProgress = await _db.LessonProgress
            .AsNoTracking()
            .Include(p => p.Lesson)
                .ThenInclude(l => l.Chapter)
            .Where(p => p.StudentId == studentId)
            .ToListAsync(ct);

        // For each enrollment, count lessons in the subject's chapters
        var subjectIds = enrollments.Select(e => e.SubjectId).Distinct().ToList();

        // Count total lessons per subject (via Chapter → Lesson join)
        var totalLessonsPerSubject = await _db.Lessons
            .AsNoTracking()
            .Include(l => l.Chapter)
            .Where(l => subjectIds.Contains(l.Chapter.SubjectId))
            .GroupBy(l => l.Chapter.SubjectId)
            .Select(g => new { SubjectId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.SubjectId, x => x.Count, ct);

        // Build per-enrollment summaries
        var enrollmentSummaries = enrollments.Select(e =>
        {
            var subjectProgress = allProgress
                .Where(p => p.Lesson?.Chapter?.SubjectId == e.SubjectId)
                .ToList();

            var completed   = subjectProgress.Count(p => p.Status == ProgressStatus.Completed);
            var inProgress  = subjectProgress.Count(p => p.Status == ProgressStatus.InProgress);

            decimal? avgScore = subjectProgress.Any(p => p.ScorePercent.HasValue)
                ? subjectProgress
                    .Where(p => p.ScorePercent.HasValue)
                    .Average(p => p.ScorePercent!.Value)
                : null;

            return new EnrollmentSummaryDto(
                EnrollmentId:      e.Id,
                SubjectId:         e.SubjectId,
                SubjectName:       e.Subject.Name,
                Status:            e.Status,
                EnrolledAt:        e.EnrolledAt,
                TotalLessons:      totalLessonsPerSubject.GetValueOrDefault(e.SubjectId, 0),
                CompletedLessons:  completed,
                InProgressLessons: inProgress,
                AverageScore:      avgScore.HasValue
                    ? Math.Round(avgScore.Value, 1)
                    : null);
        }).ToList();

        return new ChildDetailDto(summary, enrollmentSummaries);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Resolves the ParentProfile.Id for the calling user.
    /// Returns null if no ParentProfile exists (rather than throwing).
    /// </summary>
    private async Task<Guid?> ResolveParentProfileIdAsync(CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId is null)
            return null;

        return await _db.ParentProfiles
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .Select(p => (Guid?)p.Id)
            .FirstOrDefaultAsync(ct);
    }
}
