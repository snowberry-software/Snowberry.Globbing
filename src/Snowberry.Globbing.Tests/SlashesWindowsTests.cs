namespace Snowberry.Globbing.Tests;

/// <summary>
/// Tests for Windows path handling (backslash support) ported from picomatch.
/// </summary>
public class SlashesWindowsTests
{

    [Theory]
    [InlineData("a\\b", "a/b", true)]
    [InlineData("a\\a", "a/b", false)]
    [InlineData("a\\c", "a/b", false)]
    [InlineData("b\\a", "a/b", false)]
    [InlineData("b\\b", "a/b", false)]
    [InlineData("b\\c", "a/b", false)]
    public void ShouldMatchWindowsPathSeparatorsWithStringLiteral(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { Windows = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("a\\a", "(a/b)", false)]
    [InlineData("a\\b", "(a/b)", true)]
    [InlineData("a\\c", "(a/b)", false)]
    [InlineData("b\\a", "(a/b)", false)]
    [InlineData("b\\b", "(a/b)", false)]
    [InlineData("b\\c", "(a/b)", false)]
    public void ShouldMatchWindowsPathSeparatorsWithParens(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { Windows = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("a\\a", "a/(a|c)", true)]
    [InlineData("a\\b", "a/(a|c)", false)]
    [InlineData("a\\c", "a/(a|c)", true)]
    [InlineData("a\\a", "a/(a|b|c)", true)]
    [InlineData("a\\b", "a/(a|b|c)", true)]
    [InlineData("a\\c", "a/(a|b|c)", true)]
    public void ShouldMatchBackslashesWhenFollowedByRegexOr(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { Windows = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("a\\a", "a/[b-c]", false)]
    [InlineData("a\\b", "a/[b-c]", true)]
    [InlineData("a\\c", "a/[b-c]", true)]
    [InlineData("a\\x", "a/[b-c]", false)]
    [InlineData("a\\a", "a/[a-z]", true)]
    [InlineData("a\\b", "a/[a-z]", true)]
    [InlineData("a\\c", "a/[a-z]", true)]
    [InlineData("a\\x", "a/[a-z]", true)]
    public void ShouldSupportMatchingBackslashesWithRegexRanges(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { Windows = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("a\\a", "a/b", false)]
    [InlineData("a\\b", "a/b", false)]
    [InlineData("a\\c", "a/b", false)]
    [InlineData("b\\a", "a/b", false)]
    [InlineData("b\\b", "a/b", false)]
    [InlineData("b\\c", "a/b", false)]
    public void ShouldNotMatchLiteralBackslashesWithLiteralForwardSlashesWhenWindowsIsDisabled(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { Windows = false };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("a\\a", "a/(a|c)", false)]
    [InlineData("a\\b", "a/(a|c)", false)]
    [InlineData("a\\c", "a/(a|c)", false)]
    [InlineData("a\\a", "a/(a|b|c)", false)]
    [InlineData("a\\b", "a/(a|b|c)", false)]
    [InlineData("a\\c", "a/(a|b|c)", false)]
    public void ShouldNotMatchBackslashesWithForwardSlashesWhenWindowsIsDisabled(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { Windows = false };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("a\\a", "a/[b-c]", false)]
    [InlineData("a\\b", "a/[b-c]", false)]
    [InlineData("a\\c", "a/[b-c]", false)]
    [InlineData("a\\x", "a/[b-c]", false)]
    [InlineData("a\\a", "a/[a-z]", false)]
    [InlineData("a\\b", "a/[a-z]", false)]
    [InlineData("a\\c", "a/[a-z]", false)]
    [InlineData("a\\x", "a/[a-z]", false)]
    public void ShouldNotMatchBackslashesWithRangesWhenWindowsIsDisabled(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { Windows = false };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("a", "*", true)]
    [InlineData("b", "*", true)]
    [InlineData("a\\a", "*", false)]
    [InlineData("a\\b", "*", false)]
    [InlineData("a\\c", "*", false)]
    [InlineData("x\\y", "*", false)]
    public void ShouldNotMatchSlashesWithSingleStars(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { Windows = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("a", "*/*", false)]
    [InlineData("a\\a", "*/*", true)]
    [InlineData("a\\b", "*/*", true)]
    [InlineData("a\\c", "*/*", true)]
    [InlineData("x\\y", "*/*", true)]
    [InlineData("a\\a\\a", "*/*", false)]
    public void StarSlashStarShouldMatchTwoSegments(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { Windows = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("a", "*/*/*", false)]
    [InlineData("a\\a", "*/*/*", false)]
    [InlineData("a\\a\\a", "*/*/*", true)]
    [InlineData("a\\a\\b", "*/*/*", true)]
    [InlineData("a\\a\\a\\a", "*/*/*", false)]
    public void ThreeStarSegmentsShouldMatchThreeSegments(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { Windows = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("a", "a/*", false)]
    [InlineData("a\\a", "a/*", true)]
    [InlineData("a\\b", "a/*", true)]
    [InlineData("a\\c", "a/*", true)]
    [InlineData("a\\a\\a", "a/*", false)]
    [InlineData("x\\y", "a/*", false)]
    public void PrefixedStarShouldMatchOneSegment(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { Windows = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("a\\a\\a", "a/*/a", true)]
    [InlineData("a\\a\\b", "a/*/a", false)]
    [InlineData("a\\b\\a", "a/*/a", true)]
    [InlineData("a\\a\\a\\a", "a/*/a", false)]
    public void StarBetweenSegmentsShouldMatchOneSegment(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { Windows = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("a\\a", "a/**", true)]
    [InlineData("a\\b", "a/**", true)]
    [InlineData("a\\c", "a/**", true)]
    [InlineData("a\\x", "a/**", true)]
    [InlineData("a\\x\\y", "a/**", true)]
    [InlineData("a\\x\\y\\z", "a/**", true)]
    public void ShouldSupportGlobstars(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { Windows = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("a\\a", "a/**/*", true)]
    [InlineData("a\\b", "a/**/*", true)]
    [InlineData("a\\c", "a/**/*", true)]
    [InlineData("a\\x", "a/**/*", true)]
    [InlineData("a\\x\\y", "a/**/*", true)]
    [InlineData("a\\x\\y\\z", "a/**/*", true)]
    public void ShouldSupportGlobstarsWithStar(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { Windows = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("a\\a", "a/**", false)]
    [InlineData("a\\b", "a/**", false)]
    [InlineData("a\\c", "a/**", false)]
    [InlineData("a\\x", "a/**", false)]
    [InlineData("a\\x\\y", "a/**", false)]
    [InlineData("a\\x\\y\\z", "a/**", false)]
    public void ShouldNotMatchBackslashesWithGlobstarsWhenDisabled(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { Windows = false };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("a.txt", "a*.txt", true)]
    [InlineData("a\\b.txt", "a*.txt", false)]
    [InlineData("a\\x\\y.txt", "a*.txt", false)]
    public void StarShouldNotMatchBackslashInExtensionPattern(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { Windows = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("a.txt", "a/**/*.txt", false)]
    [InlineData("a\\b.txt", "a/**/*.txt", true)]
    [InlineData("a\\x\\y.txt", "a/**/*.txt", true)]
    [InlineData("a\\x\\y\\z", "a/**/*.txt", false)]
    public void GlobstarShouldMatchNestedFilesWithExtension(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { Windows = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("a.txt", "a/*.txt", false)]
    [InlineData("a\\b.txt", "a/*.txt", true)]
    [InlineData("a\\x\\y.txt", "a/*.txt", false)]
    public void SingleStarShouldMatchOneSegmentWithExtension(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { Windows = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("a.txt", "a/*/*.txt", false)]
    [InlineData("a\\b.txt", "a/*/*.txt", false)]
    [InlineData("a\\x\\y.txt", "a/*/*.txt", true)]
    public void TwoStarsShouldMatchTwoSegmentsWithExtension(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { Windows = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("a", "!a/b", true)]
    [InlineData("a\\a", "!a/b", true)]
    [InlineData("a\\b", "!a/b", false)]
    [InlineData("a\\c", "!a/b", true)]
    [InlineData("b\\a", "!a/b", true)]
    [InlineData("b\\b", "!a/b", true)]
    [InlineData("b\\c", "!a/b", true)]
    public void ShouldSupportNegationPatterns(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { Windows = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("a", "!*/c", true)]
    [InlineData("a\\a", "!*/c", true)]
    [InlineData("a\\b", "!*/c", true)]
    [InlineData("a\\c", "!*/c", false)]
    [InlineData("b\\a", "!*/c", true)]
    [InlineData("b\\b", "!*/c", true)]
    [InlineData("b\\c", "!*/c", false)]
    public void ShouldSupportNegationWithStar(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { Windows = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("a", "!a/(b)", true)]
    [InlineData("a\\a", "!a/(b)", true)]
    [InlineData("a\\b", "!a/(b)", false)]
    [InlineData("a\\c", "!a/(b)", true)]
    [InlineData("b\\a", "!a/(b)", true)]
    [InlineData("b\\b", "!a/(b)", true)]
    [InlineData("b\\c", "!a/(b)", true)]
    public void ShouldSupportNegationWithParens(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { Windows = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("a", "!(a/b)", true)]
    [InlineData("a\\a", "!(a/b)", true)]
    [InlineData("a\\b", "!(a/b)", false)]
    [InlineData("a\\c", "!(a/b)", true)]
    [InlineData("b\\a", "!(a/b)", true)]
    [InlineData("b\\b", "!(a/b)", true)]
    [InlineData("b\\c", "!(a/b)", true)]
    public void ShouldSupportNegationExtglob(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { Windows = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

}

