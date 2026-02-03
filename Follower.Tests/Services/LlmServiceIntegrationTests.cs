using DotNetEnv;
using Follower.Configuration;
using Follower.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Follower.Tests.Services;

public class LlmServiceIntegrationTests
{
    private readonly LlmService? _sut;
    private readonly bool _credentialsAvailable;

    public LlmServiceIntegrationTests()
    {
        // Try to load .env from project directory
        var envPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "Follower", ".env");
        if (File.Exists(envPath))
        {
            Env.Load(envPath);
        }

        var endpoint = Environment.GetEnvironmentVariable("Agent__AzureOpenAIEndpoint");
        var key = Environment.GetEnvironmentVariable("Agent__AzureOpenAIKey");
        var deployment = Environment.GetEnvironmentVariable("Agent__AzureOpenAIDeployment");

        _credentialsAvailable = !string.IsNullOrEmpty(endpoint)
            && !string.IsNullOrEmpty(key)
            && !string.IsNullOrEmpty(deployment);

        if (_credentialsAvailable)
        {
            var options = Options.Create(new AgentOptions
            {
                AzureOpenAIEndpoint = endpoint!,
                AzureOpenAIKey = key!,
                AzureOpenAIDeployment = deployment!,
                EnableWebSearch = true
            });
            var logger = new Mock<ILogger<LlmService>>();
            var httpClientFactory = new Mock<IHttpClientFactory>();
            httpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient());
            _sut = new LlmService(options, httpClientFactory.Object, logger.Object);
        }
    }

    private void SkipIfNoCredentials()
    {
        Skip.If(!_credentialsAvailable, "Azure OpenAI credentials not configured. Set Agent__AzureOpenAIEndpoint, Agent__AzureOpenAIKey, and Agent__AzureOpenAIDeployment env vars.");
    }

    [SkippableFact]
    public async Task GenerateTweetAsync_WithTopicOnly_ReturnsTweetUnder280Chars()
    {
        SkipIfNoCredentials();

        // Arrange
        var topic = "Microservices add complexity. Most startups should start with a monolith.";

        // Act
        var result = await _sut!.GenerateTweetAsync(topic);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Length.Should().BeLessOrEqualTo(280);
    }

    [SkippableFact]
    public async Task GenerateTweetAsync_WithTopicAndContent_ReturnsTweetUnder280Chars()
    {
        SkipIfNoCredentials();

        // Arrange
        var topic = "Interesting take on startup architecture";
        var content = """
            Most successful startups began with simple monolithic architectures.
            Facebook, Twitter, and Shopify all started as monoliths.
            The complexity of microservices often outweighs the benefits for early-stage companies.
            """;

        // Act
        var result = await _sut!.GenerateTweetAsync(topic, content);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Length.Should().BeLessOrEqualTo(280);
    }

    [SkippableFact]
    public async Task RefineTweetAsync_WithRealApi_ReturnsRefinedTweet()
    {
        SkipIfNoCredentials();

        // Arrange
        var currentTweet = "Microservices are overrated for most startups.";
        var feedback = "Make it more provocative and add a contrarian angle";

        // Act
        var result = await _sut!.RefineTweetAsync(currentTweet, feedback);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Length.Should().BeLessOrEqualTo(280);
        result.Should().NotBe(currentTweet); // Should be different from original
    }
}
