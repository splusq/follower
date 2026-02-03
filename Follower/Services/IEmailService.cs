using Follower.Models;

namespace Follower.Services;

public interface IEmailService
{
    /// <summary>
    /// Gets all unread emails from the inbox.
    /// New topics have InReplyTo = null, replies have InReplyTo set.
    /// </summary>
    Task<IReadOnlyList<EmailMessage>> GetUnreadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Replies to an existing email thread with proper threading headers.
    /// </summary>
    /// <param name="original">The email to reply to</param>
    /// <param name="body">The reply body text</param>
    Task ReplyAsync(EmailMessage original, string body, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks an email as read.
    /// </summary>
    /// <param name="uid">The IMAP UID of the email</param>
    Task MarkAsReadAsync(string uid, CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves an email to the archive folder.
    /// </summary>
    /// <param name="uid">The IMAP UID of the email</param>
    Task ArchiveAsync(string uid, CancellationToken cancellationToken = default);
}
