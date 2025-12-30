namespace Snowberry.Globbing.Tests;

public class BasicMatchingTests
{
    [Fact]
    public void TestSimpleWildcard()
    {
        var matcher = GlobMatcher.Create("*.js");

        Assert.True(matcher("a.js"));
        Assert.True(matcher("test.js"));
        Assert.False(matcher("a.md"));
        Assert.False(matcher("test.txt"));
    }

    [Fact]
    public void TestQuestionMark()
    {
        var matcher = GlobMatcher.Create("a?c");

        Assert.True(matcher("abc"));
        Assert.True(matcher("adc"));
        Assert.False(matcher("ac"));
        Assert.False(matcher("abcd"));
    }

    [Fact]
    public void TestGlobstar()
    {
        var matcher = GlobMatcher.Create("**/*.js");

        Assert.True(matcher("a.js"));
        Assert.True(matcher("foo/a.js"));
        Assert.True(matcher("foo/bar/a.js"));
        Assert.False(matcher("a.md"));
    }

    [Fact]
    public void TestBrackets()
    {
        var matcher = GlobMatcher.Create("[abc].js");

        Assert.True(matcher("a.js"));
        Assert.True(matcher("b.js"));
        Assert.True(matcher("c.js"));
        Assert.False(matcher("d.js"));
    }

    [Fact]
    public void TestNegation()
    {
        var matcher = GlobMatcher.Create("!*.md");

        Assert.False(matcher("test.md"));
        Assert.True(matcher("test.js"));
    }

    [Fact]
    public void TestIsMatch()
    {
        Assert.True(GlobMatcher.IsMatch("foo.js", "*.js"));
        Assert.False(GlobMatcher.IsMatch("foo.md", "*.js"));
    }

    [Fact]
    public void TestIsMatchMultiplePatterns()
    {
        Assert.True(GlobMatcher.IsMatch("a.a", ["b.*", "*.a"]));
        Assert.False(GlobMatcher.IsMatch("a.a", "b.*"));
    }

    [Fact]
    public void TestDotFiles()
    {
        var matcher = GlobMatcher.Create("*", new GlobbingOptions { Dot = false });

        Assert.False(matcher(".dotfile"));
        Assert.True(matcher("regular"));
    }

    [Fact]
    public void TestDotFilesWithOption()
    {
        var matcher = GlobMatcher.Create("*", new GlobbingOptions { Dot = true });

        Assert.True(matcher(".dotfile"));
        Assert.True(matcher("regular"));
    }

    [Fact]
    public void TestMatchBase()
    {
        Assert.True(GlobMatcher.MatchBase("foo/bar.js", "*.js"));
        Assert.False(GlobMatcher.MatchBase("foo/bar.md", "*.js"));
    }

    [Fact]
    public void TestCaseInsensitive()
    {
        var matcher = GlobMatcher.Create("*.JS", new GlobbingOptions { NoCase = true });

        Assert.True(matcher("test.js"));
        Assert.True(matcher("test.JS"));
        Assert.True(matcher("test.Js"));
    }

    [Fact]
    public void TestExtglob_Plus()
    {
        var matcher = GlobMatcher.Create("+(a)");

        Assert.True(matcher("a"));
        Assert.True(matcher("aa"));
        Assert.True(matcher("aaa"));
        Assert.False(matcher("b"));
    }

    [Fact]
    public void TestExtglob_Star()
    {
        var matcher = GlobMatcher.Create("a*(z)", new GlobbingOptions { NoExtglob = false });

        // Note: Empty string matching depends on context
        Assert.True(matcher("a"));
        Assert.True(matcher("az"));
        Assert.True(matcher("azz"));
        Assert.False(matcher("b"));
    }

    [Fact]
    public void TestExtglob_Question()
    {
        var matcher = GlobMatcher.Create("a?(z)", new GlobbingOptions { NoExtglob = false });

        // Matches a with optional z
        Assert.True(matcher("a"));
        Assert.True(matcher("az"));
        Assert.False(matcher("azz"));
    }

    [Fact]
    public void TestExtglob_At()
    {
        var matcher = GlobMatcher.Create("@(a|b)");

        Assert.True(matcher("a"));
        Assert.True(matcher("b"));
        Assert.False(matcher("c"));
        Assert.False(matcher("ab"));
    }

    [Fact]
    public void TestExtglob_Negate()
    {
        var matcher = GlobMatcher.Create("!(a)");

        Assert.False(matcher("a"));
        Assert.True(matcher("b"));
        Assert.True(matcher("c"));
    }

    [Fact]
    public void TestScan()
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
    public void TestParse()
    {
        var result = GlobMatcher.Parse("*.js");

        Assert.NotNull(result);
        Assert.NotNull(result.Output);
        Assert.Contains("js", result.Output);
    }

    [Fact]
    public void TestEmptyPattern_ThrowsException()
    {
        Assert.Throws<ArgumentException>(() => GlobMatcher.Create(""));
    }

    [Fact]
    public void TestNullPattern_ThrowsException()
    {
        Assert.Throws<ArgumentException>(() => GlobMatcher.Create((string)null!));
    }
}
