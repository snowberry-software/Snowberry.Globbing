namespace Snowberry.Globbing.Tests;

/// <summary>
/// Tests for handling of potential regex exploits (ReDoS) ported from picomatch.
/// </summary>
public class MaliciousTests
{

    [Theory]
    [InlineData("constructor", "constructor", true)]
    [InlineData("__proto__", "__proto__", true)]
    [InlineData("toString", "toString", true)]
    public void ShouldAcceptObjectInstanceProperties(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Fact]
    public void ShouldThrowErrorWhenPatternIsTooLong()
    {
        string longPattern = new('*', Constants.c_MaxLength + 1);
        Assert.Throws<ArgumentException>(() => GlobMatcher.IsMatch("foo", longPattern));
    }

    [Fact]
    public void ShouldAllowMaxLengthToBeCustomized()
    {
        var options = new GlobbingOptions { MaxLength = 499 };
        string longPattern = new string('\\', 500) + "A";
        Assert.Throws<ArgumentException>(() => GlobMatcher.IsMatch("A", longPattern, options));
    }

    [Fact]
    public void ShouldSupportLongEscapeSequences()
    {
        // Long escape sequence within the length limit must compile and run without throwing.
        string escapeSequence = new string('\\', 100) + "A";
        var ex = Record.Exception(() => GlobMatcher.IsMatch("A", "!" + escapeSequence));
        Assert.Null(ex);
    }

    [Fact]
    public void ShouldHandleNegationWithLongEscapeSequences()
    {
        string escapeSequence = new string('\\', 100) + "A";
        // Negation extglob with a long escape sequence must compile and run without throwing.
        var ex = Record.Exception(() => GlobMatcher.IsMatch("A", "!(" + escapeSequence + ")"));
        Assert.Null(ex);
    }

}
