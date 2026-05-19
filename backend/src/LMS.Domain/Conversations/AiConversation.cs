using LMS.Domain.Common;
using LMS.Domain.Curriculum;
using LMS.Domain.Students;

namespace LMS.Domain.Conversations;

/// <summary>
/// Represents a single AI tutoring conversation session for a student.
///
/// Design notes:
/// - A conversation is bounded by StartedAt/EndedAt; each distinct session is a
///   separate record. A student may have many conversations for the same lesson.
/// - LessonId is optional: the student may open the AI chat outside any lesson context.
/// - AiMessage is a child entity of this aggregate. Always add messages via AddMessage()
///   so the aggregate enforces the "no messages after End()" invariant.
/// - Identity anchor: StudentId is a FK to StudentProfile.Id. When Phase 3 builds the
///   AI context assembly service, always traverse to the durable User.Id via
///   StudentProfile.UserId — do not treat StudentProfile.Id as a long-term identity key.
///   A denormalized UserId column may be added to this table in Phase 3 for query efficiency.
/// - Phase 3: add model name, system-prompt version, total token usage, conversation
///   rating (student feedback 1–5), teacher review flag, and moderation status.
/// </summary>
public sealed class AiConversation : AuditableEntity
{
    private readonly List<AiMessage> _messages = [];

    private AiConversation() { }

    /// <summary>FK to the StudentProfile conducting this conversation.</summary>
    public Guid StudentId { get; private set; }

    /// <summary>Optional FK to the Lesson that triggered this conversation.</summary>
    public Guid? LessonId { get; private set; }

    public DateTime  StartedAt { get; private set; }

    /// <summary>Set when the conversation ends (user closes chat, session timeout, etc.).</summary>
    public DateTime? EndedAt { get; private set; }

    /// <summary>null = platform-wide; non-null = scoped to this tenant.</summary>
    public Guid? TenantId { get; private set; }

    // Reference navigation.
    public StudentProfile Student { get; private set; } = null!;
    public Lesson?        Lesson  { get; private set; }

    /// <summary>Ordered conversation messages. EF Core uses the backing field via HasField.</summary>
    public IReadOnlyCollection<AiMessage> Messages => _messages.AsReadOnly();

    public static AiConversation Create(Guid studentId, Guid? lessonId = null, Guid? tenantId = null)
    {
        if (studentId == Guid.Empty)
            throw new ArgumentException("StudentId must not be empty.", nameof(studentId));

        return new AiConversation
        {
            StudentId = studentId,
            LessonId  = lessonId,
            StartedAt = DateTime.UtcNow,
            TenantId  = tenantId,
            CreatedAt = DateTime.UtcNow,
        };
    }

    /// <summary>
    /// Appends a new message to the conversation and returns it.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the conversation has already ended.</exception>
    public AiMessage AddMessage(MessageRole role, string content, int? tokenCount = null)
    {
        if (EndedAt.HasValue)
            throw new InvalidOperationException("Cannot add a message to an ended conversation.");

        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        var message = AiMessage.Create(Id, role, content, tokenCount);
        _messages.Add(message);
        UpdatedAt = DateTime.UtcNow;
        return message;
    }

    /// <summary>
    /// Marks the conversation as ended. Idempotent — safe to call multiple times.
    /// </summary>
    public void End()
    {
        if (EndedAt.HasValue) return;

        EndedAt   = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
