namespace Snowberry.Globbing.Tests;

/// <summary>
/// Tests for parentheses (non-extglobs) ported from picomatch.
/// </summary>
public class ParensTests
{
    [Theory]
    [InlineData("a", "(a)*", true)]
    [InlineData("az", "(a)*", true)]
    [InlineData("zz", "(a)*", false)]
    [InlineData("ab", "(a|b)*", true)]
    [InlineData("abc", "(a|b)*", true)]
    [InlineData("aa", "(a)*", true)]
    [InlineData("aaab", "(a|b)*", true)]
    [InlineData("aaabbb", "(a|b)*", true)]
    public void ShouldSupportStarsFollowingParens(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a/b", "(a)*", false)]
    [InlineData("a/b", "(a|b)*", false)]
    public void ShouldNotMatchSlashesWithSingleStars(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }
}
