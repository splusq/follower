using Follower.Models;

namespace Follower.Services;

public interface ILlmService
{
    Task<string> AnalyzeStyleAsync(IEnumerable<InfluencerExample> influencers, CancellationToken cancellationToken = default);
    Task<string> GenerateTweetAsync(string notes, string styleProfile, CancellationToken cancellationToken = default);
    Task<string> RefineTweetAsync(string feedback, string currentTweet, string styleProfile, CancellationToken cancellationToken = default);
}
