namespace Follower.Services;

public interface ILlmService
{
    Task<string> AnalyzeStyleAsync(IEnumerable<string> examples, CancellationToken cancellationToken = default);
    Task<string> GenerateTweetAsync(string notes, string styleProfile, CancellationToken cancellationToken = default);
    Task<string> RefineTweetAsync(string feedback, string currentTweet, string styleProfile, CancellationToken cancellationToken = default);
}
