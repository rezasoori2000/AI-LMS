using LMS.Application.Common.Interfaces;
using LMS.Domain.Students;
using LMS.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Admin.Students;

/// <summary>
/// Implements <see cref="IStudentAdminService"/> using <see cref="ILmsDbContext"/>.
///
/// Design notes:
/// - All write operations go through domain methods (<c>LinkParent</c> / <c>UnlinkParent</c>)
///   rather than setting properties directly, so domain invariants are enforced.
/// - No TenantId filtering in Phase 1 — added in Phase 3 via EF global query filter.
/// - No pagination — acceptable for Phase 1 class sizes (≤ 100 students per tenant).
/// </summary>
public sealed class StudentAdminService : IStudentAdminService
{
    private readonly ILmsDbContext       _db;
    private readonly ICurrentUserService _currentUser;

    public StudentAdminService(ILmsDbContext db, ICurrentUserService currentUser)
    {
        _db          = db;
        _currentUser = currentUser;
    }

    // ── Queries ───────────────────────────────────────────────────────────────

    public async Task<List<StudentLinkSummaryDto>> GetStudentsAsync(
        CancellationToken ct = default)
    {
        // Load all students with their User and Grade in one query.
        var students = await _db.StudentProfiles
            .AsNoTracking()
            .Include(s => s.User)
            .Include(s => s.Grade)
            .OrderBy(s => s.User.LastName)
            .ThenBy(s => s.User.FirstName)
            .ToListAsync(ct);

        if (students.Count == 0)
            return [];

        // Load linked parent profiles for the students that have one.
        var parentIds = students
            .Where(s => s.ParentId.HasValue)
            .Select(s => s.ParentId!.Value)
            .Distinct()
            .ToList();

        var parents = parentIds.Count > 0
            ? await _db.ParentProfiles
                .AsNoTracking()
                .Include(p => p.User)
                .Where(p => parentIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, ct)
            : new Dictionary<Guid, ParentProfile>();

        // Load teacher assignments via the M:M join table.
        // Phase 1 convention: at most one assignment per student; take the most recent.
        var studentProfileIds = students.Select(s => s.Id).ToList();
        var allAssignments = await _db.TeacherStudentAssignments
            .AsNoTracking()
            .Where(a => studentProfileIds.Contains(a.StudentProfileId))
            .ToListAsync(ct);

        // One assignment per student: latest wins (Phase 1 safety net).
        var assignmentByStudent = allAssignments
            .GroupBy(a => a.StudentProfileId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(a => a.AssignedAt).First());

        var teacherUserIds = assignmentByStudent.Values
            .Select(a => a.TeacherUserId)
            .Distinct()
            .ToList();

        var teachers = teacherUserIds.Count > 0
            ? await _db.Users
                .AsNoTracking()
                .Where(u => teacherUserIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, ct)
            : new Dictionary<Guid, Domain.Users.User>();

        return students.Select(s =>
        {
            ParentProfile? parent = s.ParentId.HasValue
                ? parents.GetValueOrDefault(s.ParentId.Value)
                : null;

            assignmentByStudent.TryGetValue(s.Id, out var teacherAssignment);
            Domain.Users.User? teacher = teacherAssignment is not null
                ? teachers.GetValueOrDefault(teacherAssignment.TeacherUserId)
                : null;

            return new StudentLinkSummaryDto(
                StudentId:       s.Id,
                FullName:        $"{s.User.FirstName} {s.User.LastName}".Trim(),
                GradeName:       s.Grade?.Name,
                ParentProfileId: s.ParentId,
                ParentFullName:  parent is not null
                    ? $"{parent.User.FirstName} {parent.User.LastName}".Trim()
                    : null,
                ParentEmail:     parent?.User.Email,
                TeacherUserId:   teacherAssignment?.TeacherUserId,
                TeacherFullName: teacher is not null
                    ? $"{teacher.FirstName} {teacher.LastName}".Trim()
                    : null);
        }).ToList();
    }

    public async Task<List<ParentOptionDto>> GetParentOptionsAsync(
        CancellationToken ct = default)
    {
        return await _db.ParentProfiles
            .AsNoTracking()
            .Include(p => p.User)
            .OrderBy(p => p.User.LastName)
            .ThenBy(p => p.User.FirstName)
            .Select(p => new ParentOptionDto(
                p.Id,
                $"{p.User.FirstName} {p.User.LastName}".Trim(),
                p.User.Email))
            .ToListAsync(ct);
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    public async Task<StudentLinkSummaryDto> AssignParentAsync(
        Guid                studentId,
        AssignParentRequest request,
        CancellationToken   ct = default)
    {
        // Load the student (tracked — we need to mutate it).
        var student = await _db.StudentProfiles
            .Include(s => s.User)
            .Include(s => s.Grade)
            .FirstOrDefaultAsync(s => s.Id == studentId, ct)
            ?? throw new KeyNotFoundException($"Student profile '{studentId}' was not found.");

        Domain.Students.ParentProfile? newParent = null;

        if (request.ParentProfileId.HasValue)
        {
            // Validate the target parent profile exists.
            newParent = await _db.ParentProfiles
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == request.ParentProfileId.Value, ct)
                ?? throw new ArgumentException(
                    $"Parent profile '{request.ParentProfileId.Value}' was not found.",
                    nameof(request));

            student.LinkParent(request.ParentProfileId.Value);
        }
        else
        {
            student.UnlinkParent();
        }

        await _db.SaveChangesAsync(ct);

        // Resolve current teacher from the join table (Phase 1: at most one).
        var teacherAssignment = await _db.TeacherStudentAssignments
            .AsNoTracking()
            .Where(a => a.StudentProfileId == student.Id)
            .OrderByDescending(a => a.AssignedAt)
            .FirstOrDefaultAsync(ct);

        Domain.Users.User? currentTeacher = teacherAssignment is not null
            ? await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == teacherAssignment.TeacherUserId, ct)
            : null;

        return new StudentLinkSummaryDto(
            StudentId:       student.Id,
            FullName:        $"{student.User.FirstName} {student.User.LastName}".Trim(),
            GradeName:       student.Grade?.Name,
            ParentProfileId: student.ParentId,
            ParentFullName:  newParent is not null
                ? $"{newParent.User.FirstName} {newParent.User.LastName}".Trim()
                : null,
            ParentEmail:     newParent?.User.Email,
            TeacherUserId:   teacherAssignment?.TeacherUserId,
            TeacherFullName: currentTeacher is not null
                ? $"{currentTeacher.FirstName} {currentTeacher.LastName}".Trim()
                : null);
    }

    public async Task<List<TeacherOptionDto>> GetTeacherOptionsAsync(
        CancellationToken ct = default)
    {
        return await _db.Users
            .AsNoTracking()
            .Where(u => u.Role == UserRole.Teacher && u.IsActive)
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .Select(u => new TeacherOptionDto(
                u.Id,
                $"{u.FirstName} {u.LastName}".Trim(),
                u.Email))
            .ToListAsync(ct);
    }

    public async Task<StudentLinkSummaryDto> AssignTeacherAsync(
        Guid                 studentId,
        AssignTeacherRequest request,
        CancellationToken    ct = default)
    {
        // Not mutating StudentProfile — load as no-tracking for DTO assembly.
        var student = await _db.StudentProfiles
            .AsNoTracking()
            .Include(s => s.User)
            .Include(s => s.Grade)
            .FirstOrDefaultAsync(s => s.Id == studentId, ct)
            ?? throw new KeyNotFoundException($"Student profile '{studentId}' was not found.");

        // Phase 1 convention: replace-assign — remove all existing assignments first.
        var existing = await _db.TeacherStudentAssignments
            .Where(a => a.StudentProfileId == studentId)
            .ToListAsync(ct);
        _db.TeacherStudentAssignments.RemoveRange(existing);

        Domain.Users.User? newTeacher = null;

        if (request.TeacherUserId.HasValue)
        {
            // Validate that the target user exists and holds the Teacher role.
            newTeacher = await _db.Users
                .FirstOrDefaultAsync(u => u.Id == request.TeacherUserId.Value
                                       && u.Role == UserRole.Teacher, ct)
                ?? throw new ArgumentException(
                    $"Teacher user '{request.TeacherUserId.Value}' was not found or is not a teacher.",
                    nameof(request));

            var adminId    = _currentUser.UserId ?? Guid.Empty;
            var assignment = TeacherStudentAssignment.Create(
                request.TeacherUserId.Value, studentId, adminId);
            _db.TeacherStudentAssignments.Add(assignment);
        }

        await _db.SaveChangesAsync(ct);

        // Resolve current parent name if one is linked
        Domain.Students.ParentProfile? currentParent = student.ParentId.HasValue
            ? await _db.ParentProfiles
                .AsNoTracking()
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == student.ParentId.Value, ct)
            : null;

        return new StudentLinkSummaryDto(
            StudentId:       student.Id,
            FullName:        $"{student.User.FirstName} {student.User.LastName}".Trim(),
            GradeName:       student.Grade?.Name,
            ParentProfileId: student.ParentId,
            ParentFullName:  currentParent is not null
                ? $"{currentParent.User.FirstName} {currentParent.User.LastName}".Trim()
                : null,
            ParentEmail:     currentParent?.User.Email,
            TeacherUserId:   request.TeacherUserId,
            TeacherFullName: newTeacher is not null
                ? $"{newTeacher.FirstName} {newTeacher.LastName}".Trim()
                : null);
    }
}
