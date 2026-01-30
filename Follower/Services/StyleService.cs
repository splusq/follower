using Follower.Models;
using Microsoft.Extensions.Logging;

namespace Follower.Services;

public class StyleService : IStyleService
{
    private readonly IEmailService _emailService;
    private readonly ILlmService _llmService;
    private readonly ILogger<StyleService> _logger;

    public StyleService(IEmailService emailService, ILlmService llmService, ILogger<StyleService> logger)
    {
        _emailService = emailService;
        _llmService = llmService;
        _logger = logger;
    }

    public async Task<StyleProfile> GetStyleProfileAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching influencer tweets for style analysis");

        var influencerEmails = await _emailService.GetInfluencerTweetsAsync(cancellationToken);

        if (influencerEmails.Count == 0)
        {
            _logger.LogWarning("No influencer tweets found in Influencers folder");
            return new StyleProfile("", DateTimeOffset.UtcNow);
        }

        // Each email = one influencer, subject = name, body = their tweets (one per line)
        var influencers = influencerEmails
            .Where(e => !string.IsNullOrWhiteSpace(e.Body))
            .Select(e => new InfluencerExample(e.Subject, e.Body))
            .ToList();

        _logger.LogInformation("Analyzing style from {Count} influencers", influencers.Count);

        var profileText = await _llmService.AnalyzeStyleAsync(influencers, cancellationToken);

        return new StyleProfile(profileText, DateTimeOffset.UtcNow);
    }
}
