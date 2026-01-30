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

    public async Task<TweetDraft> GenerateAsync(EmailMessage draft, StyleProfile style, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating tweet from draft: {Subject}", draft.Subject);

        var tweetText = await _llmService.GenerateTweetAsync(draft.Body, style.ProfileText, cancellationToken);

        return new TweetDraft(tweetText, draft.Id, 1);
    }

    public async Task<TweetDraft> RefineAsync(string feedback, TweetDraft currentDraft, StyleProfile style, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Refining tweet based on feedback for draft: {DraftId}", currentDraft.SourceDraftId);

        var refinedText = await _llmService.RefineTweetAsync(feedback, currentDraft.Text, style.ProfileText, cancellationToken);

        return new TweetDraft(refinedText, currentDraft.SourceDraftId, currentDraft.Sequence);
    }
}
