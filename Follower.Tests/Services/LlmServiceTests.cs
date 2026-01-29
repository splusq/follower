using Follower.Configuration;
using Follower.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Follower.Tests.Services;

public class LlmServiceTests
{
    private readonly LlmService _sut;
    private readonly Mock<IOptions<AgentOptions>> _optionsMock;
    private readonly Mock<ILogger<LlmService>> _loggerMock;

    public LlmServiceTests()
    {
        _optionsMock = new Mock<IOptions<AgentOptions>>();
        _optionsMock.Setup(x => x.Value).Returns(new AgentOptions
        {
            AnthropicApiKey = "test-key",
            AnthropicModel = "claude-sonnet-4-20250514"
        });
        _loggerMock = new Mock<ILogger<LlmService>>();
        _sut = new LlmService(_optionsMock.Object, _loggerMock.Object);
    }

    [Fact(Skip = "Requires real API key")]
    public async Task AnalyzeStyleAsync_WithExamples_ReturnsStyleProfile()
    {
        // Arrange
        var examples = new[] { "example tweet 1", "example tweet 2" };

        // Act
        var result = await _sut.AnalyzeStyleAsync(examples);

        // Assert
        result.Should().NotBeNullOrEmpty();
    }

    [Fact(Skip = "Requires real API key")]
    public async Task GenerateTweetAsync_WithNotesAndStyle_ReturnsTweet()
    {
        // Act
        var result = await _sut.GenerateTweetAsync("notes about AI", "casual tone");

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Length.Should().BeLessOrEqualTo(280);
    }

    [Fact(Skip = "Requires real API key")]
    public async Task RefineTweetAsync_WithFeedback_ReturnsRefinedTweet()
    {
        // Act
        var result = await _sut.RefineTweetAsync("make it shorter", "This is a very long tweet", "casual tone");

        // Assert
        result.Should().NotBeNullOrEmpty();
    }
}
