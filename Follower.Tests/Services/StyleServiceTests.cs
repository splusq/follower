using Follower.Models;
using Follower.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Follower.Tests.Services;

public class StyleServiceTests
{
    private readonly StyleService _sut;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<ILlmService> _llmServiceMock;
    private readonly Mock<ILogger<StyleService>> _loggerMock;

    public StyleServiceTests()
    {
        _emailServiceMock = new Mock<IEmailService>();
        _llmServiceMock = new Mock<ILlmService>();
        _loggerMock = new Mock<ILogger<StyleService>>();
        _sut = new StyleService(_emailServiceMock.Object, _llmServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetStyleProfileAsync_WithInfluencers_ReturnsStyleProfile()
    {
        // Arrange - each email = one influencer, subject = name, body = their tweets
        var influencerEmails = new List<EmailMessage>
        {
            new("1", "Paul Graham", "Hot take: the best code is deleted code.\nYour startup doesn't need Kubernetes.", DateTimeOffset.UtcNow),
            new("2", "DHH", "Rails is still the best.\nMajestic monoliths forever.", DateTimeOffset.UtcNow)
        };
        _emailServiceMock
            .Setup(x => x.GetInfluencerTweetsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(influencerEmails);

        _llmServiceMock
            .Setup(x => x.AnalyzeStyleAsync(It.IsAny<IEnumerable<InfluencerExample>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Tone: provocative, confident. Short sentences.");

        // Act
        var result = await _sut.GetStyleProfileAsync();

        // Assert
        result.Should().NotBeNull();
        result.ProfileText.Should().Be("Tone: provocative, confident. Short sentences.");
        result.GeneratedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetStyleProfileAsync_WithNoInfluencers_ReturnsEmptyProfile()
    {
        // Arrange
        _emailServiceMock
            .Setup(x => x.GetInfluencerTweetsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EmailMessage>());

        // Act
        var result = await _sut.GetStyleProfileAsync();

        // Assert
        result.Should().NotBeNull();
        result.ProfileText.Should().BeEmpty();
        _llmServiceMock.Verify(x => x.AnalyzeStyleAsync(It.IsAny<IEnumerable<InfluencerExample>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetStyleProfileAsync_FiltersEmptyBodies()
    {
        // Arrange
        var influencerEmails = new List<EmailMessage>
        {
            new("1", "Paul Graham", "Valid tweets here", DateTimeOffset.UtcNow),
            new("2", "Empty Person", "", DateTimeOffset.UtcNow),
            new("3", "Whitespace Only", "   ", DateTimeOffset.UtcNow),
            new("4", "DHH", "More valid tweets", DateTimeOffset.UtcNow)
        };
        _emailServiceMock
            .Setup(x => x.GetInfluencerTweetsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(influencerEmails);

        _llmServiceMock
            .Setup(x => x.AnalyzeStyleAsync(It.IsAny<IEnumerable<InfluencerExample>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Style profile");

        // Act
        await _sut.GetStyleProfileAsync();

        // Assert - verify only non-empty bodies were passed (2 influencers)
        _llmServiceMock.Verify(x => x.AnalyzeStyleAsync(
            It.Is<IEnumerable<InfluencerExample>>(influencers => influencers.Count() == 2),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetStyleProfileAsync_PassesInfluencerNamesFromSubject()
    {
        // Arrange
        var influencerEmails = new List<EmailMessage>
        {
            new("1", "Elon Musk", "Tweet content here", DateTimeOffset.UtcNow)
        };
        _emailServiceMock
            .Setup(x => x.GetInfluencerTweetsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(influencerEmails);

        _llmServiceMock
            .Setup(x => x.AnalyzeStyleAsync(It.IsAny<IEnumerable<InfluencerExample>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Profile");

        // Act
        await _sut.GetStyleProfileAsync();

        // Assert - verify influencer name is passed correctly
        _llmServiceMock.Verify(x => x.AnalyzeStyleAsync(
            It.Is<IEnumerable<InfluencerExample>>(influencers =>
                influencers.First().Name == "Elon Musk" &&
                influencers.First().Tweets == "Tweet content here"),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetStyleProfileAsync_PassesCancellationToken()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var influencerEmails = new List<EmailMessage>
        {
            new("1", "Influencer", "Content", DateTimeOffset.UtcNow)
        };
        _emailServiceMock
            .Setup(x => x.GetInfluencerTweetsAsync(cts.Token))
            .ReturnsAsync(influencerEmails);

        _llmServiceMock
            .Setup(x => x.AnalyzeStyleAsync(It.IsAny<IEnumerable<InfluencerExample>>(), cts.Token))
            .ReturnsAsync("Profile");

        // Act
        await _sut.GetStyleProfileAsync(cts.Token);

        // Assert
        _emailServiceMock.Verify(x => x.GetInfluencerTweetsAsync(cts.Token), Times.Once);
        _llmServiceMock.Verify(x => x.AnalyzeStyleAsync(It.IsAny<IEnumerable<InfluencerExample>>(), cts.Token), Times.Once);
    }
}
