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

    public async Task<IReadOnlyList<EmailMessage>> GetUnreadAsync(CancellationToken cancellationToken = default)
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
                messages.Add(ToEmailMessage(uid, message));
            }

            _logger.LogInformation("Found {Count} unread emails", messages.Count);
            return messages;
        }, cancellationToken);
    }

    public async Task ReplyAsync(EmailMessage original, string body, CancellationToken cancellationToken = default)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("", _options.EmailUsername));
        message.To.Add(new MailboxAddress("", original.From)); // Reply to original sender

        // Threading headers
        message.InReplyTo = original.MessageId;
        message.References.Add(original.MessageId);

        // Keep Re: prefix for threading
        message.Subject = original.Subject.StartsWith("Re:", StringComparison.OrdinalIgnoreCase)
            ? original.Subject
            : $"Re: {original.Subject}";

        message.Body = new TextPart("plain") { Text = body };

        using var client = new SmtpClient();
        await client.ConnectAsync(_options.SmtpServer, _options.SmtpPort, true, cancellationToken);
        await client.AuthenticateAsync(_options.EmailUsername, _options.EmailPassword, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);

        _logger.LogInformation("Sent reply to {To} for thread: {Subject}", original.From, message.Subject);
    }

    public async Task MarkAsReadAsync(string uid, CancellationToken cancellationToken = default)
    {
        await ExecuteImapAsync(async client =>
        {
            var inbox = client.Inbox;
            await inbox.OpenAsync(FolderAccess.ReadWrite, cancellationToken);

            if (!UniqueId.TryParse(uid, out var uniqueId))
            {
                _logger.LogWarning("Invalid UID: {Uid}", uid);
                return;
            }

            await inbox.AddFlagsAsync(uniqueId, MessageFlags.Seen, true, cancellationToken);
            _logger.LogDebug("Marked message {Uid} as read", uid);
        }, cancellationToken);
    }

    public async Task ArchiveAsync(string uid, CancellationToken cancellationToken = default)
    {
        await ExecuteImapAsync(async client =>
        {
            var inbox = client.Inbox;
            await inbox.OpenAsync(FolderAccess.ReadWrite, cancellationToken);

            if (!UniqueId.TryParse(uid, out var uniqueId))
            {
                _logger.LogWarning("Invalid UID: {Uid}", uid);
                return;
            }

            var archiveFolder = await GetOrCreateFolderAsync(client, _options.ArchiveFolder, cancellationToken);
            await inbox.MoveToAsync(uniqueId, archiveFolder, cancellationToken);

            _logger.LogInformation("Archived message {Uid}", uid);
        }, cancellationToken);
    }

    private async Task<IMailFolder?> GetFolderAsync(ImapClient client, string folderName, CancellationToken cancellationToken)
    {
        // Check for special folders first
        if (folderName.Equals("Archive", StringComparison.OrdinalIgnoreCase) && client.GetFolder(SpecialFolder.Archive) is { } archive)
        {
            return archive;
        }

        // Fall back to looking in personal namespace subfolders
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

    private static EmailMessage ToEmailMessage(UniqueId uid, MimeMessage message)
    {
        var body = message.TextBody;
        if (string.IsNullOrEmpty(body) && message.HtmlBody != null)
        {
            body = HtmlConverter.ToMarkdown(message.HtmlBody);
        }

        // Extract sender email address
        var from = message.From.Mailboxes.FirstOrDefault()?.Address ?? "";

        return new EmailMessage(
            uid.ToString(),
            message.MessageId ?? "",
            message.InReplyTo,
            from,
            message.Subject ?? "",
            body ?? "",
            message.Date
        );
    }
}
