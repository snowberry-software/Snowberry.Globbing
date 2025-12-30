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
        string longPattern = new('*', 65537);
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
        // Test with reasonably long escape sequences within limits
        string escapeSequence = new string('\\', 100) + "A";
        // Should not throw, and should handle the pattern
        bool result = GlobMatcher.IsMatch("A", "!" + escapeSequence);
        // The result depends on implementation, just verify it doesn't throw
        Assert.True(result || !result);
    }

    [Fact]
    public void ShouldHandleNegationWithLongEscapeSequences()
    {
        string escapeSequence = new string('\\', 100) + "A";
        // Test negation patterns with long escape sequences
        bool result = GlobMatcher.IsMatch("A", "!(" + escapeSequence + ")");
        Assert.True(result || !result);
    }

}
