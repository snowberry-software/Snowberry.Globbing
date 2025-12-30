namespace Snowberry.Globbing.Tests;

/// <summary>
/// Tests for star (*) patterns ported from picomatch.
/// </summary>
public class StarsTests
{

    [Theory]
    [InlineData("z.js", "z*", true)]
    [InlineData("zzjs", "z*.js", false)]
    [InlineData("zzjs", "*z.js", false)]
    public void ShouldRespectDotsDefinedInGlobPattern(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a.js", "**/*.js", true)]
    [InlineData("a.js", "**/a*", true)]
    [InlineData("a.js", "**/a*.js", true)]
    [InlineData("abc", "**/abc", true)]
    public void ShouldMatchPathsWithNoSlashes(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a/b/c/z.js", "*.js", false)]
    [InlineData("a/b/z.js", "*.js", false)]
    [InlineData("a/z.js", "*.js", false)]
    [InlineData("z.js", "*.js", true)]
    [InlineData("a/.ab", "*/*", false)]
    [InlineData(".ab", "*", false)]
    [InlineData("z.js", "z*.js", true)]
    [InlineData("a/z", "*/*", true)]
    [InlineData("a/z.js", "*/z*.js", true)]
    [InlineData("a/z.js", "a/z*.js", true)]
    [InlineData("ab", "*", true)]
    [InlineData("abc", "*", true)]
    public void ShouldMatchAnythingExceptSlashesAndLeadingDots(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("bar", "f*", false)]
    [InlineData("foo", "*r", false)]
    [InlineData("foo", "b*", false)]
    [InlineData("foo/bar", "*", false)]
    [InlineData("abc", "*c", true)]
    [InlineData("abc", "a*", true)]
    [InlineData("abc", "a*c", true)]
    [InlineData("bar", "*r", true)]
    [InlineData("bar", "b*", true)]
    [InlineData("foo", "f*", true)]
    public void ShouldMatchBasicStarPatterns(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("one abc two", "*abc*", true)]
    [InlineData("a         b", "a*b", true)]
    public void ShouldMatchSpaces(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("foo", "*a*", false)]
    [InlineData("bar", "*a*", true)]
    [InlineData("oneabctwo", "*abc*", true)]
    [InlineData("a-b.c-d", "*-bc-*", false)]
    [InlineData("a-b.c-d", "*-*.*-*", true)]
    [InlineData("a-b.c-d", "*-b*c-*", true)]
    [InlineData("a-b.c-d", "*-b.c-*", true)]
    [InlineData("a-b.c-d", "*.*", true)]
    [InlineData("a-b.c-d", "*.*-*", true)]
    [InlineData("a-b.c-d", "*.*-d", true)]
    [InlineData("a-b.c-d", "*.c-*", true)]
    [InlineData("a-b.c-d", "*b.*d", true)]
    [InlineData("a-b.c-d", "a*.c*", true)]
    [InlineData("a-b.c-d", "a-*.*-d", true)]
    [InlineData("a.b", "*.*", true)]
    [InlineData("a.b", "*.b", true)]
    [InlineData("a.b", "a.*", true)]
    [InlineData("a.b", "a.b", true)]
    public void ShouldSupportMultipleNonConsecutiveStarsInPathSegment(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a-b.c-d", "**-bc-**", false)]
    [InlineData("a-b.c-d", "**-**.**-**", true)]
    [InlineData("a-b.c-d", "**-b**c-**", true)]
    [InlineData("a-b.c-d", "**-b.c-**", true)]
    [InlineData("a-b.c-d", "**.**", true)]
    [InlineData("a-b.c-d", "**.**-**", true)]
    [InlineData("a-b.c-d", "**.**-d", true)]
    [InlineData("a-b.c-d", "**.c-**", true)]
    [InlineData("a-b.c-d", "**b.**d", true)]
    [InlineData("a-b.c-d", "a**.c**", true)]
    [InlineData("a-b.c-d", "a-**.**-d", true)]
    [InlineData("a.b", "**.**", true)]
    [InlineData("a.b", "**.b", true)]
    [InlineData("a.b", "a.**", true)]
    [InlineData("a.b", "a.b", true)]
    public void ShouldSupportMultipleStarsInSegment(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("/ab", "*/*", true)]
    [InlineData(".", ".", true)]
    [InlineData("a/.b", "a/", false)]
    [InlineData("/ab", "/*", true)]
    [InlineData("/ab", "/??", true)]
    [InlineData("/ab", "/?b", true)]
    [InlineData("/cd", "/*", true)]
    [InlineData("a", "a", true)]
    [InlineData("a/.b", "a/.*", true)]
    [InlineData("a/b", "?/?", true)]
    [InlineData("a/b/c/d/e/j/n/p/o/z/c.md", "a/**/j/**/z/*.md", true)]
    [InlineData("a/b/c/d/e/z/c.md", "a/**/z/*.md", true)]
    [InlineData("a/b/c/xyz.md", "a/b/c/*.md", true)]
    [InlineData("a/b/z/.a", "a/*/z/.a", true)]
    [InlineData("a/b/z/.a", "bz", false)]
    [InlineData("a/bb.bb/aa/b.b/aa/c/xyz.md", "a/**/c/*.md", true)]
    [InlineData("a/bb.bb/aa/bb/aa/c/xyz.md", "a/**/c/*.md", true)]
    [InlineData("a/bb.bb/c/xyz.md", "a/*/c/*.md", true)]
    [InlineData("a/bb/c/xyz.md", "a/*/c/*.md", true)]
    [InlineData("a/bbbb/c/xyz.md", "a/*/c/*.md", true)]
    [InlineData("aaa", "*", true)]
    [InlineData("ab", "*", true)]
    [InlineData("ab", "ab", true)]
    public void ShouldReturnTrueWhenPatternMatches(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("/ab", "*/", false)]
    [InlineData("/ab", "*/a", false)]
    [InlineData("/ab", "/", false)]
    [InlineData("/ab", "/?", false)]
    [InlineData("/ab", "/a", false)]
    [InlineData("/ab", "?/?", false)]
    [InlineData("/ab", "a/*", false)]
    [InlineData("a/.b", "a/", false)]
    [InlineData("a/b/c", "a/*", false)]
    [InlineData("a/b/c", "a/b", false)]
    [InlineData("a/b/c/d/e/z/c.md", "b/c/d/e", false)]
    [InlineData("a/b/z/.a", "b/z", false)]
    [InlineData("ab", "*/*", false)]
    [InlineData("ab", "/a", false)]
    [InlineData("ab", "a", false)]
    [InlineData("ab", "b", false)]
    [InlineData("ab", "c", false)]
    [InlineData("abcd", "ab", false)]
    [InlineData("abcd", "bc", false)]
    [InlineData("abcd", "c", false)]
    [InlineData("abcd", "cd", false)]
    [InlineData("abcd", "d", false)]
    [InlineData("abcd", "f", false)]
    [InlineData("ef", "/*", false)]
    public void ShouldReturnFalseWhenPatternDoesNotMatch(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("aaa", "*/*/*", false)]
    [InlineData("aaa/bb/aa/rr", "*/*/*", false)]
    [InlineData("aaa/bba/ccc", "aaa*", false)]
    [InlineData("aaa/bba/ccc", "aaa**", false)]
    [InlineData("aaa/bba/ccc", "aaa/*", false)]
    [InlineData("aaa/bba/ccc", "aaa/*ccc", false)]
    [InlineData("aaa/bba/ccc", "aaa/*z", false)]
    [InlineData("aaa/bbb", "*/*/*", false)]
    [InlineData("ab/zzz/ejkl/hi", "*/*jk*/*i", false)]
    [InlineData("aaa/bba/ccc", "*/*/*", true)]
    [InlineData("aaa/bba/ccc", "aaa/**", true)]
    [InlineData("aaa/bbb", "aaa/*", true)]
    [InlineData("ab/zzz/ejkl/hi", "*/*z*/*/*i", true)]
    [InlineData("abzzzejklhi", "*j*i", true)]
    public void ShouldMatchPathSegmentForEachSingleStar(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a", "*", true)]
    [InlineData("b", "*", true)]
    [InlineData("a/a", "*", false)]
    [InlineData("a/a/a", "*", false)]
    [InlineData("a/a/b", "*", false)]
    [InlineData("a/a/a/a", "*", false)]
    [InlineData("a/a/a/a/a", "*", false)]
    [InlineData("a", "*/*", false)]
    [InlineData("a/a", "*/*", true)]
    [InlineData("a/a/a", "*/*", false)]
    [InlineData("a", "*/*/*", false)]
    [InlineData("a/a", "*/*/*", false)]
    [InlineData("a/a/a", "*/*/*", true)]
    [InlineData("a/a/a/a", "*/*/*", false)]
    [InlineData("a", "*/*/*/*", false)]
    [InlineData("a/a", "*/*/*/*", false)]
    [InlineData("a/a/a", "*/*/*/*", false)]
    [InlineData("a/a/a/a", "*/*/*/*", true)]
    [InlineData("a/a/a/a/a", "*/*/*/*", false)]
    [InlineData("a", "*/*/*/*/*", false)]
    [InlineData("a/a", "*/*/*/*/*", false)]
    [InlineData("a/a/a", "*/*/*/*/*", false)]
    [InlineData("a/a/b", "*/*/*/*/*", false)]
    [InlineData("a/a/a/a", "*/*/*/*/*", false)]
    [InlineData("a/a/a/a/a", "*/*/*/*/*", true)]
    [InlineData("a/a/a/a/a/a", "*/*/*/*/*", false)]
    public void ShouldSupportSingleGlobs(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a", "a/*", false)]
    [InlineData("a/a", "a/*", true)]
    [InlineData("a/a/a", "a/*", false)]
    [InlineData("a/a/a/a", "a/*", false)]
    [InlineData("a/a/a/a/a", "a/*", false)]
    [InlineData("a", "a/*/*", false)]
    [InlineData("a/a", "a/*/*", false)]
    [InlineData("a/a/a", "a/*/*", true)]
    [InlineData("b/a/a", "a/*/*", false)]
    [InlineData("a/a/a/a", "a/*/*", false)]
    [InlineData("a/a/a/a/a", "a/*/*", false)]
    [InlineData("a", "a/*/*/*", false)]
    [InlineData("a/a", "a/*/*/*", false)]
    [InlineData("a/a/a", "a/*/*/*", false)]
    [InlineData("a/a/a/a", "a/*/*/*", true)]
    [InlineData("a/a/a/a/a", "a/*/*/*", false)]
    [InlineData("a", "a/*/*/*/*", false)]
    [InlineData("a/a", "a/*/*/*/*", false)]
    [InlineData("a/a/a", "a/*/*/*/*", false)]
    [InlineData("a/a/b", "a/*/*/*/*", false)]
    [InlineData("a/a/a/a", "a/*/*/*/*", false)]
    [InlineData("a/a/a/a/a", "a/*/*/*/*", true)]
    public void ShouldSupportPrefixedSingleGlobs(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a", "a/*/a", false)]
    [InlineData("a/a", "a/*/a", false)]
    [InlineData("a/a/a", "a/*/a", true)]
    [InlineData("a/a/b", "a/*/a", false)]
    [InlineData("a/a/a/a", "a/*/a", false)]
    [InlineData("a/a/a/a/a", "a/*/a", false)]
    [InlineData("a", "a/*/b", false)]
    [InlineData("a/a", "a/*/b", false)]
    [InlineData("a/a/a", "a/*/b", false)]
    [InlineData("a/a/b", "a/*/b", true)]
    [InlineData("a/a/a/a", "a/*/b", false)]
    [InlineData("a/a/a/a/a", "a/*/b", false)]
    public void ShouldSupportSingleGlobsWithSpecificPaths(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a", "*/**/a", false)]
    [InlineData("a/a/b", "*/**/a", false)]
    [InlineData("a/a", "*/**/a", true)]
    [InlineData("a/a/a", "*/**/a", true)]
    [InlineData("a/a/a/a", "*/**/a", true)]
    [InlineData("a/a/a/a/a", "*/**/a", true)]
    public void ShouldOnlyMatchSingleFolderPerStarWhenGlobstarsAreUsed(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a", "*/", false)]
    [InlineData("a", "*/*", false)]
    [InlineData("a", "a/*", false)]
    [InlineData("a/", "*/*", false)]
    [InlineData("a/", "a/*", false)]
    [InlineData("a/a", "*", false)]
    [InlineData("a/a", "*/", false)]
    [InlineData("a/x/y", "*/", false)]
    [InlineData("a/x/y", "*/*", false)]
    [InlineData("a/x/y", "a/*", false)]
    [InlineData("a/", "*", true)]
    [InlineData("a", "*", true)]
    [InlineData("a/", "*/", true)]
    [InlineData("a/", "*{,/}", true)]
    [InlineData("a/a", "*/*", true)]
    [InlineData("a/a", "a/*", true)]
    public void ShouldNotMatchTrailingSlashWhenStarIsLastChar(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a/", "*", false, true)]
    public void ShouldNotMatchTrailingSlashWhenStarIsLastCharWithStrictSlashes(string input, string pattern, bool expected, bool strictSlashes)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, new GlobbingOptions { StrictSlashes = strictSlashes }));
    }

    [Theory]
    [InlineData("a.txt", "a/**/*.txt", false)]
    [InlineData("a/x/y.txt", "a/**/*.txt", true)]
    [InlineData("a/x/y/z", "a/**/*.txt", false)]
    [InlineData("a.txt", "a/*.txt", false)]
    [InlineData("a/b.txt", "a/*.txt", true)]
    [InlineData("a/x/y.txt", "a/*.txt", false)]
    [InlineData("a/x/y/z", "a/*.txt", false)]
    [InlineData("a.txt", "a*.txt", true)]
    [InlineData("a/b.txt", "a*.txt", false)]
    [InlineData("a/x/y.txt", "a*.txt", false)]
    [InlineData("a/x/y/z", "a*.txt", false)]
    [InlineData("a.txt", "*.txt", true)]
    [InlineData("a/b.txt", "*.txt", false)]
    [InlineData("a/x/y.txt", "*.txt", false)]
    [InlineData("a/x/y/z", "*.txt", false)]
    public void ShouldWorkWithFileExtensions(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("foo/baz/bar", "foo**bar", false)]
    [InlineData("foobazbar", "foo**bar", true)]
    public void ShouldNotMatchSlashesWhenGlobstarsAreNotExclusiveInPathSegment(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("foo", "foo{,/**}", true)]
    public void ShouldMatchSlashesWhenDefinedInBraces(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a/b", "a*", false)]
    [InlineData("a/a/bb", "a/**/b", false)]
    [InlineData("a/bb", "a/**/b", false)]
    [InlineData("foo", "*/**", false)]
    [InlineData("foo/bar", "**/", false)]
    [InlineData("foo/bar", "**/*/", false)]
    [InlineData("foo/bar", "*/*/", false)]
    [InlineData("/home/foo/..", "**/..")]
    [InlineData("a", "**/a", true)]
    [InlineData("a/a", "**", true)]
    [InlineData("a/a", "a/**", true)]
    [InlineData("a/", "a/**", true)]
    [InlineData("a", "a/**", true)]
    [InlineData("a/a", "**/", false)]
    [InlineData("a", "**/a/**", true)]
    [InlineData("a/a", "*/**/a", true)]
    [InlineData("foo/", "*/**", true)]
    [InlineData("foo/bar", "**/*", true)]
    [InlineData("foo/bar", "*/*", true)]
    [InlineData("foo/bar", "*/**", true)]
    [InlineData("foo/bar/", "**/", true)]
    [InlineData("foo/bar/", "**/*", true)]
    [InlineData("foo/bar/", "**/*/", true)]
    [InlineData("foo/bar/", "*/**", true)]
    [InlineData("foo/bar/", "*/*/", true)]
    [InlineData("bar/baz/foo", "*/foo", false)]
    [InlineData("deep/foo/bar", "**/bar/*", false)]
    [InlineData("deep/foo/bar/baz/x", "*/bar/**", false)]
    [InlineData("ef", "/*", false)]
    [InlineData("foo/bar", "foo?bar", false)]
    [InlineData("foo/bar/baz", "**/bar*", false)]
    [InlineData("foo/bar/baz", "**/bar**", false)]
    [InlineData("foo/baz/bar", "foo**bar", false)]
    [InlineData("foo/baz/bar", "foo*bar", false)]
    [InlineData("foo", "foo/**", true)]
    [InlineData("/ab", "/*", true)]
    [InlineData("/cd", "/*", true)]
    [InlineData("/ef", "/*", true)]
    [InlineData("a/b/j/c/z/x.md", "a/**/j/**/z/*.md", true)]
    [InlineData("a/j/z/x.md", "a/**/j/**/z/*.md", true)]
    [InlineData("bar/baz/foo", "**/foo", true)]
    [InlineData("deep/foo/bar/baz", "**/bar/*", true)]
    [InlineData("deep/foo/bar/baz/", "**/bar/**", true)]
    [InlineData("deep/foo/bar/baz/x", "**/bar/*/*", true)]
    [InlineData("foo/b/a/z/bar", "foo/**/**/bar", true)]
    [InlineData("foo/b/a/z/bar", "foo/**/bar", true)]
    [InlineData("foo/bar", "foo/**/**/bar", true)]
    [InlineData("foo/bar", "foo/**/bar", true)]
    [InlineData("foo/bar/baz/x", "*/bar/**", true)]
    [InlineData("foo/baz/bar", "foo/**/**/bar", true)]
    [InlineData("foo/baz/bar", "foo/**/bar", true)]
    [InlineData("XXX/foo", "**/foo", true)]
    public void ShouldCorrectlyMatchSlashes(string input, string pattern, bool expected = true)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("foo/bar/", "**/*", false, true)]
    public void ShouldCorrectlyMatchSlashesWithStrictSlashes(string input, string pattern, bool expected, bool strictSlashes)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, new GlobbingOptions { StrictSlashes = strictSlashes }));
    }

    [Theory]
    [InlineData("ab", "./*", true)]
    [InlineData("ab", "./*/", false)]
    [InlineData("ab/", "./*/", true)]
    public void ShouldIgnoreLeadingDotSlashWhenDefinedOnPattern(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("foo", "**/*", true)]
    [InlineData("foo", "**/*{,/}", true)]
    [InlineData("foo/", "**/*{,/}", true)]
    [InlineData("foo/bar", "**/*{,/}", true)]
    [InlineData("foo/bar/", "**/*{,/}", true)]
    public void ShouldOptionallyMatchTrailingSlashesWithBraces(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

}
