using Follower.Services;
using FluentAssertions;
using Xunit;

namespace Follower.Tests.Services;

public class XTwitterServiceTests
{
    private readonly XTwitterService _sut;

    public XTwitterServiceTests()
    {
        _sut = new XTwitterService();
    }

    [Fact]
    public async Task PostTweetAsync_WhenCalled_ThrowsNotImplementedException()
    {
        // Act
        var act = () => _sut.PostTweetAsync("Hello world!");

        // Assert
        await act.Should().ThrowAsync<NotImplementedException>();
    }
}
