namespace Snowberry.Globbing.Tests;

/// <summary>
/// Tests for invalid (exclusive) dots like .. and . in paths.
/// These should NOT be matched by glob patterns.
/// Ported from: https://github.com/micromatch/picomatch/blob/master/test/dots-invalid.js
/// </summary>
public class DotsInvalidTests
{

    [Theory]
    [InlineData("../abc", "*/*")]
    [InlineData("../abc", "*/abc")]
    [InlineData("../abc", "*/abc/*")]
    [InlineData("../abc", ".*/*")]
    [InlineData("../abc", ".*/abc")]
    [InlineData("../abc", "*./*")]
    [InlineData("../abc", "*./abc")]
    public void DoubleDots_Leading_WithStar_ShouldNotMatch(string input, string pattern)
    {
        Assert.False(GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("../abc", "**")]
    [InlineData("../abc", "**/**")]
    [InlineData("../abc", "**/**/**")]
    [InlineData("../abc", "**/abc")]
    [InlineData("../abc", "**/abc/**")]
    [InlineData("../abc", "abc/**")]
    [InlineData("../abc", "abc/**/**")]
    [InlineData("../abc", "abc/**/**/**")]
    [InlineData("../abc", "**/**/abc/**")]
    [InlineData("../abc", "**/**/abc/**/**")]
    public void DoubleDots_Leading_WithGlobstar_ShouldNotMatch(string input, string pattern)
    {
        Assert.False(GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("../abc", ".**")]
    [InlineData("../abc", ".**/**")]
    [InlineData("../abc", ".**/abc")]
    [InlineData("../abc", "*.*/**")]
    [InlineData("../abc", "*.*/abc")]
    [InlineData("../abc", "**./**")]
    [InlineData("../abc", "**./abc")]
    public void DoubleDots_Leading_WithDotGlobstar_ShouldNotMatch(string input, string pattern)
    {
        Assert.False(GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("/../abc", "*/*")]
    [InlineData("/../abc", "/*/*")]
    [InlineData("/../abc", "*/*/*")]
    [InlineData("abc/../abc", "*/*/*")]
    [InlineData("abc/../abc/abc", "*/*/*/*")]
    public void DoubleDots_Nested_WithStar_ShouldNotMatch(string input, string pattern)
    {
        Assert.False(GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("/../abc", "**")]
    [InlineData("/../abc", "**/**")]
    [InlineData("/../abc", "/**/**")]
    [InlineData("/../abc", "**/**/**")]
    [InlineData("abc/../abc", "**/**/**")]
    [InlineData("abc/../abc/abc", "**/**/**/**")]
    public void DoubleDots_Nested_WithGlobstar_ShouldNotMatch(string input, string pattern)
    {
        Assert.False(GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("abc/..", "*/*")]
    [InlineData("abc/..", "*/*/")]
    [InlineData("abc/..", "*/*/*")]
    [InlineData("abc/../", "*/*")]
    [InlineData("abc/../", "*/*/")]
    [InlineData("abc/../", "*/*/*")]
    [InlineData("abc/../abc/../", "*/*/*/*")]
    [InlineData("abc/../abc/../", "*/*/*/*/")]
    [InlineData("abc/../abc/abc/../", "*/*/*/*/*")]
    public void DoubleDots_Trailing_WithStar_ShouldNotMatch(string input, string pattern)
    {
        Assert.False(GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("abc/..", "**/**")]
    [InlineData("abc/..", "**/**/")]
    [InlineData("abc/..", "**/**/**")]
    [InlineData("abc/../", "**/**")]
    [InlineData("abc/../", "**/**/")]
    [InlineData("abc/../", "**/**/**")]
    [InlineData("abc/../abc/../", "**/**/**/**")]
    [InlineData("abc/../abc/../", "**/**/**/**/")]
    [InlineData("abc/../abc/abc/../", "**/**/**/**/**")]
    public void DoubleDots_Trailing_WithGlobstar_ShouldNotMatch(string input, string pattern)
    {
        Assert.False(GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("../abc", "*/*")]
    [InlineData("../abc", "*/abc")]
    [InlineData("../abc", "*/abc/*")]
    [InlineData("../abc", "**")]
    [InlineData("../abc", "**/**")]
    [InlineData("../abc", "**/**/**")]
    [InlineData("../abc", "**/abc")]
    [InlineData("../abc", "**/abc/**")]
    public void DoubleDots_Leading_WithDotOption_ShouldNotMatch(string input, string pattern)
    {
        var options = new GlobbingOptions { Dot = true };
        Assert.False(GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("/../abc", "*/*")]
    [InlineData("/../abc", "/*/*")]
    [InlineData("/../abc", "*/*/*")]
    [InlineData("abc/../abc", "*/*/*")]
    [InlineData("abc/../abc/abc", "*/*/*/*")]
    [InlineData("/../abc", "**")]
    [InlineData("/../abc", "**/**")]
    [InlineData("abc/../abc", "**/**/**")]
    public void DoubleDots_Nested_WithDotOption_ShouldNotMatch(string input, string pattern)
    {
        var options = new GlobbingOptions { Dot = true };
        Assert.False(GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("abc/..", "*/*")]
    [InlineData("abc/..", "*/*/")]
    [InlineData("abc/..", "*/*/*")]
    [InlineData("abc/../", "*/*")]
    [InlineData("abc/../", "*/*/")]
    [InlineData("abc/..", "**/**")]
    [InlineData("abc/../", "**/**")]
    public void DoubleDots_Trailing_WithDotOption_ShouldNotMatch(string input, string pattern)
    {
        var options = new GlobbingOptions { Dot = true };
        Assert.False(GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("../abc", "*/*")]
    [InlineData("../abc", "*/abc")]
    [InlineData("../abc", "**")]
    [InlineData("../abc", "**/**")]
    [InlineData("../abc", "**/abc")]
    public void DoubleDots_Leading_WithStrictSlashesOption_ShouldNotMatch(string input, string pattern)
    {
        var options = new GlobbingOptions { StrictSlashes = true };
        Assert.False(GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("/../abc", "*/*")]
    [InlineData("/../abc", "/*/*")]
    [InlineData("abc/../abc", "*/*/*")]
    [InlineData("/../abc", "**")]
    [InlineData("/../abc", "**/**")]
    [InlineData("abc/../abc", "**/**/**")]
    public void DoubleDots_Nested_WithStrictSlashesOption_ShouldNotMatch(string input, string pattern)
    {
        var options = new GlobbingOptions { StrictSlashes = true };
        Assert.False(GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("abc/..", "*/*")]
    [InlineData("abc/..", "*/*/")]
    [InlineData("abc/../", "*/*")]
    [InlineData("abc/..", "**/**")]
    [InlineData("abc/../", "**/**")]
    public void DoubleDots_Trailing_WithStrictSlashesOption_ShouldNotMatch(string input, string pattern)
    {
        var options = new GlobbingOptions { StrictSlashes = true };
        Assert.False(GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("../abc", "*/*")]
    [InlineData("../abc", "*/abc")]
    [InlineData("../abc", "**")]
    [InlineData("../abc", "**/**")]
    [InlineData("../abc", "**/abc")]
    public void DoubleDots_Leading_WithBothOptions_ShouldNotMatch(string input, string pattern)
    {
        var options = new GlobbingOptions { Dot = true, StrictSlashes = true };
        Assert.False(GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("/../abc", "*/*")]
    [InlineData("abc/../abc", "*/*/*")]
    [InlineData("/../abc", "**")]
    [InlineData("/../abc", "**/**")]
    [InlineData("abc/../abc", "**/**/**")]
    public void DoubleDots_Nested_WithBothOptions_ShouldNotMatch(string input, string pattern)
    {
        var options = new GlobbingOptions { Dot = true, StrictSlashes = true };
        Assert.False(GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("abc/..", "*/*")]
    [InlineData("abc/../", "*/*")]
    [InlineData("abc/..", "**/**")]
    [InlineData("abc/../", "**/**")]
    public void DoubleDots_Trailing_WithBothOptions_ShouldNotMatch(string input, string pattern)
    {
        var options = new GlobbingOptions { Dot = true, StrictSlashes = true };
        Assert.False(GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("./abc", "*")]
    [InlineData("./abc", "*/*")]
    [InlineData("./abc", "*/abc")]
    [InlineData("./abc", "*/abc/*")]
    [InlineData("./abc", ".*/*")]
    [InlineData("./abc", ".*/abc")]
    [InlineData("./abc", "*./*")]
    [InlineData("./abc", "*./abc")]
    public void SingleDots_Leading_WithStar_ShouldNotMatch(string input, string pattern)
    {
        Assert.False(GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("./abc", "**")]
    [InlineData("./abc", "**/**")]
    [InlineData("./abc", "**/**/**")]
    [InlineData("./abc", "**/abc")]
    [InlineData("./abc", "**/abc/**")]
    [InlineData("./abc", "abc/**")]
    [InlineData("./abc", "abc/**/**")]
    public void SingleDots_Leading_WithGlobstar_ShouldNotMatch(string input, string pattern)
    {
        Assert.False(GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("/./abc", "*/*")]
    [InlineData("/./abc", "/*/*")]
    [InlineData("/./abc", "*/*/*")]
    [InlineData("abc/./abc", "*/*/*")]
    [InlineData("abc/./abc/abc", "*/*/*/*")]
    public void SingleDots_Nested_WithStar_ShouldNotMatch(string input, string pattern)
    {
        Assert.False(GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("/./abc", "**")]
    [InlineData("/./abc", "**/**")]
    [InlineData("/./abc", "/**/**")]
    [InlineData("/./abc", "**/**/**")]
    [InlineData("abc/./abc", "**/**/**")]
    [InlineData("abc/./abc/abc", "**/**/**/**")]
    public void SingleDots_Nested_WithGlobstar_ShouldNotMatch(string input, string pattern)
    {
        Assert.False(GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("abc/.", "*/*")]
    [InlineData("abc/.", "*/*/")]
    [InlineData("abc/.", "*/*/*")]
    [InlineData("abc/./", "*/*")]
    [InlineData("abc/./", "*/*/")]
    [InlineData("abc/./", "*/*/*")]
    public void SingleDots_Trailing_WithStar_ShouldNotMatch(string input, string pattern)
    {
        Assert.False(GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("abc/.", "**/**")]
    [InlineData("abc/.", "**/**/")]
    [InlineData("abc/.", "**/**/**")]
    [InlineData("abc/./", "**/**")]
    [InlineData("abc/./", "**/**/")]
    [InlineData("abc/./", "**/**/**")]
    public void SingleDots_Trailing_WithGlobstar_ShouldNotMatch(string input, string pattern)
    {
        Assert.False(GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("./abc", "*/*")]
    [InlineData("./abc", "*/abc")]
    [InlineData("./abc", "**")]
    [InlineData("./abc", "**/**")]
    [InlineData("./abc", "**/abc")]
    public void SingleDots_Leading_WithDotOption_ShouldNotMatch(string input, string pattern)
    {
        var options = new GlobbingOptions { Dot = true };
        Assert.False(GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("/./abc", "*/*")]
    [InlineData("/./abc", "/*/*")]
    [InlineData("abc/./abc", "*/*/*")]
    [InlineData("/./abc", "**")]
    [InlineData("/./abc", "**/**")]
    [InlineData("abc/./abc", "**/**/**")]
    public void SingleDots_Nested_WithDotOption_ShouldNotMatch(string input, string pattern)
    {
        var options = new GlobbingOptions { Dot = true };
        Assert.False(GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("abc/.", "*/*")]
    [InlineData("abc/.", "*/*/")]
    [InlineData("abc/./", "*/*")]
    [InlineData("abc/.", "**/**")]
    [InlineData("abc/./", "**/**")]
    public void SingleDots_Trailing_WithDotOption_ShouldNotMatch(string input, string pattern)
    {
        var options = new GlobbingOptions { Dot = true };
        Assert.False(GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("./abc", "*/*")]
    [InlineData("./abc", "*/abc")]
    [InlineData("./abc", "**")]
    [InlineData("./abc", "**/**")]
    [InlineData("./abc", "**/abc")]
    public void SingleDots_Leading_WithStrictSlashesOption_ShouldNotMatch(string input, string pattern)
    {
        var options = new GlobbingOptions { StrictSlashes = true };
        Assert.False(GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("/./abc", "*/*")]
    [InlineData("/./abc", "/*/*")]
    [InlineData("abc/./abc", "*/*/*")]
    [InlineData("/./abc", "**")]
    [InlineData("/./abc", "**/**")]
    [InlineData("abc/./abc", "**/**/**")]
    public void SingleDots_Nested_WithStrictSlashesOption_ShouldNotMatch(string input, string pattern)
    {
        var options = new GlobbingOptions { StrictSlashes = true };
        Assert.False(GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("abc/.", "*/*")]
    [InlineData("abc/.", "*/*/")]
    [InlineData("abc/./", "*/*")]
    [InlineData("abc/.", "**/**")]
    [InlineData("abc/./", "**/**")]
    public void SingleDots_Trailing_WithStrictSlashesOption_ShouldNotMatch(string input, string pattern)
    {
        var options = new GlobbingOptions { StrictSlashes = true };
        Assert.False(GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("./abc", "*/*")]
    [InlineData("./abc", "*/abc")]
    [InlineData("./abc", "**")]
    [InlineData("./abc", "**/**")]
    [InlineData("./abc", "**/abc")]
    public void SingleDots_Leading_WithBothOptions_ShouldNotMatch(string input, string pattern)
    {
        var options = new GlobbingOptions { Dot = true, StrictSlashes = true };
        Assert.False(GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("/./abc", "*/*")]
    [InlineData("abc/./abc", "*/*/*")]
    [InlineData("/./abc", "**")]
    [InlineData("/./abc", "**/**")]
    [InlineData("abc/./abc", "**/**/**")]
    public void SingleDots_Nested_WithBothOptions_ShouldNotMatch(string input, string pattern)
    {
        var options = new GlobbingOptions { Dot = true, StrictSlashes = true };
        Assert.False(GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("abc/.", "*/*")]
    [InlineData("abc/./", "*/*")]
    [InlineData("abc/.", "**/**")]
    [InlineData("abc/./", "**/**")]
    public void SingleDots_Trailing_WithBothOptions_ShouldNotMatch(string input, string pattern)
    {
        var options = new GlobbingOptions { Dot = true, StrictSlashes = true };
        Assert.False(GlobMatcher.IsMatch(input, pattern, options));
    }

}
