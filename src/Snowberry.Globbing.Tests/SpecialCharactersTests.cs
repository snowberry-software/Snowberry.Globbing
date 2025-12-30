namespace Snowberry.Globbing.Tests;

/// <summary>
/// Tests for special characters ported from picomatch.
/// </summary>
public class SpecialCharactersTests
{

    // In POSIX mode (Windows=false), backslash is an escape character
    // Pattern "\\" matches literal backslash in input
    [Theory]
    [InlineData("\\", "\\\\", true)]
    [InlineData("a\\b", "a\\\\b", true)]
    [InlineData("ab", "a\\\\b", false)]
    public void BackslashShouldBeEscaped(string input, string pattern, bool expected)
    {
        // In Windows=false mode, backslash is an escape character
        var options = new GlobbingOptions { Windows = false };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("[", "\\[", true)]
    [InlineData("]", "\\]", true)]
    [InlineData("[a]", "\\[a\\]", true)]
    [InlineData("a", "\\[a\\]", false)]
    public void BracketsShouldBeEscaped(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("[", "[[]", true)]
    [InlineData("]", "[]]", true)]
    public void BracketsCanBeInCharacterClass(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("(", "\\(", true)]
    [InlineData(")", "\\)", true)]
    [InlineData("(a)", "\\(a\\)", true)]
    [InlineData("a", "\\(a\\)", false)]
    public void ParenthesesShouldBeEscaped(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("(", "[(]", true)]
    [InlineData(")", "[)]", true)]
    public void ParenthesesCanBeInCharacterClass(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("{", "\\{", true)]
    [InlineData("}", "\\}", true)]
    [InlineData("{a}", "\\{a\\}", true)]
    [InlineData("a", "\\{a\\}", false)]
    public void BracesShouldBeEscaped(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("$", "\\$", true)]
    [InlineData("$a", "\\$a", true)]
    [InlineData("a$", "a\\$", true)]
    public void DollarSignShouldBeEscaped(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("$", "$", true)]
    [InlineData("$a", "$*", true)]
    public void DollarSignCanBeLiteralInSomeContexts(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("^", "\\^", true)]
    [InlineData("^a", "\\^a", true)]
    [InlineData("a^", "a\\^", true)]
    public void CaretShouldBeEscaped(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("^", "^", true)]
    [InlineData("^a", "^*", true)]
    public void CaretCanBeLiteralInSomeContexts(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("+", "\\+", true)]
    [InlineData("+a", "\\+a", true)]
    [InlineData("a+", "a\\+", true)]
    [InlineData("a+b", "a\\+b", true)]
    public void PlusSignShouldBeEscaped(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("|", "\\|", true)]
    [InlineData("|a", "\\|a", true)]
    [InlineData("a|b", "a\\|b", true)]
    public void PipeShouldBeEscaped(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("@", "@", true)]
    [InlineData("@a", "@*", true)]
    [InlineData("a@b", "a@b", true)]
    public void AtSignIsLiteral(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("#", "#", true)]
    [InlineData("#a", "#*", true)]
    [InlineData("a#b", "a#b", true)]
    public void HashIsLiteral(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("%", "%", true)]
    [InlineData("%a", "%*", true)]
    [InlineData("a%b", "a%b", true)]
    public void PercentIsLiteral(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("&", "&", true)]
    [InlineData("&a", "&*", true)]
    [InlineData("a&b", "a&b", true)]
    public void AmpersandIsLiteral(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("=", "=", true)]
    [InlineData("=a", "=*", true)]
    [InlineData("a=b", "a=b", true)]
    public void EqualsIsLiteral(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("-", "-", true)]
    [InlineData("-a", "-*", true)]
    [InlineData("a-b", "a-b", true)]
    public void HyphenIsLiteral(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("-", "[-]", true)]
    [InlineData("a", "[a-c]", true)]
    [InlineData("b", "[a-c]", true)]
    [InlineData("-", "[a-c]", false)]
    public void HyphenInBracketsDependsOnPosition(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("_", "_", true)]
    [InlineData("_a", "_*", true)]
    [InlineData("a_b", "a_b", true)]
    public void UnderscoreIsLiteral(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("~", "~", true)]
    [InlineData("~a", "~*", true)]
    [InlineData("a~b", "a~b", true)]
    public void TildeIsLiteral(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("`", "`", true)]
    [InlineData("`a", "`*", true)]
    [InlineData("a`b", "a`b", true)]
    public void BacktickIsLiteral(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData(":", ":", true)]
    [InlineData(":a", ":*", true)]
    [InlineData("a:b", "a:b", true)]
    public void ColonIsLiteral(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData(";", ";", true)]
    [InlineData(";a", ";*", true)]
    [InlineData("a;b", "a;b", true)]
    public void SemicolonIsLiteral(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("'", "'", true)]
    [InlineData("'a'", "'*'", true)]
    public void SingleQuoteIsLiteral(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    // Double quote handling
    // Simple case: single double-quote matches itself (via direct string comparison)
    [Theory]
    [InlineData("\"", "\"", true)]           // Single double-quote matches itself
    public void DoubleQuoteIsLiteral(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    // In JS picomatch, double quotes make characters literal (glob chars are escaped)
    // Pattern "\"*\"" becomes regex /^(?:\*)$/ matching literal "*"
    [Theory]
    [InlineData("*", "\"*\"", true)]         // "\"*\"" matches literal "*"
    [InlineData("a", "\"*\"", false)]        // "\"*\"" does NOT match "a" 
    public void DoubleQuoteShouldEscapeGlobChars(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData(",", ",", true)]
    [InlineData(",a", ",*", true)]
    [InlineData("a,b", "a,b", true)]
    public void CommaIsLiteral(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("<", "<", true)]
    [InlineData(">", ">", true)]
    [InlineData("<a>", "<*>", true)]
    public void AngleBracketsAreLiteral(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("*", "\\*", true)]
    [InlineData("**", "\\*\\*", true)]
    [InlineData("a", "\\*", false)]
    [InlineData("abc", "\\*", false)]
    public void EscapedStarShouldMatchLiteral(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a*b", "a\\*b", true)]
    [InlineData("aXb", "a\\*b", false)]
    [InlineData("ab", "a\\*b", false)]
    public void EscapedStarMixedWithLiteralsShouldMatch(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("?", "\\?", true)]
    [InlineData("a", "\\?", false)]
    public void EscapedQuestionMarkShouldMatchLiteral(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a?b", "a\\?b", true)]
    [InlineData("aXb", "a\\?b", false)]
    [InlineData("ab", "a\\?b", false)]
    public void EscapedQuestionMarkMixedWithLiteralsShouldMatch(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData(".", ".", true)]
    [InlineData("a", ".", false)]
    public void DotIsLiteralInGlob(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData(".", "\\.", true)]
    [InlineData("a", "\\.", false)]
    public void EscapedDotShouldMatchLiteral(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a.b", "a.b", true)]
    [InlineData("aXb", "a.b", false)]
    public void DotInPatternIsLiteral(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a*b?c", "a\\*b\\?c", true)]
    [InlineData("a[b]c", "a\\[b\\]c", true)]
    [InlineData("a{b}c", "a\\{b\\}c", true)]
    public void MixedEscapedSpecialCharsShouldMatch(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("file.txt", "file.txt", true)]
    [InlineData("file-name.txt", "file-name.txt", true)]
    [InlineData("file_name.txt", "file_name.txt", true)]
    [InlineData("file name.txt", "file name.txt", true)]
    public void CommonFileNameCharactersShouldMatch(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("1", "*/*", false)]
    [InlineData("1/1", "*/*", true)]
    [InlineData("1/2", "*/*", true)]
    [InlineData("1/1/1", "*/*", false)]
    [InlineData("1/1/2", "*/*", false)]
    [InlineData("1", "*/*/1", false)]
    [InlineData("1/1", "*/*/1", false)]
    [InlineData("1/2", "*/*/1", false)]
    [InlineData("1/1/1", "*/*/1", true)]
    [InlineData("1/1/2", "*/*/1", false)]
    [InlineData("1", "*/*/2", false)]
    [InlineData("1/1", "*/*/2", false)]
    [InlineData("1/2", "*/*/2", false)]
    [InlineData("1/1/1", "*/*/2", false)]
    [InlineData("1/1/2", "*/*/2", true)]
    public void ShouldMatchNumbersInTheInputString(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
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
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
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
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
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
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("foo(bar)baz", "foo[bar()]+baz", true)]
    public void ShouldMatchLiteralParensWithBrackets(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("bar/", "**", true)]
    [InlineData("A://", "**", true)]
    [InlineData("B:foo/a/b/c/d", "**", true)]
    [InlineData("C:/Users/", "**", true)]
    public void ShouldMatchWindowsDrivesWithGlobstars(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("my/folder - 1", "*/*", true)]
    [InlineData("my/folder - copy (1)", "*/*", true)]
    [InlineData("my/folder - copy [1]", "*/*", true)]
    [InlineData("my/folder - foo + bar - copy [1]", "*/*", true)]
    [InlineData("my/folder - foo + bar - copy [1]", "*", false)]
    [InlineData("my/folder - 1", "*/*-*", true)]
    [InlineData("my/folder - copy (1)", "*/*-*", true)]
    [InlineData("my/folder - copy [1]", "*/*-*", true)]
    [InlineData("my/folder - foo + bar - copy [1]", "*/*-*", true)]
    [InlineData("my/folder - 1", "*/*1", true)]
    [InlineData("my/folder - copy (1)", "*/*1", false)]
    public void ShouldMatchDashesSurroundedBySpaces(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("foo/bar - 1", "**/*[1]", true)]
    [InlineData("foo/bar - copy (1)", "**/*[1]", false)]
    [InlineData("foo/bar (1)", "**/*[1]", false)]
    [InlineData("foo/bar (4)", "**/*[1]", false)]
    [InlineData("foo/bar (7)", "**/*[1]", false)]
    [InlineData("foo/bar (42)", "**/*[1]", false)]
    [InlineData("foo/bar - copy [1]", "**/*[1]", true)]
    [InlineData("foo/bar - foo + bar - copy [1]", "**/*[1]", true)]
    public void ShouldSupportSquareBracketsInGlobs(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
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
    [InlineData("foo/bar (4)", "**/*\\[*\\]", false)]
    [InlineData("foo/bar (7)", "**/*\\[*\\]", false)]
    [InlineData("foo/bar (42)", "**/*\\[*\\]", false)]
    [InlineData("foo/bar - copy [1]", "**/*\\[*\\]", true)]
    [InlineData("foo/bar - foo + bar - copy [1]", "**/*\\[*\\]", true)]
    public void ShouldMatchEscapedBracketLiterals(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

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

    [Theory]
    [InlineData("a", "(a)*", true)]
    [InlineData("ab", "(a|b)*", true)]
    [InlineData("aa", "(a)*", true)]
    [InlineData("aaab", "(a|b)*", true)]
    [InlineData("aaabbb", "(a|b)*", true)]
    public void ShouldSupportStarsFollowingParens(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a/b", "(a)*", false)]
    [InlineData("a/b", "[a]*", false)]
    [InlineData("a/b", "a*", false)]
    [InlineData("a/b", "(a|b)*", false)]
    public void ShouldNotMatchSlashesWithSingleStars(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
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
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("+", "*", true)]
    [InlineData("/+", "/*", true)]
    [InlineData("+/+", "*/*", true)]
    [InlineData("+/+/", "*/*/", true)]
    [InlineData("/+", "/+", true)]
    [InlineData("/+", "/?", true)]
    [InlineData("+/+", "?/?", true)]
    [InlineData("+/+", "+/+", true)]
    [InlineData("foo+/bar+", "*/*", true)]
    public void ShouldMatchLiteralPlus(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a", "[a]+", true)]
    [InlineData("aa", "[a]+", true)]
    [InlineData("aaa", "[a]+", true)]
    [InlineData("az", "[a-z]+", true)]
    [InlineData("zzz", "[a-z]+", true)]
    public void ShouldSupportPlusSignsFollowingBrackets(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a", "(a)+", true)]
    [InlineData("ab", "(a|b)+", true)]
    [InlineData("aa", "(a)+", true)]
    [InlineData("aaab", "(a|b)+", true)]
    [InlineData("aaabbb", "(a|b)+", true)]
    public void ShouldSupportPlusSignsFollowingParens(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a+b/src/glimini.js", "a+b/src/*.js", true)]
    [InlineData("+b/src/glimini.js", "+b/src/*.js", true)]
    [InlineData("coffee+/src/glimini.js", "coffee+/src/*.js", true)]
    [InlineData("coffee+/src/glimini.js", "coffee+/src/*", true)]
    public void ShouldEscapePlusSignsToMatchStringLiterals(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("$", "!($)", false)]
    [InlineData("$", "!$", false)]
    [InlineData("$$", "!$", true)]
    [InlineData("$$", "!($)", true)]
    [InlineData("$$$", "!($)", true)]
    [InlineData("^", "!($)", true)]
    [InlineData("$", "!($$)", true)]
    [InlineData("$$", "!($$)", false)]
    [InlineData("$$$", "!($$)", true)]
    [InlineData("^", "!($$)", true)]
    [InlineData("$", "!($*)", false)]
    [InlineData("$$", "!($*)", false)]
    [InlineData("$$$", "!($*)", false)]
    [InlineData("^", "!($*)", true)]
    [InlineData("$", "*", true)]
    [InlineData("$$", "*", true)]
    [InlineData("$$$", "*", true)]
    [InlineData("^", "*", true)]
    [InlineData("$", "$*", true)]
    [InlineData("$$", "$*", true)]
    [InlineData("$$$", "$*", true)]
    [InlineData("^", "$*", false)]
    [InlineData("$", "*$*", true)]
    [InlineData("$$", "*$*", true)]
    [InlineData("$$$", "*$*", true)]
    [InlineData("^", "*$*", false)]
    [InlineData("$", "*$", true)]
    [InlineData("$$", "*$", true)]
    [InlineData("$$$", "*$", true)]
    [InlineData("^", "*$", false)]
    [InlineData("$", "?$", false)]
    [InlineData("$$", "?$", true)]
    [InlineData("$$$", "?$", false)]
    [InlineData("^", "?$", false)]
    public void ShouldMatchDollarSignsExtended(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("^", "^", true)]
    [InlineData("^/foo", "^/*", true)]
    [InlineData("foo^", "*^", true)]
    [InlineData("^foo/foo", "^foo/*", true)]
    [InlineData("foo^/foo", "foo^/*", true)]
    [InlineData("^", "!(^)", false)]
    [InlineData("^^", "!(^)", true)]
    [InlineData("^^^", "!(^)", true)]
    [InlineData("&", "!(^)", true)]
    [InlineData("^", "!(^^)", true)]
    [InlineData("^^", "!(^^)", false)]
    [InlineData("^^^", "!(^^)", true)]
    [InlineData("&", "!(^^)", true)]
    [InlineData("^", "!(^*)", false)]
    [InlineData("^^", "!(^*)", false)]
    [InlineData("^^^", "!(^*)", false)]
    [InlineData("&", "!(^*)", true)]
    [InlineData("^", "^*", true)]
    [InlineData("^^", "^*", true)]
    [InlineData("^^^", "^*", true)]
    [InlineData("&", "^*", false)]
    [InlineData("^", "*^*", true)]
    [InlineData("^^", "*^*", true)]
    [InlineData("^^^", "*^*", true)]
    [InlineData("&", "*^*", false)]
    [InlineData("^", "*^", true)]
    [InlineData("^^", "*^", true)]
    [InlineData("^^^", "*^", true)]
    [InlineData("&", "*^", false)]
    [InlineData("^", "?^", false)]
    [InlineData("^^", "?^", true)]
    [InlineData("^^^", "?^", false)]
    [InlineData("&", "?^", false)]
    public void ShouldMatchCaretsExtended(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("my/folder +1", "*/*", true)]
    [InlineData("my/folder -1", "*/*", true)]
    [InlineData("my/folder *1", "*/*", true)]
    [InlineData("my/folder", "*/*", true)]
    [InlineData("my/folder+foo+bar&baz", "*/*", true)]
    [InlineData("my/folder - $1.00", "*/*", true)]
    [InlineData("my/folder - ^1.00", "*/*", true)]
    [InlineData("my/folder - %1.00", "*/*", true)]
    public void ShouldMatchSpecialCharactersInPaths(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("my/folder +1", "*/!(*%)*", true)]
    [InlineData("my/folder -1", "*/!(*%)*", true)]
    [InlineData("my/folder *1", "*/!(*%)*", true)]
    [InlineData("my/folder", "*/!(*%)*", true)]
    [InlineData("my/folder+foo+bar&baz", "*/!(*%)*", true)]
    [InlineData("my/folder - $1.00", "*/!(*%)*", true)]
    [InlineData("my/folder - ^1.00", "*/!(*%)*", true)]
    [InlineData("my/folder - %1.00", "*/!(*%)*", false)]
    public void ShouldMatchSpecialCharactersWithExtglobNegation(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("my/folder +1", "*/*$*", false)]
    [InlineData("my/folder -1", "*/*$*", false)]
    [InlineData("my/folder *1", "*/*$*", false)]
    [InlineData("my/folder", "*/*$*", false)]
    [InlineData("my/folder+foo+bar&baz", "*/*$*", false)]
    [InlineData("my/folder - $1.00", "*/*$*", true)]
    [InlineData("my/folder - ^1.00", "*/*$*", false)]
    [InlineData("my/folder - %1.00", "*/*$*", false)]
    public void ShouldMatchDollarSignInPaths(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("my/folder +1", "*/*^*", false)]
    [InlineData("my/folder -1", "*/*^*", false)]
    [InlineData("my/folder *1", "*/*^*", false)]
    [InlineData("my/folder", "*/*^*", false)]
    [InlineData("my/folder+foo+bar&baz", "*/*^*", false)]
    [InlineData("my/folder - $1.00", "*/*^*", false)]
    [InlineData("my/folder - ^1.00", "*/*^*", true)]
    [InlineData("my/folder - %1.00", "*/*^*", false)]
    public void ShouldMatchCaretInPaths(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("my/folder +1", "*/*&*", false)]
    [InlineData("my/folder -1", "*/*&*", false)]
    [InlineData("my/folder *1", "*/*&*", false)]
    [InlineData("my/folder", "*/*&*", false)]
    [InlineData("my/folder+foo+bar&baz", "*/*&*", true)]
    [InlineData("my/folder - $1.00", "*/*&*", false)]
    [InlineData("my/folder - ^1.00", "*/*&*", false)]
    [InlineData("my/folder - %1.00", "*/*&*", false)]
    public void ShouldMatchAmpersandInPaths(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("my/folder +1", "*/*+*", true)]
    [InlineData("my/folder -1", "*/*+*", false)]
    [InlineData("my/folder *1", "*/*+*", false)]
    [InlineData("my/folder", "*/*+*", false)]
    [InlineData("my/folder+foo+bar&baz", "*/*+*", true)]
    [InlineData("my/folder - $1.00", "*/*+*", false)]
    [InlineData("my/folder - ^1.00", "*/*+*", false)]
    [InlineData("my/folder - %1.00", "*/*+*", false)]
    public void ShouldMatchPlusInPaths(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("my/folder +1", "*/*-*", false)]
    [InlineData("my/folder -1", "*/*-*", true)]
    [InlineData("my/folder *1", "*/*-*", false)]
    [InlineData("my/folder", "*/*-*", false)]
    [InlineData("my/folder+foo+bar&baz", "*/*-*", false)]
    [InlineData("my/folder - $1.00", "*/*-*", true)]
    [InlineData("my/folder - ^1.00", "*/*-*", true)]
    [InlineData("my/folder - %1.00", "*/*-*", true)]
    public void ShouldMatchDashInPaths(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("my/folder +1", "*/*\\**", false)]
    [InlineData("my/folder -1", "*/*\\**", false)]
    [InlineData("my/folder *1", "*/*\\**", true)]
    [InlineData("my/folder", "*/*\\**", false)]
    [InlineData("my/folder+foo+bar&baz", "*/*\\**", false)]
    [InlineData("my/folder - $1.00", "*/*\\**", false)]
    [InlineData("my/folder - ^1.00", "*/*\\**", false)]
    [InlineData("my/folder - %1.00", "*/*\\**", false)]
    public void ShouldMatchEscapedStarInPaths(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

}
