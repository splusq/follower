using Follower.Configuration;
using Follower.Models;
using Follower.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Follower.Tests;

public class WorkerTests
{
    private readonly Worker _sut;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<ITweetService> _tweetServiceMock;
    private readonly Mock<IXTwitterService> _xTwitterServiceMock;
    private readonly Mock<IOptions<AgentOptions>> _optionsMock;
    private readonly Mock<ILogger<Worker>> _loggerMock;

    public WorkerTests()
    {
        _emailServiceMock = new Mock<IEmailService>();
        _tweetServiceMock = new Mock<ITweetService>();
        _xTwitterServiceMock = new Mock<IXTwitterService>();
        _optionsMock = new Mock<IOptions<AgentOptions>>();
        _loggerMock = new Mock<ILogger<Worker>>();

        _optionsMock.Setup(x => x.Value).Returns(new AgentOptions { PollIntervalSeconds = 1 });

        _sut = new Worker(
            _emailServiceMock.Object,
            _tweetServiceMock.Object,
            _xTwitterServiceMock.Object,
            _optionsMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WhenStarted_LogsWorkerStarting()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        _emailServiceMock
            .Setup(x => x.GetUnreadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<EmailMessage>());

        // Act - cancel immediately to stop the loop
        cts.CancelAfter(100);

        try
        {
            await _sut.StartAsync(cts.Token);
            await Task.Delay(50);
            await _sut.StopAsync(CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Worker starting")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenProcessingCycle_CallsGetUnread()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        _emailServiceMock
            .Setup(x => x.GetUnreadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<EmailMessage>());

        // Act
        cts.CancelAfter(200);

        try
        {
            await _sut.StartAsync(cts.Token);
            await Task.Delay(150);
            await _sut.StopAsync(CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // Assert
        _emailServiceMock.Verify(
            x => x.GetUnreadAsync(It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ProcessCycle_WithNewTopic_GeneratesTweetAndReplies()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var newTopic = new EmailMessage(
            "123",
            "msg-id-123",
            null, // Not a reply - new topic
            "user@example.com",
            "Microservices",
            "Thoughts on microservices vs monolith",
            DateTimeOffset.UtcNow);

        _emailServiceMock
            .Setup(x => x.GetUnreadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { newTopic });

        _tweetServiceMock
            .Setup(x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TweetDraft("Skip microservices. Ship faster."));

        // Act
        cts.CancelAfter(200);

        try
        {
            await _sut.StartAsync(cts.Token);
            await Task.Delay(150);
            await _sut.StopAsync(CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // Assert - should generate tweet and reply
        _tweetServiceMock.Verify(
            x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
        _emailServiceMock.Verify(
            x => x.ReplyAsync(newTopic, It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
        _emailServiceMock.Verify(
            x => x.MarkAsReadAsync("123", It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ProcessCycle_WithReplyContainingPost_PostsTweetAndArchives()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var reply = new EmailMessage(
            "456",
            "msg-id-456",
            "msg-id-123", // Is a reply
            "user@example.com",
            "Re: Microservices",
            "/post\n\n\"Skip microservices. Ship faster.\"",
            DateTimeOffset.UtcNow);

        _emailServiceMock
            .Setup(x => x.GetUnreadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { reply });

        _xTwitterServiceMock
            .Setup(x => x.PostTweetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        cts.CancelAfter(200);

        try
        {
            await _sut.StartAsync(cts.Token);
            await Task.Delay(150);
            await _sut.StopAsync(CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // Assert
        _xTwitterServiceMock.Verify(
            x => x.PostTweetAsync("Skip microservices. Ship faster.", It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
        _emailServiceMock.Verify(
            x => x.ArchiveAsync("456", It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }
}
