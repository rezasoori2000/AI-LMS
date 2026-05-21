using LMS.Application.Common.Interfaces;
using LMS.Application.Student;
using LMS.Domain.Conversations;
using LMS.Domain.Progress;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LMS.Application.AiTutor;

/// <summary>
/// Assembles a <see cref="TutorContextSnapshot"/> from database state.
///
/// Responsibilities:
///   - Load the minimum data required for a lesson-grounded AI response.
///   - Enforce the CorrectAnswer exclusion at the data layer (it is never selected).
///   - Build the conversation history from the already-loaded AiConversation entity.
///
/// This class is used exclusively by <see cref="TutorService"/>, which instantiates it
/// directly (sharing the same scoped <see cref="ILmsDbContext"/>).  It is not registered
/// in DI and not part of any public API.
///
/// All queries use AsNoTracking (read-only). The caller owns any tracked entities.
///
/// EF Core concurrency note:
///   All queries are executed sequentially — the shared DbContext does not support
///   concurrent async operations within a single scope.
/// </summary>
internal sealed class TutorContextAssembler
{
    private readonly ILmsDbContext _db;

    public TutorContextAssembler(ILmsDbContext db) => _db = db;

    /// <summary>
    /// Assembles the full context snapshot for a tutor request.
    /// </summary>
    /// <param name="studentProfileId">The calling student's profile ID (already verified).</param>
    /// <param name="conversation">The active AiConversation (already loaded, may be tracked).</param>
    /// <param name="hintForQuestionId">Optional question the student wants a hint for.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<TutorContextSnapshot> AssembleAsync(
        Guid           studentProfileId,
        AiConversation conversation,
        Guid?          hintForQuestionId,
        CancellationToken ct)
    {
        var lessonId = conversation.LessonId
            ?? throw new StudentAccessDeniedException("lesson", conversation.Id);

        // 1. Load lesson + chapter + subject + grade
        var lesson = await _db.Lessons
            .Include(l => l.Chapter)
                .ThenInclude(c => c.Subject)
            .Include(l => l.Chapter)
                .ThenInclude(c => c.Grade)
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == lessonId, ct)
            ?? throw new StudentAccessDeniedException("lesson", lessonId);

        // 2. Load student profile + user + grade (sequential — same scoped DbContext)
        var student = await _db.StudentProfiles
            .Include(s => s.User)
            .Include(s => s.Grade)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == studentProfileId, ct)
            ?? throw new StudentAccessDeniedException("student", studentProfileId);

        // 3. Load lesson progress status (null = not started)
        var progressStatus = await _db.LessonProgress
            .AsNoTracking()
            .Where(p => p.StudentId == studentProfileId && p.LessonId == lessonId)
            .Select(p => (ProgressStatus?)p.Status)
            .FirstOrDefaultAsync(ct);

        // 4. Build conversation history from already-loaded messages (oldest first)
        var history = conversation.Messages
            .OrderBy(m => m.SentAt)
            .Select(m => new ConversationTurnDto(
                Role:    m.Role == MessageRole.User ? "student" : "tutor",
                Content: m.Content,
                SentAt:  m.SentAt))
            .ToList();

        // 5. Grade: prefer the student's own assigned grade; fall back to the
        //    chapter's curriculum grade so context is always populated.
        var gradeName = student.Grade?.Name ?? lesson.Chapter.Grade.Name;

        // 6. Optional hint question — text and options only; CorrectAnswer excluded (B-1)
        string?               questionText    = null;
        string?               questionType    = null;
        IReadOnlyList<string>? questionOptions = null;

        if (hintForQuestionId.HasValue)
        {
            var hint = await _db.Questions
                .AsNoTracking()
                .Where(q => q.Id == hintForQuestionId.Value && q.LessonId == lessonId)
                .Select(q => new { q.Text, q.Type, q.OptionsJson }) // CorrectAnswer intentionally absent
                .FirstOrDefaultAsync(ct);

            if (hint is not null)
            {
                questionText    = hint.Text;
                questionType    = hint.Type.ToString();
                questionOptions = hint.OptionsJson is not null
                    ? JsonSerializer.Deserialize<List<string>>(hint.OptionsJson)?.AsReadOnly()
                    : null;
            }
        }

        return new TutorContextSnapshot
        {
            StudentProfileId     = studentProfileId,
            UserId               = student.UserId,
            StudentFirstName     = student.User.FirstName ?? "Student",
            TenantId             = student.TenantId,
            LessonId             = lessonId,
            LessonTitle          = lesson.Title,
            LessonContent        = lesson.Content ?? string.Empty,
            GradeName            = gradeName,
            SubjectName          = lesson.Chapter.Subject.Name,
            HintForQuestionId    = hintForQuestionId,
            QuestionText         = questionText,
            QuestionType         = questionType,
            QuestionOptions      = questionOptions,
            LessonProgressStatus = (progressStatus ?? ProgressStatus.NotStarted).ToString(),
            ConversationId       = conversation.Id,
            History              = history,
        };
    }
}
