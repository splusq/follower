using System.ClientModel;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.AI.OpenAI;
using Follower.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;

namespace Follower.Services;

public class LlmService : ILlmService
{
    private readonly ChatClient? _chatClient;
    private readonly HttpClient _httpClient;
    private readonly AgentOptions _options;
    private readonly ILogger<LlmService> _logger;

    public LlmService(IOptions<AgentOptions> options, IHttpClientFactory httpClientFactory, ILogger<LlmService> logger)
    {
        _options = options.Value;
        _httpClient = httpClientFactory.CreateClient();
        _logger = logger;

        if (_options.LlmProvider.Equals("Ollama", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Using Ollama provider at {Endpoint} with model {Model}",
                _options.OllamaEndpoint, _options.OllamaModel);

            var client = new OpenAIClient(
                new ApiKeyCredential("ollama"),
                new OpenAIClientOptions { Endpoint = new Uri(_options.OllamaEndpoint) });
            _chatClient = client.GetChatClient(_options.OllamaModel);
        }
        else
        {
            _logger.LogInformation("Using Azure OpenAI Responses API with WebSearch: {WebSearch}",
                _options.EnableWebSearch);

            // For Azure OpenAI Responses API, we'll use HttpClient directly
            _chatClient = null;
        }
    }

    public async Task<string> GenerateTweetAsync(string topic, string? content = null, bool enableWebSearch = false, CancellationToken cancellationToken = default)
    {
        // Use web search if explicitly requested OR if enabled globally in config
        var useWebSearch = enableWebSearch || _options.EnableWebSearch;
        _logger.LogInformation("Generating tweet for topic: {Topic}, has content: {HasContent}, webSearch: {WebSearch}",
            topic, !string.IsNullOrWhiteSpace(content), useWebSearch);

        var contentSection = string.IsNullOrWhiteSpace(content)
            ? ""
            : $"""

            Source material:
            ---
            {content}
            ---

            """;

        var webSearchInstruction = useWebSearch
            ? "Search the web for recent, relevant information about this topic to make the tweet timely and informed."
            : "";

        var prompt = $"""
            You are a viral Twitter ghostwriter.

            Topic: {topic}
            {contentSection}
            {webSearchInstruction}

            CRITICAL: Tweet MUST be under 250 characters. This is non-negotiable.

            RULES:
            - First 5 words stop the scroll - bold, surprising, contrarian
            - One idea. No fluff. Short sentences.
            - Sound human, not like a brand
            - NO hashtags, NO emojis
            - NO "I think" or hedge words
            - NO markdown formatting
            - Single paragraph, no line breaks
            - URLs are OK if relevant

            Output ONLY the tweet text on a single line. Nothing else.
            """;

        string result;
        if (_chatClient != null)
        {
            // Use Ollama/standard OpenAI
            var response = await _chatClient.CompleteChatAsync(
                [new UserChatMessage(prompt)],
                cancellationToken: cancellationToken
            );
            result = response.Value.Content[0].Text.Trim();
        }
        else
        {
            // Use Azure OpenAI Responses API with web search
            result = await CallAzureResponsesApiAsync(prompt, useWebSearch, cancellationToken);
        }

        // Clean up the tweet
        result = CleanTweet(result);

        // Always enforce the 280 character limit
        if (result.Length > 280)
        {
            _logger.LogWarning("Tweet exceeds 280 characters ({Length}), truncating", result.Length);
            result = TruncateTweet(result);
        }

        _logger.LogInformation("Generated tweet: {Length} chars", result.Length);
        return result;
    }

    private async Task<string> ShortenTweetAsync(string tweet, CancellationToken cancellationToken)
    {
        var prompt = $"""
            This tweet is too long ({tweet.Length} chars). Shorten to under 250 characters while keeping the punch.

            Original: {tweet}

            Write ONLY the shortened tweet. No quotes, no explanation.
            """;

        string result;
        if (_chatClient != null)
        {
            var response = await _chatClient.CompleteChatAsync(
                [new UserChatMessage(prompt)],
                cancellationToken: cancellationToken
            );
            result = response.Value.Content[0].Text.Trim();
        }
        else
        {
            result = await CallAzureResponsesApiAsync(prompt, enableWebSearch: false, cancellationToken);
        }

        result = result.Trim('"', '"', '"', '\'');

        // If still too long after retry, truncate at last sentence/space before 280
        if (result.Length > 280)
        {
            _logger.LogWarning("Tweet still too long after shortening ({Length}), truncating", result.Length);
            result = TruncateTweet(result);
        }

        return result;
    }

    private static string CleanTweet(string tweet)
    {
        // Remove quotes
        tweet = tweet.Trim('"', '"', '"', '\'', '`');

        // Remove markdown formatting
        tweet = tweet.Replace("**", "").Replace("__", "").Replace("*", "").Replace("_", "");

        // Remove blockquote markers (various formats)
        tweet = System.Text.RegularExpressions.Regex.Replace(tweet, @"^>\s*", "", System.Text.RegularExpressions.RegexOptions.Multiline);
        tweet = tweet.Replace("\n>", "\n").Replace("\r>", "\r");

        // Replace newlines with spaces
        tweet = tweet.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");

        // Collapse multiple spaces
        tweet = System.Text.RegularExpressions.Regex.Replace(tweet, @"\s+", " ");

        return tweet.Trim();
    }

    private static string TruncateTweet(string tweet)
    {
        if (tweet.Length <= 280) return tweet;

        // Try to cut at a sentence boundary
        var truncated = tweet[..277];
        var lastPeriod = truncated.LastIndexOf('.');
        var lastQuestion = truncated.LastIndexOf('?');
        var lastExclaim = truncated.LastIndexOf('!');
        var lastSentence = Math.Max(lastPeriod, Math.Max(lastQuestion, lastExclaim));

        if (lastSentence > 200)
        {
            return tweet[..(lastSentence + 1)];
        }

        // Otherwise cut at last space and add ellipsis
        var lastSpace = truncated.LastIndexOf(' ');
        if (lastSpace > 200)
        {
            return tweet[..lastSpace] + "...";
        }

        return truncated + "...";
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

        string result;
        if (_chatClient != null)
        {
            var response = await _chatClient.CompleteChatAsync(
                [new UserChatMessage(prompt)],
                cancellationToken: cancellationToken
            );
            result = response.Value.Content[0].Text.Trim();
        }
        else
        {
            // No web search needed for refinement
            result = await CallAzureResponsesApiAsync(prompt, enableWebSearch: false, cancellationToken);
        }

        // Clean up and enforce limit
        result = CleanTweet(result);
        if (result.Length > 280)
        {
            _logger.LogWarning("Refined tweet exceeds 280 characters ({Length}), truncating", result.Length);
            result = TruncateTweet(result);
        }

        _logger.LogInformation("Refined tweet: {Length} chars", result.Length);
        return result;
    }

    private async Task<string> CallAzureResponsesApiAsync(string input, bool enableWebSearch, CancellationToken cancellationToken)
    {
        // Azure OpenAI Responses API endpoint
        var endpoint = $"{_options.AzureOpenAIEndpoint.TrimEnd('/')}/openai/v1/responses";

        var request = new ResponsesApiRequest
        {
            Model = _options.AzureOpenAIDeployment,
            Input = input,
            Tools = enableWebSearch ? [new Tool { Type = "web_search_preview" }] : null
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);
        httpRequest.Headers.Add("api-key", _options.AzureOpenAIKey);
        httpRequest.Content = JsonContent.Create(request, options: JsonOptions);

        _logger.LogDebug("Calling Azure OpenAI Responses API: {Endpoint}", endpoint);

        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Azure OpenAI Responses API error: {Status} - {Error}", response.StatusCode, responseBody);
            throw new HttpRequestException($"Azure OpenAI API error: {response.StatusCode} - {responseBody}");
        }

        _logger.LogDebug("Azure OpenAI Responses API raw response: {Response}", responseBody);

        // Parse the response - try multiple possible structures
        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        // Try output_text first (simple format)
        if (root.TryGetProperty("output_text", out var outputText))
        {
            return outputText.GetString()?.Trim() ?? "";
        }

        // Try output array format
        if (root.TryGetProperty("output", out var output) && output.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in output.EnumerateArray())
            {
                if (item.TryGetProperty("type", out var type) && type.GetString() == "message")
                {
                    if (item.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var contentItem in content.EnumerateArray())
                        {
                            if (contentItem.TryGetProperty("type", out var contentType) && contentType.GetString() == "output_text")
                            {
                                if (contentItem.TryGetProperty("text", out var text))
                                {
                                    return text.GetString()?.Trim() ?? "";
                                }
                            }
                            // Also try just "text" type
                            if (contentItem.TryGetProperty("type", out var textType) && textType.GetString() == "text")
                            {
                                if (contentItem.TryGetProperty("text", out var text))
                                {
                                    return text.GetString()?.Trim() ?? "";
                                }
                            }
                        }
                    }
                }
            }
        }

        // Try choices array (chat completions format)
        if (root.TryGetProperty("choices", out var choices) && choices.ValueKind == JsonValueKind.Array)
        {
            var firstChoice = choices.EnumerateArray().FirstOrDefault();
            if (firstChoice.TryGetProperty("message", out var message))
            {
                if (message.TryGetProperty("content", out var messageContent))
                {
                    return messageContent.GetString()?.Trim() ?? "";
                }
            }
        }

        _logger.LogWarning("Could not parse response, returning raw: {Response}", responseBody);
        return "";
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private class ResponsesApiRequest
    {
        public string Model { get; set; } = "";
        public string Input { get; set; } = "";
        public List<Tool>? Tools { get; set; }
        public int MaxOutputTokens { get; set; } = 1024; // Needs room for web search reasoning + tweet
    }

    private class Tool
    {
        public string Type { get; set; } = "";
    }
}
