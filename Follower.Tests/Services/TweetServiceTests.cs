using Follower.Models;
using Follower.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace Follower.Tests.Services;

public class TweetServiceTests
{
    private readonly TweetService _sut;
    private readonly Mock<ILlmService> _llmServiceMock;

    public TweetServiceTests()
    {
        _llmServiceMock = new Mock<ILlmService>();
        _sut = new TweetService(_llmServiceMock.Object);
    }

    [Fact]
    public async Task GenerateAsync_WhenCalled_ThrowsNotImplementedException()
    {
        // Arrange
        var draft = new EmailMessage("id", "subject", "body", DateTimeOffset.UtcNow);
        var style = new StyleProfile("profile", DateTimeOffset.UtcNow);

        // Act
        var act = () => _sut.GenerateAsync(draft, style);

        // Assert
        await act.Should().ThrowAsync<NotImplementedException>();
    }

    [Fact]
    public async Task RefineAsync_WhenCalled_ThrowsNotImplementedException()
    {
        // Arrange
        var currentDraft = new TweetDraft("text", "source-id", 1);
        var style = new StyleProfile("profile", DateTimeOffset.UtcNow);

        // Act
        var act = () => _sut.RefineAsync("feedback", currentDraft, style);

        // Assert
        await act.Should().ThrowAsync<NotImplementedException>();
    }
}
