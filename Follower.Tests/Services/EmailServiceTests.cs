using Follower.Configuration;
using Follower.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Follower.Tests.Services;

public class EmailServiceTests
{
    private readonly EmailService _sut;
    private readonly Mock<IOptions<AgentOptions>> _optionsMock;

    public EmailServiceTests()
    {
        _optionsMock = new Mock<IOptions<AgentOptions>>();
        _optionsMock.Setup(x => x.Value).Returns(new AgentOptions());
        _sut = new EmailService(_optionsMock.Object);
    }

    [Fact]
    public async Task GetUnreadRepliesAsync_WhenCalled_ThrowsNotImplementedException()
    {
        // Act
        var act = () => _sut.GetUnreadRepliesAsync();

        // Assert
        await act.Should().ThrowAsync<NotImplementedException>();
    }

    [Fact]
    public async Task GetDraftsAsync_WhenCalled_ThrowsNotImplementedException()
    {
        // Act
        var act = () => _sut.GetDraftsAsync();

        // Assert
        await act.Should().ThrowAsync<NotImplementedException>();
    }

    [Fact]
    public async Task GetInfluencerTweetsAsync_WhenCalled_ThrowsNotImplementedException()
    {
        // Act
        var act = () => _sut.GetInfluencerTweetsAsync();

        // Assert
        await act.Should().ThrowAsync<NotImplementedException>();
    }

    [Fact]
    public async Task SendAsync_WhenCalled_ThrowsNotImplementedException()
    {
        // Act
        var act = () => _sut.SendAsync("to@example.com", "subject", "body");

        // Assert
        await act.Should().ThrowAsync<NotImplementedException>();
    }

    [Fact]
    public async Task MoveToArchiveAsync_WhenCalled_ThrowsNotImplementedException()
    {
        // Act
        var act = () => _sut.MoveToArchiveAsync("message-id");

        // Assert
        await act.Should().ThrowAsync<NotImplementedException>();
    }

    [Fact]
    public async Task CountArchivedByPrefixAsync_WhenCalled_ThrowsNotImplementedException()
    {
        // Act
        var act = () => _sut.CountArchivedByPrefixAsync("2024-01-01");

        // Assert
        await act.Should().ThrowAsync<NotImplementedException>();
    }
}
