namespace Snowberry.Globbing.Tests;

/// <summary>
/// Tests for options.ignore.
/// Ported from: https://github.com/micromatch/picomatch/blob/master/test/options.ignore.js
/// </summary>
public class OptionsIgnoreTests
{
    [Theory]
    [InlineData("a", "a", true)]
    [InlineData("b", "a", false)]
    public void Ignore_BasicMatching(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Fact]
    public void Ignore_ShouldExcludeSinglePattern()
    {
        var options = new GlobbingOptions
        {
            Ignore = new[] { "b" }
        };

        Assert.True(GlobMatcher.IsMatch("a", "*", options));
        Assert.False(GlobMatcher.IsMatch("b", "*", options));
        Assert.True(GlobMatcher.IsMatch("c", "*", options));
    }

    [Fact]
    public void Ignore_ShouldExcludeMultiplePatterns()
    {
        var options = new GlobbingOptions
        {
            Ignore = new[] { "b", "c" }
        };

        Assert.True(GlobMatcher.IsMatch("a", "*", options));
        Assert.False(GlobMatcher.IsMatch("b", "*", options));
        Assert.False(GlobMatcher.IsMatch("c", "*", options));
        Assert.True(GlobMatcher.IsMatch("d", "*", options));
    }

    [Fact]
    public void Ignore_ShouldWorkWithGlobPatterns()
    {
        var options = new GlobbingOptions
        {
            Ignore = new[] { "*.txt" }
        };

        Assert.True(GlobMatcher.IsMatch("foo.js", "*", options));
        Assert.False(GlobMatcher.IsMatch("foo.txt", "*", options));
        Assert.True(GlobMatcher.IsMatch("bar.md", "*", options));
        Assert.False(GlobMatcher.IsMatch("bar.txt", "*", options));
    }

    [Fact]
    public void Ignore_ShouldWorkWithGlobstars()
    {
        var options = new GlobbingOptions
        {
            Ignore = new[] { "**/node_modules/**" }
        };

        Assert.True(GlobMatcher.IsMatch("src/file.js", "**/*.js", options));
        Assert.False(GlobMatcher.IsMatch("node_modules/pkg/file.js", "**/*.js", options));
        Assert.True(GlobMatcher.IsMatch("lib/file.js", "**/*.js", options));
    }

    [Fact]
    public void Ignore_ShouldWorkWithNegation()
    {
        var options = new GlobbingOptions
        {
            Ignore = new[] { "!*.js" }
        };

        // Negation in ignore means "don't ignore .js files"
        Assert.True(GlobMatcher.IsMatch("foo.js", "*", options));
    }

    [Fact]
    public void Ignore_ShouldSupportDirectoryPatterns()
    {
        var options = new GlobbingOptions
        {
            Ignore = new[] { "**/test/**" }
        };

        Assert.True(GlobMatcher.IsMatch("src/app.js", "**/*.js", options));
        Assert.False(GlobMatcher.IsMatch("test/app.js", "**/*.js", options));
        Assert.False(GlobMatcher.IsMatch("src/test/app.js", "**/*.js", options));
    }

    [Fact]
    public void Ignore_EmptyArray_ShouldMatchAll()
    {
        var options = new GlobbingOptions
        {
            Ignore = Array.Empty<string>()
        };

        Assert.True(GlobMatcher.IsMatch("a", "*", options));
        Assert.True(GlobMatcher.IsMatch("b", "*", options));
        Assert.True(GlobMatcher.IsMatch("anything", "*", options));
    }

    [Fact]
    public void Ignore_Null_ShouldMatchAll()
    {
        var options = new GlobbingOptions
        {
            Ignore = null
        };

        Assert.True(GlobMatcher.IsMatch("a", "*", options));
        Assert.True(GlobMatcher.IsMatch("b", "*", options));
    }
}
