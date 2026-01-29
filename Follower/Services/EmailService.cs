using Follower.Configuration;
using Follower.Models;
using Follower.Utils;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailKit.Search;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Follower.Services;

public class EmailService : IEmailService
{
    private readonly AgentOptions _options;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<AgentOptions> options, ILogger<EmailService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<EmailMessage>> GetUnreadRepliesAsync(CancellationToken cancellationToken = default)
    {
        return await ExecuteImapAsync(async client =>
        {
            var inbox = client.Inbox;
            await inbox.OpenAsync(FolderAccess.ReadOnly, cancellationToken);

            var uids = await inbox.SearchAsync(SearchQuery.NotSeen, cancellationToken);
            var messages = new List<EmailMessage>();

            foreach (var uid in uids)
            {
                var message = await inbox.GetMessageAsync(uid, cancellationToken);
                messages.Add(ToEmailMessage(uid.ToString(), message));
            }

            _logger.LogInformation("Found {Count} unread replies", messages.Count);
            return messages;
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<EmailMessage>> GetDraftsAsync(CancellationToken cancellationToken = default)
    {
        return await GetMessagesFromFolderAsync(_options.DraftsFolder, cancellationToken);
    }

    public async Task<IReadOnlyList<EmailMessage>> GetInfluencerTweetsAsync(CancellationToken cancellationToken = default)
    {
        return await GetMessagesFromFolderAsync(_options.InfluencersFolder, cancellationToken);
    }

    public async Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("", _options.EmailUsername));
        message.To.Add(new MailboxAddress("", to));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };

        using var client = new SmtpClient();
        await client.ConnectAsync(_options.SmtpServer, _options.SmtpPort, true, cancellationToken);
        await client.AuthenticateAsync(_options.EmailUsername, _options.EmailPassword, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);

        _logger.LogInformation("Sent email to {To} with subject: {Subject}", to, subject);
    }

    public async Task MoveToArchiveAsync(string messageId, CancellationToken cancellationToken = default)
    {
        await ExecuteImapAsync(async client =>
        {
            var inbox = client.Inbox;
            await inbox.OpenAsync(FolderAccess.ReadWrite, cancellationToken);

            // Find message by UID
            if (!UniqueId.TryParse(messageId, out var uid))
            {
                _logger.LogWarning("Invalid message ID: {MessageId}", messageId);
                return;
            }

            // Get or create archive folder
            var archiveFolder = await GetOrCreateFolderAsync(client, _options.ArchiveFolder, cancellationToken);

            // Move message
            await inbox.MoveToAsync(uid, archiveFolder, cancellationToken);
            _logger.LogInformation("Moved message {MessageId} to archive", messageId);
        }, cancellationToken);
    }

    public async Task<int> CountArchivedByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        return await ExecuteImapAsync(async client =>
        {
            var archiveFolder = await GetFolderAsync(client, _options.ArchiveFolder, cancellationToken);
            if (archiveFolder == null)
            {
                return 0;
            }

            await archiveFolder.OpenAsync(FolderAccess.ReadOnly, cancellationToken);

            var query = SearchQuery.SubjectContains(prefix);
            var uids = await archiveFolder.SearchAsync(query, cancellationToken);

            _logger.LogDebug("Found {Count} archived messages with prefix: {Prefix}", uids.Count, prefix);
            return uids.Count;
        }, cancellationToken);
    }

    private async Task<IReadOnlyList<EmailMessage>> GetMessagesFromFolderAsync(string folderName, CancellationToken cancellationToken)
    {
        return await ExecuteImapAsync<IReadOnlyList<EmailMessage>>(async client =>
        {
            var folder = await GetFolderAsync(client, folderName, cancellationToken);
            if (folder == null)
            {
                _logger.LogWarning("Folder not found: {FolderName}", folderName);
                return Array.Empty<EmailMessage>();
            }

            await folder.OpenAsync(FolderAccess.ReadOnly, cancellationToken);

            var messages = new List<EmailMessage>();
            for (int i = 0; i < folder.Count; i++)
            {
                var message = await folder.GetMessageAsync(i, cancellationToken);
                messages.Add(ToEmailMessage(i.ToString(), message));
            }

            _logger.LogInformation("Found {Count} messages in {Folder}", messages.Count, folderName);
            return messages;
        }, cancellationToken);
    }

    private async Task<IMailFolder?> GetFolderAsync(ImapClient client, string folderName, CancellationToken cancellationToken)
    {
        var personal = client.GetFolder(client.PersonalNamespaces[0]);
        var folders = await personal.GetSubfoldersAsync(false, cancellationToken);

        return folders.FirstOrDefault(f => f.Name.Equals(folderName, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<IMailFolder> GetOrCreateFolderAsync(ImapClient client, string folderName, CancellationToken cancellationToken)
    {
        var existing = await GetFolderAsync(client, folderName, cancellationToken);
        if (existing != null)
        {
            return existing;
        }

        var personal = client.GetFolder(client.PersonalNamespaces[0]);
        var newFolder = await personal.CreateAsync(folderName, true, cancellationToken);
        _logger.LogInformation("Created folder: {FolderName}", folderName);
        return newFolder;
    }

    private async Task ExecuteImapAsync(Func<ImapClient, Task> action, CancellationToken cancellationToken)
    {
        using var client = new ImapClient();
        await client.ConnectAsync(_options.ImapServer, _options.ImapPort, true, cancellationToken);
        await client.AuthenticateAsync(_options.EmailUsername, _options.EmailPassword, cancellationToken);

        await action(client);

        await client.DisconnectAsync(true, cancellationToken);
    }

    private async Task<T> ExecuteImapAsync<T>(Func<ImapClient, Task<T>> action, CancellationToken cancellationToken)
    {
        using var client = new ImapClient();
        await client.ConnectAsync(_options.ImapServer, _options.ImapPort, true, cancellationToken);
        await client.AuthenticateAsync(_options.EmailUsername, _options.EmailPassword, cancellationToken);

        var result = await action(client);

        await client.DisconnectAsync(true, cancellationToken);
        return result;
    }

    private static EmailMessage ToEmailMessage(string id, MimeMessage message)
    {
        var body = message.TextBody;
        if (string.IsNullOrEmpty(body) && message.HtmlBody != null)
        {
            body = HtmlConverter.ToMarkdown(message.HtmlBody);
        }

        return new EmailMessage(
            id,
            message.Subject ?? "",
            body ?? "",
            message.Date
        );
    }
}
