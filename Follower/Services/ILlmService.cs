namespace Follower.Services;

public interface ILlmService
{
    /// <summary>
    /// Generates a punchy tweet from a topic and optional source content.
    /// </summary>
    /// <param name="topic">The topic/angle for the tweet</param>
    /// <param name="content">Optional source content (e.g., article text) to base the tweet on</param>
    /// <param name="enableWebSearch">Whether to search the web for recent info</param>
    Task<string> GenerateTweetAsync(string topic, string? content = null, bool enableWebSearch = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refines an existing tweet based on user feedback.
    /// </summary>
    Task<string> RefineTweetAsync(string currentTweet, string feedback, CancellationToken cancellationToken = default);
}
