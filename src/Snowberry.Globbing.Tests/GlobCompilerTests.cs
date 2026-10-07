using Snowberry.Globbing.Compilation;

namespace Snowberry.Globbing.Tests;

public class GlobCompilerTests
{
    [Theory]
    [InlineData("*.{js,ts}")]
    [InlineData("@(a|b)/*.cs")]
    public void BracesAndAtExtglob_DoNotCapture(string pattern)
    {
        new Glob(pattern, TestOptions.Posix).ToRegex().GetGroupNumbers().Should().ContainSingle();
    }

    [Fact]
    public void CaptureGroups_CaptureStarsLazily()
    {
        var match = new Glob("*.{js,ts}", TestOptions.Posix with { CaptureGroups = true }).ToRegex().Match("a.b.ts");

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
        var glob = new Glob(pattern, TestOptions.Posix with { CaptureGroups = true });

        glob.IsMatch(input).Should().Be(expected);
    }

    [Fact]
    public void ClassWithPosixClass_HasNoLiteralFallback()
    {
        new Glob("[[:space:]]", TestOptions.Posix).ToRegexString().Should().NotContain("\\[");
    }

    [Theory]
    [InlineData("aa.", true)]
    [InlineData("aab", false)]
    public void DotAfterBraceRange_IsLiteral(string input, bool expected)
    {
        Glob.IsMatch(input, "{a..c}a.", TestOptions.Posix).Should().Be(expected);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EscapedSlash_MatchesSeparator(bool bashCompatibility)
    {
        Glob.IsMatch("a/b", "a\\/b", TestOptions.Posix with { BashCompatibility = bashCompatibility }).Should().BeTrue();
    }

    [Theory]
    [InlineData("*.JS", "a.JS", true)]
    [InlineData("*.mp3", "a.mp3", true)]
    [InlineData("*.d_ts", "a.d_ts", true)]
    [InlineData("*.Md", "a.md", false)]
    public void ExtensionShape_MatchesExtensionCaseSensitively(string pattern, string input, bool expected)
    {
        Glob.IsMatch(input, pattern, TestOptions.Posix).Should().Be(expected);
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
        Glob.IsMatch(input, pattern, TestOptions.Posix).Should().Be(expected);
    }

    [Theory]
    [InlineData("ab", false)]
    [InlineData("a/b", true)]
    [InlineData("b", true)]
    public void LeadingGlobstar_WithMatchSubstring_StartsAtSegment(string input, bool expected)
    {
        Glob.IsMatch(input, "**/b", TestOptions.Posix with { MatchSubstring = true }).Should().Be(expected);
    }

    [Theory]
    [InlineData('(')]
    [InlineData('[')]
    [InlineData('{')]
    public void ManyUnclosedOpeners_MatchLiterally(char opener)
    {
        string pattern = "a" + new string(opener, 20000);

        Glob.IsMatch(pattern, pattern, TestOptions.Posix).Should().BeTrue();
        Glob.IsMatch("a", pattern, TestOptions.Posix).Should().BeFalse();
    }

    [Fact]
    public void ManyUnclosedParentheses_DoNotOverflowTheStack()
    {
        string pattern = new('(', 20000);

        Glob.IsMatch(pattern, pattern, TestOptions.Posix).Should().BeTrue();
        Glob.IsMatch("x", pattern, TestOptions.Posix).Should().BeFalse();
    }

    [Theory]
    [InlineData("1", true)]
    [InlineData("3", true)]
    [InlineData("2", false)]
    public void NumericRangeWithStep_SkipsValues(string input, bool expected)
    {
        Glob.IsMatch(input, "{1..5..2}", TestOptions.Posix).Should().Be(expected);
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
        Glob.IsMatch(input, pattern, TestOptions.Posix).Should().Be(expected);
    }

    [Theory]
    [InlineData("1", true)]
    [InlineData("5", true)]
    [InlineData("10", true)]
    [InlineData("0", false)]
    [InlineData("11", false)]
    public void NumericRange_MatchesEveryNumber(string input, bool expected)
    {
        Glob.IsMatch(input, "{1..10}", TestOptions.Posix).Should().Be(expected);
    }

    [Theory]
    [InlineData("01", true)]
    [InlineData("03", true)]
    [InlineData("1", false)]
    public void PaddedNumericRange_KeepsPadding(string input, bool expected)
    {
        Glob.IsMatch(input, "{01..03}", TestOptions.Posix).Should().Be(expected);
    }

    [Theory]
    [InlineData("a|b", "a|b", true)]
    [InlineData("a|b", "a", false)]
    [InlineData("a/b|c", "c", true)]
    public void Pipe_IsLiteralOnlyInPlainPatterns(string pattern, string input, bool expected)
    {
        Glob.IsMatch(input, pattern, TestOptions.Posix).Should().Be(expected);
    }

    [Theory]
    [InlineData("+(a)+", "aa+", true)]
    [InlineData("*(b)+", "bb+", true)]
    [InlineData("@(a)+", "aa", true)]
    public void PlusAfterExtglob_IsLiteralOnlyAfterQuantifiedExtglob(string pattern, string input, bool expected)
    {
        Glob.IsMatch(input, pattern, TestOptions.Posix).Should().Be(expected);
    }

    [Theory]
    [InlineData("a*(b)?", "a")]
    [InlineData("a*(b)?", "abb")]
    [InlineData("?(c)?", "c")]
    public void QuestionMarkAfterQuantifiedExtglob_MatchesLikeAnOptionalQuantifier(string pattern, string input)
    {
        Glob.IsMatch(input, pattern, TestOptions.Posix).Should().BeTrue();
    }

    [Theory]
    [InlineData("ba*", true)]
    [InlineData("bab", false)]
    public void QuotesAfterText_MatchLiterally(string input, bool expected)
    {
        Glob.IsMatch(input, "b\"a*\"", TestOptions.Posix).Should().Be(expected);
    }

    [Theory]
    [InlineData("$$", "$$", true)]
    [InlineData("a^^", "a^^", true)]
    [InlineData("||", "||", true)]
    [InlineData("", "||", false)]
    public void RepeatedSpecialCharacters_MatchLiterally(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern, TestOptions.Posix).Should().Be(expected);
    }

    [Theory]
    [InlineData("a{b", "a{b", true)]
    [InlineData("a{b", "ab", false)]
    [InlineData("{a,b", "{a,b", true)]
    [InlineData("{a,b", "a", false)]
    [InlineData("x/{a,b", "x/{a,b", true)]
    public void UnclosedBrace_MatchesLiterally(string pattern, string input, bool expected)
    {
        Glob.IsMatch(input, pattern, TestOptions.Posix).Should().Be(expected);
    }

    [Theory]
    [InlineData("*.js", true)]
    [InlineData("**/*.cs", true)]
    [InlineData("*", true)]
    [InlineData(".*", false)]
    [InlineData("*-*.js", false)]
    [InlineData("src/*.js", false)]
    [InlineData("!*.js", false)]
    public void Compile_DeferShapeHint_LeavesOutOnlyTheHintOfAShapeThatStartsWithAStar(string pattern, bool deferred)
    {
        var options = TestOptions.Posix;
        var eager = GlobCompiler.Compile(pattern, options);

        var compilation = GlobCompiler.Compile(pattern, options, deferShapeHint: true);

        compilation.HintDeferred.Should().Be(deferred);
        compilation.Source.Should().Be(eager.Source);
        var hint = deferred ? GlobCompiler.FindDeferredHint(pattern, options) : compilation.Hint;
        (hint?.HasPrefix).Should().Be(eager.Hint?.HasPrefix);
        (hint?.HasOrdered).Should().Be(eager.Hint?.HasOrdered);
        (hint?.EstimatedRejectCost).Should().Be(eager.Hint?.EstimatedRejectCost);
        if (deferred)
            compilation.Hint.Should().BeNull();
    }
}