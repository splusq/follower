using Follower.Models;
using Microsoft.Extensions.Options;
using Follower.Configuration;

namespace Follower.Services;

public class EmailService : IEmailService
{
    private readonly AgentOptions _options;

    public EmailService(IOptions<AgentOptions> options)
    {
        _options = options.Value;
    }

    public Task<IReadOnlyList<EmailMessage>> GetUnreadRepliesAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<EmailMessage>> GetDraftsAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<EmailMessage>> GetInfluencerTweetsAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task MoveToArchiveAsync(string messageId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<int> CountArchivedByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
