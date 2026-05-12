using LMS.Application.Common.Interfaces;
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
    private readonly ILmsDbContext _db;

    public StudentAdminService(ILmsDbContext db) => _db = db;

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
            : new Dictionary<Guid, Domain.Students.ParentProfile>();

        return students.Select(s =>
        {
            Domain.Students.ParentProfile? parent = s.ParentId.HasValue
                ? parents.GetValueOrDefault(s.ParentId.Value)
                : null;

            return new StudentLinkSummaryDto(
                StudentId:       s.Id,
                FullName:        $"{s.User.FirstName} {s.User.LastName}".Trim(),
                GradeName:       s.Grade?.Name,
                ParentProfileId: s.ParentId,
                ParentFullName:  parent is not null
                    ? $"{parent.User.FirstName} {parent.User.LastName}".Trim()
                    : null,
                ParentEmail: parent?.User.Email);
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

        return new StudentLinkSummaryDto(
            StudentId:       student.Id,
            FullName:        $"{student.User.FirstName} {student.User.LastName}".Trim(),
            GradeName:       student.Grade?.Name,
            ParentProfileId: student.ParentId,
            ParentFullName:  newParent is not null
                ? $"{newParent.User.FirstName} {newParent.User.LastName}".Trim()
                : null,
            ParentEmail: newParent?.User.Email);
    }
}
