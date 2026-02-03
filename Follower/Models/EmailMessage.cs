namespace Follower.Models;

/// <summary>
/// Represents an email message with threading support.
/// </summary>
/// <param name="Uid">IMAP UID for folder operations (move, delete, mark read)</param>
/// <param name="MessageId">Message-ID header for threading (globally unique)</param>
/// <param name="InReplyTo">In-Reply-To header - null if this is a new topic, set if this is a reply</param>
/// <param name="From">Sender's email address</param>
/// <param name="Subject">Email subject line</param>
/// <param name="Body">Email body (plain text or converted from HTML)</param>
/// <param name="Date">When the email was sent</param>
public record EmailMessage(
    string Uid,
    string MessageId,
    string? InReplyTo,
    string From,
    string Subject,
    string Body,
    DateTimeOffset Date
)
{
    /// <summary>
    /// True if this is a reply to another email (part of a thread).
    /// </summary>
    public bool IsReply => InReplyTo != null;
}
