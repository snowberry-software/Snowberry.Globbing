namespace Snowberry.Globbing.Tests;

/// <summary>
/// Tests for the Scan API ported from picomatch.
/// </summary>
public class ScanApiTests
{

    [Fact]
    public void ScanShouldReturnInputPattern()
    {
        var result = GlobMatcher.Scan("*");
        Assert.Equal("*", result.Input);
    }

    [Fact]
    public void ScanShouldDetectGlobPattern()
    {
        var result = GlobMatcher.Scan("*");
        Assert.True(result.IsGlob);
    }

    [Fact]
    public void ScanShouldDetectNonGlobPattern()
    {
        var result = GlobMatcher.Scan("foo");
        Assert.False(result.IsGlob);
    }

    [Theory]
    [InlineData("foo", "foo")]
    [InlineData("foo/bar", "foo/bar")]
    [InlineData("foo/bar/baz", "foo/bar/baz")]
    public void ScanShouldReturnBaseForNonGlobPatterns(string pattern, string expectedBase)
    {
        var result = GlobMatcher.Scan(pattern);
        Assert.Equal(expectedBase, result.Base);
    }

    // Note: JS returns empty string for base when glob is at root level
    [Theory]
    [InlineData("*", "")]
    [InlineData("**", "")]
    [InlineData("foo/*", "foo")]
    [InlineData("foo/**", "foo")]
    [InlineData("foo/bar/*", "foo/bar")]
    [InlineData("foo/bar/**", "foo/bar")]
    [InlineData("a/b/c/*.txt", "a/b/c")]
    public void ScanShouldReturnBaseBeforeGlobPortion(string pattern, string expectedBase)
    {
        var result = GlobMatcher.Scan(pattern);
        Assert.Equal(expectedBase, result.Base);
    }

    [Theory]
    [InlineData("a/**/b", "a")]
    [InlineData("a/**/b/*.txt", "a")]
    public void ScanShouldReturnBaseBeforeGlobstar(string pattern, string expectedBase)
    {
        var result = GlobMatcher.Scan(pattern);
        Assert.Equal(expectedBase, result.Base);
    }

    [Theory]
    [InlineData("*", "*")]
    [InlineData("**", "**")]
    [InlineData("foo/*", "*")]
    [InlineData("foo/**", "**")]
    [InlineData("foo/bar/*", "*")]
    [InlineData("foo/bar/**", "**")]
    [InlineData("a/b/c/*.txt", "*.txt")]
    public void ScanShouldReturnGlobPortion(string pattern, string expectedGlob)
    {
        var result = GlobMatcher.Scan(pattern);
        Assert.Equal(expectedGlob, result.Glob);
    }

    [Theory]
    [InlineData("a/**/b", "**/b")]
    [InlineData("a/**/b/*.txt", "**/b/*.txt")]
    public void ScanShouldReturnGlobPortionWithGlobstar(string pattern, string expectedGlob)
    {
        var result = GlobMatcher.Scan(pattern);
        Assert.Equal(expectedGlob, result.Glob);
    }

    [Theory]
    [InlineData("{a,b}", true)]
    [InlineData("{a,b,c}", true)]
    [InlineData("foo/{a,b}/bar", true)]
    public void ScanShouldDetectBracePatterns(string pattern, bool expectedIsBrace)
    {
        var result = GlobMatcher.Scan(pattern);
        Assert.Equal(expectedIsBrace, result.IsBrace);
    }

    [Theory]
    [InlineData("foo", false)]
    [InlineData("*", false)]
    [InlineData("**", false)]
    [InlineData("[abc]", false)]
    [InlineData("*.{js,ts}", false)] // Brace not at start of glob
    public void ScanShouldDetectNonBracePatterns(string pattern, bool expectedIsBrace)
    {
        var result = GlobMatcher.Scan(pattern);
        Assert.Equal(expectedIsBrace, result.IsBrace);
    }

    [Theory]
    [InlineData("[abc]", true)]
    [InlineData("[a-z]", true)]
    [InlineData("[!abc]", true)]
    [InlineData("foo/[abc]/bar", true)]
    public void ScanShouldDetectBracketPatterns(string pattern, bool expectedIsBracket)
    {
        var result = GlobMatcher.Scan(pattern);
        Assert.Equal(expectedIsBracket, result.IsBracket);
    }

    [Theory]
    [InlineData("foo", false)]
    [InlineData("*", false)]
    [InlineData("**", false)]
    [InlineData("{a,b}", false)]
    [InlineData("*.[ch]", false)] // Bracket not at start of glob
    public void ScanShouldDetectNonBracketPatterns(string pattern, bool expectedIsBracket)
    {
        var result = GlobMatcher.Scan(pattern);
        Assert.Equal(expectedIsBracket, result.IsBracket);
    }

    [Theory]
    [InlineData("*", false)]
    [InlineData("foo", false)]
    [InlineData("foo/*", false)]
    [InlineData("foo/*/bar", false)]
    [InlineData("**", false)] // JS returns false even for standalone **
    [InlineData("a/**", false)]
    [InlineData("a/**/b", false)]
    [InlineData("**/*.txt", false)]
    [InlineData("foo/**/bar/**/baz", false)]
    public void ScanShouldDetectGlobstarPatterns(string pattern, bool expectedIsGlobstar)
    {
        var result = GlobMatcher.Scan(pattern);
        Assert.Equal(expectedIsGlobstar, result.IsGlobstar);
    }

    [Theory]
    [InlineData("!(foo)", true)]
    [InlineData("@(foo)", true)]
    [InlineData("*(foo)", true)]
    [InlineData("+(foo)", true)]
    [InlineData("?(foo)", true)]
    [InlineData("!(a|b)", true)]
    [InlineData("@(a|b)", true)]
    [InlineData("*(a|b)", true)]
    [InlineData("foo/!(bar)/baz", true)]
    public void ScanShouldDetectExtglobPatterns(string pattern, bool expectedIsExtglob)
    {
        var result = GlobMatcher.Scan(pattern);
        Assert.Equal(expectedIsExtglob, result.IsExtglob);
    }

    [Theory]
    [InlineData("foo", false)]
    [InlineData("*", false)]
    [InlineData("**", false)]
    [InlineData("[abc]", false)]
    [InlineData("{a,b}", false)]
    public void ScanShouldDetectNonExtglobPatterns(string pattern, bool expectedIsExtglob)
    {
        var result = GlobMatcher.Scan(pattern);
        Assert.Equal(expectedIsExtglob, result.IsExtglob);
    }

    [Theory]
    [InlineData("!foo", true)]
    [InlineData("!*", true)]
    [InlineData("!**", true)]
    [InlineData("!foo/bar", true)]
    [InlineData("!foo/**/bar", true)]
    public void ScanShouldDetectNegatedPatterns(string pattern, bool expectedNegated)
    {
        var result = GlobMatcher.Scan(pattern);
        Assert.Equal(expectedNegated, result.Negated);
    }

    [Theory]
    [InlineData("foo", false)]
    [InlineData("*", false)]
    [InlineData("**", false)]
    [InlineData("foo/bar", false)]
    public void ScanShouldDetectNonNegatedPatterns(string pattern, bool expectedNegated)
    {
        var result = GlobMatcher.Scan(pattern);
        Assert.Equal(expectedNegated, result.Negated);
    }

    [Theory]
    [InlineData("!(foo)", true)]
    [InlineData("!(a|b)", true)]
    [InlineData("!(a)/!(b)", true)] // Starts with !(
    public void ScanShouldDetectNegatedExtglobPatterns(string pattern, bool expectedNegatedExtglob)
    {
        var result = GlobMatcher.Scan(pattern);
        Assert.Equal(expectedNegatedExtglob, result.NegatedExtglob);
    }

    [Theory]
    [InlineData("@(foo)", false)]
    [InlineData("*(foo)", false)]
    [InlineData("+(foo)", false)]
    [InlineData("?(foo)", false)]
    [InlineData("foo", false)]
    [InlineData("!foo", false)]
    [InlineData("foo/!(bar)/baz", false)] // Not at start of pattern
    public void ScanShouldDetectNonNegatedExtglobPatterns(string pattern, bool expectedNegatedExtglob)
    {
        var result = GlobMatcher.Scan(pattern);
        Assert.Equal(expectedNegatedExtglob, result.NegatedExtglob);
    }

    [Fact]
    public void ScanShouldReturnParts()
    {
        var result = GlobMatcher.Scan("foo/bar/baz", new Models.ScanOptions { Parts = true });
        Assert.NotNull(result.Parts);
        Assert.Equal(3, result.Parts.Count);
        Assert.Equal("foo", result.Parts[0]);
        Assert.Equal("bar", result.Parts[1]);
        Assert.Equal("baz", result.Parts[2]);
    }

    [Fact]
    public void ScanShouldReturnPartsWithGlob()
    {
        var result = GlobMatcher.Scan("foo/*/bar", new Models.ScanOptions { Parts = true });
        Assert.NotNull(result.Parts);
        Assert.Equal(3, result.Parts.Count);
        Assert.Equal("foo", result.Parts[0]);
        Assert.Equal("*", result.Parts[1]);
        Assert.Equal("bar", result.Parts[2]);
    }

    [Fact]
    public void ScanShouldReturnPartsWithGlobstar()
    {
        var result = GlobMatcher.Scan("foo/**/bar", new Models.ScanOptions { Parts = true });
        Assert.NotNull(result.Parts);
        Assert.Contains("foo", result.Parts);
        Assert.Contains("**", result.Parts);
        Assert.Contains("bar", result.Parts);
    }

    [Theory]
    [InlineData("./foo", "./")]
    [InlineData("./foo/bar", "./")]
    public void ScanShouldReturnPrefixForDotSlash(string pattern, string expectedPrefix)
    {
        var result = GlobMatcher.Scan(pattern);
        Assert.Equal(expectedPrefix, result.Prefix);
    }

    [Theory]
    [InlineData("/foo", "")]
    [InlineData("/foo/bar", "")]
    [InlineData("/foo/bar/*", "")]
    public void ScanShouldReturnPrefixForLeadingSlash(string pattern, string expectedPrefix)
    {
        var result = GlobMatcher.Scan(pattern);
        Assert.Equal(expectedPrefix, result.Prefix);
    }

    [Theory]
    [InlineData("*", 0)]
    [InlineData("foo/*", 0)]
    [InlineData("foo/bar/*", 0)]
    public void ScanShouldReturnStartPosition(string pattern, int expectedStart)
    {
        var result = GlobMatcher.Scan(pattern);
        Assert.Equal(expectedStart, result.Start);
    }

    [Fact]
    public void ScanShouldHandleComplexPattern()
    {
        var result = GlobMatcher.Scan("src/**/*.{js,ts}");
        Assert.True(result.IsGlob);
        Assert.False(result.IsGlobstar); // JS returns false
        Assert.False(result.IsBrace); // Brace not at start of glob
        Assert.False(result.IsExtglob);
        Assert.False(result.Negated);
        Assert.Equal("src", result.Base);
        Assert.Equal("**/*.{js,ts}", result.Glob);
    }

    [Fact]
    public void ScanShouldHandleNegatedComplexPattern()
    {
        var result = GlobMatcher.Scan("!src/**/*.test.js");
        Assert.True(result.IsGlob);
        Assert.False(result.IsGlobstar); // JS returns false
        Assert.True(result.Negated);
        Assert.Equal("src", result.Base);
    }

    [Fact]
    public void ScanShouldHandleExtglobWithBraces()
    {
        var result = GlobMatcher.Scan("src/!(test)/**/*.{js,ts}");
        Assert.True(result.IsGlob);
        Assert.False(result.IsGlobstar); // JS returns false
        Assert.False(result.IsBrace); // Brace not at start of glob
        Assert.True(result.IsExtglob);
        Assert.False(result.NegatedExtglob); // !(test) is not at start of pattern
    }

    [Theory]
    [InlineData("foo\\bar")]
    [InlineData("foo\\*")]
    [InlineData("foo\\**\\bar")]
    public void ScanShouldHandleWindowsPaths(string pattern)
    {
        // Scan should work correctly with backslash paths
        var result = GlobMatcher.Scan(pattern);
        // The Input might still contain backslashes, but the scan should work correctly
        Assert.NotNull(result);
        Assert.Equal(pattern, result.Input);
    }

}
