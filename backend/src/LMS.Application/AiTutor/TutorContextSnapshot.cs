namespace LMS.Application.AiTutor;

/// <summary>
/// The assembled context snapshot that the backend sends to the AI service for
/// every tutor request.
///
/// Assembly contract:
///   All authorization and enrollment checks must pass BEFORE context is assembled.
///   The assembler is responsible for populating this record; the AI service trusts
///   its contents without performing further auth checks.
///
/// Security invariants (enforced by the assembler — see TutorBoundaryNotes.cs):
///   B-1. <c>CorrectAnswer</c> is NEVER included, even for hint requests.
///        The AI service provides hints from lesson content only, not by revealing
///        the answer.
///   B-2. Only enrolled students can trigger context assembly.
///        Non-enrolled access throws <see cref="LMS.Application.Student.StudentAccessDeniedException"/>
///        before this record is created.
///   B-3. Conversation history is scoped to the current session (ConversationId).
///        Cross-session memory requires LearnerProfile (Phase 3 addition).
///   B-4. LearnerProfile fields (TopicMasterySnapshot, LearnerPreferences) are absent.
///        MVP context is lesson-scoped only; Phase 3 will introduce adaptive context.
/// </summary>
public sealed record TutorContextSnapshot
{
    // ── Student identity ───────────────────────────────────────────────────

    /// <summary>The calling student's StudentProfile.Id.</summary>
    public required Guid   StudentProfileId { get; init; }

    /// <summary>The calling student's User.Id (durable identity anchor).</summary>
    public required Guid   UserId           { get; init; }

    /// <summary>
    /// Student first name for prompt personalization (tone only).
    /// No last name — limits exposure in AI service logs.
    /// </summary>
    public required string StudentFirstName { get; init; }

    /// <summary>Null = platform-wide; non-null = tenant-scoped.</summary>
    public          Guid?  TenantId         { get; init; }

    // ── Lesson context ─────────────────────────────────────────────────────

    public required Guid   LessonId      { get; init; }
    public required string LessonTitle   { get; init; }

    /// <summary>
    /// Full lesson text (plain text / markdown). The AI service uses this as the
    /// primary knowledge source for grounded tutor responses.
    /// </summary>
    public required string LessonContent { get; init; }

    /// <summary>Grade name, e.g. "Grade 5". Guides age-appropriate tone.</summary>
    public required string GradeName     { get; init; }

    /// <summary>Subject name, e.g. "Mathematics".</summary>
    public required string SubjectName   { get; init; }

    // ── Question hint context (optional) ──────────────────────────────────

    /// <summary>
    /// The question the student is requesting a hint for.
    /// Null when the student is asking a general lesson question rather than
    /// requesting a hint for a specific exercise question.
    /// </summary>
    public Guid?   HintForQuestionId { get; init; }

    /// <summary>Question text. Populated only when <see cref="HintForQuestionId"/> is set.</summary>
    public string? QuestionText      { get; init; }

    /// <summary>
    /// "MultipleChoice" | "TrueFalse" | "ShortAnswer".
    /// Populated only when <see cref="HintForQuestionId"/> is set.
    /// </summary>
    public string? QuestionType      { get; init; }

    /// <summary>
    /// Option texts for MultipleChoice questions (4 items).
    /// Populated only when <see cref="HintForQuestionId"/> is set and type is MultipleChoice.
    /// <para><b>CorrectAnswer is intentionally absent (see B-1 above).</b></para>
    /// </summary>
    public IReadOnlyList<string>? QuestionOptions { get; init; }

    // ── Progress context ───────────────────────────────────────────────────

    /// <summary>
    /// Current lesson progress status for this student.
    /// "NotStarted" | "InProgress" | "Completed".
    /// </summary>
    public required string LessonProgressStatus { get; init; }

    // ── Conversation context (current session only) ────────────────────────

    public required Guid ConversationId { get; init; }

    /// <summary>
    /// All messages in this conversation, ordered oldest-first.
    /// Limited to the current session (ConversationId).
    /// All messages are sent; the AI service prompt builder caps usage at
    /// HISTORY_WINDOW_TURNS = 10 (oldest dropped first) to bound LLM context.
    /// Phase 2: also cap here before serializing to reduce payload size.
    /// </summary>
    public required IReadOnlyList<ConversationTurnDto> History { get; init; }
}

/// <summary>A single message turn in the conversation history.</summary>
/// <param name="Role">"student" | "tutor"</param>
/// <param name="Content">The raw message text.</param>
/// <param name="SentAt">UTC timestamp.</param>
public sealed record ConversationTurnDto(
    string   Role,
    string   Content,
    DateTime SentAt);
