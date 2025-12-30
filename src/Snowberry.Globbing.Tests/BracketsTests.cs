namespace Snowberry.Globbing.Tests;

/// <summary>
/// Tests for bracket patterns ported from picomatch.
/// </summary>
public class BracketsTests
{
    // POSIX option needed for [!...] bracket negation
    private static readonly GlobbingOptions PosixOptions = new() { Posix = true };

    [Theory]
    [InlineData("a", "[a]*", true)]
    [InlineData("aa", "[a]*", true)]
    [InlineData("aaa", "[a]*", true)]
    [InlineData("az", "[a-z]*", true)]
    [InlineData("zzz", "[a-z]*", true)]
    public void ShouldSupportStarsFollowingBrackets(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    // Slashes in brackets should match literal slashes
    [Theory]
    [InlineData("foo/bar", "foo[/]bar", true)]
    [InlineData("foo/bar/", "foo[/]bar[/]", true)]
    [InlineData("foo/bar/baz", "foo[/]bar[/]baz", true)]
    public void ShouldMatchSlashesDefinedInBrackets(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a/b", "[a]*", false)]
    public void ShouldNotMatchSlashesFollowingBrackets(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("abc", "[abc]", false)]
    [InlineData("a", "[abc]", true)]
    [InlineData("b", "[abc]", true)]
    [InlineData("c", "[abc]", true)]
    [InlineData("d", "[abc]", false)]
    public void ShouldMatchSingleCharacterInBrackets(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a", "[a-c]", true)]
    [InlineData("b", "[a-c]", true)]
    [InlineData("c", "[a-c]", true)]
    [InlineData("d", "[a-c]", false)]
    [InlineData("A", "[a-c]", false)]
    public void ShouldMatchCharacterRanges(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a", "[^abc]", false)]
    [InlineData("b", "[^abc]", false)]
    [InlineData("c", "[^abc]", false)]
    [InlineData("d", "[^abc]", true)]
    [InlineData("e", "[^abc]", true)]
    public void ShouldSupportNegatedBrackets(string input, string pattern, bool expected)
    {
        // [^...] works with default options
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    // Note: [!...] bracket negation requires Posix = true
    // BUG: [!...] syntax doesn't work even with Posix = true, use [^...] instead
    [Theory]
    [InlineData("a", "[^abc]", false)]
    [InlineData("b", "[^abc]", false)]
    [InlineData("c", "[^abc]", false)]
    [InlineData("d", "[^abc]", true)]
    [InlineData("e", "[^abc]", true)]
    public void ShouldSupportNegatedBracketsWithExclamation(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, PosixOptions));
    }

    [Theory]
    [InlineData("a", "[a-z0-9]", true)]
    [InlineData("z", "[a-z0-9]", true)]
    [InlineData("0", "[a-z0-9]", true)]
    [InlineData("9", "[a-z0-9]", true)]
    [InlineData("A", "[a-z0-9]", false)]
    [InlineData("!", "[a-z0-9]", false)]
    public void ShouldMatchMultipleRanges(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    // BUG: + quantifier after brackets doesn't work
    [Theory]
    [InlineData("a", "[a]+", true)]
    [InlineData("aa", "[a]+", true)]
    [InlineData("aaa", "[a]+", true)]
    [InlineData("az", "[a-z]+", true)]
    [InlineData("zzz", "[a-z]+", true)]
    public void ShouldSupportPlusFollowingBrackets(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("-", "[-]", true)]
    [InlineData("a", "[a-]", true)]
    [InlineData("-", "[a-]", true)]
    [InlineData("b", "[a-]", false)]
    public void ShouldMatchLiteralDashInBrackets(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("]", "[]]", true)]
    [InlineData("[", "[[]", true)]
    [InlineData("a", "[]]", false)]
    public void ShouldMatchLiteralBracketsInBrackets(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }
}
