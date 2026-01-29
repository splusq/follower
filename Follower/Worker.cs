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

    private Task ProcessReplyAsync(Models.EmailMessage reply, CancellationToken cancellationToken)
    {
        // TODO: Parse reply to determine action (approve/refine/reject)
        // If approved, post tweet via XTwitterService
        // If refine, generate new draft via TweetService
        // Archive the reply
        throw new NotImplementedException();
    }

    private Task GenerateNewTweetAsync(CancellationToken cancellationToken)
    {
        // TODO: Get next draft from Drafts folder
        // Get style profile
        // Generate tweet
        // Send for approval via email
        throw new NotImplementedException();
    }
}
