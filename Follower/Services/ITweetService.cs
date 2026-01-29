using Follower.Models;

namespace Follower.Services;

public interface ITweetService
{
    Task<TweetDraft> GenerateAsync(EmailMessage draft, StyleProfile style, CancellationToken cancellationToken = default);
    Task<TweetDraft> RefineAsync(string feedback, TweetDraft currentDraft, StyleProfile style, CancellationToken cancellationToken = default);
}
