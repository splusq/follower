using Follower.Configuration;
using Follower.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Follower;

public class Worker : BackgroundService
{
    private readonly IEmailService _emailService;
    private readonly IStyleService _styleService;
    private readonly ITweetService _tweetService;
    private readonly IXTwitterService _xTwitterService;
    private readonly AgentOptions _options;
    private readonly ILogger<Worker> _logger;

    public Worker(
        IEmailService emailService,
        IStyleService styleService,
        ITweetService tweetService,
        IXTwitterService xTwitterService,
        IOptions<AgentOptions> options,
        ILogger<Worker> logger)
    {
        _emailService = emailService;
        _styleService = styleService;
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

        // Step 1: Check for unread replies (human feedback)
        var replies = await _emailService.GetUnreadRepliesAsync(cancellationToken);
        foreach (var reply in replies)
        {
            await ProcessReplyAsync(reply, cancellationToken);
        }

        // Step 2: Check if we need to generate new tweets
        var todayPrefix = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var tweetsToday = await _emailService.CountArchivedByPrefixAsync(todayPrefix, cancellationToken);

        if (tweetsToday < _options.DailyTweetTarget)
        {
            await GenerateNewTweetAsync(cancellationToken);
        }

        _logger.LogInformation("Processing cycle complete");
    }

    private async Task ProcessReplyAsync(Models.EmailMessage reply, CancellationToken cancellationToken)
    {
        var body = reply.Body.Trim();
        var command = ParseCommand(body);

        _logger.LogInformation("Processing reply with command: {Command}", command ?? "feedback");

        // Extract the tweet from the quoted portion of the reply
        var quotedTweet = ExtractQuotedTweet(body);
        if (string.IsNullOrEmpty(quotedTweet))
        {
            _logger.LogWarning("Could not extract tweet from reply, skipping");
            await _emailService.MoveToArchiveAsync(reply.Id, null, cancellationToken);
            return;
        }

        switch (command)
        {
            case "/post":
                var posted = await _xTwitterService.PostTweetAsync(quotedTweet, cancellationToken);
                if (posted)
                {
                    _logger.LogInformation("Tweet posted successfully");
                }
                else
                {
                    _logger.LogError("Failed to post tweet");
                }
                break;

            case "/reject":
                _logger.LogInformation("Tweet rejected");
                break;

            default:
                // Treat as refinement feedback
                var feedback = GetFeedbackText(body);
                if (!string.IsNullOrWhiteSpace(feedback))
                {
                    await RefineAndSendAsync(quotedTweet, feedback, cancellationToken);
                }
                break;
        }

        await _emailService.MoveToArchiveAsync(reply.Id, null, cancellationToken);
    }

    private async Task GenerateNewTweetAsync(CancellationToken cancellationToken)
    {
        var drafts = await _emailService.GetDraftsAsync(cancellationToken);
        if (drafts.Count == 0)
        {
            _logger.LogInformation("No drafts available");
            return;
        }

        var draft = drafts[0];
        _logger.LogInformation("Generating tweet from draft: {Subject}", draft.Subject);

        var style = await _styleService.GetStyleProfileAsync(cancellationToken);
        var tweetDraft = await _tweetService.GenerateAsync(draft, style, cancellationToken);

        await SendApprovalEmailAsync(tweetDraft.Text, cancellationToken);
        await _emailService.MoveToArchiveAsync(draft.Id, _options.DraftsFolder, cancellationToken);
    }

    private async Task RefineAndSendAsync(string currentTweet, string feedback, CancellationToken cancellationToken)
    {
        var style = await _styleService.GetStyleProfileAsync(cancellationToken);
        var refined = await _tweetService.RefineAsync(feedback, new Models.TweetDraft(currentTweet, "", 1), style, cancellationToken);

        await SendApprovalEmailAsync(refined.Text, cancellationToken);
    }

    private async Task SendApprovalEmailAsync(string tweetText, CancellationToken cancellationToken)
    {
        var subject = $"[Tweet] {DateTime.UtcNow:yyyy-MM-dd-HHmmss}";
        var body = $"""
            Tweet ready for review:

            "{tweetText}"

            Reply with:
            /post - publish this tweet
            /reject - discard this tweet
            Or reply with feedback to refine it
            """;

        await _emailService.SendAsync(_options.EmailUsername, subject, body, cancellationToken);
        _logger.LogInformation("Sent approval email for tweet");
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
