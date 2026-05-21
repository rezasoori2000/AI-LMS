namespace LMS.Application.AiTutor;

/// <summary>
/// Abstraction over the AI language-model call.
///
/// The provider receives an already-assembled <see cref="TutorProviderRequest"/>
/// (context snapshot + student message) and returns a raw reply string.
///
/// It does NOT know about:
///   - Database entities, sessions, or persistence
///   - Student authentication or conversation ownership
///   - Conversation history management (history is already embedded in the context snapshot)
///
/// Implementations:
///   Phase 1: <c>StubTutorProvider</c> — returns a deterministic placeholder (no LLM call).
///   Phase 2: <c>OllamaTutorProvider</c> or <c>OpenAiTutorProvider</c> (real LLM calls).
///
/// To swap providers, change the DI registration in ServiceCollectionExtensions:
///   services.AddScoped&lt;ITutorProvider, OllamaTutorProvider&gt;();
/// </summary>
public interface ITutorProvider
{
    /// <summary>
    /// Sends the assembled tutor context to the language model and returns its reply.
    /// </summary>
    /// <param name="request">The assembled context snapshot and the student's message.</param>
    /// <param name="ct">Cancellation token for the LLM HTTP call.</param>
    Task<TutorProviderResponse> GetReplyAsync(
        TutorProviderRequest request,
        CancellationToken    ct = default);
}

/// <summary>
/// Everything the provider needs to generate a lesson-grounded tutor response.
/// </summary>
/// <param name="Context">
/// The assembled lesson/student/conversation context.
/// All authorization checks have already passed before this is constructed.
/// </param>
/// <param name="StudentMessage">
/// The student's free-text message (already length-validated by model binding).
/// </param>
public sealed record TutorProviderRequest(
    TutorContextSnapshot Context,
    string               StudentMessage);

/// <summary>
/// The raw response from the language model (or stub).
/// </summary>
/// <param name="Reply">The AI-generated response text.</param>
/// <param name="TokensUsed">
/// Provider-reported token count, or null if unavailable or not applicable (e.g. stub).
/// </param>
public sealed record TutorProviderResponse(
    string Reply,
    int?   TokensUsed = null);
