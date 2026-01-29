using Follower.Models;

namespace Follower.Services;

public interface IEmailService
{
    Task<IReadOnlyList<EmailMessage>> GetUnreadRepliesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmailMessage>> GetDraftsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmailMessage>> GetInfluencerTweetsAsync(CancellationToken cancellationToken = default);
    Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default);
    Task MoveToArchiveAsync(string messageId, CancellationToken cancellationToken = default);
    Task<int> CountArchivedByPrefixAsync(string prefix, CancellationToken cancellationToken = default);
}
