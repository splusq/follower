using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Follower.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Follower.Services;

public class XTwitterService : IXTwitterService
{
    private const string TwitterApiUrl = "https://api.twitter.com/2/tweets";
    private const int MaxRetries = 3;

    private readonly HttpClient _httpClient;
    private readonly AgentOptions _options;
    private readonly ILogger<XTwitterService> _logger;

    public XTwitterService(HttpClient httpClient, IOptions<AgentOptions> options, ILogger<XTwitterService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> PostTweetAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogWarning("Cannot post empty tweet");
            return false;
        }

        if (text.Length > 280)
        {
            _logger.LogWarning("Tweet exceeds 280 characters: {Length}", text.Length);
            return false;
        }

        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                var result = await PostTweetInternalAsync(text, cancellationToken);
                if (result)
                {
                    _logger.LogInformation("Tweet posted successfully");
                    return true;
                }
            }
            catch (HttpRequestException ex) when (attempt < MaxRetries)
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                _logger.LogWarning(ex, "Tweet post failed (attempt {Attempt}/{MaxRetries}), retrying in {Delay}s",
                    attempt, MaxRetries, delay.TotalSeconds);
                await Task.Delay(delay, cancellationToken);
            }
        }

        _logger.LogError("Failed to post tweet after {MaxRetries} attempts", MaxRetries);
        return false;
    }

    private async Task<bool> PostTweetInternalAsync(string text, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, TwitterApiUrl);

        var jsonBody = JsonSerializer.Serialize(new { text });
        request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

        var authHeader = GenerateOAuthHeader(HttpMethod.Post, TwitterApiUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("OAuth", authHeader);

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return true;
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogError("Twitter API error: {StatusCode} - {Body}", response.StatusCode, responseBody);

        // Retry on rate limit or server errors
        if (response.StatusCode == HttpStatusCode.TooManyRequests ||
            (int)response.StatusCode >= 500)
        {
            throw new HttpRequestException($"Twitter API returned {response.StatusCode}");
        }

        return false;
    }

    private string GenerateOAuthHeader(HttpMethod method, string url)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var nonce = Guid.NewGuid().ToString("N");

        var oauthParams = new SortedDictionary<string, string>
        {
            ["oauth_consumer_key"] = _options.TwitterApiKey,
            ["oauth_nonce"] = nonce,
            ["oauth_signature_method"] = "HMAC-SHA1",
            ["oauth_timestamp"] = timestamp,
            ["oauth_token"] = _options.TwitterAccessToken,
            ["oauth_version"] = "1.0"
        };

        // Create signature base string
        var paramString = string.Join("&", oauthParams.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));
        var signatureBase = $"{method.Method}&{Uri.EscapeDataString(url)}&{Uri.EscapeDataString(paramString)}";

        // Create signing key
        var signingKey = $"{Uri.EscapeDataString(_options.TwitterApiSecret)}&{Uri.EscapeDataString(_options.TwitterAccessTokenSecret)}";

        // Generate signature
        using var hmac = new HMACSHA1(Encoding.ASCII.GetBytes(signingKey));
        var hash = hmac.ComputeHash(Encoding.ASCII.GetBytes(signatureBase));
        var signature = Convert.ToBase64String(hash);

        oauthParams["oauth_signature"] = signature;

        // Build header string
        return string.Join(", ", oauthParams.Select(p => $"{Uri.EscapeDataString(p.Key)}=\"{Uri.EscapeDataString(p.Value)}\""));
    }
}
