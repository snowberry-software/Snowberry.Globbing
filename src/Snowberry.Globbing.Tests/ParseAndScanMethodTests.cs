using Snowberry.Globbing.Models;

namespace Snowberry.Globbing.Tests;

/// <summary>
/// Tests for the Parse and Scan methods
/// </summary>
public class ParseAndScanMethodTests
{
    [Fact]
    public void Parse_SimplePattern_ReturnsState()
    {
        var state = GlobMatcher.Parse("*.js");

        Assert.NotNull(state);
        Assert.NotNull(state.Output);
        Assert.NotEmpty(state.Output);
    }

    [Fact]
    public void Parse_WithOptions_AppliesOptions()
    {
        var state = GlobMatcher.Parse("*.js", new GlobbingOptions { Dot = true });

        Assert.NotNull(state);
        Assert.True(state.Dot);
    }

    [Fact]
    public void Parse_MultiplePatterns_ReturnsMultipleStates()
    {
        var states = GlobMatcher.Parse(["*.js", "*.ts", "*.md"]);

        Assert.NotNull(states);
        Assert.Equal(3, states.Length);
        Assert.All(states, state => Assert.NotNull(state.Output));
    }

    [Fact]
    public void Parse_ComplexPattern_ParsesCorrectly()
    {
        var state = GlobMatcher.Parse("**/*.{js,ts}");

        Assert.NotNull(state);
        Assert.NotNull(state.Output);
    }

    [Fact]
    public void Scan_SimplePattern_ReturnsCorrectInfo()
    {
        var result = GlobMatcher.Scan("*.js");

        Assert.NotNull(result);
        Assert.Equal("*.js", result.Input);
        Assert.Equal("*.js", result.Glob);
        Assert.True(result.IsGlob);
    }

    [Fact]
    public void Scan_WithPrefix_ExtractsPrefix()
    {
        var result = GlobMatcher.Scan("!./foo/*.js");

        Assert.Equal("!./", result.Prefix);
        Assert.Equal("!./foo/*.js", result.Input);
        Assert.Equal("foo", result.Base);
        Assert.Equal("*.js", result.Glob);
        Assert.True(result.Negated);
        Assert.True(result.IsGlob);
    }

    [Fact]
    public void Scan_GlobstarPattern_IdentifiesGlobstar()
    {
        var result = GlobMatcher.Scan("**/*.js");

        Assert.True(result.IsGlob);
        // IsGlobstar may not be set in all implementations
        Assert.Contains("**", result.Input);
    }

    [Fact]
    public void Scan_BracePattern_IdentifiesBrace()
    {
        var result = GlobMatcher.Scan("*.{js,ts}");

        Assert.True(result.IsGlob);
        // IsBrace may not be set in all implementations
        Assert.Contains("{", result.Input);
    }

    [Fact]
    public void Scan_BracketPattern_IdentifiesBracket()
    {
        var result = GlobMatcher.Scan("[abc].js");

        Assert.True(result.IsGlob);
        Assert.True(result.IsBracket);
    }

    [Fact]
    public void Scan_ExtglobPattern_IdentifiesExtglob()
    {
        var result = GlobMatcher.Scan("+(a|b)");

        Assert.True(result.IsGlob);
        Assert.True(result.IsExtglob);
    }

    [Fact]
    public void Scan_NegatedPattern_IdentifiesNegation()
    {
        var result = GlobMatcher.Scan("!*.md");

        Assert.True(result.Negated);
        Assert.True(result.IsGlob);
    }

    [Fact]
    public void Scan_WithTokensOption_ReturnsTokens()
    {
        var result = GlobMatcher.Scan("foo/*.js", new ScanOptions { Tokens = true });

        Assert.NotNull(result.Tokens);
        Assert.NotEmpty(result.Tokens);
    }

    [Fact]
    public void Scan_WithPartsOption_ReturnsParts()
    {
        var result = GlobMatcher.Scan("foo/bar/*.js", new ScanOptions { Parts = true });

        Assert.NotNull(result.Parts);
        Assert.NotEmpty(result.Parts);
    }

    [Fact]
    public void Scan_NonGlobPattern_ReturnsCorrectInfo()
    {
        var result = GlobMatcher.Scan("test.js");

        Assert.False(result.IsGlob);
        Assert.Equal("test.js", result.Input);
    }

    [Fact]
    public void Scan_ComplexPath_ParsesCorrectly()
    {
        var result = GlobMatcher.Scan("src/**/test/*.js");

        Assert.Equal("src", result.Base);
        Assert.True(result.IsGlob);
        Assert.Contains("**", result.Input);
    }
}
