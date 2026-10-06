namespace Snowberry.Globbing.Tests;

/// <summary>
/// Tests for handling of potential regex exploits (ReDoS) ported from picomatch.
/// </summary>
public class MaliciousTests
{
    [Theory]
    [InlineData("constructor", "constructor", true)]
    [InlineData("__proto__", "__proto__", true)]
    public void ShouldAcceptObjectInstanceProperties(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Fact]
    public void ShouldThrowErrorWhenPatternIsTooLong()
    {
        string longPattern = new('*', GlobOptions.Default.MaxPatternLength + 1);
        var ex = FluentActions.Invoking(() => Glob.IsMatch("foo", longPattern)).Should().ThrowExactly<GlobParseException>().Which;
        ex.Error.Should().Be(GlobParseError.PatternTooLong);
    }

    [Fact]
    public void ShouldAllowMaxLengthToBeCustomized()
    {
        var options = new GlobOptions { MaxPatternLength = 499 };
        string longPattern = new string('\\', 500) + "A";
        var ex = FluentActions.Invoking(() => Glob.IsMatch("A", longPattern, options)).Should().ThrowExactly<GlobParseException>().Which;
        ex.Error.Should().Be(GlobParseError.PatternTooLong);
    }

    [Fact]
    public void ShouldSupportLongEscapeSequences()
    {
        // Long escape sequence within the length limit must compile and run without throwing.
        string escapeSequence = new string('\\', 100) + "A";
        var ex = Record.Exception(() => Glob.IsMatch("A", "!" + escapeSequence));
        ex.Should().BeNull();
    }

    [Fact]
    public void ShouldHandleNegationWithLongEscapeSequences()
    {
        string escapeSequence = new string('\\', 100) + "A";
        // Negation extglob with a long escape sequence must compile and run without throwing.
        var ex = Record.Exception(() => Glob.IsMatch("A", "!(" + escapeSequence + ")"));
        ex.Should().BeNull();
    }
}