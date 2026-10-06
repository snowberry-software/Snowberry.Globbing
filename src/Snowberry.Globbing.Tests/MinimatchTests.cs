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
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("foo/bar.min.js", "**/*.min.js", true)]
    [InlineData("a/b/c/bar.min.js", "**/*.min.js", true)]
    [InlineData("bar.min.js", "**/*.min.js", true)]
    public void Minimatch_GlobstarMinJs(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a/", "a", false)]
    [InlineData("a", "a/", false)]
    public void Minimatch_TrailingSlash(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a/", "a", false)]
    public void Minimatch_StrictSlashes_TrailingSlash(string input, string pattern, bool expected)
    {
        var options = new GlobOptions { StrictSlashes = true };
        Glob.IsMatch(input, pattern, options).Should().Be(expected);
    }

    [Theory]
    [InlineData("a/b/c", "*/*", false)]
    public void Minimatch_SegmentMatching(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("foo", "@(foo|bar)", true)]
    [InlineData("bar", "@(foo|bar)", true)]
    [InlineData("baz", "@(foo|bar)", false)]
    public void Minimatch_Extglob_At(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Fact]
    public void Minimatch_MatchBase_Option()
    {
        var options = new GlobOptions { MatchFileNameOnly = true };

        Glob.IsMatch("a/b/c/foo.txt", "foo.txt", options).Should().BeTrue();
        Glob.IsMatch("foo.txt", "foo.txt", options).Should().BeTrue();
        Glob.IsMatch("a/b/c/bar.txt", "foo.txt", options).Should().BeFalse();
    }

    [Fact]
    public void Minimatch_NoCase_Option()
    {
        var options = new GlobOptions { IgnoreCase = true };

        Glob.IsMatch("FOO", "foo", options).Should().BeTrue();
        Glob.IsMatch("foo", "FOO", options).Should().BeTrue();
        Glob.IsMatch("FoO", "fOo", options).Should().BeTrue();
    }
}