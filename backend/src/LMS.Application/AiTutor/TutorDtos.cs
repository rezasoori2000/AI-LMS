using System.ComponentModel.DataAnnotations;

namespace LMS.Application.AiTutor;

// ── Inbound (student → backend) ──────────────────────────────────────────────

/// <summary>
/// Opens a new AI tutor conversation for a lesson.
/// The student must be enrolled in the lesson's subject.
/// </summary>
/// <param name="LessonId">The lesson the student is currently viewing.</param>
public sealed record StartTutorSessionRequest(Guid LessonId);

/// <summary>
/// Sends a student message to the AI tutor within an active conversation.
/// </summary>
/// <param name="Message">
/// The student's free-text question or request. Max 1,000 characters.
/// Should be grounded in the current lesson (the AI prompt enforces this constraint).
/// </param>
/// <param name="HintForQuestionId">
/// Optional: the lesson question the student wants a hint for.
/// When set, the context snapshot includes the question text and options
/// but never the CorrectAnswer.
/// </param>
public sealed record TutorAskRequest(
    [Required, MaxLength(1000)] string Message,
    Guid?  HintForQuestionId = null);

// ── Outbound (backend → student) ─────────────────────────────────────────────

/// <summary>
/// Returned after successfully opening a new tutor session.
/// Pass <see cref="ConversationId"/> to subsequent /ask and /end calls.
/// </summary>
public sealed record StartTutorSessionResponse(
    Guid ConversationId,
    Guid LessonId);

/// <summary>
/// The AI tutor's reply to a student message.
/// </summary>
/// <param name="ConversationId">Echo of the active conversation ID.</param>
/// <param name="Reply">The AI-generated response text.</param>
/// <param name="TokensUsed">Provider-reported token count, or null if unavailable.</param>
public sealed record TutorReplyDto(
    Guid   ConversationId,
    string Reply,
    int?   TokensUsed = null);
