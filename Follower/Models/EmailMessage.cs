namespace Follower.Models;

public record EmailMessage(string Id, string Subject, string Body, DateTimeOffset Date);

public record EmailFolder(string Name);
