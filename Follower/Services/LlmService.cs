namespace Follower.Services;

public class LlmService : ILlmService
{
    public LlmService()
    {
    }

    public Task<string> AnalyzeStyleAsync(IEnumerable<string> examples, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<string> GenerateTweetAsync(string notes, string styleProfile, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<string> RefineTweetAsync(string feedback, string currentTweet, string styleProfile, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
