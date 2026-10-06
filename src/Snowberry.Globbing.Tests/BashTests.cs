namespace Snowberry.Globbing.Tests;

/// <summary>
/// Tests ported from the Bash 4.3 spec/unit tests in picomatch.
/// </summary>
public class BashTests
{
    [Theory]
    [InlineData("*", "a*", false)]
    [InlineData("\\*", "a*", false)]
    [InlineData("a/*", "a*", false)]
    [InlineData("b", "a*", false)]
    [InlineData("bdir/", "a*", false)]
    [InlineData("a", "a*", true)]
    [InlineData("ab", "a*", true)]
    public void ShouldHandleRegularGlobbing(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("*", "b*/", false)]
    [InlineData("a", "b*/", false)]
    [InlineData("b", "b*/", false)]
    [InlineData("bdir/", "b*/", true)]
    public void ShouldMatchDirectories(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("^", "\\^", true)]
    [InlineData("*", "\\^", false)]
    [InlineData("a", "\\^", false)]
    public void ShouldUseEscapedCharactersAsLiterals(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("*", "\\*", true)]
    [InlineData("\\*", "\\*", true)]
    [InlineData("a", "\\*", false)]
    public void ShouldMatchLiteralAsteriskWhenEscaped(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("aqa", "*q*", true)]
    [InlineData("*", "*q*", false)]
    [InlineData("a", "*q*", false)]
    public void ShouldMatchStarQStar(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("***", "\"***\"", true)]
    [InlineData("*", "\"***\"", false)]
    public void ShouldWorkForQuotedCharacters(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("abc", "[a-c]b*", true)]
    [InlineData("bb", "[a-c]b*", true)]
    [InlineData("b", "[a-c]b*", false)]
    [InlineData("bcd", "[a-c]b*", false)]
    public void BracketRangeFollowedByLiteralAndStar_MatchesPrefix(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("abd", "a*[^c]", true)]
    [InlineData("abc", "a*[^c]", false)]
    [InlineData("a", "a*[^c]", false)]
    public void ShouldSupportCharacterClassesNegation(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a-b", "a[X-]b", true)]
    [InlineData("aXb", "a[X-]b", true)]
    public void ShouldSupportRangeWithDash(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("abc", "a[b]c", true)]
    [InlineData("abd", "a[b]c", false)]
    public void ShouldMatchSingleCharacterBracket(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("abc", "a[\"b\"]c", true)]
    [InlineData("abd", "a[\"b\"]c", false)]
    public void ShouldMatchQuotedCharacterInBracket(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("abc", "a[b-d]c", true)]
    [InlineData("abd", "a[b-d]c", false)]
    public void ShouldMatchCharacterRange(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("abc", "a?c", true)]
    [InlineData("abd", "a?c", false)]
    [InlineData("abcd", "a?c", false)]
    public void ShouldMatchQuestionMarkPattern(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("man/man1/bash.1", "*/man*/bash.*", true)]
    public void ShouldMatchManPages(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("*", "[^a-c]*", true)]
    [InlineData("a", "[^a-c]*", false)]
    [InlineData("d", "[^a-c]*", true)]
    [InlineData("Beware", "[^a-c]*", true)]
    public void ShouldMatchNegatedCharacterClass(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("]", "]", true)]
    [InlineData("a-b", "a[]-]b", true)]
    [InlineData("a]b", "a[]-]b", true)]
    [InlineData("aab", "a[]-]b", false)]
    [InlineData("a]b", "a[]]b", true)]
    [InlineData("ten", "t[a-g]n", true)]
    [InlineData("ton", "t[^a-g]n", true)]
    public void ShouldSupportBasicWildmatchBracketsFeatures(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("foo/bar", "f[^eiu][^eiu][^eiu][^eiu][^eiu]r", false)]
    [InlineData("foo-bar", "f[^eiu][^eiu][^eiu][^eiu][^eiu]r", true)]
    public void ShouldSupportExtendedSlashMatchingFeatures(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("[ab]", "\\[ab]", true)]
    [InlineData("[ab]", "[\\[:]ab]", true)]
    public void ShouldMatchEscapedCharacters(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("abc", "a**c", true)]
    [InlineData("bbc", "a**c", false)]
    [InlineData("abc", "a***c", true)]
    public void ShouldConsolidateExtraStars(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("bbc", "a*****?c", false)]
    [InlineData("bbc", "?*****??", true)]
    [InlineData("bbc", "?*****?c", true)]
    [InlineData("bbc", "?***?****c", true)]
    [InlineData("bbd", "?***?****c", false)]
    [InlineData("bbc", "*******c", true)]
    public void ShouldMatchComplexStarPatterns(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("abc", "??**********?****?", false)]
    [InlineData("abc", "*c*?**", false)]
    [InlineData("abc", "a********???*******", false)]
    [InlineData("a", "[]", false)]
    [InlineData("[", "[abc", false)]
    public void OverlongWildcardsAndUnclosedBrackets_DoNotMatch(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }
}