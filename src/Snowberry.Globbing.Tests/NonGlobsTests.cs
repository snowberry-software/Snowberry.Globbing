namespace Snowberry.Globbing.Tests;

/// <summary>
/// Tests for non-glob patterns ported from picomatch.
/// </summary>
public class NonGlobsTests
{
    [Theory]
    [InlineData("abc", "abc\\*", false)]
    [InlineData("abc*", "abc\\*", true)]
    public void ShouldHandleEscapedCharactersAsLiterals(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("...", "..", false)]
    [InlineData("...", "...", true)]
    [InlineData("....", "....", true)]
    public void ShouldMatchLiteralDots(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a/", "a/", true)]
    [InlineData("a/a", "a/a", true)]
    [InlineData("a/a/a", "a/a/a", true)]
    [InlineData("a/a/a/a/a", "a/a/a/a/a", true)]
    public void ShouldMatchNonGlobsExactly(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("/a", "/a", true)]
    [InlineData("/a/", "/a/", true)]
    [InlineData("/a/a", "/a/a", true)]
    [InlineData("/a/a/a/a", "/a/a/a/a", true)]
    public void ShouldMatchNonGlobsWithLeadingSlash(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("aaa\\bbb", "aaa/bbb", true)]
    [InlineData("aaa/bbb", "aaa/bbb", true)]
    public void ShouldMatchWindowsPaths(string input, string pattern, bool expected)
    {
        var options = new GlobOptions { PathStyle = GlobPathStyle.Windows };
        Glob.IsMatch(input, pattern, options).Should().Be(expected);
    }

    [Theory]
    [InlineData("a/a", "a/c", false)]
    [InlineData("a/b", "a/c", false)]
    [InlineData("aaa", "aa", false)]
    public void ShouldNotMatchNonGlobsWhenLiteralDoesNotMatch(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }
}