using Follower.Configuration;
using Follower.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Follower.Tests.Services;

public class EmailServiceTests
{
    private readonly EmailService _sut;
    private readonly Mock<IOptions<AgentOptions>> _optionsMock;
    private readonly Mock<ILogger<EmailService>> _loggerMock;

    public EmailServiceTests()
    {
        _optionsMock = new Mock<IOptions<AgentOptions>>();
        _optionsMock.Setup(x => x.Value).Returns(new AgentOptions
        {
            ImapServer = "imap.mail.yahoo.com",
            ImapPort = 993,
            SmtpServer = "smtp.mail.yahoo.com",
            SmtpPort = 465,
            EmailUsername = "test@yahoo.com",
            EmailPassword = "test-password"
        });
        _loggerMock = new Mock<ILogger<EmailService>>();
        _sut = new EmailService(_optionsMock.Object, _loggerMock.Object);
    }

    [Fact(Skip = "Requires real email credentials")]
    public async Task GetUnreadRepliesAsync_WithValidCredentials_ReturnsMessages()
    {
        // Act
        var result = await _sut.GetUnreadRepliesAsync();

        // Assert
        result.Should().NotBeNull();
    }

    [Fact(Skip = "Requires real email credentials")]
    public async Task GetDraftsAsync_WithValidCredentials_ReturnsMessages()
    {
        // Act
        var result = await _sut.GetDraftsAsync();

        // Assert
        result.Should().NotBeNull();
    }

    [Fact(Skip = "Requires real email credentials")]
    public async Task GetInfluencerTweetsAsync_WithValidCredentials_ReturnsMessages()
    {
        // Act
        var result = await _sut.GetInfluencerTweetsAsync();

        // Assert
        result.Should().NotBeNull();
    }

    [Fact(Skip = "Requires real email credentials")]
    public async Task SendAsync_WithValidCredentials_SendsEmail()
    {
        // Act
        var act = () => _sut.SendAsync("to@example.com", "subject", "body");

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact(Skip = "Requires real email credentials")]
    public async Task MoveToArchiveAsync_WithValidCredentials_MovesMessage()
    {
        // Act
        var act = () => _sut.MoveToArchiveAsync("123");

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact(Skip = "Requires real email credentials")]
    public async Task CountArchivedByPrefixAsync_WithValidCredentials_ReturnsCount()
    {
        // Act
        var result = await _sut.CountArchivedByPrefixAsync("2024-01-01");

        // Assert
        result.Should().BeGreaterOrEqualTo(0);
    }
}
