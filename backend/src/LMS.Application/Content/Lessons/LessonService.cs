using LMS.Application.Common.Interfaces;
using LMS.Domain.Curriculum;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Content.Lessons;

public sealed class LessonService : ILessonService
{
    private readonly ILmsDbContext       _db;
    private readonly ICurrentUserService _currentUser;

    public LessonService(ILmsDbContext db, ICurrentUserService currentUser)
    {
        _db          = db;
        _currentUser = currentUser;
    }

    private bool IsSuperAdmin =>
        string.Equals(_currentUser.Role, "SuperAdmin", StringComparison.Ordinal);

    // ── Queries ───────────────────────────────────────────────────────────────

    public async Task<List<LessonDto>> GetListAsync(
        Guid?             chapterId = null,
        CancellationToken ct        = default)
    {
        var query = _db.Lessons.AsNoTracking();

        if (!IsSuperAdmin)
            query = query.Where(l => l.TenantId == _currentUser.TenantId);
        if (chapterId.HasValue)
            query = query.Where(l => l.ChapterId == chapterId.Value);

        var lessons = await query.OrderBy(l => l.Order).ToListAsync(ct);
        return lessons.Select(ToDto).ToList();
    }

    public async Task<LessonDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var lesson = await FindOwnedOrThrowAsync(id, ct);
        return ToDto(lesson);
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    public async Task<LessonDto> CreateAsync(CreateLessonRequest request, CancellationToken ct = default)
    {
        var tenantId = IsSuperAdmin ? null : _currentUser.TenantId;

        var chapterExists = await _db.Chapters.AnyAsync(
            c => c.Id == request.ChapterId, ct);
        if (!chapterExists)
            throw new ContentNotFoundException("Chapter", request.ChapterId);

        var duplicate = await _db.Lessons.AnyAsync(
            l => l.ChapterId == request.ChapterId
              && l.Order     == request.Order
              && l.TenantId  == tenantId, ct);
        if (duplicate)
            throw new ContentConflictException(
                $"A lesson at order {request.Order} already exists in this chapter.");

        var lesson = Lesson.Create(
            request.ChapterId, request.Title, request.Order,
            request.Content, request.EstimatedMinutes, tenantId);

        _db.Lessons.Add(lesson);
        await _db.SaveChangesAsync(ct);
        return ToDto(lesson);
    }

    public async Task<LessonDto> UpdateAsync(Guid id, UpdateLessonRequest request, CancellationToken ct = default)
    {
        var lesson   = await FindOwnedOrThrowAsync(id, ct);
        var tenantId = IsSuperAdmin ? lesson.TenantId : _currentUser.TenantId;

        var duplicate = await _db.Lessons.AnyAsync(
            l => l.ChapterId == lesson.ChapterId
              && l.Order     == request.Order
              && l.TenantId  == tenantId
              && l.Id        != id, ct);
        if (duplicate)
            throw new ContentConflictException(
                $"A lesson at order {request.Order} already exists in this chapter.");

        lesson.Update(request.Title, request.Order, request.Content, request.EstimatedMinutes);
        await _db.SaveChangesAsync(ct);
        return ToDto(lesson);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var lesson = await FindOwnedOrThrowAsync(id, ct);
        _db.Lessons.Remove(lesson);
        await _db.SaveChangesAsync(ct);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task<Lesson> FindOwnedOrThrowAsync(Guid id, CancellationToken ct)
    {
        Lesson? lesson;
        if (IsSuperAdmin)
            lesson = await _db.Lessons.FirstOrDefaultAsync(l => l.Id == id, ct);
        else
            lesson = await _db.Lessons.FirstOrDefaultAsync(
                l => l.Id == id && l.TenantId == _currentUser.TenantId, ct);

        return lesson ?? throw new ContentNotFoundException("Lesson", id);
    }

    private static LessonDto ToDto(Lesson l) =>
        new(l.Id, l.ChapterId, l.Title, l.Content, l.Order,
            l.EstimatedMinutes, l.TenantId, l.CreatedAt, l.UpdatedAt);
}
