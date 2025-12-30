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
    public void MatchBase_WithStringPattern_MatchesBaseName(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.MatchBase(input, pattern));
    }

    [Fact]
    public void MatchBase_WithRegex_MatchesBaseName()
    {
        var regex = GlobMatcher.MakeRe("*.js");

        Assert.True(GlobMatcher.MatchBase("foo/bar.js", regex));
        Assert.True(GlobMatcher.MatchBase("foo/bar/baz.js", regex));
        Assert.False(GlobMatcher.MatchBase("foo/bar.md", regex));
    }

    [Fact]
    public void MatchBase_WithComplexPath_ExtractsBaseNameCorrectly()
    {
        Assert.True(GlobMatcher.MatchBase("a/b/c/d/e/test.js", "*.js"));
        Assert.False(GlobMatcher.MatchBase("a/b/c/d/e/test.md", "*.js"));
    }

    [Fact]
    public void MatchBase_WithWindowsPath_HandlesCorrectly()
    {
        var options = new GlobbingOptions { Windows = true };
        Assert.True(GlobMatcher.MatchBase("foo\\bar.js", "*.js", options));
    }

    [Fact]
    public void MatchBase_WithPattern_MatchesOnlyBaseName()
    {
        // Should match basename, not full path
        Assert.True(GlobMatcher.MatchBase("src/components/Button.tsx", "*.tsx"));
        Assert.False(GlobMatcher.MatchBase("src/components/Button.tsx", "src/*.tsx"));
    }
}
