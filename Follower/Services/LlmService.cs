using Anthropic;
using Anthropic.Models.Messages;
using Follower.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Follower.Services;

public class LlmService : ILlmService
{
    private readonly AnthropicClient _client;
    private readonly AgentOptions _options;
    private readonly ILogger<LlmService> _logger;

    public LlmService(IOptions<AgentOptions> options, ILogger<LlmService> logger)
    {
        _options = options.Value;
        _logger = logger;

        // Set API key via environment variable (SDK reads from ANTHROPIC_API_KEY)
        if (!string.IsNullOrEmpty(_options.AnthropicApiKey))
        {
            Environment.SetEnvironmentVariable("ANTHROPIC_API_KEY", _options.AnthropicApiKey);
        }

        _client = new AnthropicClient();
    }

    public async Task<string> AnalyzeStyleAsync(IEnumerable<string> examples, CancellationToken cancellationToken = default)
    {
        var exampleList = examples.ToList();
        _logger.LogInformation("Analyzing style from {Count} examples", exampleList.Count);

        var examplesText = string.Join("\n\n---\n\n", exampleList.Select((e, i) => $"Example {i + 1}:\n{e}"));

        var prompt = $"""
            Analyze the following tweets and create a concise style guide that captures the author's voice.

            Focus on:
            - Tone (casual, professional, witty, provocative, etc.)
            - Sentence structure and length preferences
            - Use of punctuation, capitalization, emoji
            - Common patterns or phrases
            - How they open and close tweets
            - Their approach to technical vs accessible language

            Tweets to analyze:
            {examplesText}

            Provide a style guide that can be used to write new tweets matching this voice. Be specific and actionable.
            """;

        var response = await _client.Messages.Create(new MessageCreateParams
        {
            Model = _options.AnthropicModel,
            MaxTokens = 1024,
            Messages = [new() { Role = Role.User, Content = prompt }]
        }, cancellationToken);

        var result = ExtractText(response);
        _logger.LogDebug("Generated style profile: {Length} chars", result.Length);
        return result;
    }

    public async Task<string> GenerateTweetAsync(string notes, string styleProfile, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating tweet from notes ({Length} chars)", notes.Length);

        var prompt = $"""
            Write a single tweet based on the following notes. The tweet must be under 280 characters.

            Style guide to follow:
            {styleProfile}

            Notes/content to tweet about:
            {notes}

            Write only the tweet text, nothing else. No quotes, no explanation.
            """;

        var response = await _client.Messages.Create(new MessageCreateParams
        {
            Model = _options.AnthropicModel,
            MaxTokens = 256,
            Messages = [new() { Role = Role.User, Content = prompt }]
        }, cancellationToken);

        var result = ExtractText(response).Trim();
        _logger.LogInformation("Generated tweet: {Length} chars", result.Length);
        return result;
    }

    public async Task<string> RefineTweetAsync(string feedback, string currentTweet, string styleProfile, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Refining tweet based on feedback");

        var prompt = $"""
            Refine this tweet based on the feedback. Keep it under 280 characters.

            Current tweet:
            {currentTweet}

            Feedback:
            {feedback}

            Style guide to follow:
            {styleProfile}

            Write only the refined tweet text, nothing else. No quotes, no explanation.
            """;

        var response = await _client.Messages.Create(new MessageCreateParams
        {
            Model = _options.AnthropicModel,
            MaxTokens = 256,
            Messages = [new() { Role = Role.User, Content = prompt }]
        }, cancellationToken);

        var result = ExtractText(response).Trim();
        _logger.LogInformation("Refined tweet: {Length} chars", result.Length);
        return result;
    }

    private static string ExtractText(Message response)
    {
        foreach (var block in response.Content)
        {
            if (block.TryPickText(out var textBlock))
            {
                return textBlock.Text;
            }
        }
        return "";
    }
}
