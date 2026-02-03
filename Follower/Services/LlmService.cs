using System.ClientModel;
using Azure.AI.OpenAI;
using Follower.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;

namespace Follower.Services;

public class LlmService : ILlmService
{
    private readonly ChatClient _chatClient;
    private readonly ILogger<LlmService> _logger;

    public LlmService(IOptions<AgentOptions> options, ILogger<LlmService> logger)
    {
        var opts = options.Value;
        _logger = logger;

        if (opts.LlmProvider.Equals("Ollama", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Using Ollama provider at {Endpoint} with model {Model}",
                opts.OllamaEndpoint, opts.OllamaModel);

            var client = new OpenAIClient(
                new ApiKeyCredential("ollama"),
                new OpenAIClientOptions { Endpoint = new Uri(opts.OllamaEndpoint) });
            _chatClient = client.GetChatClient(opts.OllamaModel);
        }
        else
        {
            _logger.LogInformation("Using Azure OpenAI provider");

            var credential = new ApiKeyCredential(opts.AzureOpenAIKey);
            var client = new AzureOpenAIClient(new Uri(opts.AzureOpenAIEndpoint), credential);
            _chatClient = client.GetChatClient(opts.AzureOpenAIDeployment);
        }
    }

    public async Task<string> GenerateTweetAsync(string topic, string? content = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating tweet for topic: {Topic}, has content: {HasContent}",
            topic, !string.IsNullOrWhiteSpace(content));

        var contentSection = string.IsNullOrWhiteSpace(content)
            ? ""
            : $"""

            Source material:
            ---
            {content}
            ---

            """;

        var prompt = $"""
            You are a viral Twitter ghostwriter. Your tweets get massive engagement and build loyal followers.

            Topic: {topic}
            {contentSection}
            RULES FOR VIRAL TWEETS:
            1. First 5 words MUST stop the scroll - be bold, surprising, or contrarian
            2. One big idea only. No fluff.
            3. Write like you're texting a smart friend - casual but sharp
            4. Challenge conventional wisdom. "Everyone thinks X. They're wrong."
            5. Use pattern interrupts: short sentence. Then elaborate.
            6. End with something quotable, memorable, or that sparks debate
            7. Sound like a human with opinions, not a brand

            AVOID:
            - Hashtags, emojis, "thread" or "1/"
            - Starting with "I think" or "In my opinion"
            - Generic advice anyone could give
            - Being preachy or self-righteous
            - Hedge words (maybe, perhaps, might)

            LENGTH: Under 280 characters. Shorter is better. Punchy wins.

            Write only the tweet. No quotes, no explanation.
            """;

        var response = await _chatClient.CompleteChatAsync(
            [new UserChatMessage(prompt)],
            cancellationToken: cancellationToken
        );

        var result = response.Value.Content[0].Text.Trim();
        _logger.LogInformation("Generated tweet: {Length} chars", result.Length);
        return result;
    }

    public async Task<string> RefineTweetAsync(string currentTweet, string feedback, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Refining tweet based on feedback");

        var prompt = $"""
            Refine this tweet based on the feedback. Make it MORE viral, not less.

            Current tweet:
            {currentTweet}

            Feedback:
            {feedback}

            REMEMBER:
            - First 5 words must stop the scroll
            - Be bolder, not safer
            - One punchy idea, no fluff
            - Sound human with real opinions
            - No hashtags, no emojis
            - Under 280 chars. Shorter = better.

            Write only the refined tweet. No quotes, no explanation.
            """;

        var response = await _chatClient.CompleteChatAsync(
            [new UserChatMessage(prompt)],
            cancellationToken: cancellationToken
        );

        var result = response.Value.Content[0].Text.Trim();
        _logger.LogInformation("Refined tweet: {Length} chars", result.Length);
        return result;
    }
}
