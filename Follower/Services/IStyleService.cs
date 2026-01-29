using Follower.Models;

namespace Follower.Services;

public interface IStyleService
{
    Task<StyleProfile> GetStyleProfileAsync(CancellationToken cancellationToken = default);
}
