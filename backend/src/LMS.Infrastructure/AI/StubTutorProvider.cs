using LMS.Application.AiTutor;

namespace LMS.Infrastructure.AI;

/// <summary>
/// Phase 1 stub implementation of <see cref="ITutorProvider"/>.
///
/// Returns a deterministic placeholder response without making any LLM calls.
/// The response confirms that the tutor session is correctly wired end-to-end
/// (authentication → enrollment → context assembly → provider → persistence).
///
/// To replace with a real LLM provider in Phase 2, change the DI registration in
/// ServiceCollectionExtensions.AddApplicationServices():
///   services.AddScoped&lt;ITutorProvider, OllamaTutorProvider&gt;();
/// </summary>
public sealed class StubTutorProvider : ITutorProvider
{
    public Task<TutorProviderResponse> GetReplyAsync(
        TutorProviderRequest request,
        CancellationToken    ct = default)
    {
        var name   = request.Context.StudentFirstName;
        var lesson = request.Context.LessonTitle;

        var reply =
            $"Hi {name}! I can see you're studying \"{lesson}\". " +
            "The AI tutor is not yet connected to a language model — " +
            "this placeholder response confirms the tutor session is working correctly. " +
            "A real lesson-grounded response will be available once the LLM provider is configured.";

        return Task.FromResult(new TutorProviderResponse(reply, TokensUsed: null));
    }
}
