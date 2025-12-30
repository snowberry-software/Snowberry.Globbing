namespace Snowberry.Globbing.Tests;

/// <summary>
/// Tests for options.noglobstar.
/// Ported from: https://github.com/micromatch/picomatch/blob/master/test/options.noglobstar.js
/// </summary>
public class OptionsNoGlobstarTests
{
    [Theory]
    [InlineData("a/b/c", "**", true)]
    [InlineData("a/b/c", "**/c", true)]
    [InlineData("a/b/c", "a/**", true)]
    [InlineData("a/b/c", "a/**/c", true)]
    public void NoGlobstar_Disabled_GlobstarsMatchDirectories(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    // With noglobstar: ** is treated as * (single segment match)
    [Theory]
    [InlineData("a/b/c", "**", false)]      // ** matches single segment, not a/b/c
    [InlineData("a/b/c", "**/c", false)]    // ** matches single segment, needs x/c not a/b/c
    [InlineData("a/b/c", "a/**", false)]    // ** matches single segment, needs a/x not a/b/c
    [InlineData("a/b/c", "a/**/c", true)]   // ** matches 'b', so a/b/c matches
    public void NoGlobstar_Enabled_GlobstarsNotExpanded(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { NoGlobstar = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("a", "**", true)]       // ** matches single segment 'a'
    [InlineData("b", "**/b", false)]    // **/b requires slash before b
    public void NoGlobstar_Enabled_SingleSegmentStillMatches(string input, string pattern, bool expected)
    {
        // When noglobstar is true, ** is treated as *
        var options = new GlobbingOptions { NoGlobstar = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Fact]
    public void NoGlobstar_ShouldNotAffectSingleStar()
    {
        var options = new GlobbingOptions { NoGlobstar = true };

        // Single star should still work for matching within a segment
        Assert.True(GlobMatcher.IsMatch("foo", "*", options));
        Assert.True(GlobMatcher.IsMatch("foo.txt", "*.txt", options));
        Assert.True(GlobMatcher.IsMatch("a/foo", "a/*", options));
    }

    [Fact]
    public void NoGlobstar_ShouldNotAffectOtherGlobFeatures()
    {
        var options = new GlobbingOptions { NoGlobstar = true };

        // Extglobs should still work
        Assert.True(GlobMatcher.IsMatch("foo", "@(foo|bar)", options));
        Assert.True(GlobMatcher.IsMatch("foo", "!(baz)", options));

        // Question marks should still work
        Assert.True(GlobMatcher.IsMatch("foo", "f??", options));

        // Brackets should still work
        Assert.True(GlobMatcher.IsMatch("foo", "[f]oo", options));
    }
}
