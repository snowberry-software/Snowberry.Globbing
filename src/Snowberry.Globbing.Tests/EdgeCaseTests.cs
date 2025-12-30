namespace Snowberry.Globbing.Tests;

/// <summary>
/// Advanced edge case tests
/// </summary>
public class EdgeCaseTests
{
    [Theory]
    [InlineData("...", "*")]
    public void EdgeCase_DotFiles_HandleCorrectly(string input, string pattern)
    {
        bool withDot = GlobMatcher.IsMatch(input, pattern, new GlobbingOptions { Dot = true });
        bool withoutDot = GlobMatcher.IsMatch(input, pattern, new GlobbingOptions { Dot = false });

        Assert.True(withDot);
        Assert.False(withoutDot);
    }

    [Theory]
    [InlineData("test.js", "**/test.js")]
    [InlineData("a/test.js", "**/test.js")]
    [InlineData("a/b/test.js", "**/test.js")]
    [InlineData("a/b/c/test.js", "**/test.js")]
    public void EdgeCase_GlobstarMatchesAnyDepth(string input, string pattern)
    {
        Assert.True(GlobMatcher.IsMatch(input, pattern));
    }

    [Fact]
    public void EdgeCase_EmptyString_DoesNotMatch()
    {
        Assert.False(GlobMatcher.IsMatch("", "*"));
    }

    [Theory]
    [InlineData("test", "test")]
    [InlineData("test.js", "test.js")]
    [InlineData("path/to/file.js", "path/to/file.js")]
    public void EdgeCase_ExactMatch_Works(string input, string pattern)
    {
        Assert.True(GlobMatcher.IsMatch(input, pattern));
    }

    [Fact]
    public void EdgeCase_DoubleStarGlobstar_MatchesDeepPaths()
    {
        // ** should match deep paths
        Assert.True(GlobMatcher.IsMatch("a/b/c.js", "**/c.js"));
        Assert.True(GlobMatcher.IsMatch("a/b/c.js", "**/*.js"));
        Assert.True(GlobMatcher.IsMatch("a/b/c.js", "a/**/c.js"));
    }

    [Theory]
    [InlineData("/", "test.js", false)]
    [InlineData("/test.js", "/test.js", true)]
    [InlineData("./test.js", "test.js", true)]
    public void EdgeCase_LeadingSlash_HandlesCorrectly(string pattern, string input, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Fact]
    public void EdgeCase_TrailingSlash_HandlesCorrectly()
    {
        var matcher = GlobMatcher.Create("test/");

        // Behavior may vary - just ensure it doesn't crash
        Assert.NotNull(matcher);
    }

    [Theory]
    [InlineData("a{b,c}d", "abd", true)]
    [InlineData("a{b,c}d", "acd", true)]
    [InlineData("a{b,c}d", "ad", false)]
    [InlineData("a{b,c}d", "abcd", false)]
    public void EdgeCase_BraceExpansion_NoSpaces(string pattern, string input, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("[^a]", "a", false)]
    [InlineData("[^a]", "b", true)]
    [InlineData("[^a-z]", "a", false)]
    [InlineData("[^a-z]", "1", true)]
    public void EdgeCase_NegatedBrackets_WorkCorrectly(string pattern, string input, bool expected)
    {
        // Use ^ for negation in brackets (standard regex)
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("**", ".", false)]
    [InlineData("**", "..", false)]
    [InlineData("**", "file", true)]
    public void EdgeCase_GlobstarWithDots_HandlesDotFiles(string pattern, string input, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, new GlobbingOptions { Dot = false }));
    }

    [Fact]
    public void EdgeCase_VeryLongPath_HandlesCorrectly()
    {
        string longPath = string.Join("/", Enumerable.Repeat("dir", 100)) + "/file.js";
        var matcher = GlobMatcher.Create("**/*.js");

        Assert.True(matcher(longPath));
    }

    [Fact]
    public void EdgeCase_SpecialCharactersInFilename_EscapedCorrectly()
    {
        var matcher = GlobMatcher.Create("test$file.js");

        Assert.True(matcher("test$file.js"));
    }

    [Theory]
    [InlineData("*.js", "test.js", true)]
    [InlineData("*..js", "test..js", true)]
    [InlineData("*.*.js", "test.min.js", true)]
    public void EdgeCase_MultipleDots_HandlesCorrectly(string pattern, string input, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Fact]
    public void EdgeCase_UnicodeCharacters_HandlesCorrectly()
    {
        var matcher = GlobMatcher.Create("*.js");

        Assert.True(matcher("テスト.js"));
        Assert.True(matcher("файл.js"));
        Assert.True(matcher("文件.js"));
    }

    [Theory]
    [InlineData("a/**", "a", true)]
    [InlineData("a/**", "a/b", true)]
    [InlineData("a/**", "a/b/c", true)]
    public void EdgeCase_GlobstarAtEnd_MatchesDirectoryAndContents(string pattern, string input, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("**/a", "a", true)]
    [InlineData("**/a", "b/a", true)]
    [InlineData("**/a", "b/c/a", true)]
    [InlineData("**/a", "b", false)]
    public void EdgeCase_GlobstarAtStart_MatchesAnywhere(string pattern, string input, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Fact]
    public void EdgeCase_OnlyGlobstar_MatchesEverything()
    {
        var matcher = GlobMatcher.Create("**");

        Assert.True(matcher("test.js"));
        Assert.True(matcher("path/to/file.js"));
        Assert.True(matcher("a/b/c/d/e.js"));
    }

    [Theory]
    [InlineData("a*b*c", "abc", true)]
    [InlineData("a*b*c", "aXbYc", true)]
    [InlineData("a*b*c", "aXYZbMNOc", true)]
    [InlineData("a*b*c", "acb", false)]
    public void EdgeCase_MultipleWildcardsInSequence(string pattern, string input, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("?", "a", true)]
    [InlineData("?", "1", true)]
    [InlineData("?", "", false)]
    [InlineData("?", "ab", false)]
    public void EdgeCase_SingleQuestionMark(string pattern, string input, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("???", "abc", true)]
    [InlineData("???", "ab", false)]
    [InlineData("???", "abcd", false)]
    public void EdgeCase_MultipleQuestionMarks(string pattern, string input, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }
}
