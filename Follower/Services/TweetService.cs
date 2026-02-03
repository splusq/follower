using Follower.Models;
using Microsoft.Extensions.Logging;

namespace Follower.Services;

public class TweetService : ITweetService
{
    private readonly ILlmService _llmService;
    private readonly ILogger<TweetService> _logger;

    public TweetService(ILlmService llmService, ILogger<TweetService> logger)
    {
        _llmService = llmService;
        _logger = logger;
    }

    public async Task<TweetDraft> GenerateAsync(string topic, string? content = null, bool enableWebSearch = false, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating tweet for topic: {Topic}, has content: {HasContent}, webSearch: {WebSearch}",
            topic, !string.IsNullOrWhiteSpace(content), enableWebSearch);

        var tweetText = await _llmService.GenerateTweetAsync(topic, content, enableWebSearch, cancellationToken);

        return new TweetDraft(tweetText);
    }

    public async Task<TweetDraft> RefineAsync(string currentTweet, string feedback, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Refining tweet based on feedback");

        var refinedText = await _llmService.RefineTweetAsync(currentTweet, feedback, cancellationToken);

        return new TweetDraft(refinedText);
    }
}
