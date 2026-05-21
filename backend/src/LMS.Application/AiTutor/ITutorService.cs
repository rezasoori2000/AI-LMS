namespace LMS.Application.AiTutor;

/// <summary>
/// Orchestrates AI tutoring sessions for individual students.
///
/// Ownership model:
///   Every method resolves the calling student's StudentProfile.Id internally via
///   <see cref="LMS.Application.Common.Interfaces.ICurrentUserService"/>.
///   The controller does not supply a studentProfileId — the service is the single
///   point of ownership resolution, consistent with IStudentService.
///   A student may only interact with conversations they own.
///
/// MVP scope (Phase 1 Section 11 Parts 2+):
///   - Open and close per-lesson conversation sessions (<c>AiConversation</c> records).
///   - Assemble a <see cref="TutorContextSnapshot"/> from lesson content, progress,
///     and current conversation history, then forward it to the AI service.
///   - Persist student messages and AI replies as <c>AiMessage</c> records via the
///     <c>AiConversation</c> aggregate.
///   - Enforce enrollment ownership: only an enrolled student may open or continue
///     a tutor session for a given lesson.
///
/// Firm data boundaries — what this service MUST NOT do:
///   1. MUST NOT write <c>LessonProgress</c>, <c>QuestionAnswerRecord</c>, or
///      <c>Enrollment</c>. Those tables are owned by <c>IStudentService</c>.
///   2. MUST NOT include <c>CorrectAnswer</c> in any context snapshot or AI payload.
///   3. MUST NOT make grading decisions or modify academic records.
///   4. MUST NOT include <c>LearnerProfile</c> or <c>TopicMasterySnapshot</c> until
///      Phase 3 implements the personalization layer.
///   5. MUST NOT generate or relay content outside the scope of the current lesson
///      (the AI system prompt must constrain the model to lesson-grounded responses).
///
/// May read (context assembly only — no writes):
///   <c>Lesson</c> (content, title, grade, subject),
///   <c>LessonProgress</c> (status only),
///   <c>Question</c> (text, type, options — never <c>CorrectAnswer</c>),
///   <c>Enrollment</c> (active status check only),
///   <c>StudentProfile</c> (id, first name, grade).
///
/// May write:
///   <c>AiConversation</c> (create, end),
///   <c>AiMessage</c> (append via <c>AiConversation.AddMessage()</c>).
/// </summary>
public interface ITutorService
{
    /// <summary>
    /// Opens a new AI tutor conversation for the lesson.
    /// Creates an <c>AiConversation</c> record and returns its ID.
    /// </summary>
    /// <exception cref="LMS.Application.Student.StudentAccessDeniedException">
    /// Thrown (→ HTTP 403) when the student is not enrolled in the lesson's subject.
    /// </exception>
    Task<StartTutorSessionResponse> StartSessionAsync(
        StartTutorSessionRequest request,
        CancellationToken        ct = default);

    /// <summary>
    /// Sends a student message, assembles a <see cref="TutorContextSnapshot"/>,
    /// calls the AI service, and persists both the student message and the AI reply
    /// as <c>AiMessage</c> records on the conversation aggregate.
    /// </summary>
    /// <exception cref="LMS.Application.Student.StudentAccessDeniedException">
    /// Thrown (→ HTTP 403) when the conversation does not belong to the calling student.
    /// </exception>
    /// <exception cref="TutorConversationEndedException">
    /// Thrown (→ HTTP 409) when the conversation has already been ended.
    /// </exception>
    Task<TutorReplyDto> AskAsync(
        Guid              conversationId,
        TutorAskRequest   request,
        CancellationToken ct = default);

    /// <summary>
    /// Ends the active conversation by setting <c>AiConversation.EndedAt</c>.
    /// Idempotent — safe to call multiple times on the same conversation.
    /// </summary>
    /// <exception cref="LMS.Application.Student.StudentAccessDeniedException">
    /// Thrown (→ HTTP 403) when the conversation does not belong to the calling student.
    /// </exception>
    Task EndSessionAsync(
        Guid              conversationId,
        CancellationToken ct = default);
}
