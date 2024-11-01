using Xunit;
using Microsoft.Extensions.Logging;
using Moq;
using System.IO;

namespace ClipGitter.Tests;

public class GitManagerTests
{
    private readonly Mock<ILogger<GitManager>> _loggerMock;
    private readonly string _testRepoPath;

    public GitManagerTests()
    {
        _loggerMock = new Mock<ILogger<GitManager>>();
        _testRepoPath = Path.Combine(Path.GetTempPath(), "test-repo");
    }

    [Fact]
    public async Task PullChangesAsync_WithInvalidPath_ThrowsException()
    {
        // Arrange
        var gitManager = new GitManager(_loggerMock.Object, "invalid-path");

        // Act & Assert
        await Assert.ThrowsAsync<LibGit2Sharp.RepositoryNotFoundException>(async () =>
        {
            await gitManager.PullChangesAsync("test-env-path", "");
        });
    }

    [Fact]
    public async Task CommitAndPushAsync_WithInvalidPath_ThrowsException()
    {
        // Arrange
        var gitManager = new GitManager(_loggerMock.Object, "invalid-path");

        // Act & Assert
        await Assert.ThrowsAsync<LibGit2Sharp.RepositoryNotFoundException>(async () =>
        {
            await gitManager.CommitAndPushAsync("test.txt", "test message", "test-env-path");
        });
    }

    [Fact]
    public async Task CommitAndForcePushAsync_WithInvalidPath_ThrowsException()
    {
        // Arrange
        var gitManager = new GitManager(_loggerMock.Object, "invalid-path");

        // Act & Assert
        await Assert.ThrowsAsync<LibGit2Sharp.RepositoryNotFoundException>(async () =>
        {
            await gitManager.CommitAndForcePushAsync("test.txt", "test-env-path");
        });
    }
}
