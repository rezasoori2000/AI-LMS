using LMS.Application.Common.Interfaces;
using LMS.Domain.Catalog;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Content.Questions;

public sealed class QuestionService : IQuestionService
{
    private readonly ILmsDbContext       _db;
    private readonly ICurrentUserService _currentUser;

    public QuestionService(ILmsDbContext db, ICurrentUserService currentUser)
    {
        _db          = db;
        _currentUser = currentUser;
    }

    private bool IsSuperAdmin =>
        string.Equals(_currentUser.Role, "SuperAdmin", StringComparison.Ordinal);

    // ── Queries ───────────────────────────────────────────────────────────────

    public async Task<List<QuestionDto>> GetListAsync(
        Guid?             lessonId = null,
        CancellationToken ct       = default)
    {
        var query = _db.Questions.AsNoTracking();

        if (!IsSuperAdmin)
            query = query.Where(q => q.TenantId == _currentUser.TenantId);
        if (lessonId.HasValue)
            query = query.Where(q => q.LessonId == lessonId.Value);

        var questions = await query.OrderBy(q => q.CreatedAt).ToListAsync(ct);
        return questions.Select(ToDto).ToList();
    }

    public async Task<QuestionDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var question = await FindOwnedOrThrowAsync(id, ct);
        return ToDto(question);
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    public async Task<QuestionDto> CreateAsync(CreateQuestionRequest request, CancellationToken ct = default)
    {
        var tenantId = IsSuperAdmin ? null : _currentUser.TenantId;

        if (request.LessonId.HasValue)
        {
            var lessonExists = await _db.Lessons.AnyAsync(
                l => l.Id == request.LessonId.Value, ct);
            if (!lessonExists)
                throw new ContentNotFoundException("Lesson", request.LessonId.Value);
        }

        var question = Question.Create(
            request.Text, request.Type, request.Difficulty, request.CorrectAnswer,
            request.LessonId, request.OptionsJson, tenantId);

        _db.Questions.Add(question);
        await _db.SaveChangesAsync(ct);
        return ToDto(question);
    }

    public async Task<QuestionDto> UpdateAsync(Guid id, UpdateQuestionRequest request, CancellationToken ct = default)
    {
        var question = await FindOwnedOrThrowAsync(id, ct);

        if (request.LessonId.HasValue)
        {
            var lessonExists = await _db.Lessons.AnyAsync(
                l => l.Id == request.LessonId.Value, ct);
            if (!lessonExists)
                throw new ContentNotFoundException("Lesson", request.LessonId.Value);
        }

        question.Update(
            request.Text, request.Type, request.Difficulty, request.CorrectAnswer,
            request.OptionsJson, request.LessonId);

        await _db.SaveChangesAsync(ct);
        return ToDto(question);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var question = await FindOwnedOrThrowAsync(id, ct);
        _db.Questions.Remove(question);
        await _db.SaveChangesAsync(ct);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task<Question> FindOwnedOrThrowAsync(Guid id, CancellationToken ct)
    {
        Question? question;
        if (IsSuperAdmin)
            question = await _db.Questions.FirstOrDefaultAsync(q => q.Id == id, ct);
        else
            question = await _db.Questions.FirstOrDefaultAsync(
                q => q.Id == id && q.TenantId == _currentUser.TenantId, ct);

        return question ?? throw new ContentNotFoundException("Question", id);
    }

    private static QuestionDto ToDto(Question q) =>
        new(q.Id, q.LessonId, q.Text, q.Type, q.Difficulty,
            q.OptionsJson, q.CorrectAnswer, q.TenantId, q.CreatedAt, q.UpdatedAt);
}
