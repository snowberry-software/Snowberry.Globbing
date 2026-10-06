namespace Snowberry.Globbing.Tests;

/// <summary>
/// Advanced edge case tests
/// </summary>
public class EdgeCaseTests
{
    [Theory]
    [InlineData("...", "*")]
    public void EdgeCase_DotFiles_HandleCorrectly(string input, string pattern)
    {
        bool withDot = Glob.IsMatch(input, pattern, new GlobOptions { MatchDotFiles = true });
        bool withoutDot = Glob.IsMatch(input, pattern, new GlobOptions { MatchDotFiles = false });

        withDot.Should().BeTrue();
        withoutDot.Should().BeFalse();
    }

    [Theory]
    [InlineData("a\nb", "*", true)]
    [InlineData("a\rb", "a?b", true)]
    [InlineData("a\u2028b", "a*b", true)]
    [InlineData("a\n", "a", false)]
    [InlineData("a\n", "a*", true)]
    [InlineData("a\nb", "!(x)", true)]
    // Like picomatch, a negated pattern only matches inputs without line terminators.
    [InlineData("a\nb", "!x", false)]
    [InlineData("a\rb", "!x", false)]
    [InlineData("a\u2028b", "!x", false)]
    [InlineData("ab", "!x", true)]
    public void EdgeCase_LineTerminatorsInInput(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern, new GlobOptions { PathStyle = GlobPathStyle.Posix }).Should().Be(expected);
    }

    [Theory]
    [InlineData("a/b/c.js", "a/**/c.js", true)]
    public void EdgeCase_DefaultOptions_MatchExpectations(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("test.js", "test.js")]
    [InlineData("path/to/file.js", "path/to/file.js")]
    public void EdgeCase_ExactMatch_Works(string input, string pattern)
    {
        Glob.IsMatch(input, pattern).Should().BeTrue();
    }

    [Theory]
    [InlineData("/", "test.js", false)]
    [InlineData("/test.js", "/test.js", true)]
    [InlineData("./test.js", "test.js", true)]
    public void EdgeCase_LeadingSlash_HandlesCorrectly(string pattern, string input, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a{b,c}d", "abd", true)]
    [InlineData("a{b,c}d", "acd", true)]
    [InlineData("a{b,c}d", "ad", false)]
    [InlineData("a{b,c}d", "abcd", false)]
    public void EdgeCase_BraceExpansion_NoSpaces(string pattern, string input, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("[^a-z]", "a", false)]
    [InlineData("[^a-z]", "1", true)]
    public void EdgeCase_NegatedBrackets_WorkCorrectly(string pattern, string input, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("**", ".", false)]
    [InlineData("**", "..", false)]
    [InlineData("**", "file", true)]
    public void EdgeCase_GlobstarWithDots_HandlesDotFiles(string pattern, string input, bool expected)
    {
        Glob.IsMatch(input, pattern, new GlobOptions { MatchDotFiles = false }).Should().Be(expected);
    }

    [Fact]
    public void EdgeCase_VeryLongPath_HandlesCorrectly()
    {
        string longPath = string.Join("/", Enumerable.Repeat("dir", 100)) + "/file.js";
        var matcher = new Glob("**/*.js");

        matcher.IsMatch(longPath).Should().BeTrue();
    }

    [Theory]
    [InlineData("*.js", "test.js", true)]
    [InlineData("*..js", "test..js", true)]
    [InlineData("*.*.js", "test.min.js", true)]
    public void EdgeCase_MultipleDots_HandlesCorrectly(string pattern, string input, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Fact]
    public void EdgeCase_UnicodeCharacters_HandlesCorrectly()
    {
        var matcher = new Glob("*.js");

        matcher.IsMatch("テスト.js").Should().BeTrue();
        matcher.IsMatch("файл.js").Should().BeTrue();
        matcher.IsMatch("文件.js").Should().BeTrue();
    }

    [Theory]
    [InlineData("a/**", "a", true)]
    [InlineData("a/**", "a/b", true)]
    [InlineData("a/**", "a/b/c", true)]
    public void EdgeCase_GlobstarAtEnd_MatchesDirectoryAndContents(string pattern, string input, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("**/a", "a", true)]
    [InlineData("**/a", "b/a", true)]
    [InlineData("**/a", "b/c/a", true)]
    [InlineData("**/a", "b", false)]
    public void EdgeCase_GlobstarAtStart_MatchesAnywhere(string pattern, string input, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a*b*c", "abc", true)]
    [InlineData("a*b*c", "aXbYc", true)]
    [InlineData("a*b*c", "acb", false)]
    public void EdgeCase_MultipleWildcardsInSequence(string pattern, string input, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("?", "a", true)]
    [InlineData("?", "", false)]
    [InlineData("?", "ab", false)]
    public void EdgeCase_SingleQuestionMark(string pattern, string input, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("???", "abcd", false)]
    public void EdgeCase_MultipleQuestionMarks(string pattern, string input, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }
}