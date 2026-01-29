namespace Follower.Services;

public interface IXTwitterService
{
    Task<bool> PostTweetAsync(string text, CancellationToken cancellationToken = default);
}
