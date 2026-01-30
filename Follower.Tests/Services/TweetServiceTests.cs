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
    public async Task GenerateAsync_WithDraftAndStyle_ReturnsTweetDraft()
    {
        // Arrange
        var draft = new EmailMessage("draft-123", "My Notes", "Microservices are complex for startups", DateTimeOffset.UtcNow);
        var style = new StyleProfile("Tone: provocative, confident.", DateTimeOffset.UtcNow);

        _llmServiceMock
            .Setup(x => x.GenerateTweetAsync(draft.Body, style.ProfileText, It.IsAny<CancellationToken>()))
            .ReturnsAsync("Hot take: skip the microservices, ship the monolith.");

        // Act
        var result = await _sut.GenerateAsync(draft, style);

        // Assert
        result.Should().NotBeNull();
        result.Text.Should().Be("Hot take: skip the microservices, ship the monolith.");
        result.SourceDraftId.Should().Be("draft-123");
        result.Sequence.Should().Be(1);
    }

    [Fact]
    public async Task GenerateAsync_PassesCorrectParametersToLlm()
    {
        // Arrange
        var draft = new EmailMessage("id", "subject", "notes content", DateTimeOffset.UtcNow);
        var style = new StyleProfile("style guide text", DateTimeOffset.UtcNow);

        _llmServiceMock
            .Setup(x => x.GenerateTweetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("tweet");

        // Act
        await _sut.GenerateAsync(draft, style);

        // Assert
        _llmServiceMock.Verify(x => x.GenerateTweetAsync("notes content", "style guide text", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefineAsync_WithFeedback_ReturnsRefinedDraft()
    {
        // Arrange
        var currentDraft = new TweetDraft("Original tweet text", "draft-456", 2);
        var style = new StyleProfile("Tone: casual", DateTimeOffset.UtcNow);
        var feedback = "Make it shorter";

        _llmServiceMock
            .Setup(x => x.RefineTweetAsync(feedback, currentDraft.Text, style.ProfileText, It.IsAny<CancellationToken>()))
            .ReturnsAsync("Shorter tweet.");

        // Act
        var result = await _sut.RefineAsync(feedback, currentDraft, style);

        // Assert
        result.Should().NotBeNull();
        result.Text.Should().Be("Shorter tweet.");
        result.SourceDraftId.Should().Be("draft-456");
        result.Sequence.Should().Be(2);
    }

    [Fact]
    public async Task RefineAsync_PreservesSourceDraftIdAndSequence()
    {
        // Arrange
        var currentDraft = new TweetDraft("text", "original-source-id", 3);
        var style = new StyleProfile("style", DateTimeOffset.UtcNow);

        _llmServiceMock
            .Setup(x => x.RefineTweetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("refined text");

        // Act
        var result = await _sut.RefineAsync("feedback", currentDraft, style);

        // Assert
        result.SourceDraftId.Should().Be("original-source-id");
        result.Sequence.Should().Be(3);
    }

    [Fact]
    public async Task GenerateAsync_PassesCancellationToken()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var draft = new EmailMessage("id", "subject", "body", DateTimeOffset.UtcNow);
        var style = new StyleProfile("style", DateTimeOffset.UtcNow);

        _llmServiceMock
            .Setup(x => x.GenerateTweetAsync(It.IsAny<string>(), It.IsAny<string>(), cts.Token))
            .ReturnsAsync("tweet");

        // Act
        await _sut.GenerateAsync(draft, style, cts.Token);

        // Assert
        _llmServiceMock.Verify(x => x.GenerateTweetAsync(It.IsAny<string>(), It.IsAny<string>(), cts.Token), Times.Once);
    }

    [Fact]
    public async Task RefineAsync_PassesCancellationToken()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var currentDraft = new TweetDraft("text", "id", 1);
        var style = new StyleProfile("style", DateTimeOffset.UtcNow);

        _llmServiceMock
            .Setup(x => x.RefineTweetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), cts.Token))
            .ReturnsAsync("refined");

        // Act
        await _sut.RefineAsync("feedback", currentDraft, style, cts.Token);

        // Assert
        _llmServiceMock.Verify(x => x.RefineTweetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), cts.Token), Times.Once);
    }
}
