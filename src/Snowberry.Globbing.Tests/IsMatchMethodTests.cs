namespace Snowberry.Globbing.Tests;

/// <summary>
/// Comprehensive tests for the IsMatch method
/// </summary>
public class IsMatchMethodTests
{
    [Theory]
    [InlineData("test.js", "*.js", true)]
    [InlineData("test.md", "*.js", false)]
    [InlineData("app.ts", "*.ts", true)]
    [InlineData("readme.txt", "*.txt", true)]
    public void IsMatch_SinglePattern_ReturnsExpectedResult(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("test.js", new[] { "*.js", "*.ts" }, true)]
    [InlineData("test.ts", new[] { "*.js", "*.ts" }, true)]
    [InlineData("test.md", new[] { "*.js", "*.ts" }, false)]
    [InlineData("app.css", new[] { "*.js", "*.ts", "*.css" }, true)]
    public void IsMatch_MultiplePatterns_ReturnsExpectedResult(string input, string[] patterns, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, patterns));
    }

    [Theory]
    [InlineData("src/app.js", "**/*.js", true)]
    [InlineData("src/lib/utils.js", "**/*.js", true)]
    [InlineData("test.js", "**/*.js", true)]
    [InlineData("test.md", "**/*.js", false)]
    public void IsMatch_GlobstarPattern_ReturnsExpectedResult(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Fact]
    public void IsMatch_WithOptions_AppliesOptions()
    {
        Assert.True(GlobMatcher.IsMatch("TEST.JS", "*.js", new GlobbingOptions { NoCase = true }));
        Assert.False(GlobMatcher.IsMatch("TEST.JS", "*.js", new GlobbingOptions { NoCase = false }));
    }

    [Theory]
    [InlineData(".gitignore", "*", true, true)]
    [InlineData(".gitignore", "*", false, false)]
    [InlineData("file.txt", "*", true, true)]
    [InlineData("file.txt", "*", false, true)]
    public void IsMatch_DotOption_AffectsDotfileMatching(string input, string pattern, bool dotOption, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, new GlobbingOptions { Dot = dotOption }));
    }

    [Theory]
    [InlineData("my-test-file.js", "test", true, true)]
    [InlineData("testing.js", "test", true, true)]
    [InlineData("test", "test", true, true)]
    [InlineData("file.js", "test", true, false)]
    [InlineData("my-test-file.js", "test", false, false)]
    public void IsMatch_ContainsOption_AffectsMatching(string input, string pattern, bool contains, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, new GlobbingOptions { Contains = contains }));
    }

    [Theory]
    [InlineData("foo/bar/baz.js", "**/baz.js", true)]
    [InlineData("baz.js", "**/baz.js", true)]
    [InlineData("foo/baz.js", "**/baz.js", true)]
    [InlineData("bar.js", "**/baz.js", false)]
    public void IsMatch_TrailingGlobstar_MatchesCorrectly(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("src/test.js", "src/*.js", true)]
    [InlineData("src/lib/test.js", "src/*.js", false)]
    [InlineData("src/lib/test.js", "src/**/*.js", true)]
    public void IsMatch_SingleVsDoubleAsterisk_BehaviorDiffers(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("abc", "a?c", true)]
    [InlineData("adc", "a?c", true)]
    [InlineData("ac", "a?c", false)]
    [InlineData("abcd", "a?c", false)]
    public void IsMatch_QuestionMark_MatchesSingleCharacter(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a.js", "[abc].js", true)]
    [InlineData("b.js", "[abc].js", true)]
    [InlineData("c.js", "[abc].js", true)]
    [InlineData("d.js", "[abc].js", false)]
    [InlineData("test-1.js", "test-[0-9].js", true)]
    [InlineData("test-a.js", "test-[0-9].js", false)]
    public void IsMatch_CharacterClass_MatchesCorrectly(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("test.js", "*.{js,ts}", true)]
    [InlineData("test.ts", "*.{js,ts}", true)]
    [InlineData("test.md", "*.{js,ts}", false)]
    [InlineData("app.jsx", "*.{js,jsx,ts,tsx}", true)]
    public void IsMatch_BraceExpansion_MatchesCorrectly(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("test.js", "!*.md", true)]
    [InlineData("test.md", "!*.md", false)]
    [InlineData("app.txt", "!*.md", true)]
    public void IsMatch_Negation_MatchesCorrectly(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("foo/bar.js", "foo/bar.js", true)]
    [InlineData("foo/bar/baz.js", "foo/*/baz.js", true)]
    [InlineData("foo/bar/qux/baz.js", "foo/*/baz.js", false)]
    [InlineData("foo/bar/qux/baz.js", "foo/**/baz.js", true)]
    public void IsMatch_PathSeparators_HandleCorrectly(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("", "*", false)]
    [InlineData("test", "", false)]
    public void IsMatch_EmptyInput_HandlesCorrectly(string input, string pattern, bool expected)
    {
        // Empty pattern throws, so we test empty input
        if (string.IsNullOrEmpty(pattern))
        {
            Assert.Throws<ArgumentException>(() => GlobMatcher.IsMatch(input, pattern));
            return;
        }

        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("test-file-name.js", "*-*-*.js", true)]
    [InlineData("test-file.js", "*-*-*.js", false)]
    [InlineData("a-b-c-d.js", "*-*-*.js", true)]
    public void IsMatch_MultipleWildcards_MatchesCorrectly(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("test", "test*", true)]
    [InlineData("testing", "test*", true)]
    [InlineData("test.js", "test*", true)]
    [InlineData("mytest", "test*", false)]
    public void IsMatch_TrailingWildcard_MatchesCorrectly(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("test", "*test", true)]
    [InlineData("mytest", "*test", true)]
    [InlineData("test.js", "*test", false)]
    public void IsMatch_LeadingWildcard_MatchesCorrectly(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("test", "*test*", true)]
    [InlineData("mytest", "*test*", true)]
    [InlineData("testing", "*test*", true)]
    [InlineData("mytesting", "*test*", true)]
    [InlineData("file", "*test*", false)]
    public void IsMatch_SurroundingWildcards_MatchesCorrectly(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }
}
