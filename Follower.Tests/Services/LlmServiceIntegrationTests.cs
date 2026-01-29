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
                AzureOpenAIDeployment = deployment!
            });
            var logger = new Mock<ILogger<LlmService>>();
            _sut = new LlmService(options, logger.Object);
        }
    }

    private void SkipIfNoCredentials()
    {
        Skip.If(!_credentialsAvailable, "Azure OpenAI credentials not configured. Set Agent__AzureOpenAIEndpoint, Agent__AzureOpenAIKey, and Agent__AzureOpenAIDeployment env vars.");
    }

    [SkippableFact]
    public async Task AnalyzeStyleAsync_WithRealApi_ReturnsStyleProfile()
    {
        SkipIfNoCredentials();

        // Arrange
        var examples = new[]
        {
            "Hot take: the best code is the code you don't write. Delete more, ship less, sleep better.",
            "Everyone's building AI wrappers. Few are building AI infrastructure. Be the infrastructure.",
            "Your startup doesn't need Kubernetes. It needs customers."
        };

        // Act
        var result = await _sut!.AnalyzeStyleAsync(examples);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Length.Should().BeGreaterThan(100); // Should be a meaningful analysis
    }

    [SkippableFact]
    public async Task GenerateTweetAsync_WithRealApi_ReturnsTweetUnder280Chars()
    {
        SkipIfNoCredentials();

        // Arrange
        var notes = "Microservices add complexity. Most startups should start with a monolith.";
        var styleProfile = "Tone: provocative, confident. Short punchy sentences. No emoji. Challenge conventional wisdom.";

        // Act
        var result = await _sut!.GenerateTweetAsync(notes, styleProfile);

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
        var styleProfile = "Tone: provocative, confident. Short punchy sentences.";

        // Act
        var result = await _sut!.RefineTweetAsync(feedback, currentTweet, styleProfile);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Length.Should().BeLessOrEqualTo(280);
        result.Should().NotBe(currentTweet); // Should be different from original
    }
}
