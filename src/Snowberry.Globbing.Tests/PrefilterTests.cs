namespace Snowberry.Globbing.Tests;

public class PrefilterTests
{
    [Theory]
    [InlineData("ABC.JS", true)]
    [InlineData("abc.js", true)]
    public void IgnoreCase_MatchesAnyCase(string input, bool expected)
    {
        Glob.IsMatch(input, "*.js", TestOptions.Posix with { IgnoreCase = true }).Should().Be(expected);
    }

    [Theory]
    [InlineData("xyzabc{1..2}def", "1|2", "2def")]
    [InlineData("*xyzabc{1..2}def*", "?", "xyzabdef")]
    public void BraceRangeExpander_Output_IsNotTreatedAsLiteralText(string pattern, string fragment, string input)
    {
        var options = TestOptions.Posix with { BraceRangeExpander = _ => fragment };
        bool expected = new Glob(pattern, options).ToRegex().IsMatch(input);

        expected.Should().BeTrue();
        new Glob(pattern, options).IsMatch(input).Should().BeTrue();
        new Glob([pattern, "**/a.b", "**/c.d", "e*/**"], options).IsMatch(input).Should().BeTrue();
    }

    [Fact]
    public void IgnorePatternWhitespace_IgnoresLiteralSpaces()
    {
        var options = TestOptions.Posix with { RegexOptions = RegexOptions.IgnorePatternWhitespace };

        Glob.IsMatch("foobar", "foo bar*", options).Should().BeTrue();
    }

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
        Glob.IsMatch(input, pattern, TestOptions.Posix).Should().Be(expected);
    }

    [Theory]
    [InlineData(64)]
    [InlineData(65)]
    [InlineData(500)]
    public void LongLiteralText_DecidesWithoutChangingResults(int length)
    {
        string literal = string.Concat(Enumerable.Repeat("ab", length)).Substring(0, length) + "z";
        var glob = new Glob("*/" + literal + "/*", TestOptions.Posix);

        glob.IsMatch("x/" + literal + "/y").Should().BeTrue();
        glob.IsMatch("x/" + literal.Substring(0, length) + "/y").Should().BeFalse();
        glob.IsMatch("x/" + literal.Substring(0, 64) + "/y").Should().BeFalse();
    }

    [Theory]
    [InlineData("a/b/x.js", true)]
    [InlineData("a/b/x.ts", true)]
    [InlineData("a/b/x.md", false)]
    public void MatchFileNameOnly_ChecksEveryPatternAgainstTheFileName(string input, bool expected)
    {
        var glob = new Glob(["*.js", "*.ts"], TestOptions.Posix with { MatchFileNameOnly = true });

        glob.IsMatch(input).Should().Be(expected);
    }

    [Fact]
    public void MultiplePatterns_ReportTheFirstMatchingPattern()
    {
        var glob = new Glob(["**/*.js", "**/*.ts", "!**/node_modules/**"], TestOptions.Posix);

        glob.Match("src/a.ts").Pattern.Should().Be("**/*.ts");
        glob.Match("src/a.md").Pattern.Should().Be("!**/node_modules/**");
        glob.IsMatch("node_modules/a.md").Should().BeFalse();
        glob.IsMatch("node_modules/a.js").Should().BeTrue();
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
        Glob.IsMatch(input, "!**/node_modules/**", TestOptions.Posix).Should().Be(expected);
    }

    [Theory]
    [InlineData("a.md", false)]
    [InlineData("a.txt", true)]
    [InlineData("a.txt\nb.md", false)]
    public void NegatedPattern_WithMultiline_KeepsRegexSemantics(string input, bool expected)
    {
        var options = TestOptions.Posix with { RegexOptions = RegexOptions.Multiline };

        Glob.IsMatch(input, "!*.md", options).Should().Be(expected);
    }

    [Theory]
    [InlineData("b", true)]
    [InlineData("aab", true)]
    [InlineData("ac", false)]
    public void Unescape_RawQuantifierKeepsRegexSemantics(string input, bool expected)
    {
        Glob.IsMatch(input, "a\\*b", TestOptions.Posix with { Unescape = true }).Should().Be(expected);
    }

    [Fact]
    public void WindowsStyle_AcceptsTrailingBackslash()
    {
        Glob.IsMatch("b.js\\", "*.js", TestOptions.Windows).Should().BeTrue();
    }

    [Theory]
    [InlineData("*b*c*d", "bcd", true)]
    [InlineData("*b*c*d", "cbd", false)]
    [InlineData("*ab*b", "abb", true)]
    [InlineData("*ab*b", "ab", false)]
    [InlineData("a*a*a", "aaa", true)]
    [InlineData("a*a*a", "aa", false)]
    [InlineData("a/**/b/**/c/**/d", "a/b/c/d", true)]
    [InlineData("a/**/b/**/c/**/d", "a/b/x/c/y/d", true)]
    [InlineData("a/**/b/**/c/**/d", "a/c/b/d", false)]
    [InlineData("x*.min*.js", "x.min.js/", false)]
    [InlineData("**/x*.min*", "a/x.min/", true)]
    public void LiteralRuns_InOrder_DecideWithoutChangingResults(string pattern, string input, bool expected)
    {
        Glob.IsMatch(input, pattern, TestOptions.Posix).Should().Be(expected);
        Glob.IsMatch(input.AsSpan(), pattern, TestOptions.Posix).Should().Be(expected);
        new Glob(pattern, TestOptions.Posix).ToRegex().IsMatch(input).Should().Be(expected);
    }

    [Fact]
    public void LiteralRuns_MissingMiddleRun_RejectsWithoutRunningTheRegex()
    {
        // Each globstar may stop at any "/b" segment, so the regex alone backtracks polynomially; the run "c" is missing.
        var glob = new Glob("a/**/b/**/b/**/c/**/d", TestOptions.Posix with { MatchTimeout = TimeSpan.FromMilliseconds(200) });
        string input = "a" + string.Concat(Enumerable.Repeat("/b", 2000)) + "/x/d";

        glob.IsMatch(input).Should().BeFalse();
    }
}