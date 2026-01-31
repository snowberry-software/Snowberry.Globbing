using System.Text.RegularExpressions;
using Snowberry.Globbing.Models;

namespace Snowberry.Globbing.Tests;

/// <summary>
/// API tests ported from picomatch.
/// </summary>
public class ApiTests
{

    [Fact]
    public void CreateShouldReturnMatchingFunction()
    {
        var isMatch = GlobMatcher.Create("*.js");
        Assert.True(isMatch("a.js"));
        Assert.True(isMatch("b.js"));
        Assert.False(isMatch("a.txt"));
    }

    [Fact]
    public void CreateShouldAcceptOptions()
    {
        var options = new GlobbingOptions { NoCase = true };
        var isMatch = GlobMatcher.Create("*.JS", options);
        Assert.True(isMatch("a.js"));
        Assert.True(isMatch("a.JS"));
    }

    [Fact]
    public void CreateShouldWorkWithMultiplePatterns()
    {
        var isMatch = GlobMatcher.Create(["*.js", "*.ts"]);
        Assert.True(isMatch("a.js"));
        Assert.True(isMatch("a.ts"));
        Assert.False(isMatch("a.txt"));
    }

    [Theory]
    [InlineData("a.js", "*.js", true)]
    [InlineData("a.txt", "*.js", false)]
    [InlineData("a/b/c.js", "**/*.js", true)]
    [InlineData("a/b/c.txt", "**/*.js", false)]
    public void IsMatchShouldReturnCorrectResult(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Fact]
    public void IsMatchShouldAcceptOptions()
    {
        var options = new GlobbingOptions { NoCase = true };
        Assert.True(GlobMatcher.IsMatch("A.JS", "*.js", options));
    }

    [Fact]
    public void IsMatchShouldHandleEmptyPattern()
    {
        Assert.Throws<ArgumentException>(() => GlobMatcher.IsMatch("a", ""));
    }

    [Fact]
    public void IsMatchShouldHandleEmptyInput()
    {
        Assert.False(GlobMatcher.IsMatch("", "*")); // Empty string doesn't match wildcard
    }

    [Fact]
    public void ParseShouldReturnParseResult()
    {
        var result = GlobMatcher.Parse("*.js");
        Assert.NotNull(result);
    }

    [Fact]
    public void ParseShouldReturnParseResultWithRegexSource()
    {
        var result = GlobMatcher.Parse("*.js");
        Assert.NotNull(result.Output);
        Assert.False(string.IsNullOrEmpty(result.Output));
    }

    [Fact]
    public void ScanShouldReturnScanResult()
    {
        var result = GlobMatcher.Scan("foo/bar/*.js");
        Assert.NotNull(result);
    }

    [Fact]
    public void ScanShouldIdentifyBase()
    {
        var result = GlobMatcher.Scan("foo/bar/*.js");
        Assert.Equal("foo/bar", result.Base);
    }

    [Fact]
    public void ScanShouldIdentifyGlob()
    {
        var result = GlobMatcher.Scan("foo/bar/*.js");
        Assert.Equal("*.js", result.Glob);
    }

    [Fact]
    public void ScanShouldIdentifyIsGlob()
    {
        var globResult = GlobMatcher.Scan("foo/bar/*.js");
        Assert.True(globResult.IsGlob);

        var nonGlobResult = GlobMatcher.Scan("foo/bar/baz.js");
        Assert.False(nonGlobResult.IsGlob);
    }

    [Fact]
    public void ScanShouldIdentifyNegation()
    {
        var negatedResult = GlobMatcher.Scan("!foo/bar/*.js");
        Assert.True(negatedResult.Negated);

        var nonNegatedResult = GlobMatcher.Scan("foo/bar/*.js");
        Assert.False(nonNegatedResult.Negated);
    }

    [Fact]
    public void ToRegexShouldReturnRegex()
    {
        var regex = GlobMatcher.ToRegex("*.js");
        Assert.NotNull(regex);
        Assert.IsAssignableFrom<Regex>(regex); // Use IsAssignableFrom instead of IsType
    }

    // Test that MakeRe returns correct regex for matching
    [Fact]
    public void ToRegexShouldMatchCorrectly()
    {
        var regex = GlobMatcher.MakeRe("*.js");
        Assert.Matches(regex, "a.js");
        Assert.Matches(regex, "foo.js");
        Assert.DoesNotMatch(regex, "a.txt");
    }

    // Test that MakeRe accepts options
    [Fact]
    public void ToRegexShouldAcceptOptions()
    {
        var options = new GlobbingOptions { NoCase = true };
        var regex = GlobMatcher.MakeRe("*.JS", options);
        Assert.Matches(regex, "a.js");
        Assert.Matches(regex, "a.JS");
    }

    [Fact]
    public void MakeReShouldReturnRegex()
    {
        var regex = GlobMatcher.MakeRe("*.js");
        Assert.NotNull(regex);
        Assert.IsType<Regex>(regex);
    }

    [Fact]
    public void MakeReShouldMatchCorrectly()
    {
        var regex = GlobMatcher.MakeRe("*.js");
        Assert.Matches(regex, "a.js");
        Assert.DoesNotMatch(regex, "a.txt");
    }

    [Theory]
    [InlineData("*.js", true)]
    [InlineData("**/*.js", true)]
    [InlineData("foo/bar.js", false)]
    [InlineData("foo?bar", true)]
    [InlineData("[abc]", true)]
    [InlineData("{a,b}", true)]
    public void ScanIsGlobShouldIdentifyGlobPatterns(string pattern, bool expected)
    {
        var result = GlobMatcher.Scan(pattern);
        Assert.Equal(expected, result.IsGlob);
    }

    [Theory]
    [InlineData("a", "a", true)]
    [InlineData("abc", "abc", true)]
    [InlineData("abc", "def", false)]
    public void IsMatchShouldHandleLiteralPatterns(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a/b/c", "a/b/c", true)]
    [InlineData("a/b/c", "a/b/d", false)]
    [InlineData("a/b/c", "a/*/c", true)]
    [InlineData("a/b/c", "a/**/c", true)]
    public void IsMatchShouldHandlePaths(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("foo", "foo", true)]
    [InlineData("foo/", "foo/", true)]
    [InlineData("foo/", "foo", false)]
    [InlineData("foo", "foo/", false)]
    public void IsMatchShouldHandleTrailingSlash(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Fact]
    public void IsMatchShouldAcceptMultiplePatternsAsArray()
    {
        Assert.True(GlobMatcher.IsMatch("a.js", ["*.txt", "*.js"]));
        Assert.True(GlobMatcher.IsMatch("a.txt", ["*.txt", "*.js"]));
        Assert.False(GlobMatcher.IsMatch("a.md", ["*.txt", "*.js"]));
    }

    [Fact]
    public void IsMatchWithMultiplePatternsReturnsTrueOnFirstMatch()
    {
        // Any match should return true
        Assert.True(GlobMatcher.IsMatch("a.js", ["*.txt", "*.js", "*.md"]));
    }

    [Fact]
    public void CreateShouldProvideStateInCallback()
    {
        bool callbackCalled = false;
        var options = new GlobbingOptions
        {
            OnMatch = result =>
            {
                callbackCalled = true;
                Assert.NotNull(result);
            }
        };
        var isMatch = GlobMatcher.Create("*.js", options);
        isMatch("a.js");
        Assert.True(callbackCalled);
    }

    [Fact]
    public void NocaseShouldAddIgnoreCaseFlag()
    {
        var options = new GlobbingOptions { NoCase = true };
        var regex = GlobMatcher.MakeRe("*.js", options);
        Assert.True((regex.Options & RegexOptions.IgnoreCase) == RegexOptions.IgnoreCase);
    }

    [Theory]
    [InlineData("foo/bar/baz.js")]
    [InlineData("a/b/c")]
    public void ScanInputShouldBeOriginalPattern(string pattern)
    {
        var result = GlobMatcher.Scan(pattern);
        Assert.Equal(pattern, result.Input);
    }

    [Theory]
    [InlineData("a.js", "!(*.txt)", true)]
    [InlineData("a.txt", "!(*.txt)", false)]
    public void IsMatchShouldHandleNegationExtglobs(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("ab", "a?(b|c)", true)]
    [InlineData("ac", "a?(b|c)", true)]
    [InlineData("a", "a?(b|c)", true)]
    [InlineData("ad", "a?(b|c)", false)]
    public void IsMatchShouldHandleOptionalExtglobs(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("ab", "a*(b|c)", true)]
    [InlineData("abc", "a*(b|c)", true)]
    [InlineData("abbb", "a*(b|c)", true)]
    [InlineData("accc", "a*(b|c)", true)]
    [InlineData("a", "a*(b|c)", true)]
    public void IsMatchShouldHandleZeroOrMoreExtglobs(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("ab", "a+(b|c)", true)]
    [InlineData("abc", "a+(b|c)", true)]
    [InlineData("abbb", "a+(b|c)", true)]
    [InlineData("a", "a+(b|c)", false)]
    public void IsMatchShouldHandleOneOrMoreExtglobs(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("ab", "a@(b|c)", true)]
    [InlineData("ac", "a@(b|c)", true)]
    [InlineData("a", "a@(b|c)", false)]
    [InlineData("abc", "a@(b|c)", false)]
    public void IsMatchShouldHandleExactlyOneExtglobs(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Fact]
    public void GenerateRegex_WithString_ShouldReturnRegexPattern()
    {
        string pattern = GlobMatcher.GenerateRegex("*.js");
        Assert.NotNull(pattern);
        Assert.False(string.IsNullOrEmpty(pattern));
    }

    [Fact]
    public void GenerateRegex_WithString_ShouldIncludeAnchors()
    {
        string pattern = GlobMatcher.GenerateRegex("*.js");
        Assert.StartsWith("^", pattern);
        Assert.EndsWith("$", pattern);
    }

    [Fact]
    public void GenerateRegex_WithString_ShouldMatchCorrectly()
    {
        string pattern = GlobMatcher.GenerateRegex("*.js");
        var regex = new Regex(pattern);

        Assert.Matches(regex, "test.js");
        Assert.Matches(regex, "app.js");
        Assert.DoesNotMatch(regex, "test.txt");
    }

    [Fact]
    public void GenerateRegex_WithString_ShouldAcceptOptions()
    {
        var options = new GlobbingOptions { Contains = true };
        string pattern = GlobMatcher.GenerateRegex("bar", options);

        Assert.DoesNotContain("^", pattern);
        Assert.DoesNotContain("$", pattern);
    }

    [Fact]
    public void GenerateRegex_WithString_ContainsOption_ShouldMatchSubstring()
    {
        var options = new GlobbingOptions { Contains = true };
        string pattern = GlobMatcher.GenerateRegex("bar", options);
        var regex = new Regex(pattern);

        Assert.Matches(regex, "foobar");
        Assert.Matches(regex, "barbaz");
        Assert.Matches(regex, "foobarbaz");
    }

    [Fact]
    public void GenerateRegex_WithString_ShouldThrowForEmptyInput()
    {
        Assert.Throws<ArgumentException>(() => GlobMatcher.GenerateRegex(""));
    }

    [Fact]
    public void GenerateRegex_WithString_ShouldThrowForNullInput()
    {
        Assert.Throws<ArgumentException>(() => GlobMatcher.GenerateRegex((string)null!));
    }

    [Fact]
    public void GenerateRegex_WithParseState_ShouldReturnRegexPattern()
    {
        var state = GlobMatcher.Parse("*.js");
        string pattern = GlobMatcher.GenerateRegex(state);

        Assert.NotNull(pattern);
        Assert.False(string.IsNullOrEmpty(pattern));
    }

    [Fact]
    public void GenerateRegex_WithParseState_ShouldIncludeAnchors()
    {
        var state = GlobMatcher.Parse("*.js");
        string pattern = GlobMatcher.GenerateRegex(state);

        Assert.StartsWith("^", pattern);
        Assert.EndsWith("$", pattern);
    }

    [Fact]
    public void GenerateRegex_WithParseState_ShouldMatchCorrectly()
    {
        var state = GlobMatcher.Parse("*.js");
        string pattern = GlobMatcher.GenerateRegex(state);
        var regex = new Regex(pattern);

        Assert.Matches(regex, "test.js");
        Assert.DoesNotMatch(regex, "test.txt");
    }

    [Fact]
    public void GenerateRegex_WithParseState_ShouldThrowForNullState()
    {
        Assert.Throws<ArgumentNullException>(() => GlobMatcher.GenerateRegex((ParseState)null!));
    }

    [Fact]
    public void GenerateRegex_WithParseState_ContainsOption_ShouldOmitAnchors()
    {
        var state = GlobMatcher.Parse("bar");
        var options = new GlobbingOptions { Contains = true };
        string pattern = GlobMatcher.GenerateRegex(state, options);

        Assert.DoesNotContain("^(?:", pattern);
        Assert.DoesNotContain(")$", pattern);
    }

    [Fact]
    public void GenerateRegex_WithParseState_NegatedPattern_ShouldWrapWithNegativeLookahead()
    {
        var state = GlobMatcher.Parse("!*.md");
        string pattern = GlobMatcher.GenerateRegex(state);

        Assert.Contains("(?!", pattern);
        Assert.EndsWith(".*$", pattern);
    }

    [Fact]
    public void GenerateRegex_WithParseState_NegatedPattern_ShouldMatchCorrectly()
    {
        var state = GlobMatcher.Parse("!*.md");
        string pattern = GlobMatcher.GenerateRegex(state);
        var regex = new Regex(pattern);

        Assert.Matches(regex, "test.js");
        Assert.Matches(regex, "app.txt");
        Assert.DoesNotMatch(regex, "readme.md");
    }

    [Theory]
    [InlineData("*.js")]
    [InlineData("**/*.ts")]
    [InlineData("src/[abc]/*.js")]
    [InlineData("{a,b,c}.txt")]
    public void GenerateRegex_WithString_ShouldProduceValidRegex(string glob)
    {
        string pattern = GlobMatcher.GenerateRegex(glob);

        // Should not throw when creating regex
        var regex = new Regex(pattern);
        Assert.NotNull(regex);
    }

    [Fact]
    public void GenerateRegex_WithString_ShouldProduceSameResultAsMakeRe()
    {
        string pattern = "*.js";
        string generatedPattern = GlobMatcher.GenerateRegex(pattern);
        var makeReRegex = GlobMatcher.MakeRe(pattern);

        var generatedRegex = new Regex(generatedPattern);

        // Both should match the same inputs
        Assert.Equal(makeReRegex.IsMatch("test.js"), generatedRegex.IsMatch("test.js"));
        Assert.Equal(makeReRegex.IsMatch("test.txt"), generatedRegex.IsMatch("test.txt"));
    }
}
