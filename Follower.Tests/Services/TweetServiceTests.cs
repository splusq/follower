using Follower.Models;
using Follower.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Follower.Tests.Services;

public class TweetServiceTests
{
    private readonly TweetService _sut;
    private readonly Mock<ILlmService> _llmServiceMock;
    private readonly Mock<ILogger<TweetService>> _loggerMock;

    public TweetServiceTests()
    {
        _llmServiceMock = new Mock<ILlmService>();
        _loggerMock = new Mock<ILogger<TweetService>>();
        _sut = new TweetService(_llmServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GenerateAsync_WithTopic_ReturnsTweetDraft()
    {
        // Arrange
        var topic = "Microservices are complex for startups";

        _llmServiceMock
            .Setup(x => x.GenerateTweetAsync(topic, null, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync("Hot take: skip the microservices, ship the monolith.");

        // Act
        var result = await _sut.GenerateAsync(topic);

        // Assert
        result.Should().NotBeNull();
        result.Text.Should().Be("Hot take: skip the microservices, ship the monolith.");
    }

    [Fact]
    public async Task GenerateAsync_WithTopicAndContent_PassesBothToLlm()
    {
        // Arrange
        var topic = "AI is changing software development";
        var content = "Article content from paywalled source...";

        _llmServiceMock
            .Setup(x => x.GenerateTweetAsync(topic, content, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync("tweet");

        // Act
        await _sut.GenerateAsync(topic, content);

        // Assert
        _llmServiceMock.Verify(x => x.GenerateTweetAsync(topic, content, false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GenerateAsync_WithWebSearch_PassesFlagToLlm()
    {
        // Arrange
        var topic = "Latest AI news";

        _llmServiceMock
            .Setup(x => x.GenerateTweetAsync(topic, null, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync("tweet with web search");

        // Act
        await _sut.GenerateAsync(topic, null, enableWebSearch: true);

        // Assert
        _llmServiceMock.Verify(x => x.GenerateTweetAsync(topic, null, true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefineAsync_WithFeedback_ReturnsRefinedDraft()
    {
        // Arrange
        var currentTweet = "Original tweet text";
        var feedback = "Make it shorter";

        _llmServiceMock
            .Setup(x => x.RefineTweetAsync(currentTweet, feedback, It.IsAny<CancellationToken>()))
            .ReturnsAsync("Shorter tweet.");

        // Act
        var result = await _sut.RefineAsync(currentTweet, feedback);

        // Assert
        result.Should().NotBeNull();
        result.Text.Should().Be("Shorter tweet.");
    }

    [Fact]
    public async Task RefineAsync_PassesCorrectParametersToLlm()
    {
        // Arrange
        var currentTweet = "Current tweet";
        var feedback = "More punchy";

        _llmServiceMock
            .Setup(x => x.RefineTweetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("refined text");

        // Act
        await _sut.RefineAsync(currentTweet, feedback);

        // Assert
        _llmServiceMock.Verify(x => x.RefineTweetAsync(currentTweet, feedback, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GenerateAsync_PassesCancellationToken()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var topic = "test topic";

        _llmServiceMock
            .Setup(x => x.GenerateTweetAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<bool>(), cts.Token))
            .ReturnsAsync("tweet");

        // Act
        await _sut.GenerateAsync(topic, null, false, cts.Token);

        // Assert
        _llmServiceMock.Verify(x => x.GenerateTweetAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<bool>(), cts.Token), Times.Once);
    }

    [Fact]
    public async Task RefineAsync_PassesCancellationToken()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var currentTweet = "tweet";
        var feedback = "feedback";

        _llmServiceMock
            .Setup(x => x.RefineTweetAsync(It.IsAny<string>(), It.IsAny<string>(), cts.Token))
            .ReturnsAsync("refined");

        // Act
        await _sut.RefineAsync(currentTweet, feedback, cts.Token);

        // Assert
        _llmServiceMock.Verify(x => x.RefineTweetAsync(It.IsAny<string>(), It.IsAny<string>(), cts.Token), Times.Once);
    }
}
