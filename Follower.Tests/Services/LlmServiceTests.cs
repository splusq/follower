using Follower.Services;
using FluentAssertions;
using Xunit;

namespace Follower.Tests.Services;

public class LlmServiceTests
{
    private readonly LlmService _sut;

    public LlmServiceTests()
    {
        _sut = new LlmService();
    }

    [Fact]
    public async Task AnalyzeStyleAsync_WhenCalled_ThrowsNotImplementedException()
    {
        // Arrange
        var examples = new[] { "example tweet 1", "example tweet 2" };

        // Act
        var act = () => _sut.AnalyzeStyleAsync(examples);

        // Assert
        await act.Should().ThrowAsync<NotImplementedException>();
    }

    [Fact]
    public async Task GenerateTweetAsync_WhenCalled_ThrowsNotImplementedException()
    {
        // Act
        var act = () => _sut.GenerateTweetAsync("notes", "style profile");

        // Assert
        await act.Should().ThrowAsync<NotImplementedException>();
    }

    [Fact]
    public async Task RefineTweetAsync_WhenCalled_ThrowsNotImplementedException()
    {
        // Act
        var act = () => _sut.RefineTweetAsync("feedback", "current tweet", "style profile");

        // Assert
        await act.Should().ThrowAsync<NotImplementedException>();
    }
}
