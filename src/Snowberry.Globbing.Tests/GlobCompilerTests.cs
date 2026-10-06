namespace Snowberry.Globbing.Tests;

public class GlobCompilerTests
{
    private static readonly GlobOptions s_Posix = new() { PathStyle = GlobPathStyle.Posix };

    [Fact]
    public void DeeplyNestedGroups_ThrowNestingTooDeep()
    {
        string pattern = new string('(', 300) + "a" + new string(')', 300);

        var e = FluentActions.Invoking(() => new Glob(pattern, s_Posix)).Should().ThrowExactly<GlobParseException>().Which;

        e.Error.Should().Be(GlobParseError.NestingTooDeep);
    }

    [Theory]
    [InlineData('(')]
    [InlineData('[')]
    [InlineData('{')]
    public void ManyUnclosedOpeners_MatchLiterally(char opener)
    {
        string pattern = "a" + new string(opener, 20000);

        Glob.IsMatch(pattern, pattern, s_Posix).Should().BeTrue();
        Glob.IsMatch("a", pattern, s_Posix).Should().BeFalse();
    }

    [Theory]
    [InlineData("1", true)]
    [InlineData("5", true)]
    [InlineData("10", true)]
    [InlineData("0", false)]
    [InlineData("11", false)]
    public void NumericRange_MatchesEveryNumber(string input, bool expected)
    {
        Glob.IsMatch(input, "{1..10}", s_Posix).Should().Be(expected);
    }

    [Theory]
    [InlineData("01", true)]
    [InlineData("03", true)]
    [InlineData("1", false)]
    public void PaddedNumericRange_KeepsPadding(string input, bool expected)
    {
        Glob.IsMatch(input, "{01..03}", s_Posix).Should().Be(expected);
    }

    [Theory]
    [InlineData("1", true)]
    [InlineData("3", true)]
    [InlineData("2", false)]
    public void NumericRangeWithStep_SkipsValues(string input, bool expected)
    {
        Glob.IsMatch(input, "{1..5..2}", s_Posix).Should().Be(expected);
    }

    [Theory]
    [InlineData("aa.", true)]
    [InlineData("aab", false)]
    public void DotAfterBraceRange_IsLiteral(string input, bool expected)
    {
        Glob.IsMatch(input, "{a..c}a.", s_Posix).Should().Be(expected);
    }

    [Theory]
    [InlineData("ba*", true)]
    [InlineData("bab", false)]
    public void QuotesAfterText_MatchLiterally(string input, bool expected)
    {
        Glob.IsMatch(input, "b\"a*\"", s_Posix).Should().Be(expected);
    }

    [Theory]
    [InlineData("$$", "$$", true)]
    [InlineData("a^^", "a^^", true)]
    [InlineData("||", "||", true)]
    [InlineData("", "||", false)]
    public void RepeatedSpecialCharacters_MatchLiterally(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern, s_Posix).Should().Be(expected);
    }

    [Theory]
    [InlineData("+(a)+", "aa+", true)]
    [InlineData("*(b)+", "bb+", true)]
    [InlineData("@(a)+", "aa", true)]
    public void PlusAfterQuantifiedExtglob_IsLiteral(string pattern, string input, bool expected)
    {
        Glob.IsMatch(input, pattern, s_Posix).Should().Be(expected);
    }

    [Theory]
    [InlineData("a*(b)?", "a")]
    [InlineData("a*(b)?", "abb")]
    [InlineData("?(c)?", "c")]
    public void QuestionMarkAfterExtglob_IsLazyQuantifier(string pattern, string input)
    {
        Glob.IsMatch(input, pattern, s_Posix).Should().BeTrue();
    }

    [Theory]
    [InlineData("a|b", "a|b", true)]
    [InlineData("a|b", "a", false)]
    [InlineData("a/b|c", "c", true)]
    public void Pipe_IsLiteralOnlyInPlainPatterns(string pattern, string input, bool expected)
    {
        Glob.IsMatch(input, pattern, s_Posix).Should().Be(expected);
    }

    [Fact]
    public void ClassWithPosixClass_HasNoLiteralFallback()
    {
        new Glob("[[:space:]]", s_Posix).ToRegexString().Should().NotContain("\\[");
    }

    [Theory]
    [InlineData("x{**/b,c}", "xb", false)]
    [InlineData("x{**/b,c}", "x/b", true)]
    [InlineData("x{**/b,c}", "xc", true)]
    [InlineData("**/b", "b", true)]
    [InlineData("**/b", "/b", true)]
    [InlineData("**/b", "a/.c/b", false)]
    public void LeadingGlobstar_SpansWholeSegments(string pattern, string input, bool expected)
    {
        Glob.IsMatch(input, pattern, s_Posix).Should().Be(expected);
    }

    [Theory]
    [InlineData("ab", false)]
    [InlineData("a/b", true)]
    [InlineData("b", true)]
    public void LeadingGlobstar_WithMatchSubstring_StartsAtSegment(string input, bool expected)
    {
        Glob.IsMatch(input, "**/b", s_Posix with { MatchSubstring = true }).Should().Be(expected);
    }

    [Theory]
    [InlineData("*.{js,ts}")]
    [InlineData("@(a|b)/*.cs")]
    public void BracesAndAtExtglob_DoNotCapture(string pattern)
    {
        new Glob(pattern, s_Posix).ToRegex().GetGroupNumbers().Should().ContainSingle();
    }

    [Fact]
    public void CaptureGroups_CaptureStarsLazily()
    {
        var match = new Glob("*.{js,ts}", s_Posix with { CaptureGroups = true }).ToRegex().Match("a.b.ts");

        match.Groups[1].Value.Should().Be("a.b");
        match.Groups[2].Value.Should().Be("ts");
    }

    [Theory]
    [InlineData("!(*.md)", "a.js", true)]
    [InlineData("!(*.md)", "a.md", false)]
    [InlineData("*.!(js)", "a.ts", true)]
    [InlineData("*.!(js)", "a.js", false)]
    public void CaptureGroups_WithNegatedExtglob_CompilesAndMatches(string pattern, string input, bool expected)
    {
        var glob = new Glob(pattern, s_Posix with { CaptureGroups = true });

        glob.IsMatch(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("a{b", "a{b", true)]
    [InlineData("a{b", "ab", false)]
    [InlineData("{a,b", "{a,b", true)]
    [InlineData("{a,b", "a", false)]
    [InlineData("x/{a,b", "x/{a,b", true)]
    public void UnclosedBrace_MatchesLiterally(string pattern, string input, bool expected)
    {
        Glob.IsMatch(input, pattern, s_Posix).Should().Be(expected);
    }

    [Fact]
    public void ManyUnclosedParentheses_DoNotOverflowTheStack()
    {
        string pattern = new('(', 20000);

        Glob.IsMatch(pattern, pattern, s_Posix).Should().BeTrue();
        Glob.IsMatch("x", pattern, s_Posix).Should().BeFalse();
    }

    [Theory]
    [InlineData("*.JS", "a.JS", true)]
    [InlineData("*.mp3", "a.mp3", true)]
    [InlineData("*.d_ts", "a.d_ts", true)]
    [InlineData("*.Md", "a.md", false)]
    public void ExtensionShape_MatchesExtensionCaseSensitively(string pattern, string input, bool expected)
    {
        Glob.IsMatch(input, pattern, s_Posix).Should().Be(expected);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EscapedSlash_MatchesSeparator(bool bashCompatibility)
    {
        Glob.IsMatch("a/b", "a\\/b", s_Posix with { BashCompatibility = bashCompatibility }).Should().BeTrue();
    }

    [Theory]
    [InlineData("{-9223372036854775808..-9223372036854775806}", "-9223372036854775807", true)]
    [InlineData("{9223372036854775806..9223372036854775807}", "9223372036854775807", true)]
    [InlineData("{9223372036854775806..9223372036854775807}", "9223372036854775805", false)]
    [InlineData("{1..2..-9223372036854775808}", "1", true)]
    [InlineData("{1..2..-9223372036854775808}", "2", false)]
    [InlineData("{-9223372036854775808..1}", "0", false)]
    [InlineData("{-9223372036854775808..1}", "{-9223372036854775808..1}", true)]
    [InlineData("{1..99999999999999999999}", "5", false)]
    [InlineData("{1..99999999999999999999}", "{1..99999999999999999999}", true)]
    public void NumericRange_AtTheLimitsOfLong_MatchesWithoutOverflow(string pattern, string input, bool expected)
    {
        Glob.IsMatch(input, pattern, s_Posix).Should().Be(expected);
    }
}
