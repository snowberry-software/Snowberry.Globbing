namespace Snowberry.Globbing.Tests;

public class GlobParseExceptionTests
{
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

    [Theory]
    [InlineData("a[b", GlobParseError.MissingClosingBracket)]
    [InlineData("a(b", GlobParseError.MissingClosingParenthesis)]
    [InlineData("a{b", GlobParseError.MissingClosingBrace)]
    [InlineData("a)b", GlobParseError.MissingOpeningParenthesis)]
    public void Constructor_WithStrictBrackets_ReportsUnbalancedDelimiter(string pattern, GlobParseError error)
    {
        var e = FluentActions.Invoking(() => new Glob(pattern, new GlobOptions { StrictBrackets = true })).Should().ThrowExactly<GlobParseException>().Which;

        e.Error.Should().Be(error);
        e.Pattern.Should().Be(pattern);
    }

    [Fact]
    public void Constructor_WithTooLongPattern_ReportsPatternTooLong()
    {
        var e = FluentActions.Invoking(() => new Glob("abcdef", new GlobOptions { MaxPatternLength = 5 })).Should().ThrowExactly<GlobParseException>().Which;

        e.Error.Should().Be(GlobParseError.PatternTooLong);
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

    [Fact]
    public void PatternTooLong_ReportsTheMaximumAsOffset()
    {
        var e = FluentActions.Invoking(() => new Glob(new string('a', 11), new GlobOptions { MaxPatternLength = 10 })).Should().ThrowExactly<GlobParseException>().Which;

        e.Error.Should().Be(GlobParseError.PatternTooLong);
        e.Offset.Should().Be(10);
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
}
