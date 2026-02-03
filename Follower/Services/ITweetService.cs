using Follower.Models;

namespace Follower.Services;

public interface ITweetService
{
    /// <summary>
    /// Generates a punchy tweet from a topic and optional source content.
    /// </summary>
    /// <param name="topic">The topic to tweet about (from user's email subject/intro)</param>
    /// <param name="content">Optional source content (e.g., paywalled article text)</param>
    Task<TweetDraft> GenerateAsync(string topic, string? content = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refines an existing tweet based on user feedback.
    /// </summary>
    Task<TweetDraft> RefineAsync(string currentTweet, string feedback, CancellationToken cancellationToken = default);
}
