using LMS.Domain.Common;

namespace LMS.Domain.Conversations;

/// <summary>
/// A single message within an AI conversation.
///
/// Design notes:
/// - AiMessage is a child entity owned by AiConversation. Do NOT instantiate directly —
///   use AiConversation.AddMessage() which enforces aggregate invariants.
/// - Messages are append-only: they are never updated or deleted (audit-log semantics).
///   The EF Core configuration will set the table to be insert-only in Part 2.
/// - TokenCount is optional; returned by the AI provider and used in Phase 3 for
///   usage billing and per-tenant rate limiting.
/// - Phase 3: add ModelId, LatencyMs, IsFlagged (moderation result), ToolCallId.
/// </summary>
public sealed class AiMessage : Entity
{
    private AiMessage() { }

    public Guid        ConversationId { get; private set; }
    public MessageRole Role           { get; private set; }
    public string      Content        { get; private set; } = string.Empty;
    public DateTime    SentAt         { get; private set; }

    /// <summary>
    /// Number of tokens consumed by this message, as reported by the AI provider.
    /// null until the provider response is received.
    /// Phase 3: aggregate per-tenant for billing.
    /// </summary>
    public int? TokenCount { get; private set; }

    // Reference navigation.
    public AiConversation Conversation { get; private set; } = null!;

    // Internal factory — called only by AiConversation.AddMessage().
    internal static AiMessage Create(
        Guid        conversationId,
        MessageRole role,
        string      content,
        int?        tokenCount = null)
    {
        return new AiMessage
        {
            ConversationId = conversationId,
            Role           = role,
            Content        = content,
            SentAt         = DateTime.UtcNow,
            TokenCount     = tokenCount,
        };
    }
}
