using Follower.Configuration;
using Follower.Models;
using Follower.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Follower;

public class Worker : BackgroundService
{
    private readonly IEmailService _emailService;
    private readonly ITweetService _tweetService;
    private readonly IXTwitterService _xTwitterService;
    private readonly AgentOptions _options;
    private readonly ILogger<Worker> _logger;

    public Worker(
        IEmailService emailService,
        ITweetService tweetService,
        IXTwitterService xTwitterService,
        IOptions<AgentOptions> options,
        ILogger<Worker> logger)
    {
        _emailService = emailService;
        _tweetService = tweetService;
        _xTwitterService = xTwitterService;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessCycleAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during processing cycle");
            }

            await Task.Delay(TimeSpan.FromSeconds(_options.PollIntervalSeconds), stoppingToken);
        }

        _logger.LogInformation("Worker stopping");
    }

    private async Task ProcessCycleAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting processing cycle");

        var unreadEmails = await _emailService.GetUnreadAsync(cancellationToken);

        foreach (var email in unreadEmails)
        {
            if (email.IsReply)
            {
                // This is a reply to an existing thread - process command
                await ProcessReplyAsync(email, cancellationToken);
            }
            else
            {
                // This is a new topic request - generate tweet
                await ProcessNewTopicAsync(email, cancellationToken);
            }
        }

        _logger.LogInformation("Processing cycle complete");
    }

    private async Task ProcessNewTopicAsync(EmailMessage email, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing new topic: {Subject}", email.Subject);

        // Parse topic and optional content separated by ---
        var (topic, content) = ParseTopicAndContent(email.Subject, email.Body);

        var tweet = await _tweetService.GenerateAsync(topic, content, cancellationToken);

        // Reply with the generated tweet
        var replyBody = FormatTweetForReview(tweet.Text);
        await _emailService.ReplyAsync(email, replyBody, cancellationToken);

        // Mark as read (but don't archive - thread is still active)
        await _emailService.MarkAsReadAsync(email.Uid, cancellationToken);
    }

    /// <summary>
    /// Parses email into topic and optional content.
    /// Format: Subject is the main topic. Body can have additional context,
    /// and if it contains ---, everything after is treated as source content.
    /// </summary>
    private static (string topic, string? content) ParseTopicAndContent(string subject, string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return (subject, null);
        }

        // Check for --- separator
        var separatorIndex = body.IndexOf("\n---\n", StringComparison.Ordinal);
        if (separatorIndex == -1)
        {
            separatorIndex = body.IndexOf("\r\n---\r\n", StringComparison.Ordinal);
        }

        if (separatorIndex >= 0)
        {
            // Split into topic context and content
            var topicContext = body[..separatorIndex].Trim();
            var content = body[(separatorIndex + 5)..].Trim(); // Skip past \n---\n

            var topic = string.IsNullOrWhiteSpace(topicContext)
                ? subject
                : $"{subject}\n\n{topicContext}";

            return (topic, string.IsNullOrWhiteSpace(content) ? null : content);
        }

        // No separator - entire body is topic context
        return ($"{subject}\n\n{body}", null);
    }

    private async Task ProcessReplyAsync(EmailMessage reply, CancellationToken cancellationToken)
    {
        var body = reply.Body.Trim();
        var command = ParseCommand(body);

        _logger.LogInformation("Processing reply with command: {Command}", command ?? "feedback");

        // Extract the tweet from the quoted portion of the reply
        var quotedTweet = ExtractQuotedTweet(body);

        switch (command)
        {
            case "/post":
                if (string.IsNullOrEmpty(quotedTweet))
                {
                    _logger.LogWarning("Could not extract tweet from reply, skipping post");
                }
                else
                {
                    var posted = await _xTwitterService.PostTweetAsync(quotedTweet, cancellationToken);
                    if (posted)
                    {
                        _logger.LogInformation("Tweet posted successfully");
                    }
                    else
                    {
                        _logger.LogError("Failed to post tweet");
                    }
                }
                await _emailService.ArchiveAsync(reply.Uid, cancellationToken);
                break;

            case "/reject":
                _logger.LogInformation("Tweet rejected");
                await _emailService.ArchiveAsync(reply.Uid, cancellationToken);
                break;

            default:
                // Treat as refinement feedback
                if (string.IsNullOrEmpty(quotedTweet))
                {
                    _logger.LogWarning("Could not extract tweet from reply, skipping refinement");
                    await _emailService.MarkAsReadAsync(reply.Uid, cancellationToken);
                }
                else
                {
                    var feedback = GetFeedbackText(body);
                    if (!string.IsNullOrWhiteSpace(feedback))
                    {
                        var refined = await _tweetService.RefineAsync(quotedTweet, feedback, cancellationToken);
                        var replyBody = FormatTweetForReview(refined.Text);
                        await _emailService.ReplyAsync(reply, replyBody, cancellationToken);
                    }
                    await _emailService.MarkAsReadAsync(reply.Uid, cancellationToken);
                }
                break;
        }
    }

    private static string FormatTweetForReview(string tweetText)
    {
        return $"""
            Tweet ready for review:

            "{tweetText}"

            Reply with:
            /post - publish this tweet
            /reject - discard this tweet
            Or reply with feedback to refine it
            """;
    }

    private static string? ParseCommand(string body)
    {
        var lines = body.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var trimmed = line.Trim().ToLowerInvariant();
            if (trimmed == "/post" || trimmed == "/reject")
            {
                return trimmed;
            }
        }
        return null;
    }

    private static string? ExtractQuotedTweet(string body)
    {
        // Look for tweet between quotes in the email
        var startQuote = body.IndexOf('"');
        var endQuote = body.LastIndexOf('"');

        if (startQuote >= 0 && endQuote > startQuote)
        {
            return body.Substring(startQuote + 1, endQuote - startQuote - 1).Trim();
        }

        return null;
    }

    private static string GetFeedbackText(string body)
    {
        // Get text before any quoted content (the user's actual reply)
        var lines = body.Split('\n');
        var feedbackLines = new List<string>();

        foreach (var line in lines)
        {
            // Stop at quote markers or the original tweet
            if (line.TrimStart().StartsWith('>') || line.Contains("Tweet ready for review"))
            {
                break;
            }
            feedbackLines.Add(line);
        }

        return string.Join('\n', feedbackLines).Trim();
    }
}
