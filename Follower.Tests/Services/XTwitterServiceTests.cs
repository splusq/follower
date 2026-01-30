using System.Net;
using Follower.Configuration;
using Follower.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Xunit;

namespace Follower.Tests.Services;

public class XTwitterServiceTests
{
    private readonly Mock<HttpMessageHandler> _httpHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly Mock<IOptions<AgentOptions>> _optionsMock;
    private readonly Mock<ILogger<XTwitterService>> _loggerMock;
    private readonly XTwitterService _sut;

    public XTwitterServiceTests()
    {
        _httpHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpHandlerMock.Object);

        _optionsMock = new Mock<IOptions<AgentOptions>>();
        _optionsMock.Setup(x => x.Value).Returns(new AgentOptions
        {
            TwitterApiKey = "test-api-key",
            TwitterApiSecret = "test-api-secret",
            TwitterAccessToken = "test-access-token",
            TwitterAccessTokenSecret = "test-access-token-secret"
        });

        _loggerMock = new Mock<ILogger<XTwitterService>>();

        _sut = new XTwitterService(_httpClient, _optionsMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task PostTweetAsync_WithValidTweet_ReturnsTrue()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.Created, """{"data":{"id":"123","text":"Hello"}}""");

        // Act
        var result = await _sut.PostTweetAsync("Hello world!");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task PostTweetAsync_WithEmptyText_ReturnsFalse()
    {
        // Act
        var result = await _sut.PostTweetAsync("");

        // Assert
        result.Should().BeFalse();
        VerifyNoHttpCalls();
    }

    [Fact]
    public async Task PostTweetAsync_WithWhitespaceText_ReturnsFalse()
    {
        // Act
        var result = await _sut.PostTweetAsync("   ");

        // Assert
        result.Should().BeFalse();
        VerifyNoHttpCalls();
    }

    [Fact]
    public async Task PostTweetAsync_WithTweetOver280Chars_ReturnsFalse()
    {
        // Arrange
        var longTweet = new string('a', 281);

        // Act
        var result = await _sut.PostTweetAsync(longTweet);

        // Assert
        result.Should().BeFalse();
        VerifyNoHttpCalls();
    }

    [Fact]
    public async Task PostTweetAsync_WithTweetExactly280Chars_ReturnsTrue()
    {
        // Arrange
        var tweet = new string('a', 280);
        SetupHttpResponse(HttpStatusCode.Created, """{"data":{"id":"123"}}""");

        // Act
        var result = await _sut.PostTweetAsync(tweet);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task PostTweetAsync_OnApiError_ReturnsFalse()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.Unauthorized, """{"error":"Unauthorized"}""");

        // Act
        var result = await _sut.PostTweetAsync("Hello!");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task PostTweetAsync_SendsCorrectContentType()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        _httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent("""{"data":{"id":"123"}}""")
            });

        // Act
        await _sut.PostTweetAsync("Hello!");

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Content!.Headers.ContentType!.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task PostTweetAsync_IncludesOAuthHeader()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        _httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent("""{"data":{"id":"123"}}""")
            });

        // Act
        await _sut.PostTweetAsync("Hello!");

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Headers.Authorization.Should().NotBeNull();
        capturedRequest.Headers.Authorization!.Scheme.Should().Be("OAuth");
        capturedRequest.Headers.Authorization.Parameter.Should().Contain("oauth_consumer_key");
        capturedRequest.Headers.Authorization.Parameter.Should().Contain("oauth_signature");
    }

    [Fact]
    public async Task PostTweetAsync_SendsJsonBody()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        _httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent("""{"data":{"id":"123"}}""")
            });

        // Act
        await _sut.PostTweetAsync("Test tweet");

        // Assert
        capturedRequest.Should().NotBeNull();
        var body = await capturedRequest!.Content!.ReadAsStringAsync();
        body.Should().Contain("\"text\"");
        body.Should().Contain("Test tweet");
    }

    private void SetupHttpResponse(HttpStatusCode statusCode, string content)
    {
        _httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content)
            });
    }

    private void VerifyNoHttpCalls()
    {
        _httpHandlerMock
            .Protected()
            .Verify(
                "SendAsync",
                Times.Never(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
    }
}
