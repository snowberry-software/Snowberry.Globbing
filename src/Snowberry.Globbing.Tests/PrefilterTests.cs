namespace Snowberry.Globbing.Tests;

public class PrefilterTests
{
    private static readonly GlobOptions s_Posix = new() { PathStyle = GlobPathStyle.Posix };

    [Theory]
    [InlineData("**/*.js", "a/b.js", true)]
    [InlineData("**/*.js", "a/b.js/", true)]
    [InlineData("**/*.js", "a/b.ts", false)]
    [InlineData("src/**/*.js", "src/a/b.js", true)]
    [InlineData("src/**/*.js", "lib/a/b.js", false)]
    [InlineData("*abc*", "xabcx", true)]
    [InlineData("*abc*", "xabx", false)]
    public void LiteralText_DecidesWithoutChangingResults(string pattern, string input, bool expected)
    {
        Glob.IsMatch(input, pattern, s_Posix).Should().Be(expected);
    }

    [Fact]
    public void WindowsStyle_AcceptsTrailingBackslash()
    {
        Glob.IsMatch("b.js\\", "*.js", new GlobOptions { PathStyle = GlobPathStyle.Windows }).Should().BeTrue();
    }

    [Theory]
    [InlineData("b", true)]
    [InlineData("aab", true)]
    [InlineData("ac", false)]
    public void Unescape_RawQuantifierKeepsRegexSemantics(string input, bool expected)
    {
        Glob.IsMatch(input, "a\\*b", s_Posix with { Unescape = true }).Should().Be(expected);
    }

    [Fact]
    public void IgnorePatternWhitespace_IgnoresLiteralSpaces()
    {
        var options = s_Posix with { RegexOptions = System.Text.RegularExpressions.RegexOptions.IgnorePatternWhitespace };

        Glob.IsMatch("foobar", "foo bar*", options).Should().BeTrue();
    }

    [Theory]
    [InlineData("ABC.JS", true)]
    [InlineData("abc.js", true)]
    public void IgnoreCase_MatchesAnyCase(string input, bool expected)
    {
        Glob.IsMatch(input, "*.js", s_Posix with { IgnoreCase = true }).Should().Be(expected);
    }

    [Theory]
    [InlineData("src/a.js", true)]
    [InlineData("node_modules/a.js", false)]
    [InlineData("src/node_modules/a.js", false)]
    [InlineData("a\nb", false)]
    [InlineData("a\r.js", false)]
    [InlineData("a\u2028b", false)]
    public void NegatedPattern_MatchesInputsTheBodyDoesNot(string input, bool expected)
    {
        Glob.IsMatch(input, "!**/node_modules/**", s_Posix).Should().Be(expected);
    }

    [Theory]
    [InlineData("a.md", false)]
    [InlineData("a.txt", true)]
    [InlineData("a.txt\nb.md", false)]
    public void NegatedPattern_WithMultiline_KeepsRegexSemantics(string input, bool expected)
    {
        var options = s_Posix with { RegexOptions = System.Text.RegularExpressions.RegexOptions.Multiline };

        Glob.IsMatch(input, "!*.md", options).Should().Be(expected);
    }

    [Theory]
    [InlineData("a/b/x.js", true)]
    [InlineData("a/b/x.ts", true)]
    [InlineData("a/b/x.md", false)]
    public void MatchFileNameOnly_ChecksEveryPatternAgainstTheFileName(string input, bool expected)
    {
        var glob = new Glob(["*.js", "*.ts"], s_Posix with { MatchFileNameOnly = true });

        glob.IsMatch(input).Should().Be(expected);
    }

    [Fact]
    public void MultiplePatterns_ReportTheFirstMatchingPattern()
    {
        var glob = new Glob(["**/*.js", "**/*.ts", "!**/node_modules/**"], s_Posix);

        glob.Match("src/a.ts").Pattern.Should().Be("**/*.ts");
        glob.Match("src/a.md").Pattern.Should().Be("!**/node_modules/**");
        glob.IsMatch("node_modules/a.md").Should().BeFalse();
        glob.IsMatch("node_modules/a.js").Should().BeTrue();
    }
}
