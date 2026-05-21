namespace LMS.Application.AiTutor;

/// <summary>
/// Thrown when a student attempts to send a message to a conversation that has
/// already been ended (<see cref="LMS.Domain.Conversations.AiConversation.EndedAt"/> is set).
///
/// Maps to HTTP 409 Conflict (handled by ExceptionHandlingMiddleware).
///
/// Why 409 and not 400:
///   The request is structurally valid — the student provided a real conversation ID
///   and a valid message.  The conflict is a state issue: the resource (the conversation)
///   is in a terminal state that prevents the operation from succeeding.
/// </summary>
public sealed class TutorConversationEndedException : Exception
{
    public TutorConversationEndedException(Guid conversationId)
        : base($"Conversation '{conversationId}' has already ended. Start a new session to continue.") { }
}
