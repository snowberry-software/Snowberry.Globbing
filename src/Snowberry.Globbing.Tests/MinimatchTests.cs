namespace Snowberry.Globbing.Tests;

/// <summary>
/// Tests for minimatch compatibility.
/// Ported from: https://github.com/micromatch/picomatch/blob/master/test/minimatch.js
/// </summary>
public class MinimatchTests
{
    [Theory]
    [InlineData("bar.min.js", "*.min.js", true)]
    [InlineData("bar.js", "*.min.js", false)]
    [InlineData("foo/bar.min.js", "*.min.js", false)]
    public void Minimatch_MinJs(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("foo/bar.min.js", "**/*.min.js", true)]
    [InlineData("a/b/c/bar.min.js", "**/*.min.js", true)]
    [InlineData("bar.min.js", "**/*.min.js", true)]
    public void Minimatch_GlobstarMinJs(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a", "a", true)]
    [InlineData("a/", "a", false)]
    [InlineData("a/", "a/", true)]
    [InlineData("a", "a/", false)]
    public void Minimatch_TrailingSlash(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a/", "a", false)]
    public void Minimatch_StrictSlashes_TrailingSlash(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { StrictSlashes = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("a/b", "*/*", true)]
    [InlineData("a/b/c", "*/*/*", true)]
    [InlineData("a/b/c", "*/*", false)]
    [InlineData("a", "*/*", false)]
    public void Minimatch_SegmentMatching(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("foo", "!(bar)", true)]
    [InlineData("bar", "!(bar)", false)]
    [InlineData("foobar", "!(bar)", true)]
    public void Minimatch_Extglob_Negation(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("foo", "@(foo|bar)", true)]
    [InlineData("bar", "@(foo|bar)", true)]
    [InlineData("baz", "@(foo|bar)", false)]
    public void Minimatch_Extglob_At(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("foo", "+(foo)", true)]
    [InlineData("foofoo", "+(foo)", true)]
    [InlineData("bar", "+(foo)", false)]
    public void Minimatch_Extglob_Plus(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("foo", "*(foo)", true)]
    [InlineData("foofoo", "*(foo)", true)]
    [InlineData("", "*(foo)", false)]
    [InlineData("bar", "*(foo)", false)]
    public void Minimatch_Extglob_Star(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("foo", "?(foo)", true)]
    [InlineData("", "?(foo)", false)]
    [InlineData("foofoo", "?(foo)", false)]
    public void Minimatch_Extglob_Question(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Fact]
    public void Minimatch_MatchBase_Option()
    {
        var options = new GlobbingOptions { BaseName = true };

        Assert.True(GlobMatcher.IsMatch("a/b/c/foo.txt", "foo.txt", options));
        Assert.True(GlobMatcher.IsMatch("foo.txt", "foo.txt", options));
        Assert.False(GlobMatcher.IsMatch("a/b/c/bar.txt", "foo.txt", options));
    }

    [Fact]
    public void Minimatch_NoCase_Option()
    {
        var options = new GlobbingOptions { NoCase = true };

        Assert.True(GlobMatcher.IsMatch("FOO", "foo", options));
        Assert.True(GlobMatcher.IsMatch("foo", "FOO", options));
        Assert.True(GlobMatcher.IsMatch("FoO", "fOo", options));
    }

    [Theory]
    [InlineData(".dotfile", "*", false)]
    [InlineData(".dotfile", ".*", true)]
    [InlineData("a/.dotfile", "*/*", false)]
    [InlineData("a/.dotfile", "*/.*", true)]
    public void Minimatch_DotFiles(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData(".dotfile", "*", true)]
    [InlineData("a/.dotfile", "*/*", true)]
    public void Minimatch_DotFiles_WithDotOption(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { Dot = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }
}
