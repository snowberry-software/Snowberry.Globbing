namespace Snowberry.Globbing.Tests;

/// <summary>
/// Tests for POSIX slash handling ported from picomatch.
/// </summary>
public class SlashesPosixTests
{
    [Theory]
    [InlineData("/ef", "/*", true)]
    [InlineData("/foo/bar.txt", "/foo/*", true)]
    [InlineData("/foo/bar.txt", "/foo/**", true)]
    [InlineData("/foo/bar.txt", "/foo/**/**/*.txt", true)]
    [InlineData("/foo/bar.txt", "/foo/**/**/bar.txt", true)]
    [InlineData("/foo/bar.txt", "/foo/**/*.txt", true)]
    [InlineData("/foo/bar.txt", "/foo/**/bar.txt", true)]
    [InlineData("/foo/bar.txt", "/foo/*/bar.txt", false)]
    [InlineData("/foo/bar/baz.txt", "/foo/*", false)]
    [InlineData("/foo/bar/baz.txt", "/foo/**", true)]
    public void ShouldHandleLeadingSlashes(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("https://foo.com/bar/baz/app.min.js", "https://foo.com/*", false)]
    [InlineData("https://foo.com/bar/baz/app.min.js", "https://foo.com/**", true)]
    [InlineData("https://foo.com/bar/baz/app.min.js", "https://foo.com/**/app.min.js", true)]
    [InlineData("https://foo.com/bar/baz/app.min.js", "https://foo.com/*/*/app.min.js", true)]
    [InlineData("https://foo.com/bar/baz/app.min.js", "https://foo.com/*/app.min.js", false)]
    public void ShouldMatchDoubleSlashesInUrls(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData(".md", "\\*", false)]
    [InlineData("*.md", "\\*", false)]
    [InlineData("*.md", "\\*.md", true)]
    [InlineData(".md", "\\*.md", false)]
    [InlineData("**.md", "\\*.md", false)]
    [InlineData("*.md", "\\**.md", true)]
    [InlineData("**.md", "\\**.md", true)]
    [InlineData("**a.md", "\\**.md", true)]
    [InlineData(".md", "\\**.md", false)]
    public void ShouldMatchEscapedStar(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a/a", "**", true)]
    [InlineData("a/x/y/z", "**", true)]
    [InlineData("a/x/y/z/a", "**/a", true)]
    [InlineData("a/a/a", "**/a", true)]
    [InlineData("/a", "**/a", true)]
    [InlineData("/a/a", "**/a", true)]
    [InlineData("a/a", "a/**", true)]
    [InlineData("a/x/y/z", "a/**", true)]
    [InlineData("a/a/", "a/**", true)]
    [InlineData("/a", "a/**", false)]
    [InlineData("a/a", "a/**/*", true)]
    [InlineData("a/x/y/z", "a/**/*", true)]
    [InlineData("a/a", "**/a/**", true)]
    [InlineData("/a", "**/a/**", true)]
    [InlineData("/a/", "**/a/**", true)]
    [InlineData("/a/a", "**/a/**", true)]
    [InlineData("a/b.txt", "a/**/*.txt", true)]
    [InlineData("a/b/foo/bar/baz.qux", "a/b/**/bar/**/*.*", true)]
    [InlineData("a/b/bar/baz.qux", "a/b/**/bar/**/*.*", true)]
    [InlineData("foo.txt", "*/*.txt", false)]
    [InlineData("foo.txt", "**/foo.txt", true)]
    [InlineData("foo/bar.txt", "**/*.txt", true)]
    [InlineData("foo/bar/baz.txt", "**/*.txt", true)]
    public void ShouldMatchGlobstarPatterns(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a/b", "a/b", true)]
    [InlineData("a/c", "a/b", false)]
    [InlineData("b/b", "a/b", false)]
    [InlineData("a/b", "(a/b)", true)]
    [InlineData("a/a", "(a/b)", false)]
    [InlineData("b/c", "(a/b)", false)]
    public void ShouldMatchLiteralString(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a/b", "*", false)]
    [InlineData("x/y", "*", false)]
    [InlineData("aa", "*", true)]
    [InlineData("a/c", "*/*", true)]
    [InlineData("a/a/b", "*/*/*", true)]
    [InlineData("a/x", "a/*", true)]
    [InlineData("x/y", "a/*", false)]
    [InlineData("a/b/a", "a/*/a", true)]
    [InlineData("a/b/b", "a/*/b", true)]
    [InlineData("aa", "a*", true)]
    [InlineData("a/a", "a*", false)]
    [InlineData("b", "*b", true)]
    [InlineData("ab", "*b", true)]
    [InlineData("a", "*b", false)]
    public void ShouldMatchSingleStarPatterns(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("https://foo.com/bar/baz/app.min.js", "https://foo.com/**", false)]
    [InlineData("https://foo.com/bar/baz/app.min.js", "https://foo.com/**/app.min.js", false)]
    public void ShouldNotMatchUrlsWhenGlobstarDisabled(string input, string pattern, bool expected)
    {
        var options = new GlobOptions { Globstar = false };
        Glob.IsMatch(input, pattern, options).Should().Be(expected);
    }

    [Theory]
    [InlineData("a/a/", "*/", false)]
    [InlineData("a", "*/*/", false)]
    [InlineData("a/", "*/*/", false)]
    [InlineData("a/a", "*/*/", false)]
    [InlineData("a/a/", "*/*/", true)]
    [InlineData("x/y/", "*/*/", true)]
    public void ShouldRespectTrailingSlashesOnPatterns(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a/a", "!a/b", true)]
    [InlineData("a/b", "!a/b", false)]
    [InlineData("b/a", "!a/b", true)]
    [InlineData("a/b", "!a/(b)", false)]
    [InlineData("a/c", "!a/(b)", true)]
    [InlineData("a/b", "!(a/b)", false)]
    [InlineData("a/c", "!(a/b)", true)]
    public void ShouldSupportNegationPatterns(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a/a", "a/(a|c)", true)]
    [InlineData("a/b", "a/(a|c)", false)]
    [InlineData("a/c", "a/(a|c)", true)]
    [InlineData("a/b", "a/(a|b|c)", true)]
    public void ShouldSupportRegexLogicalOr(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a/a", "a/[b-c]", false)]
    [InlineData("a/b", "a/[b-c]", true)]
    [InlineData("a/c", "a/[b-c]", true)]
    [InlineData("a/x", "a/[a-z]", true)]
    [InlineData("a/x/y", "a/[a-z]", false)]
    public void ShouldSupportRegexRanges(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }
}