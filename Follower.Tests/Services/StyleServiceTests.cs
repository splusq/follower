using Follower.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace Follower.Tests.Services;

public class StyleServiceTests
{
    private readonly StyleService _sut;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<ILlmService> _llmServiceMock;

    public StyleServiceTests()
    {
        _emailServiceMock = new Mock<IEmailService>();
        _llmServiceMock = new Mock<ILlmService>();
        _sut = new StyleService(_emailServiceMock.Object, _llmServiceMock.Object);
    }

    [Fact]
    public async Task GetStyleProfileAsync_WhenCalled_ThrowsNotImplementedException()
    {
        // Act
        var act = () => _sut.GetStyleProfileAsync();

        // Assert
        await act.Should().ThrowAsync<NotImplementedException>();
    }
}
