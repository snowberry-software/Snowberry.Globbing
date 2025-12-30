namespace Snowberry.Globbing.Tests;

/// <summary>
/// Tests for the Test method
/// </summary>
public class TestMethodTests
{
    [Fact]
    public void Test_WithValidInput_ReturnsMatchResult()
    {
        var regex = GlobMatcher.MakeRe("*.js");
        var result = GlobMatcher.Test("test.js", regex);

        Assert.True(result.IsMatch);
        Assert.NotNull(result.Match);
        Assert.Equal("test.js", result.Output);
    }

    [Fact]
    public void Test_WithNonMatchingInput_ReturnsFalse()
    {
        var regex = GlobMatcher.MakeRe("*.js");
        var result = GlobMatcher.Test("test.md", regex);

        Assert.False(result.IsMatch);
    }

    [Fact]
    public void Test_WithEmptyInput_ReturnsFalseResult()
    {
        var regex = GlobMatcher.MakeRe("*.js");
        var result = GlobMatcher.Test("", regex);

        Assert.False(result.IsMatch);
        Assert.Null(result.Match);
        Assert.Equal("", result.Output);
    }

    [Fact]
    public void Test_WithFormatOption_AppliesFormat()
    {
        var regex = GlobMatcher.MakeRe("*.js");
        var options = new GlobbingOptions
        {
            Format = (str) => str.ToUpper()
        };
        var result = GlobMatcher.Test("test.js", regex, options);

        Assert.Equal("TEST.JS", result.Output);
    }

    [Fact]
    public void Test_WithWindowsOption_ConvertsSlashes()
    {
        var regex = GlobMatcher.MakeRe("*.js");
        var options = new GlobbingOptions { Windows = true };
        var result = GlobMatcher.Test("test.js", regex, options);

        Assert.True(result.IsMatch);
    }
}
