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
    private readonly Mock<IStyleService> _styleServiceMock;
    private readonly Mock<ITweetService> _tweetServiceMock;
    private readonly Mock<IXTwitterService> _xTwitterServiceMock;
    private readonly Mock<IOptions<AgentOptions>> _optionsMock;
    private readonly Mock<ILogger<Worker>> _loggerMock;

    public WorkerTests()
    {
        _emailServiceMock = new Mock<IEmailService>();
        _styleServiceMock = new Mock<IStyleService>();
        _tweetServiceMock = new Mock<ITweetService>();
        _xTwitterServiceMock = new Mock<IXTwitterService>();
        _optionsMock = new Mock<IOptions<AgentOptions>>();
        _loggerMock = new Mock<ILogger<Worker>>();

        _optionsMock.Setup(x => x.Value).Returns(new AgentOptions { PollIntervalSeconds = 1 });

        _sut = new Worker(
            _emailServiceMock.Object,
            _styleServiceMock.Object,
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
            .Setup(x => x.GetUnreadRepliesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<EmailMessage>());
        _emailServiceMock
            .Setup(x => x.CountArchivedByPrefixAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

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
    public async Task ExecuteAsync_WhenProcessingCycle_CallsGetUnreadReplies()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        _emailServiceMock
            .Setup(x => x.GetUnreadRepliesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<EmailMessage>());
        _emailServiceMock
            .Setup(x => x.CountArchivedByPrefixAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(10); // Above daily target so no new tweet generation

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
            x => x.GetUnreadRepliesAsync(It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }
}
