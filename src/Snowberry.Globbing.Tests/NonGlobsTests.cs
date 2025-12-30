namespace Snowberry.Globbing.Tests;

/// <summary>
/// Tests for non-glob patterns ported from picomatch.
/// </summary>
public class NonGlobsTests
{

    [Theory]
    [InlineData("/ab", "/a", false)]
    [InlineData("a/a", "a/b", false)]
    [InlineData("a/a", "a/c", false)]
    [InlineData("a/b", "a/c", false)]
    [InlineData("a/c", "a/b", false)]
    [InlineData("aaa", "aa", false)]
    [InlineData("ab", "/a", false)]
    [InlineData("ab", "a", false)]
    public void ShouldNotMatchNonGlobsWhenLiteralDoesNotMatch(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("/a", "/a", true)]
    [InlineData("/a/", "/a/", true)]
    [InlineData("/a/a", "/a/a", true)]
    [InlineData("/a/a/", "/a/a/", true)]
    [InlineData("/a/a/a", "/a/a/a", true)]
    [InlineData("/a/a/a/", "/a/a/a/", true)]
    [InlineData("/a/a/a/a", "/a/a/a/a", true)]
    [InlineData("/a/a/a/a/a", "/a/a/a/a/a", true)]
    public void ShouldMatchNonGlobsWithLeadingSlash(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a", "a", true)]
    [InlineData("a/", "a/", true)]
    [InlineData("a/a", "a/a", true)]
    [InlineData("a/a/", "a/a/", true)]
    [InlineData("a/a/a", "a/a/a", true)]
    [InlineData("a/a/a/", "a/a/a/", true)]
    [InlineData("a/a/a/a", "a/a/a/a", true)]
    [InlineData("a/a/a/a/a", "a/a/a/a/a", true)]
    public void ShouldMatchNonGlobsExactly(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData(".", ".", true)]
    [InlineData("..", "..", true)]
    [InlineData("...", "..", false)]
    [InlineData("...", "...", true)]
    [InlineData("....", "....", true)]
    [InlineData("....", "...", false)]
    public void ShouldMatchLiteralDots(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("abc", "abc\\*", false)]
    [InlineData("abc*", "abc\\*", true)]
    public void ShouldHandleEscapedCharactersAsLiterals(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("aaa\\bbb", "aaa/bbb", true)]
    [InlineData("aaa/bbb", "aaa/bbb", true)]
    public void ShouldMatchWindowsPaths(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { Windows = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

}
