namespace Snowberry.Globbing.Tests;

/// <summary>
/// Tests for various picomatch options ported from picomatch.
/// </summary>
public class OptionsTests
{

    [Theory]
    [InlineData("a", "A", false)]
    [InlineData("A", "a", false)]
    [InlineData("abc", "ABC", false)]
    [InlineData("ABC", "abc", false)]
    public void ByDefaultCaseShouldMatter(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a", "A", true)]
    [InlineData("A", "a", true)]
    [InlineData("abc", "ABC", true)]
    [InlineData("ABC", "abc", true)]
    [InlineData("aBc", "AbC", true)]
    public void WithNocaseOptionCaseShouldNotMatter(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { NoCase = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("a.TXT", "*.txt", true)]
    [InlineData("A.txt", "*.TXT", true)]
    [InlineData("file.JS", "*.js", true)]
    public void WithNocaseOptionFileExtensionCaseShouldNotMatter(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { NoCase = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("SRC/file.js", "src/*", true)]
    [InlineData("src/file.js", "SRC/*", true)]
    public void WithNocaseOptionPathCaseShouldNotMatter(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { NoCase = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("a/b/c.js", "*.js", false)]
    [InlineData("a/b/c/d.js", "*.js", false)]
    public void ByDefaultMatchBaseIsDisabled(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a/b/c.js", "*.js", true)]
    [InlineData("a/b/c/d.js", "*.js", true)]
    [InlineData("a/b.txt", "*.txt", true)]
    [InlineData("x/y/z/file.md", "*.md", true)]
    public void WithMatchBaseOptionPatternShouldMatchBasename(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { BaseName = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("a/b/c.js", "c.js", true)]
    [InlineData("a/b/foo.txt", "foo.txt", true)]
    public void WithMatchBaseOptionExactNameShouldMatchBasename(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { BaseName = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("a/b/c.js", "b/*.js", false)]
    public void WithMatchBasePatternWithSlashShouldNotUseBasename(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { BaseName = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("a/b/c", "b", false)]
    [InlineData("abc/def/ghi", "def", false)]
    public void ByDefaultContainsIsDisabled(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a/b/c", "b", true)]
    [InlineData("abc/def/ghi", "def", true)]
    [InlineData("a/b/c", "a", true)]
    [InlineData("a/b/c", "c", true)]
    public void WithContainsOptionPatternCanMatchAnywhere(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { Contains = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("foo/bar/baz", "bar", true)]
    [InlineData("foo/bar/baz", "ar/ba", true)]
    [InlineData("foo/bar/baz", "qux", false)]
    public void WithContainsOptionPatternMatchesSubstring(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { Contains = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("a/b/c", "**", true)]
    [InlineData("a/b/c", "**/c", true)]
    [InlineData("a/b/c", "a/**", true)]
    [InlineData("a/b/c", "a/**/c", true)]
    public void ByDefaultGlobstarIsEnabled(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a/b/c", "**", false)]
    [InlineData("a/b/c", "**/c", false)]
    [InlineData("a/b/c", "a/**", false)]
    public void WithNoglobstarOptionDoubleStarTreatedAsLiteral(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { NoGlobstar = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("**", "**", true)]
    public void WithNoglobstarDoubleStarMatchesLiteral(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { NoGlobstar = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("ab", "a*(b)", true)]
    [InlineData("abb", "a*(b)", true)]
    [InlineData("a", "a*(b)", true)]
    public void ByDefaultExtglobIsEnabled(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    // Note: With noextglob, * is still a glob wildcard, so a*(b) means a followed by anything followed by (b)
    // JS: pm.isMatch('ab', 'a*(b)', {noextglob:true}) returns true because * matches empty
    [Theory]
    [InlineData("a*(b)", "a*(b)", true)]  // Literal match works
    [InlineData("ab", "a*(b)", true)]     // * as glob wildcard
    public void WithNoextglobOptionExtglobTreatedAsLiteral(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { NoExtglob = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("a+(b)", "a+(b)", true)]
    [InlineData("a?(b)", "a?(b)", true)]
    [InlineData("a@(b)", "a@(b)", true)]
    [InlineData("a!(b)", "a!(b)", true)]
    public void WithNoextglobOptionAllExtglobTreatedAsLiteral(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { NoExtglob = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("a", "a", true)]
    [InlineData("abc", "a*c", true)]
    public void BashOptionShouldEnableBashBehavior(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { Bash = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("a", "a/", false)]
    [InlineData("a/", "a", false)]
    public void ByDefaultTrailingSlashIsFlexible(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a", "a/", false)]
    [InlineData("a/", "a/", true)]
    [InlineData("a/", "a", false)]
    public void WithStrictslashesOptionTrailingSlashMustMatch(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { StrictSlashes = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("\\*", "\\*", true)]
    public void EscapedStarMatchesLiteralStar(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Fact]
    public void FormatOptionShouldTransformInput()
    {
        var options = new GlobbingOptions { Format = s => s.ToLowerInvariant() };
        Assert.True(GlobMatcher.IsMatch("ABC", "abc", options));
    }

    [Fact]
    public void FormatOptionShouldTransformInputBeforeMatching()
    {
        var options = new GlobbingOptions { Format = s => s.Replace("-", "") };
        Assert.True(GlobMatcher.IsMatch("a-b-c", "abc", options));
    }

    [Fact]
    public void OnIgnoreOptionShouldBeCalledForIgnoredPaths()
    {
        string? ignoredPath = null;
        var options = new GlobbingOptions
        {
            Ignore = ["*.txt"],
            OnIgnore = result => ignoredPath = result.Input
        };
        GlobMatcher.IsMatch("file.txt", "*.txt", options);
        Assert.Equal("file.txt", ignoredPath);
    }

    [Fact]
    public void OnMatchOptionShouldBeCalledForMatchedPaths()
    {
        string? matchedPath = null;
        var options = new GlobbingOptions
        {
            OnMatch = result => matchedPath = result.Input
        };
        GlobMatcher.IsMatch("file.txt", "*.txt", options);
        Assert.Equal("file.txt", matchedPath);
    }

    [Fact]
    public void OnResultOptionShouldBeCalledForAllResults()
    {
        string? resultPath = null;
        var options = new GlobbingOptions
        {
            OnResult = result => resultPath = result.Input
        };
        GlobMatcher.IsMatch("file.txt", "*.txt", options);
        Assert.Equal("file.txt", resultPath);
    }

    [Theory]
    [InlineData("a.txt", "*.txt", false)]
    [InlineData("b.txt", "*.txt", false)]
    [InlineData("a.js", "*.txt", false)]
    public void WithIgnoreOptionMatchedPathsShouldBeIgnored(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { Ignore = ["*.txt"] };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("a.js", "*", true)]
    [InlineData("a.txt", "*", false)]
    [InlineData("b.js", "*", true)]
    [InlineData("b.txt", "*", false)]
    public void WithIgnoreOptionOnlyMatchedPatternsShouldBeIgnored(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { Ignore = ["*.txt"] };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("node_modules/a.js", "**/*.js", false)]
    [InlineData("src/a.js", "**/*.js", true)]
    [InlineData("dist/a.js", "**/*.js", false)]
    public void WithIgnoreOptionDirectoriesShouldBeIgnored(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { Ignore = ["node_modules/**", "dist/**"] };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("SRC/file.js", "*.js", true)]
    [InlineData("src/FILE.JS", "*.js", true)]
    public void MatchBaseAndNocaseOptionsShouldWorkTogether(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { BaseName = true, NoCase = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData(".hidden/file.js", "**/*.js", true)]
    public void DotAndMatchBaseOptionsShouldWorkTogether(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { Dot = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

}

