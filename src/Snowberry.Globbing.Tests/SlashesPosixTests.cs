namespace Snowberry.Globbing.Tests;

/// <summary>
/// Tests for POSIX slash handling ported from picomatch.
/// </summary>
public class SlashesPosixTests
{

    [Theory]
    [InlineData("a/a", "(a/b)", false)]
    [InlineData("a/b", "(a/b)", true)]
    [InlineData("a/c", "(a/b)", false)]
    [InlineData("b/a", "(a/b)", false)]
    [InlineData("b/b", "(a/b)", false)]
    [InlineData("b/c", "(a/b)", false)]
    public void ShouldMatchLiteralStringInParens(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a/a", "a/b", false)]
    [InlineData("a/b", "a/b", true)]
    [InlineData("a/c", "a/b", false)]
    [InlineData("b/a", "a/b", false)]
    [InlineData("b/b", "a/b", false)]
    [InlineData("b/c", "a/b", false)]
    public void ShouldMatchLiteralString(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a/a", "a/(a|c)", true)]
    [InlineData("a/b", "a/(a|c)", false)]
    [InlineData("a/c", "a/(a|c)", true)]
    [InlineData("a/a", "a/(a|b|c)", true)]
    [InlineData("a/b", "a/(a|b|c)", true)]
    [InlineData("a/c", "a/(a|b|c)", true)]
    public void ShouldSupportRegexLogicalOr(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a/a", "a/[b-c]", false)]
    [InlineData("a/b", "a/[b-c]", true)]
    [InlineData("a/c", "a/[b-c]", true)]
    [InlineData("a/a", "a/[a-z]", true)]
    [InlineData("a/b", "a/[a-z]", true)]
    [InlineData("a/c", "a/[a-z]", true)]
    [InlineData("a/x/y", "a/[a-z]", false)]
    [InlineData("a/x", "a/[a-z]", true)]
    public void ShouldSupportRegexRanges(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a", "*", true)]
    [InlineData("b", "*", true)]
    [InlineData("a/a", "*", false)]
    [InlineData("a/b", "*", false)]
    [InlineData("a/c", "*", false)]
    [InlineData("a/x", "*", false)]
    [InlineData("a/a/a", "*", false)]
    [InlineData("x/y", "*", false)]
    public void SingleStarShouldNotMatchSlashes(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a", "*/*", false)]
    [InlineData("a/a", "*/*", true)]
    [InlineData("a/b", "*/*", true)]
    [InlineData("a/c", "*/*", true)]
    [InlineData("x/y", "*/*", true)]
    [InlineData("a/a/a", "*/*", false)]
    public void StarSlashStarShouldMatchTwoSegments(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a", "*/*/*", false)]
    [InlineData("a/a", "*/*/*", false)]
    [InlineData("a/a/a", "*/*/*", true)]
    [InlineData("a/a/b", "*/*/*", true)]
    [InlineData("a/a/a/a", "*/*/*", false)]
    public void ThreeStarsShouldMatchThreeSegments(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a", "a/*", false)]
    [InlineData("a/a", "a/*", true)]
    [InlineData("a/b", "a/*", true)]
    [InlineData("a/x", "a/*", true)]
    [InlineData("a/a/a", "a/*", false)]
    [InlineData("x/y", "a/*", false)]
    public void PrefixedStarShouldMatchOneSegment(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a/a/a", "a/*/a", true)]
    [InlineData("a/a/b", "a/*/a", false)]
    [InlineData("a/b/a", "a/*/a", true)]
    public void StarBetweenSegmentsShouldMatchMiddleSegment(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a/a/b", "a/*/b", true)]
    [InlineData("a/b/b", "a/*/b", true)]
    [InlineData("a/a/a", "a/*/b", false)]
    public void StarWithSuffixShouldMatchCorrectly(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a", "**", true)]
    [InlineData("a/", "**", true)]
    [InlineData("a/a", "**", true)]
    [InlineData("a/b", "**", true)]
    [InlineData("a/x/y", "**", true)]
    [InlineData("a/x/y/z", "**", true)]
    public void GlobstarShouldMatchAnyDepth(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a", "**/a", true)]
    [InlineData("a/a", "**/a", true)]
    [InlineData("a/x/y/z/a", "**/a", true)]
    [InlineData("a/", "**/a", false)]
    [InlineData("a/b", "**/a", false)]
    public void GlobstarPrefixShouldMatchAnyLeadingPath(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a", "a/**", true)]
    [InlineData("a/", "a/**", true)]
    [InlineData("a/a", "a/**", true)]
    [InlineData("a/x/y", "a/**", true)]
    [InlineData("a/x/y/z", "a/**", true)]
    public void GlobstarSuffixShouldMatchAnyTrailingPath(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a", "a/**/*", false)]
    [InlineData("a/a", "a/**/*", true)]
    [InlineData("a/b", "a/**/*", true)]
    [InlineData("a/x/y", "a/**/*", true)]
    [InlineData("a/x/y/z", "a/**/*", true)]
    public void GlobstarSlashStarShouldRequireAtLeastOneSegment(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a/b/foo/bar/baz.qux", "a/b/**/bar/**/*.*", true)]
    [InlineData("a/b/bar/baz.qux", "a/b/**/bar/**/*.*", true)]
    public void ShouldMatchComplexGlobstarPatterns(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a/a", "!a/b", true)]
    [InlineData("a/b", "!a/b", false)]
    [InlineData("a/c", "!a/b", true)]
    [InlineData("b/a", "!a/b", true)]
    public void ShouldSupportNegationPatterns(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a/a", "!a/(b)", true)]
    [InlineData("a/b", "!a/(b)", false)]
    [InlineData("a/c", "!a/(b)", true)]
    public void ShouldSupportNegationWithParens(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a/a", "!(a/b)", true)]
    [InlineData("a/b", "!(a/b)", false)]
    [InlineData("a/c", "!(a/b)", true)]
    public void ShouldSupportNegationExtglob(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a.txt", "a/**/*.txt", false)]
    [InlineData("a/b.txt", "a/**/*.txt", true)]
    [InlineData("a/x/y.txt", "a/**/*.txt", true)]
    [InlineData("a/x/y/z", "a/**/*.txt", false)]
    public void ShouldMatchGlobstarWithExtension(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a.txt", "a/*.txt", false)]
    [InlineData("a/b.txt", "a/*.txt", true)]
    [InlineData("a/x/y.txt", "a/*.txt", false)]
    public void ShouldMatchSingleStarWithExtension(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a.txt", "a*.txt", true)]
    [InlineData("a/b.txt", "a*.txt", false)]
    public void StarShouldNotMatchSlashesInExtensionPattern(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a.txt", "*.txt", true)]
    [InlineData("a/b.txt", "*.txt", false)]
    public void StarExtensionShouldMatchSingleSegment(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("ef", "/*", false)]
    [InlineData("/ef", "/*", true)]
    [InlineData("/foo/bar.txt", "/foo/*", true)]
    [InlineData("/foo/bar.txt", "/foo/**", true)]
    public void ShouldMatchLeadingSlashes(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("/foo/bar.txt", "/foo/**/**/*.txt", true)]
    [InlineData("/foo/bar.txt", "/foo/**/**/bar.txt", true)]
    [InlineData("/foo/bar.txt", "/foo/**/*.txt", true)]
    [InlineData("/foo/bar.txt", "/foo/**/bar.txt", true)]
    public void ShouldMatchGlobstarsWithLeadingSlash(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("/foo/bar.txt", "/foo/*/bar.txt", false)]
    [InlineData("/foo/bar/baz.txt", "/foo/*", false)]
    [InlineData("/foo/bar/baz.txt", "/foo/**", true)]
    public void ShouldHandleNestedPathsWithLeadingSlash(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("https://foo.com/bar/baz/app.min.js", "https://foo.com/*", false)]
    [InlineData("https://foo.com/bar/baz/app.min.js", "https://foo.com/**", true)]
    [InlineData("https://foo.com/bar/baz/app.min.js", "https://foo.com/**/app.min.js", true)]
    [InlineData("https://foo.com/bar/baz/app.min.js", "https://foo.com/*/*/app.min.js", true)]
    [InlineData("https://foo.com/bar/baz/app.min.js", "https://foo.com/*/app.min.js", false)]
    public void ShouldMatchDoubleSlashesInUrls(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("https://foo.com/bar/baz/app.min.js", "https://foo.com/**", false)]
    [InlineData("https://foo.com/bar/baz/app.min.js", "https://foo.com/**/app.min.js", false)]
    public void ShouldNotMatchUrlsWhenGlobstarDisabled(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { NoGlobstar = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("a", "*/", false)]
    [InlineData("a/", "*/", true)]
    [InlineData("a/a", "*/", false)]
    [InlineData("a/a/", "*/", false)]
    public void ShouldRespectTrailingSlashesOnPatterns(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a", "*/*/", false)]
    [InlineData("a/", "*/*/", false)]
    [InlineData("a/a", "*/*/", false)]
    [InlineData("a/a/", "*/*/", true)]
    [InlineData("a/b/", "*/*/", true)]
    [InlineData("x/y/", "*/*/", true)]
    public void ShouldMatchTrailingSlashesWithTwoStars(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData(".md", "\\*", false)]
    [InlineData("*", "\\*", true)]
    [InlineData("**", "\\*", false)]
    [InlineData("*.md", "\\*", false)]
    public void ShouldMatchLiteralStarWhenEscaped(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("*.md", "\\*.md", true)]
    [InlineData(".md", "\\*.md", false)]
    [InlineData("**.md", "\\*.md", false)]
    public void ShouldMatchLiteralStarWithExtensionWhenEscaped(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("*.md", "\\**.md", true)]
    [InlineData("**.md", "\\**.md", true)]
    [InlineData("**a.md", "\\**.md", true)]
    [InlineData(".md", "\\**.md", false)]
    public void ShouldMatchDoubleStarAsLiteralStarFollowedByGlob(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a/.b", "a/**/z/*.md", false)]
    [InlineData("a/b/c/j/e/z/c.txt", "a/**/j/**/z/*.md", false)]
    [InlineData("a/.b", "a/.*", true)]
    [InlineData("a/b/c/d/e/j/n/p/o/z/c.md", "a/**/j/**/z/*.md", true)]
    [InlineData("a/b/c/d/e/z/c.md", "a/**/z/*.md", true)]
    [InlineData("a/b/c/xyz.md", "a/b/c/*.md", true)]
    public void ShouldMatchFilePaths(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("foo.txt", "*/*.txt", false)]
    [InlineData("foo.txt", "**/foo.txt", true)]
    [InlineData("foo/bar.txt", "**/*.txt", true)]
    [InlineData("foo/bar/baz.txt", "**/*.txt", true)]
    public void ShouldMatchNestedFilePaths(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a", "*", true)]
    [InlineData("aa", "*", true)]
    [InlineData("aaa", "*", true)]
    [InlineData("ab", "*", true)]
    [InlineData("b", "*", true)]
    [InlineData("a/a", "*", false)]
    public void StarShouldMatchOneOrMoreCharacters(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a", "a*", true)]
    [InlineData("aa", "a*", true)]
    [InlineData("aaa", "a*", true)]
    [InlineData("ab", "a*", true)]
    [InlineData("b", "a*", false)]
    [InlineData("a/a", "a*", false)]
    public void StarWithPrefixShouldMatch(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("ab", "*b", true)]
    [InlineData("b", "*b", true)]
    [InlineData("bb", "*b", true)]
    [InlineData("a", "*b", false)]
    [InlineData("c", "*b", false)]
    public void StarWithSuffixShouldMatch(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a", "**/a", true)]
    [InlineData("a/a", "**/a", true)]
    [InlineData("a/a/a", "**/a", true)]
    [InlineData("/a", "**/a", true)]
    [InlineData("/a/a", "**/a", true)]
    [InlineData("a/", "**/a", false)]
    public void GlobstarSlashAShouldMatchFilesNamedA(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a", "a/**", true)]
    [InlineData("a/", "a/**", true)]
    [InlineData("a/a", "a/**", true)]
    [InlineData("a/a/", "a/**", true)]
    [InlineData("a/a/a", "a/**", true)]
    [InlineData("/a", "a/**", false)]
    public void ASlashGlobstarShouldMatchPathsStartingWithA(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a", "**/a/**", true)]
    [InlineData("a/", "**/a/**", true)]
    [InlineData("a/a", "**/a/**", true)]
    [InlineData("/a", "**/a/**", true)]
    [InlineData("/a/", "**/a/**", true)]
    [InlineData("/a/a", "**/a/**", true)]
    public void GlobstarSlashASlashGlobstarShouldMatchPathsContainingA(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

}
