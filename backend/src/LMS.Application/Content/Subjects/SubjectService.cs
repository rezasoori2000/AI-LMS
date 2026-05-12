using LMS.Application.Common.Interfaces;
using LMS.Domain.Curriculum;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Content.Subjects;

public sealed class SubjectService : ISubjectService
{
    private readonly ILmsDbContext       _db;
    private readonly ICurrentUserService _currentUser;

    public SubjectService(ILmsDbContext db, ICurrentUserService currentUser)
    {
        _db          = db;
        _currentUser = currentUser;
    }

    private bool IsSuperAdmin =>
        string.Equals(_currentUser.Role, "SuperAdmin", StringComparison.Ordinal);

    // ── Queries ───────────────────────────────────────────────────────────────

    public async Task<List<SubjectDto>> GetListAsync(CancellationToken ct = default)
    {
        var query = _db.Subjects.AsNoTracking();

        if (!IsSuperAdmin)
            query = query.Where(s => s.TenantId == _currentUser.TenantId);

        var subjects = await query.OrderBy(s => s.Name).ToListAsync(ct);
        return subjects.Select(ToDto).ToList();
    }

    public async Task<SubjectDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var subject = await FindOwnedOrThrowAsync(id, ct);
        return ToDto(subject);
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    public async Task<SubjectDto> CreateAsync(CreateSubjectRequest request, CancellationToken ct = default)
    {
        var tenantId = IsSuperAdmin ? null : _currentUser.TenantId;
        var slug     = request.Slug.ToLowerInvariant().Trim();

        var duplicate = await _db.Subjects.AnyAsync(
            s => s.Slug == slug && s.TenantId == tenantId, ct);
        if (duplicate)
            throw new ContentConflictException(
                $"A subject with slug '{slug}' already exists.");

        var subject = Subject.Create(request.Name, slug, request.Description, tenantId);
        _db.Subjects.Add(subject);
        await _db.SaveChangesAsync(ct);
        return ToDto(subject);
    }

    public async Task<SubjectDto> UpdateAsync(Guid id, UpdateSubjectRequest request, CancellationToken ct = default)
    {
        var subject  = await FindOwnedOrThrowAsync(id, ct);
        var tenantId = IsSuperAdmin ? subject.TenantId : _currentUser.TenantId;
        var slug     = request.Slug.ToLowerInvariant().Trim();

        var duplicate = await _db.Subjects.AnyAsync(
            s => s.Slug == slug && s.TenantId == tenantId && s.Id != id, ct);
        if (duplicate)
            throw new ContentConflictException(
                $"A subject with slug '{slug}' already exists.");

        subject.Update(request.Name, slug, request.Description);
        await _db.SaveChangesAsync(ct);
        return ToDto(subject);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var subject = await FindOwnedOrThrowAsync(id, ct);
        _db.Subjects.Remove(subject);
        await _db.SaveChangesAsync(ct);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task<Subject> FindOwnedOrThrowAsync(Guid id, CancellationToken ct)
    {
        Subject? subject;
        if (IsSuperAdmin)
            subject = await _db.Subjects.FirstOrDefaultAsync(s => s.Id == id, ct);
        else
            subject = await _db.Subjects.FirstOrDefaultAsync(
                s => s.Id == id && s.TenantId == _currentUser.TenantId, ct);

        return subject ?? throw new ContentNotFoundException("Subject", id);
    }

    private static SubjectDto ToDto(Subject s) =>
        new(s.Id, s.Name, s.Slug, s.Description, s.TenantId, s.CreatedAt, s.UpdatedAt);
}
