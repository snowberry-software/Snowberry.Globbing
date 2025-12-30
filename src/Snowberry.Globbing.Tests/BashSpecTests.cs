namespace Snowberry.Globbing.Tests;

/// <summary>
/// Bash specification tests for dotglob, glob, and globstar patterns.
/// Ported from: https://github.com/micromatch/picomatch/blob/master/test/bash.spec.js
/// </summary>
public class BashSpecTests
{
    private static readonly GlobbingOptions BashOptions = new() { Bash = true };

    [Theory]
    [InlineData("a/b/.x", "**/.x/**", true)]
    [InlineData(".x", "**/.x/**", true)]
    [InlineData(".x/", "**/.x/**", true)]
    [InlineData(".x/a", "**/.x/**", true)]
    [InlineData(".x/a/b", "**/.x/**", true)]
    [InlineData(".x/.x", "**/.x/**", true)]
    [InlineData("a/.x", "**/.x/**", true)]
    [InlineData("a/b/.x/c", "**/.x/**", true)]
    [InlineData("a/b/.x/c/d", "**/.x/**", true)]
    [InlineData("a/b/.x/c/d/e", "**/.x/**", true)]
    [InlineData("a/b/.x/", "**/.x/**", true)]
    [InlineData("a/.x/b", "**/.x/**", true)]
    [InlineData("a/.x/b/.x/c", "**/.x/**", false)]
    [InlineData(".bashrc", "?bashrc", false)]
    [InlineData(".bar.baz/", ".*.*", true)]
    [InlineData(".bar.baz/", ".*.*/", true)]
    [InlineData(".bar.baz", ".*.*", true)]
    public void Dotglob_ShouldMatchCorrectly(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, BashOptions));
    }

    [Theory]
    [InlineData("a/c/b", "a/*/b", true)]
    [InlineData("a/.d/b", "a/*/b", false)]
    [InlineData("a/./b", "a/*/b", false)]
    [InlineData("a/../b", "a/*/b", false)]
    [InlineData("ab", "ab**", true)]
    [InlineData("abcdef", "ab**", true)]
    [InlineData("abef", "ab**", true)]
    [InlineData("abcfef", "ab**", true)]
    [InlineData("ab", "ab***ef", false)]
    [InlineData("abcdef", "ab***ef", true)]
    [InlineData("abef", "ab***ef", true)]
    [InlineData("abcfef", "ab***ef", true)]
    [InlineData("abbc", "ab?bc", false)]
    [InlineData("abc", "ab?bc", false)]
    public void Glob_ShouldMatchCorrectly(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, BashOptions));
    }

    [Theory]
    [InlineData("a.a", "[a-d]*.[a-b]", true)]
    [InlineData("a.b", "[a-d]*.[a-b]", true)]
    [InlineData("c.a", "[a-d]*.[a-b]", true)]
    [InlineData("a.a.a", "[a-d]*.[a-b]", true)]
    [InlineData("a.a.a", "[a-d]*.[a-b]*.[a-b]", true)]
    public void Glob_CharacterRanges_ShouldMatchCorrectly(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, BashOptions));
    }

    [Theory]
    [InlineData("a.a", "*.[a-b]", true)]
    [InlineData("a.b", "*.[a-b]", true)]
    [InlineData("a.a.a", "*.[a-b]", true)]
    [InlineData("c.a", "*.[a-b]", true)]
    [InlineData("d.a.d", "*.[a-b]", false)]
    [InlineData("a.bb", "*.[a-b]", false)]
    [InlineData("a.ccc", "*.[a-b]", false)]
    [InlineData("c.ccc", "*.[a-b]", false)]
    public void Glob_StarWithCharacterRange_ShouldMatchCorrectly(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, BashOptions));
    }

    [Theory]
    [InlineData("a.a", "*.[a-b]*", true)]
    [InlineData("a.b", "*.[a-b]*", true)]
    [InlineData("a.a.a", "*.[a-b]*", true)]
    [InlineData("c.a", "*.[a-b]*", true)]
    [InlineData("d.a.d", "*.[a-b]*", true)]
    [InlineData("d.a.d", "*.[a-b]*.[a-b]*", false)]
    [InlineData("d.a.d", "*.[a-d]*.[a-d]*", true)]
    [InlineData("a.bb", "*.[a-b]*", true)]
    [InlineData("a.ccc", "*.[a-b]*", false)]
    [InlineData("c.ccc", "*.[a-b]*", false)]
    public void Glob_StarWithCharacterRangeStar_ShouldMatchCorrectly(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, BashOptions));
    }

    [Theory]
    [InlineData("a.a", "*[a-b].[a-b]*", true)]
    [InlineData("a.b", "*[a-b].[a-b]*", true)]
    [InlineData("a.a.a", "*[a-b].[a-b]*", true)]
    [InlineData("c.a", "*[a-b].[a-b]*", false)]
    [InlineData("d.a.d", "*[a-b].[a-b]*", false)]
    [InlineData("a.bb", "*[a-b].[a-b]*", true)]
    [InlineData("a.ccc", "*[a-b].[a-b]*", false)]
    [InlineData("c.ccc", "*[a-b].[a-b]*", false)]
    public void Glob_StarCharacterRangePattern_ShouldMatchCorrectly(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, BashOptions));
    }

    [Theory]
    [InlineData("abd", "[a-y]*[^c]", true)]
    [InlineData("abe", "[a-y]*[^c]", true)]
    [InlineData("bb", "[a-y]*[^c]", true)]
    [InlineData("bcd", "[a-y]*[^c]", true)]
    [InlineData("ca", "[a-y]*[^c]", true)]
    [InlineData("cb", "[a-y]*[^c]", true)]
    [InlineData("dd", "[a-y]*[^c]", true)]
    [InlineData("de", "[a-y]*[^c]", true)]
    [InlineData("bdir/", "[a-y]*[^c]", true)]
    [InlineData("abd", "**/*", true)]
    public void Glob_NegatedCharacterClass_ShouldMatchCorrectly(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, BashOptions));
    }

    [Theory]
    [InlineData("a.js", "**/*.js", true)]
    [InlineData("a/a.js", "**/*.js", true)]
    [InlineData("a/a/b.js", "**/*.js", true)]
    [InlineData("a/b/z.js", "a/b/**/*.js", true)]
    [InlineData("a/b/c/z.js", "a/b/**/*.js", true)]
    [InlineData("foo.md", "**/*.md", true)]
    [InlineData("foo/bar.md", "**/*.md", true)]
    public void Globstar_ShouldMatchJsAndMdFiles(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, BashOptions));
    }

    [Theory]
    [InlineData("foo/bar", "foo/**/bar", true)]
    [InlineData("foo/bar", "foo/**bar", true)]
    [InlineData("ab/a/d", "**/*", true)]
    [InlineData("ab/b", "**/*", true)]
    [InlineData("a/b/c/d/a.js", "**/*", true)]
    [InlineData("a/b/c.js", "**/*", true)]
    [InlineData("a/b/c.txt", "**/*", true)]
    [InlineData("a/b/.js/c.txt", "**/*", true)]
    [InlineData("a.js", "**/*", true)]
    [InlineData("za.js", "**/*", true)]
    [InlineData("ab", "**/*", true)]
    [InlineData("a.b", "**/*", true)]
    public void Globstar_ShouldMatchVariousPaths(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, BashOptions));
    }

    [Theory]
    [InlineData("foo/", "foo/**/", true)]
    [InlineData("foo/bar", "foo/**/", false)]
    [InlineData("foo/bazbar", "foo/**/", false)]
    [InlineData("foo/barbar", "foo/**/", false)]
    [InlineData("foo/bar/baz/qux", "foo/**/", false)]
    [InlineData("foo/bar/baz/qux/", "foo/**/", true)]
    public void Globstar_TrailingSlash_ShouldMatchCorrectly(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, BashOptions));
    }

}
