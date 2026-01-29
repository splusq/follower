using Follower.Models;

namespace Follower.Services;

public class TweetService : ITweetService
{
    private readonly ILlmService _llmService;

    public TweetService(ILlmService llmService)
    {
        _llmService = llmService;
    }

    public Task<TweetDraft> GenerateAsync(EmailMessage draft, StyleProfile style, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<TweetDraft> RefineAsync(string feedback, TweetDraft currentDraft, StyleProfile style, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
