using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using DotNetEnv;
using Follower.Configuration;
using FluentAssertions;
using Xunit;

namespace Follower.Tests.Services;

public class XTwitterServiceIntegrationTests
{
    private readonly bool _credentialsAvailable;
    private readonly AgentOptions _options;

    public XTwitterServiceIntegrationTests()
    {
        // Try to load .env from project directory
        var envPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "Follower", ".env");
        if (File.Exists(envPath))
        {
            Env.Load(envPath);
        }

        _options = new AgentOptions
        {
            TwitterApiKey = Environment.GetEnvironmentVariable("Agent__TwitterApiKey") ?? "",
            TwitterApiSecret = Environment.GetEnvironmentVariable("Agent__TwitterApiSecret") ?? "",
            TwitterAccessToken = Environment.GetEnvironmentVariable("Agent__TwitterAccessToken") ?? "",
            TwitterAccessTokenSecret = Environment.GetEnvironmentVariable("Agent__TwitterAccessTokenSecret") ?? ""
        };

        _credentialsAvailable = !string.IsNullOrEmpty(_options.TwitterApiKey)
            && !_options.TwitterApiKey.StartsWith("your-")
            && !string.IsNullOrEmpty(_options.TwitterApiSecret)
            && !string.IsNullOrEmpty(_options.TwitterAccessToken)
            && !string.IsNullOrEmpty(_options.TwitterAccessTokenSecret);
    }

    private void SkipIfNoCredentials()
    {
        Skip.If(!_credentialsAvailable, "Twitter API credentials not configured.");
    }

    [SkippableFact]
    public async Task ValidateCredentials_GetBearerToken()
    {
        SkipIfNoCredentials();

        using var httpClient = new HttpClient();

        var credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{_options.TwitterApiKey}:{_options.TwitterApiSecret}"));

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.twitter.com/oauth2/token");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        request.Content = new StringContent("grant_type=client_credentials", Encoding.UTF8, "application/x-www-form-urlencoded");

        var response = await httpClient.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        response.IsSuccessStatusCode.Should().BeTrue($"Bearer token request failed: {body}");
    }

    [SkippableFact]
    public async Task ValidateCredentials_GetAuthenticatedUser()
    {
        SkipIfNoCredentials();

        var url = "https://api.twitter.com/2/users/me";

        using var httpClient = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        var authHeader = GenerateOAuthHeader(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("OAuth", authHeader);

        var response = await httpClient.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        response.IsSuccessStatusCode.Should().BeTrue($"Twitter API returned {response.StatusCode}: {body}");
        body.Should().Contain("\"id\"");
        body.Should().Contain("\"username\"");
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

        var paramString = string.Join("&", oauthParams.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));
        var signatureBase = $"{method.Method}&{Uri.EscapeDataString(url)}&{Uri.EscapeDataString(paramString)}";

        var signingKey = $"{Uri.EscapeDataString(_options.TwitterApiSecret)}&{Uri.EscapeDataString(_options.TwitterAccessTokenSecret)}";

        using var hmac = new HMACSHA1(Encoding.ASCII.GetBytes(signingKey));
        var hash = hmac.ComputeHash(Encoding.ASCII.GetBytes(signatureBase));
        var signature = Convert.ToBase64String(hash);

        oauthParams["oauth_signature"] = signature;

        return string.Join(", ", oauthParams.Select(p => $"{Uri.EscapeDataString(p.Key)}=\"{Uri.EscapeDataString(p.Value)}\""));
    }
}
