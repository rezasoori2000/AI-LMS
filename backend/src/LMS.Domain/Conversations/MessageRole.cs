namespace LMS.Domain.Conversations;

/// <summary>
/// The role of a participant in an AI conversation message.
/// Mirrors standard Chat Completion roles used by OpenAI-compatible APIs.
/// Phase 3: add Tool (for function-call / tool-result messages).
/// </summary>
public enum MessageRole
{
    User,
    Assistant,
    System,
}
