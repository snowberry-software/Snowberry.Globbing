namespace Snowberry.Globbing.Tests;

/// <summary>
/// Tests for parentheses (non-extglobs) ported from picomatch.
/// </summary>
public class ParensTests
{
    [Theory]
    [InlineData("a/b", "(a)*", false)]
    [InlineData("a/b", "(a|b)*", false)]
    public void ShouldNotMatchSlashesWithSingleStars(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a", "(a)*", true)]
    [InlineData("zz", "(a)*", false)]
    [InlineData("ab", "(a|b)*", true)]
    [InlineData("aaabbb", "(a|b)*", true)]
    public void ShouldSupportStarsFollowingParens(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData(@"(?\!a|b)", "b", true)]
    [InlineData(@"(?\!a|b)", "?!a", true)]
    [InlineData(@"(?\!a|b)", "a", false)]
    [InlineData(@"@(?\:|x)", "x", false)]
    public void EscapedGroupPrefix_IsLiteral(string pattern, string input, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    // picomatch treats these as invalid and never matches them.
    [Theory]
    [InlineData("(*+)")]
    [InlineData("(**+)")]
    [InlineData("@(a*+)")]
    public void PlusAfterStarInParens_IsInvalid(string pattern)
    {
        FluentActions.Invoking(() => new Glob(pattern)).Should().ThrowExactly<GlobParseException>()
            .Which.Error.Should().Be(GlobParseError.InvalidPattern);
    }
}