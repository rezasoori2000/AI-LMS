using LMS.Application.Common.Interfaces;
using LMS.Domain.Curriculum;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Content.Chapters;

public sealed class ChapterService : IChapterService
{
    private readonly ILmsDbContext       _db;
    private readonly ICurrentUserService _currentUser;

    public ChapterService(ILmsDbContext db, ICurrentUserService currentUser)
    {
        _db          = db;
        _currentUser = currentUser;
    }

    private bool IsSuperAdmin =>
        string.Equals(_currentUser.Role, "SuperAdmin", StringComparison.Ordinal);

    // ── Queries ───────────────────────────────────────────────────────────────

    public async Task<List<ChapterDto>> GetListAsync(
        Guid?             subjectId = null,
        Guid?             gradeId   = null,
        CancellationToken ct        = default)
    {
        var query = _db.Chapters.AsNoTracking();

        if (!IsSuperAdmin)
            query = query.Where(c => c.TenantId == _currentUser.TenantId);
        if (subjectId.HasValue)
            query = query.Where(c => c.SubjectId == subjectId.Value);
        if (gradeId.HasValue)
            query = query.Where(c => c.GradeId == gradeId.Value);

        var chapters = await query.OrderBy(c => c.Order).ToListAsync(ct);
        return chapters.Select(ToDto).ToList();
    }

    public async Task<ChapterDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var chapter = await FindOwnedOrThrowAsync(id, ct);
        return ToDto(chapter);
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    public async Task<ChapterDto> CreateAsync(CreateChapterRequest request, CancellationToken ct = default)
    {
        var tenantId = IsSuperAdmin ? null : _currentUser.TenantId;

        // Verify referenced Subject exists and is accessible.
        var subjectExists = await _db.Subjects.AnyAsync(
            s => s.Id == request.SubjectId, ct);
        if (!subjectExists)
            throw new ContentNotFoundException("Subject", request.SubjectId);

        // Verify referenced Grade exists and is accessible.
        var gradeExists = await _db.Grades.AnyAsync(
            g => g.Id == request.GradeId, ct);
        if (!gradeExists)
            throw new ContentNotFoundException("Grade", request.GradeId);

        var duplicate = await _db.Chapters.AnyAsync(
            c => c.SubjectId == request.SubjectId
              && c.GradeId   == request.GradeId
              && c.Order     == request.Order
              && c.TenantId  == tenantId, ct);
        if (duplicate)
            throw new ContentConflictException(
                $"A chapter at order {request.Order} already exists for this subject and grade.");

        var chapter = Chapter.Create(
            request.SubjectId, request.GradeId, request.Title, request.Order,
            request.Description, tenantId);

        _db.Chapters.Add(chapter);
        await _db.SaveChangesAsync(ct);
        return ToDto(chapter);
    }

    public async Task<ChapterDto> UpdateAsync(Guid id, UpdateChapterRequest request, CancellationToken ct = default)
    {
        var chapter  = await FindOwnedOrThrowAsync(id, ct);
        var tenantId = IsSuperAdmin ? chapter.TenantId : _currentUser.TenantId;

        var duplicate = await _db.Chapters.AnyAsync(
            c => c.SubjectId == chapter.SubjectId
              && c.GradeId   == chapter.GradeId
              && c.Order     == request.Order
              && c.TenantId  == tenantId
              && c.Id        != id, ct);
        if (duplicate)
            throw new ContentConflictException(
                $"A chapter at order {request.Order} already exists for this subject and grade.");

        chapter.Update(request.Title, request.Order, request.Description);
        await _db.SaveChangesAsync(ct);
        return ToDto(chapter);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var chapter = await FindOwnedOrThrowAsync(id, ct);
        _db.Chapters.Remove(chapter);
        await _db.SaveChangesAsync(ct);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task<Chapter> FindOwnedOrThrowAsync(Guid id, CancellationToken ct)
    {
        Chapter? chapter;
        if (IsSuperAdmin)
            chapter = await _db.Chapters.FirstOrDefaultAsync(c => c.Id == id, ct);
        else
            chapter = await _db.Chapters.FirstOrDefaultAsync(
                c => c.Id == id && c.TenantId == _currentUser.TenantId, ct);

        return chapter ?? throw new ContentNotFoundException("Chapter", id);
    }

    private static ChapterDto ToDto(Chapter c) =>
        new(c.Id, c.SubjectId, c.GradeId, c.Title, c.Description, c.Order,
            c.TenantId, c.CreatedAt, c.UpdatedAt);
}
