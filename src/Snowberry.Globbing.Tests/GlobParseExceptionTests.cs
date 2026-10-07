namespace Snowberry.Globbing.Tests;

public class GlobParseExceptionTests
{
#if NET7_0_OR_GREATER
    private const RegexOptions c_NonBacktracking = RegexOptions.NonBacktracking;
#else
    // The value of RegexOptions.NonBacktracking, which .NET Framework rejects as an invalid option.
    private const RegexOptions c_NonBacktracking = (RegexOptions)1024;
#endif

    private static readonly GlobOptions s_NonBacktracking = new() { RegexOptions = c_NonBacktracking };

    [Theory]
    [InlineData("*.js")]
    [InlineData("abc")]
    [InlineData("!*.md")]
    [InlineData("src/**/*.cs")]
    [InlineData("!(a|b)")]
    public void Constructor_WithNonBacktracking_ThrowsInvalidPattern(string pattern)
    {
        var e = FluentActions.Invoking(() => new Glob(pattern, s_NonBacktracking)).Should().ThrowExactly<GlobParseException>().Which;

        e.Error.Should().Be(GlobParseError.InvalidPattern);
        e.Pattern.Should().Be(pattern);
        e.ParamName.Should().Be("pattern");
#if NET7_0_OR_GREATER
        e.InnerException.Should().BeOfType<NotSupportedException>();
#else
        e.InnerException.Should().BeOfType<ArgumentOutOfRangeException>();
#endif
    }

    [Theory]
    [InlineData("!{1..3}", false)]
    [InlineData("!a\\)\\)\\[", true)]
    public void Constructor_WithNegatedBodyThatIsNotValidRegexOnItsOwn_ThrowsInvalidPattern(string pattern, bool unescape)
    {
        var options = new GlobOptions { BraceRangeExpander = unescape ? null : _ => "))[", Unescape = unescape };

        var e = FluentActions.Invoking(() => new Glob(pattern, options)).Should().ThrowExactly<GlobParseException>().Which;

        e.Error.Should().Be(GlobParseError.InvalidPattern);
        e.Pattern.Should().Be(pattern);
        Glob.TryCreate(pattern, options, out _).Should().BeFalse();
    }

    // With IgnorePatternWhitespace, "#" starts a comment that can swallow the rest of a combined regex.
    [Fact]
    public void Constructor_WithIgnorePatternWhitespaceComment_ThrowsInvalidPattern()
    {
        string commented = "#(" + (char)10 + ")#";
        var options = new GlobOptions { RegexOptions = RegexOptions.IgnorePatternWhitespace };

        var negated = FluentActions.Invoking(() => new Glob("!" + commented, options)).Should().ThrowExactly<GlobParseException>().Which;
        var list = FluentActions.Invoking(() => new Glob([commented, "b"], options)).Should().ThrowExactly<GlobParseException>().Which;

        negated.Error.Should().Be(GlobParseError.InvalidPattern);
        list.Error.Should().Be(GlobParseError.InvalidPattern);
        list.ParamName.Should().Be("patterns");
        Glob.TryCreate("!" + commented, options, out _).Should().BeFalse();
    }

    [Fact]
    public void IsMatch_StaticWithNonBacktracking_ThrowsInvalidPattern()
    {
        FluentActions.Invoking(() => Glob.IsMatch("a.js", "*.js", s_NonBacktracking))
            .Should().ThrowExactly<GlobParseException>()
            .Which.Error.Should().Be(GlobParseError.InvalidPattern);
    }

    [Fact]
    public void TryCreate_WithNonBacktracking_ReturnsFalseWithInvalidPattern()
    {
        Glob.TryCreate("*.js", s_NonBacktracking, out var glob, out var error).Should().BeFalse();
        Glob.TryCreate("*.js", s_NonBacktracking, out _).Should().BeFalse();

        glob.Should().BeNull();
        error!.Error.Should().Be(GlobParseError.InvalidPattern);
        error.Pattern.Should().Be("*.js");
    }

#if NET7_0_OR_GREATER
    [Fact]
    public void Constructor_WithNonBacktracking_IgnorePatternThatUsesLookarounds_ThrowsForIgnorePatterns()
    {
        // A plain substring pattern compiles without lookarounds; the ignore pattern does not.
        var options = s_NonBacktracking with { MatchSubstring = true, IgnorePatterns = ["**"] };

        var e = FluentActions.Invoking(() => new Glob("abc", options)).Should().ThrowExactly<GlobParseException>().Which;

        e.Error.Should().Be(GlobParseError.InvalidPattern);
        e.ParamName.Should().Be(nameof(GlobOptions.IgnorePatterns));
        e.Pattern.Should().Be("**");
    }

    [Fact]
    public void Constructor_WithNonBacktracking_PlainSubstringPattern_Compiles()
    {
        var glob = new Glob("abc", s_NonBacktracking with { MatchSubstring = true });

        glob.IsMatch("xabcx").Should().BeTrue();
        glob.IsMatch("xabx").Should().BeFalse();
    }
#endif

    [Theory]
    [InlineData("[z-a]")]
    [InlineData("a/[z-a]/*.js")]
    public void Constructor_WithPatternThatIsNotValidRegex_ThrowsInvalidPattern(string pattern)
    {
        var e = FluentActions.Invoking(() => new Glob(pattern)).Should().ThrowExactly<GlobParseException>().Which;

        e.Error.Should().Be(GlobParseError.InvalidPattern);
        e.Pattern.Should().Be(pattern);
        e.InnerException.Should().NotBeNull();
    }

    [Fact]
    public void ParamName_NamesTheArgumentThatSuppliedThePattern()
    {
        var single = FluentActions.Invoking(() => new Glob("[z-a]")).Should().ThrowExactly<GlobParseException>().Which;
        var list = FluentActions.Invoking(() => new Glob(["*.js", "[z-a]"])).Should().ThrowExactly<GlobParseException>().Which;
        var ignore = FluentActions.Invoking(() => new Glob("*.js", new GlobOptions { IgnorePatterns = ["[z-a]"] })).Should().ThrowExactly<GlobParseException>().Which;

        single.ParamName.Should().Be("pattern");
        list.ParamName.Should().Be("patterns");
        ignore.ParamName.Should().Be(nameof(GlobOptions.IgnorePatterns));
        ignore.Pattern.Should().Be("[z-a]");
        list.Error.Should().Be(GlobParseError.InvalidPattern);
        list.InnerException.Should().NotBeNull();
    }

    [Fact]
    public void PatternTooLong_ReportsTheMaximumAsOffset()
    {
        var e = FluentActions.Invoking(() => new Glob(new string('a', 11), new GlobOptions { MaxPatternLength = 10 })).Should().ThrowExactly<GlobParseException>().Which;

        e.Error.Should().Be(GlobParseError.PatternTooLong);
        e.Offset.Should().Be(10);
    }

    [Theory]
    [InlineData("a[b", GlobParseError.MissingClosingBracket, 1)]
    [InlineData("ab]", GlobParseError.MissingOpeningBracket, 2)]
    [InlineData("a(b", GlobParseError.MissingClosingParenthesis, 1)]
    [InlineData("a)b", GlobParseError.MissingOpeningParenthesis, 1)]
    [InlineData("a{b", GlobParseError.MissingClosingBrace, 1)]
    public void StrictBrackets_ReportsTheUnbalancedDelimiterOffset(string pattern, GlobParseError error, int offset)
    {
        var e = FluentActions.Invoking(() => new Glob(pattern, new GlobOptions { StrictBrackets = true })).Should().ThrowExactly<GlobParseException>().Which;

        e.Error.Should().Be(error);
        e.Offset.Should().Be(offset);
        e.Pattern.Should().Be(pattern);
    }
}