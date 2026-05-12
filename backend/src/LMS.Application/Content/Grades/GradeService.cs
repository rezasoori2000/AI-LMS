using LMS.Application.Common.Interfaces;
using LMS.Domain.Curriculum;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Content.Grades;

public sealed class GradeService : IGradeService
{
    private readonly ILmsDbContext       _db;
    private readonly ICurrentUserService _currentUser;

    public GradeService(ILmsDbContext db, ICurrentUserService currentUser)
    {
        _db          = db;
        _currentUser = currentUser;
    }

    private bool IsSuperAdmin =>
        string.Equals(_currentUser.Role, "SuperAdmin", StringComparison.Ordinal);

    // ── Queries ───────────────────────────────────────────────────────────────

    public async Task<List<GradeDto>> GetListAsync(CancellationToken ct = default)
    {
        var query = _db.Grades.AsNoTracking();

        if (!IsSuperAdmin)
            query = query.Where(g => g.TenantId == _currentUser.TenantId);

        var grades = await query.OrderBy(g => g.Level).ToListAsync(ct);
        return grades.Select(ToDto).ToList();
    }

    public async Task<GradeDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var grade = await FindOwnedOrThrowAsync(id, ct);
        return ToDto(grade);
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    public async Task<GradeDto> CreateAsync(CreateGradeRequest request, CancellationToken ct = default)
    {
        var tenantId = IsSuperAdmin ? null : _currentUser.TenantId;

        var duplicate = await _db.Grades.AnyAsync(
            g => g.Level == request.Level && g.TenantId == tenantId, ct);
        if (duplicate)
            throw new ContentConflictException(
                $"A grade with level {request.Level} already exists.");

        var grade = Grade.Create(request.Name, request.Level, tenantId);
        _db.Grades.Add(grade);
        await _db.SaveChangesAsync(ct);
        return ToDto(grade);
    }

    public async Task<GradeDto> UpdateAsync(Guid id, UpdateGradeRequest request, CancellationToken ct = default)
    {
        var grade    = await FindOwnedOrThrowAsync(id, ct);
        var tenantId = IsSuperAdmin ? grade.TenantId : _currentUser.TenantId;

        var duplicate = await _db.Grades.AnyAsync(
            g => g.Level == request.Level && g.TenantId == tenantId && g.Id != id, ct);
        if (duplicate)
            throw new ContentConflictException(
                $"A grade with level {request.Level} already exists.");

        grade.Update(request.Name, request.Level);
        await _db.SaveChangesAsync(ct);
        return ToDto(grade);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var grade = await FindOwnedOrThrowAsync(id, ct);
        _db.Grades.Remove(grade);
        await _db.SaveChangesAsync(ct);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task<Grade> FindOwnedOrThrowAsync(Guid id, CancellationToken ct)
    {
        Grade? grade;
        if (IsSuperAdmin)
            grade = await _db.Grades.FirstOrDefaultAsync(g => g.Id == id, ct);
        else
            grade = await _db.Grades.FirstOrDefaultAsync(
                g => g.Id == id && g.TenantId == _currentUser.TenantId, ct);

        return grade ?? throw new ContentNotFoundException("Grade", id);
    }

    private static GradeDto ToDto(Grade g) =>
        new(g.Id, g.Name, g.Level, g.TenantId, g.CreatedAt, g.UpdatedAt);
}
