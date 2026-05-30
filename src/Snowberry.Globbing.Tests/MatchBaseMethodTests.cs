namespace Snowberry.Globbing.Tests;

/// <summary>
/// Tests for the MatchBase method
/// </summary>
public class MatchBaseMethodTests
{
    [Theory]
    [InlineData("foo/bar.js", "*.js", true)]
    [InlineData("foo/bar/baz.js", "*.js", true)]
    [InlineData("test.js", "*.js", true)]
    [InlineData("foo/bar.md", "*.js", false)]
    [InlineData("a/b/c/d/e/test.js", "*.js", true)]
    [InlineData("a/b/c/d/e/test.md", "*.js", false)]
    // Should match the basename only, not the full path
    [InlineData("src/components/Button.tsx", "*.tsx", true)]
    [InlineData("src/components/Button.tsx", "src/*.tsx", false)]
    public void MatchBase_WithStringPattern_MatchesBaseName(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.MatchBase(input, pattern));
    }

    [Theory]
    [InlineData("foo/bar.js", true)]
    [InlineData("foo/bar/baz.js", true)]
    [InlineData("foo/bar.md", false)]
    public void MatchBase_WithRegex_MatchesBaseName(string input, bool expected)
    {
        var regex = GlobMatcher.MakeRe("*.js");
        Assert.Equal(expected, GlobMatcher.MatchBase(input, regex));
    }

    [Fact]
    public void MatchBase_WithWindowsPath_HandlesCorrectly()
    {
        var options = new GlobbingOptions { Windows = true };
        Assert.True(GlobMatcher.MatchBase("foo\\bar.js", "*.js", options));
    }
}
