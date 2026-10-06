namespace Snowberry.Globbing.Tests;

/// <summary>
/// Tests for bracket patterns ported from picomatch.
/// </summary>
public class BracketsTests
{
    // POSIX option needed for [!...] bracket negation
    private static readonly GlobOptions s_PosixOptions = new() { PosixClasses = true };

    [Theory]
    [InlineData("a", "[^abc]", false)]
    [InlineData("d", "[^abc]", true)]
    public void CaretNegatedBrackets_WithPosixClassesEnabled_StillNegate(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern, s_PosixOptions).Should().Be(expected);
    }

    [Theory]
    [InlineData(@"[\]]")]
    [InlineData(@"a[\]b]c")]
    [InlineData(@"[\[\]]")]
    [InlineData(@"[!\]]")]
    [InlineData(@"[\][a]")]
    public void EscapedClosingBracketShouldGenerateParsableRegex(string pattern)
    {
        var options = new GlobOptions { PathStyle = GlobPathStyle.Posix };

        _ = new Regex(new Glob(pattern, options).ToRegexString());
    }

    [Theory]
    [InlineData("d", true)]
    [InlineData("!", true)]
    [InlineData("a", false)]
    [InlineData("c", false)]
    public void ExclamationAfterOpeningBracket_NegatesTheClass(string input, bool expected)
    {
        Glob.IsMatch(input, "[!abc]").Should().Be(expected);
    }

    [Theory]
    [InlineData("a", "[a-c]", true)]
    [InlineData("b", "[a-c]", true)]
    [InlineData("c", "[a-c]", true)]
    [InlineData("d", "[a-c]", false)]
    [InlineData("A", "[a-c]", false)]
    public void ShouldMatchCharacterRanges(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("]", "[]]", true)]
    [InlineData("[", "[[]", true)]
    [InlineData("a", "[]]", false)]
    public void ShouldMatchLiteralBracketsInBrackets(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("-", "[-]", true)]
    [InlineData("a", "[a-]", true)]
    [InlineData("-", "[a-]", true)]
    [InlineData("b", "[a-]", false)]
    public void ShouldMatchLiteralDashInBrackets(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a", "[a-z0-9]", true)]
    [InlineData("z", "[a-z0-9]", true)]
    [InlineData("9", "[a-z0-9]", true)]
    [InlineData("A", "[a-z0-9]", false)]
    [InlineData("!", "[a-z0-9]", false)]
    public void ShouldMatchMultipleRanges(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("abc", "[abc]", false)]
    [InlineData("a", "[abc]", true)]
    [InlineData("b", "[abc]", true)]
    [InlineData("c", "[abc]", true)]
    [InlineData("d", "[abc]", false)]
    public void ShouldMatchSingleCharacterInBrackets(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("foo/bar/", "foo[/]bar[/]", true)]
    [InlineData("foo/bar/baz", "foo[/]bar[/]baz", true)]
    public void ShouldMatchSlashesDefinedInBrackets(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a/b", "[a]*", false)]
    public void ShouldNotMatchSlashesFollowingBrackets(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("]", @"[\]]", true)]
    [InlineData("a", @"[\]]", false)]
    [InlineData("]", @"[\]a]", true)]
    [InlineData("a", @"[\]a]", true)]
    [InlineData("b", @"[\]a]", false)]
    [InlineData("a]c", @"a[\]b]c", true)]
    [InlineData("abc", @"a[\]b]c", true)]
    [InlineData(@"a\c", @"a[\]b]c", false)]
    [InlineData("[", @"[\[\]]", true)]
    [InlineData("]", @"[\[\]]", true)]
    [InlineData("]", @"[\][a]", true)]
    [InlineData("[", @"[\][a]", true)]
    public void ShouldSupportEscapedClosingBracketInBrackets(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern, new GlobOptions { PathStyle = GlobPathStyle.Posix }).Should().Be(expected);
    }

    [Theory]
    [InlineData("a", @"[!\]]", true)]
    [InlineData("]", @"[!\]]", false)]
    public void ShouldSupportEscapedClosingBracketInNegatedBrackets(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern, new GlobOptions { PosixClasses = true, PathStyle = GlobPathStyle.Posix }).Should().Be(expected);
    }

    [Theory]
    [InlineData("a", "[^abc]", false)]
    [InlineData("b", "[^abc]", false)]
    [InlineData("c", "[^abc]", false)]
    [InlineData("d", "[^abc]", true)]
    [InlineData("e", "[^abc]", true)]
    public void ShouldSupportNegatedBrackets(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a", "[a]+", true)]
    [InlineData("aa", "[a]+", true)]
    [InlineData("aaa", "[a]+", true)]
    [InlineData("az", "[a-z]+", true)]
    [InlineData("zzz", "[a-z]+", true)]
    public void ShouldSupportPlusFollowingBrackets(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a", "[a]*", true)]
    [InlineData("aa", "[a]*", true)]
    [InlineData("aaa", "[a]*", true)]
    [InlineData("az", "[a-z]*", true)]
    [InlineData("zzz", "[a-z]*", true)]
    public void ShouldSupportStarsFollowingBrackets(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }
}