using DotNetEnv;
using Follower.Configuration;
using Follower.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Follower.Tests.Services;

public class EmailServiceIntegrationTests
{
    private readonly EmailService? _sut;
    private readonly bool _credentialsAvailable;

    public EmailServiceIntegrationTests()
    {
        // Try to load .env from project directory
        var envPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "Follower", ".env");
        if (File.Exists(envPath))
        {
            Env.Load(envPath);
        }

        var imapServer = Environment.GetEnvironmentVariable("Agent__ImapServer");
        var emailUsername = Environment.GetEnvironmentVariable("Agent__EmailUsername");
        var emailPassword = Environment.GetEnvironmentVariable("Agent__EmailPassword");

        _credentialsAvailable = !string.IsNullOrEmpty(imapServer)
            && !string.IsNullOrEmpty(emailUsername)
            && !string.IsNullOrEmpty(emailPassword);

        if (_credentialsAvailable)
        {
            var options = Options.Create(new AgentOptions
            {
                ImapServer = imapServer!,
                ImapPort = int.Parse(Environment.GetEnvironmentVariable("Agent__ImapPort") ?? "993"),
                SmtpServer = Environment.GetEnvironmentVariable("Agent__SmtpServer") ?? "smtp.mail.yahoo.com",
                SmtpPort = int.Parse(Environment.GetEnvironmentVariable("Agent__SmtpPort") ?? "465"),
                EmailUsername = emailUsername!,
                EmailPassword = emailPassword!
            });
            var logger = new Mock<ILogger<EmailService>>();
            _sut = new EmailService(options, logger.Object);
        }
    }

    private void SkipIfNoCredentials()
    {
        Skip.If(!_credentialsAvailable, "Email credentials not configured. Set Agent__EmailUsername and Agent__EmailPassword env vars.");
    }

    [SkippableFact]
    public async Task GetUnreadAsync_ConnectsToImapAndReturnsMessages()
    {
        SkipIfNoCredentials();

        // Act
        var result = await _sut!.GetUnreadAsync();

        // Assert
        result.Should().NotBeNull();
        // We don't assert on count since inbox state varies
    }

    [SkippableFact]
    public async Task GetUnreadAsync_ReturnsEmailsWithThreadingInfo()
    {
        SkipIfNoCredentials();

        // Act
        var result = await _sut!.GetUnreadAsync();

        // Assert
        result.Should().NotBeNull();
        foreach (var email in result)
        {
            email.Uid.Should().NotBeNullOrEmpty();
            email.MessageId.Should().NotBeNull(); // May be empty but not null
            // InReplyTo can be null for non-replies
        }
    }
}
