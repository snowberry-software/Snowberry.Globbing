namespace Snowberry.Globbing.Tests;

/// <summary>
/// Tests for posix API - POSIX paths only by default.
/// Ported from: https://github.com/micromatch/picomatch/blob/master/test/api.posix.js
/// </summary>
public class ApiPosixTests
{
    [Fact]
    public void Should_UsePosixPathsOnlyByDefault()
    {
        // When Posix=true, backslashes should NOT be treated as path separators
        var options = new GlobbingOptions { Posix = true };
        Assert.True(GlobMatcher.IsMatch("a/b", "a/**", options));
        Assert.False(GlobMatcher.IsMatch("a\\b", "a/**", options));
    }

    [Fact]
    public void Should_StillBeManuallyConfigurableToAcceptNonPosixPaths()
    {
        var options = new GlobbingOptions { Posix = true, Windows = true };
        Assert.True(GlobMatcher.IsMatch("a/b", "a/**", options));
        Assert.True(GlobMatcher.IsMatch("a\\b", "a/**", options));
    }
}
