namespace Snowberry.Globbing.Tests;

/// <summary>
/// Bash specification tests for dotglob, glob, and globstar patterns.
/// Ported from: https://github.com/micromatch/picomatch/blob/master/test/bash.spec.js
/// </summary>
public class BashSpecTests
{
    private static readonly GlobOptions s_BashOptions = new() { BashCompatibility = true };

    [Theory]
    [InlineData("a/b/.x", "**/.x/**", true)]
    [InlineData(".x", "**/.x/**", true)]
    [InlineData(".x/", "**/.x/**", true)]
    [InlineData(".x/a/b", "**/.x/**", true)]
    [InlineData(".x/.x", "**/.x/**", true)]
    [InlineData("a/b/.x/c/d/e", "**/.x/**", true)]
    [InlineData("a/.x/b/.x/c", "**/.x/**", false)]
    [InlineData(".bashrc", "?bashrc", false)]
    [InlineData(".bar.baz/", ".*.*", true)]
    [InlineData(".bar.baz/", ".*.*/", true)]
    [InlineData(".bar.baz", ".*.*", true)]
    public void Dotglob_ShouldMatchCorrectly(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern, s_BashOptions).Should().Be(expected);
    }

    [Theory]
    [InlineData("a.a", "[a-d]*.[a-b]", true)]
    [InlineData("a.a.a", "[a-d]*.[a-b]", true)]
    [InlineData("a.a.a", "[a-d]*.[a-b]*.[a-b]", true)]
    public void Glob_CharacterRanges_ShouldMatchCorrectly(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern, s_BashOptions).Should().Be(expected);
    }

    [Theory]
    [InlineData("abd", "[a-y]*[^c]", true)]
    [InlineData("ca", "[a-y]*[^c]", true)]
    [InlineData("bdir/", "[a-y]*[^c]", true)]
    [InlineData("abd", "**/*", true)]
    public void Glob_NegatedCharacterClass_ShouldMatchCorrectly(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern, s_BashOptions).Should().Be(expected);
    }

    [Theory]
    [InlineData("a/c/b", "a/*/b", true)]
    [InlineData("a/.d/b", "a/*/b", false)]
    [InlineData("a/./b", "a/*/b", false)]
    [InlineData("a/../b", "a/*/b", false)]
    [InlineData("ab", "ab**", true)]
    [InlineData("abcdef", "ab**", true)]
    [InlineData("ab", "ab***ef", false)]
    [InlineData("abcdef", "ab***ef", true)]
    [InlineData("abbc", "ab?bc", false)]
    [InlineData("abc", "ab?bc", false)]
    public void Glob_ShouldMatchCorrectly(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern, s_BashOptions).Should().Be(expected);
    }

    [Theory]
    [InlineData("a.a", "*[a-b].[a-b]*", true)]
    [InlineData("c.a", "*[a-b].[a-b]*", false)]
    [InlineData("a.bb", "*[a-b].[a-b]*", true)]
    [InlineData("a.ccc", "*[a-b].[a-b]*", false)]
    public void Glob_StarCharacterRangePattern_ShouldMatchCorrectly(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern, s_BashOptions).Should().Be(expected);
    }

    [Theory]
    [InlineData("a.a", "*.[a-b]*", true)]
    [InlineData("d.a.d", "*.[a-b]*", true)]
    [InlineData("d.a.d", "*.[a-b]*.[a-b]*", false)]
    [InlineData("d.a.d", "*.[a-d]*.[a-d]*", true)]
    [InlineData("a.bb", "*.[a-b]*", true)]
    [InlineData("a.ccc", "*.[a-b]*", false)]
    public void Glob_StarWithCharacterRangeStar_ShouldMatchCorrectly(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern, s_BashOptions).Should().Be(expected);
    }

    [Theory]
    [InlineData("a.a", "*.[a-b]", true)]
    [InlineData("a.a.a", "*.[a-b]", true)]
    [InlineData("d.a.d", "*.[a-b]", false)]
    [InlineData("a.bb", "*.[a-b]", false)]
    public void Glob_StarWithCharacterRange_ShouldMatchCorrectly(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern, s_BashOptions).Should().Be(expected);
    }

    [Theory]
    [InlineData("a.js", "**/*.js", true)]
    [InlineData("a/a/b.js", "**/*.js", true)]
    [InlineData("a/b/z.js", "a/b/**/*.js", true)]
    [InlineData("foo/bar.md", "**/*.md", true)]
    public void Globstar_ShouldMatchJsAndMdFiles(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern, s_BashOptions).Should().Be(expected);
    }

    [Theory]
    [InlineData("foo/bar", "foo/**/bar", true)]
    [InlineData("foo/bar", "foo/**bar", true)]
    [InlineData("ab/a/d", "**/*", true)]
    [InlineData("a/b/.js/c.txt", "**/*", true)]
    [InlineData("a.js", "**/*", true)]
    public void Globstar_ShouldMatchVariousPaths(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern, s_BashOptions).Should().Be(expected);
    }

    [Theory]
    [InlineData("foo/", "foo/**/", true)]
    [InlineData("foo/bar", "foo/**/", false)]
    [InlineData("foo/bar/baz/qux", "foo/**/", false)]
    [InlineData("foo/bar/baz/qux/", "foo/**/", true)]
    public void Globstar_TrailingSlash_ShouldMatchCorrectly(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern, s_BashOptions).Should().Be(expected);
    }
}