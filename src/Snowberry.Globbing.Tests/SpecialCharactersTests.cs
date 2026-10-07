namespace Snowberry.Globbing.Tests;

/// <summary>
/// Tests for special characters ported from picomatch.
/// </summary>
public class SpecialCharactersTests
{
    [Theory]
    [InlineData("&", "&", true)]
    [InlineData("&a", "&*", true)]
    public void AmpersandIsLiteral(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("<", "<", true)]
    [InlineData(">", ">", true)]
    [InlineData("<a>", "<*>", true)]
    public void AngleBracketsAreLiteral(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("@", "@", true)]
    [InlineData("@a", "@*", true)]
    [InlineData("a@b", "a@b", true)]
    public void AtSignIsLiteral(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("\\", "\\\\", true)]
    [InlineData("a\\b", "a\\\\b", true)]
    [InlineData("ab", "a\\\\b", false)]
    public void BackslashShouldBeEscaped(string input, string pattern, bool expected)
    {
        var options = new GlobOptions { PathStyle = GlobPathStyle.Posix };
        Glob.IsMatch(input, pattern, options).Should().Be(expected);
    }

    [Theory]
    [InlineData("`", "`", true)]
    [InlineData("`a", "`*", true)]
    public void BacktickIsLiteral(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a", "[!a]", false)]
    [InlineData("b", "[!a]", true)]
    public void BangInsideBracketsNegates(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("{a}", "\\{a\\}", true)]
    [InlineData("a", "\\{a\\}", false)]
    public void BracesShouldBeEscaped(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("[", "\\[", true)]
    [InlineData("]", "\\]", true)]
    [InlineData("[a]", "\\[a\\]", true)]
    [InlineData("a", "\\[a\\]", false)]
    public void BracketsShouldBeEscaped(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("^", "^", true)]
    [InlineData("^a", "^*", true)]
    [InlineData("a", "^a", false)]
    public void CaretCanBeLiteralInSomeContexts(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("^", "\\^", true)]
    [InlineData("a^", "a\\^", true)]
    [InlineData("a", "\\^a", false)]
    public void CaretShouldBeEscaped(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData(":", ":", true)]
    [InlineData(":a", ":*", true)]
    public void ColonIsLiteral(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData(",", ",", true)]
    [InlineData(",a", ",*", true)]
    public void CommaIsLiteral(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("file.txt", "file.txt", true)]
    [InlineData("file-name.txt", "file-name.txt", true)]
    [InlineData("file_name.txt", "file_name.txt", true)]
    [InlineData("file name.txt", "file name.txt", true)]
    public void CommonFileNameCharactersShouldMatch(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("$", "$", true)]
    [InlineData("$a", "$*", true)]
    [InlineData("a", "a$", false)]
    public void DollarSignCanBeLiteralInSomeContexts(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("$", "\\$", true)]
    [InlineData("a$", "a\\$", true)]
    [InlineData("a", "a\\$", false)]
    public void DollarSignShouldBeEscaped(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a", ".", false)]
    [InlineData("aXb", "a.b", false)]
    [InlineData("a.b", "a.b", true)]
    public void DotIsLiteralInGlob(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("\"", "\"", true)]
    public void DoubleQuoteIsLiteral(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("*", "\"*\"", true)]
    [InlineData("a", "\"*\"", false)]
    public void DoubleQuoteShouldEscapeGlobChars(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("=", "=", true)]
    [InlineData("=a", "=*", true)]
    public void EqualsIsLiteral(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData(".", "\\.", true)]
    [InlineData("a", "\\.", false)]
    public void EscapedDotShouldMatchLiteral(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a*b", "a\\*b", true)]
    [InlineData("aXb", "a\\*b", false)]
    [InlineData("ab", "a\\*b", false)]
    public void EscapedStarMixedWithLiteralsShouldMatch(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("**", "\\*\\*", true)]
    [InlineData("**", "\\*", false)]
    public void EscapedStarShouldMatchLiteral(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("#", "#", true)]
    [InlineData("#a", "#*", true)]
    public void HashIsLiteral(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("-", "[a-c]", false)]
    [InlineData("-", "[-a]", true)]
    [InlineData("-", "[a-]", true)]
    public void HyphenInBracketsDependsOnPosition(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("-", "-", true)]
    [InlineData("-a", "-*", true)]
    [InlineData("a-b", "a-b", true)]
    public void HyphenIsLiteral(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    // An input equal to the pattern always matches, even when the glob itself would not match it.
    [Theory]
    [InlineData("[!a]", "[!a]", true)]
    [InlineData("[!a]", "[!b]", false)]
    public void InputIdenticalToPattern_Matches(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a*b?c", "a\\*b\\?c", true)]
    [InlineData("a[b]c", "a\\[b\\]c", true)]
    [InlineData("a{b}c", "a\\{b\\}c", true)]
    [InlineData("aXbYc", "a\\*b\\?c", false)]
    public void MixedEscapedSpecialCharsShouldMatch(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("(", "[(]", true)]
    [InlineData(")", "[)]", true)]
    [InlineData("a", "[(]", false)]
    public void ParenthesesCanBeInCharacterClass(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("(", "\\(", true)]
    [InlineData(")", "\\)", true)]
    [InlineData("(a)", "\\(a\\)", true)]
    [InlineData("a", "\\(a\\)", false)]
    public void ParenthesesShouldBeEscaped(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("%", "%", true)]
    [InlineData("%a", "%*", true)]
    public void PercentIsLiteral(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("|", "\\|", true)]
    [InlineData("a|b", "a\\|b", true)]
    [InlineData("a", "a\\|b", false)]
    public void PipeShouldBeEscaped(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("+", "\\+", true)]
    [InlineData("a+b", "a\\+b", true)]
    [InlineData("aab", "a\\+b", false)]
    public void PlusSignShouldBeEscaped(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData(";", ";", true)]
    [InlineData(";a", ";*", true)]
    public void SemicolonIsLiteral(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a+b/src/glimini.js", "a+b/src/*.js", true)]
    [InlineData("+b/src/glimini.js", "+b/src/*.js", true)]
    [InlineData("coffee+/src/glimini.js", "coffee+/src/*", true)]
    public void ShouldEscapePlusSignsToMatchStringLiterals(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("my/folder", "*/*&*", false)]
    [InlineData("my/folder+foo+bar&baz", "*/*&*", true)]
    [InlineData("my/folder - $1.00", "*/*&*", false)]
    public void ShouldMatchAmpersandInPaths(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("my/folder", "*/*^*", false)]
    [InlineData("my/folder - $1.00", "*/*^*", false)]
    [InlineData("my/folder - ^1.00", "*/*^*", true)]
    public void ShouldMatchCaretInPaths(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("^/foo", "^/*", true)]
    [InlineData("foo^", "*^", true)]
    [InlineData("foo^/foo", "foo^/*", true)]
    [InlineData("^", "!(^)", false)]
    [InlineData("^^", "!(^)", true)]
    [InlineData("&", "!(^)", true)]
    [InlineData("^^", "!(^^)", false)]
    [InlineData("^", "!(^*)", false)]
    [InlineData("&", "!(^*)", true)]
    [InlineData("^", "^*", true)]
    [InlineData("&", "^*", false)]
    [InlineData("^", "*^*", true)]
    [InlineData("&", "*^*", false)]
    [InlineData("&", "*^", false)]
    [InlineData("^", "?^", false)]
    [InlineData("^^", "?^", true)]
    [InlineData("&", "?^", false)]
    public void ShouldMatchCaretsExtended(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("my/folder +1", "*/*-*", false)]
    [InlineData("my/folder -1", "*/*-*", true)]
    [InlineData("my/folder", "*/*-*", false)]
    [InlineData("my/folder - $1.00", "*/*-*", true)]
    public void ShouldMatchDashInPaths(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("my/folder - 1", "*/*", true)]
    [InlineData("my/folder - foo + bar - copy [1]", "*/*", true)]
    [InlineData("my/folder - foo + bar - copy [1]", "*", false)]
    [InlineData("my/folder - 1", "*/*-*", true)]
    [InlineData("my/folder - foo + bar - copy [1]", "*/*-*", true)]
    [InlineData("my/folder - 1", "*/*1", true)]
    [InlineData("my/folder - copy (1)", "*/*1", false)]
    public void ShouldMatchDashesSurroundedBySpaces(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("my/folder", "*/*$*", false)]
    [InlineData("my/folder - $1.00", "*/*$*", true)]
    [InlineData("my/folder - ^1.00", "*/*$*", false)]
    public void ShouldMatchDollarSignInPaths(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("$", "!($)", false)]
    [InlineData("$", "!$", false)]
    [InlineData("$$", "!$", true)]
    [InlineData("$$", "!($)", true)]
    [InlineData("^", "!($)", true)]
    [InlineData("$", "!($$)", true)]
    [InlineData("$$", "!($$)", false)]
    [InlineData("$", "!($*)", false)]
    [InlineData("^", "!($*)", true)]
    [InlineData("^", "*", true)]
    [InlineData("$", "$*", true)]
    [InlineData("^", "$*", false)]
    [InlineData("$$", "*$*", true)]
    [InlineData("^", "*$*", false)]
    [InlineData("$$", "*$", true)]
    [InlineData("^", "*$", false)]
    [InlineData("$", "?$", false)]
    [InlineData("$$", "?$", true)]
    [InlineData("$$$", "?$", false)]
    public void ShouldMatchDollarSignsExtended(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a [b]", "a \\[b\\]", true)]
    [InlineData("a [b] c", "a [b] c", true)]
    [InlineData("a [b]", "a \\[b\\]*", true)]
    [InlineData("a [bc]", "a \\[bc\\]*", true)]
    [InlineData("a [b]", "a \\[b\\].*", false)]
    [InlineData("a [b].js", "a \\[b\\].*", true)]
    [InlineData("foo/bar - 1", "**/*\\[*\\]", false)]
    [InlineData("foo/bar - copy (1)", "**/*\\[*\\]", false)]
    [InlineData("foo/bar (1)", "**/*\\[*\\]", false)]
    [InlineData("foo/bar - copy [1]", "**/*\\[*\\]", true)]
    [InlineData("foo/bar - foo + bar - copy [1]", "**/*\\[*\\]", true)]
    public void ShouldMatchEscapedBracketLiterals(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("my/folder +1", "*/*\\**", false)]
    [InlineData("my/folder *1", "*/*\\**", true)]
    [InlineData("my/folder", "*/*\\**", false)]
    public void ShouldMatchEscapedStarInPaths(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("foo(bar)baz", "foo[bar()]+baz", true)]
    public void ShouldMatchLiteralParensWithBrackets(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("my/folder (Work, Accts)", "/*", false)]
    [InlineData("my/folder (Work, Accts)", "*/*", true)]
    [InlineData("my/folder (Work, Accts)", "*/*,*", true)]
    [InlineData("my/folder (Work, Accts)", "*/*(W*, *)*", true)]
    [InlineData("my/folder/(Work, Accts)", "**/*(W*, *)*", true)]
    [InlineData("my/folder/(Work, Accts)", "*/*(W*, *)*", false)]
    [InlineData("foo(bar)baz", "foo*baz", true)]
    public void ShouldMatchLiteralParenthesesInInput(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("+", "*", true)]
    [InlineData("+/+", "*/*", true)]
    [InlineData("/+", "/+", true)]
    [InlineData("/+", "/?", true)]
    [InlineData("+/+", "?/?", true)]
    [InlineData("+/+", "+/+", true)]
    [InlineData("foo+/bar+", "*/*", true)]
    public void ShouldMatchLiteralPlus(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("?", "*", true)]
    [InlineData("/?", "/*", true)]
    [InlineData("?/?", "*/*", true)]
    [InlineData("?/?/", "*/*/", true)]
    [InlineData("/?", "/?", true)]
    [InlineData("?/?", "?/?", true)]
    [InlineData("foo?/bar?", "*/*", true)]
    public void ShouldMatchLiteralQuestionMarkInInput(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("*", "*", true)]
    [InlineData("*/*", "*/*", true)]
    [InlineData("*/*", "?/?", true)]
    [InlineData("*/*/", "*/*/", true)]
    [InlineData("/*", "/*", true)]
    [InlineData("/*", "/?", true)]
    [InlineData("foo*/bar*", "*/*", true)]
    public void ShouldMatchLiteralStarInInput(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("1", "*/*", false)]
    [InlineData("1/1", "*/*", true)]
    [InlineData("1/1/1", "*/*", false)]
    [InlineData("1/1/2", "*/*", false)]
    [InlineData("1", "*/*/1", false)]
    [InlineData("1/1", "*/*/1", false)]
    [InlineData("1/2", "*/*/1", false)]
    [InlineData("1/1/1", "*/*/1", true)]
    [InlineData("1/1/2", "*/*/1", false)]
    [InlineData("1/1/1", "*/*/2", false)]
    [InlineData("1/1/2", "*/*/2", true)]
    public void ShouldMatchNumbersInTheInputString(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("my/folder +1", "*/*+*", true)]
    [InlineData("my/folder -1", "*/*+*", false)]
    [InlineData("my/folder+foo+bar&baz", "*/*+*", true)]
    [InlineData("my/folder - ^1.00", "*/*+*", false)]
    public void ShouldMatchPlusInPaths(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("my/folder +1", "*/!(*%)*", true)]
    [InlineData("my/folder", "*/!(*%)*", true)]
    [InlineData("my/folder - $1.00", "*/!(*%)*", true)]
    [InlineData("my/folder - %1.00", "*/!(*%)*", false)]
    public void ShouldMatchSpecialCharactersWithExtglobNegation(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("bar/", "**", true)]
    [InlineData("A://", "**", true)]
    [InlineData("B:foo/a/b/c/d", "**", true)]
    [InlineData("C:/Users/", "**", true)]
    public void ShouldMatchWindowsDrivesWithGlobstars(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData(".a", "(a)*", false)]
    [InlineData(".a", "*[a]*", false)]
    [InlineData(".a", "*[a]", false)]
    [InlineData(".a", "*a*", false)]
    [InlineData(".a", "*a", false)]
    [InlineData(".a", "*(a|b)", false)]
    public void ShouldNotMatchDotsWithStarsByDefault(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a/b", "a*", false)]
    public void ShouldNotMatchSlashesWithSingleStars(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a", "(a)+", true)]
    [InlineData("ab", "(a|b)+", true)]
    [InlineData("aaabbb", "(a|b)+", true)]
    public void ShouldSupportPlusSignsFollowingParens(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("foo/bar - 1", "**/*[1]", true)]
    [InlineData("foo/bar - copy (1)", "**/*[1]", false)]
    [InlineData("foo/bar (1)", "**/*[1]", false)]
    [InlineData("foo/bar - copy [1]", "**/*[1]", true)]
    [InlineData("foo/bar - foo + bar - copy [1]", "**/*[1]", true)]
    public void ShouldSupportSquareBracketsInGlobs(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("'", "'", true)]
    [InlineData("'a'", "'*'", true)]
    public void SingleQuoteIsLiteral(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("foo/bar", "foo[/]bar", true)]
    [InlineData("foobar", "foo[/]bar", false)]
    public void SlashInBracketsIsRequired(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("~", "~", true)]
    [InlineData("~a", "~*", true)]
    public void TildeIsLiteral(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("_", "_", true)]
    [InlineData("_a", "_*", true)]
    public void UnderscoreIsLiteral(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("_", @"\_")]
    [InlineData("é", @"\é")]
    [InlineData("_", @"[\_]")]
    [InlineData("é", @"[\é]")]
    [InlineData("a_bc", @"a\_b*")]
    public void EscapedNonAsciiOrUnderscoreLetter_MatchesItself(string input, string pattern)
    {
        var glob = new Glob(pattern);

        glob.IsMatch(input).Should().BeTrue();
        glob.ToRegexString().Should().NotContain(@"\_").And.NotContain(@"\é");
    }

    [Theory]
    [InlineData(0x200C)]
    [InlineData(0x200D)]
    [InlineData(0x00E9)]
    public void EscapedNonAsciiCharacter_CompilesToAValidRegex(int code)
    {
        string c = ((char)code).ToString();
        var glob = new Glob(@"\" + c);

        glob.IsMatch(c).Should().BeTrue();
        glob.ToRegex().IsMatch(c).Should().BeTrue();
    }
}