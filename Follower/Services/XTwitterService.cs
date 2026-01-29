namespace Follower.Services;

public class XTwitterService : IXTwitterService
{
    public XTwitterService()
    {
    }

    public Task<bool> PostTweetAsync(string text, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
