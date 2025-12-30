namespace Snowberry.Globbing.Tests;

/// <summary>
/// Tests for extglob patterns ported from picomatch.
/// </summary>
public class ExtglobsTests
{

    [Theory]
    [InlineData("cbz", "c!(.)z", true)]
    [InlineData("cbz", "c!(*)z", false)]
    [InlineData("cccz", "c!(b*)z", true)]
    [InlineData("cbz", "c!(+)z", true)]
    [InlineData("cbz", "c!(?)z", true)]
    [InlineData("cbz", "c!(@)z", true)]
    public void ShouldEscapeSpecialCharsAfterOpeningParens(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("abc", "!(abc)", false)]
    [InlineData("a", "!(a)", false)]
    [InlineData("aa", "!(a)", true)]
    [InlineData("b", "!(a)", true)]
    public void ShouldSupportNegationExtglobsAsEntirePattern(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("aac", "a!(b)c", true)]
    [InlineData("abc", "a!(b)c", false)]
    [InlineData("acc", "a!(b)c", true)]
    [InlineData("abz", "a!(z)", true)]
    [InlineData("az", "a!(z)", false)]
    public void ShouldSupportNegationExtglobsAsPartOfPattern(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a.", "a!(.)", false)]
    [InlineData(".a", "!(.)a", false)]
    [InlineData("a.c", "a!(.)c", false)]
    [InlineData("abc", "a!(.)c", true)]
    public void ShouldSupportExcludingDotsWithNegationExtglobs(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("/file.d.ts", "/!(*.d).ts", false)]
    [InlineData("/file.ts", "/!(*.d).ts", true)]
    [InlineData("/file.something.ts", "/!(*.d).ts", true)]
    [InlineData("/file.d.something.ts", "/!(*.d).ts", true)]
    [InlineData("/file.dhello.ts", "/!(*.d).ts", true)]
    public void ShouldSupportStarsInNegationExtglobs(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("/file.d.ts", "**/!(*.d).ts", false)]
    [InlineData("/file.ts", "**/!(*.d).ts", true)]
    [InlineData("/file.something.ts", "**/!(*.d).ts", true)]
    [InlineData("/file.d.something.ts", "**/!(*.d).ts", true)]
    [InlineData("/file.dhello.ts", "**/!(*.d).ts", true)]
    public void ShouldSupportStarsInNegationExtglobsWithGlobstars(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("/file.d.ts", "/!(*.d).{ts,tsx}", false)]
    [InlineData("/file.ts", "/!(*.d).{ts,tsx}", true)]
    [InlineData("/file.something.ts", "/!(*.d).{ts,tsx}", true)]
    [InlineData("/file.d.something.ts", "/!(*.d).{ts,tsx}", true)]
    [InlineData("/file.dhello.ts", "/!(*.d).{ts,tsx}", true)]
    public void ShouldSupportStarsInNegationExtglobsWithBraces(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("/file.d.ts", "/!(*.d).@(ts)", false)]
    [InlineData("/file.ts", "/!(*.d).@(ts)", true)]
    [InlineData("/file.something.ts", "/!(*.d).@(ts)", true)]
    [InlineData("/file.d.something.ts", "/!(*.d).@(ts)", true)]
    [InlineData("/file.dhello.ts", "/!(*.d).@(ts)", true)]
    public void ShouldSupportStarsInNegationExtglobsWithAtExtglob(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("foo/abc", "foo/!(abc)", false)]
    [InlineData("foo/bar", "foo/!(abc)", true)]
    [InlineData("a/z", "a/!(z)", false)]
    [InlineData("a/b", "a/!(z)", true)]
    [InlineData("c/z/v", "c/!(z)/v", false)]
    [InlineData("c/a/v", "c/!(z)/v", true)]
    public void ShouldSupportNegationExtglobsInPatternsWithSlashes(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a/a", "!(b/a)", true)]
    [InlineData("b/a", "!(b/a)", false)]
    public void ShouldSupportNegationExtglobsInPatternsWithSlashesInside(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("foo/bar", "!(!(foo))*", false)]
    public void ShouldSupportNestedNegationExtglobsWithStar(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a/a", "(!(b/a))", true)]
    [InlineData("a/a", "!((b/a))", true)]
    [InlineData("b/a", "!((b/a))", false)]
    public void ShouldSupportNegationExtglobsWithInnerParens(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a/a", "(!(?:b/a))", false)]
    [InlineData("b/a", "!((?:b/a))", false)]
    public void ShouldSupportNegationExtglobsWithNonCaptureGroup(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a/a", "!(b/(a))", true)]
    [InlineData("b/a", "!(b/(a))", false)]
    public void ShouldSupportNegationExtglobsWithInnerCaptureGroup(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("c/z", "c!(z)", false)]
    [InlineData("c/z", "c!(z)z", false)]
    [InlineData("c/z", "c!(.)z", false)]
    [InlineData("c/z", "c!(*)z", false)]
    [InlineData("c/z", "c!(+)z", false)]
    [InlineData("c/z", "c!(?)z", false)]
    [InlineData("c/z", "c!(@)z", false)]
    public void ShouldNotMatchSlashesWithExtglobsThatDoNotHaveSlashes(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("c/z", "a!(z)", false)]
    [InlineData("c/z", "c!(/)z", false)]
    [InlineData("c/z", "c!(/z)z", false)]
    [InlineData("c/b", "c!(/z)z", false)]
    [InlineData("c/b/z", "c!(/z)z", true)]
    public void ShouldSupportMatchingSlashesWithExtglobsThatHaveSlashes(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("abc", "!!(abc)", true)]
    [InlineData("abc", "!!!(abc)", false)]
    [InlineData("abc", "!!!!(abc)", true)]
    [InlineData("abc", "!!!!!(abc)", false)]
    public void ShouldSupportNegationExtglobsFollowingExclamation(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("abc", "!(!(abc))", true)]
    [InlineData("abc", "!(!(!(abc)))", false)]
    [InlineData("abc", "!(!(!(!(abc))))", true)]
    [InlineData("abc", "!(!(!(!(!(abc)))))", false)]
    public void ShouldSupportNestedNegationExtglobs(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("foo/abc", "foo/!(!(abc))", true)]
    [InlineData("foo/abc", "foo/!(!(!(abc)))", false)]
    [InlineData("foo/abc", "foo/!(!(!(!(abc))))", true)]
    [InlineData("foo/abc", "foo/!(!(!(!(!(abc)))))", false)]
    [InlineData("foo/abc", "foo/!(!(!(!(!(!(abc))))))", true)]
    [InlineData("foo/abc", "foo/!(!(!(!(!(!(!(abc)))))))", false)]
    [InlineData("foo/abc", "foo/!(!(!(!(!(!(!(!(abc))))))))", true)]
    public void ShouldSupportNestedNegationExtglobsWithPath(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("moo.cow", "!(!(moo)).!(!(cow))", true)]
    public void ShouldSupportMultipleNestedNegationExtglobs(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("moo.cow", "!(moo).!(cow)", false)]
    [InlineData("foo.cow", "!(moo).!(cow)", false)]
    [InlineData("moo.bar", "!(moo).!(cow)", false)]
    [InlineData("foo.bar", "!(moo).!(cow)", true)]
    public void ShouldSupportMultipleNegationExtglobsInPattern(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a   ", "@(!(a) )*", false)]
    [InlineData("a   b", "@(!(a) )*", false)]
    [InlineData("a  b", "@(!(a) )*", false)]
    [InlineData("a  ", "@(!(a) )*", false)]
    [InlineData("a ", "@(!(a) )*", false)]
    [InlineData("a", "@(!(a) )*", false)]
    [InlineData("aa", "@(!(a) )*", false)]
    [InlineData("b", "@(!(a) )*", false)]
    [InlineData("bb", "@(!(a) )*", false)]
    [InlineData(" a ", "@(!(a) )*", true)]
    [InlineData("b  ", "@(!(a) )*", true)]
    [InlineData("b ", "@(!(a) )*", true)]
    public void ShouldSupportMultipleNegationExtglobsWithSpaces(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("c/z", "a*!(z)", false)]
    [InlineData("abz", "a*!(z)", true)]
    [InlineData("az", "a*!(z)", true)]
    public void ShouldSupportStarWithNegationExtglob(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a", "!(a*)", false)]
    [InlineData("aa", "!(a*)", false)]
    [InlineData("ab", "!(a*)", false)]
    [InlineData("b", "!(a*)", true)]
    public void ShouldSupportNegationExtglobWithStarSuffix(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a", "!(*a*)", false)]
    [InlineData("aa", "!(*a*)", false)]
    [InlineData("ab", "!(*a*)", false)]
    [InlineData("ac", "!(*a*)", false)]
    [InlineData("b", "!(*a*)", true)]
    public void ShouldSupportNegationExtglobWithStarsAround(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a", "!(*a)", false)]
    [InlineData("aa", "!(*a)", false)]
    [InlineData("bba", "!(*a)", false)]
    [InlineData("ab", "!(*a)", true)]
    [InlineData("ac", "!(*a)", true)]
    [InlineData("b", "!(*a)", true)]
    public void ShouldSupportNegationExtglobWithStarPrefix(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a", "!(*a)*", false)]
    [InlineData("aa", "!(*a)*", false)]
    [InlineData("bba", "!(*a)*", false)]
    [InlineData("ab", "!(*a)*", false)]
    [InlineData("ac", "!(*a)*", false)]
    [InlineData("b", "!(*a)*", true)]
    public void ShouldSupportNegationExtglobWithStarPrefixAndSuffix(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a", "!(a)*", false)]
    [InlineData("abb", "!(a)*", false)]
    [InlineData("ba", "!(a)*", true)]
    public void ShouldSupportNegationExtglobFollowedByStar(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("aa", "a!(b)*", true)]
    [InlineData("ab", "a!(b)*", false)]
    [InlineData("aba", "a!(b)*", false)]
    [InlineData("ac", "a!(b)*", true)]
    public void ShouldSupportPrefixedNegationExtglobWithStar(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("ac", "!(a|b)c", false)]
    [InlineData("bc", "!(a|b)c", false)]
    [InlineData("cc", "!(a|b)c", true)]
    public void ShouldSupportLogicalOrInsideNegationExtglobs(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("ac.d", "!(a|b)c.!(d|e)", false)]
    [InlineData("bc.d", "!(a|b)c.!(d|e)", false)]
    [InlineData("cc.d", "!(a|b)c.!(d|e)", false)]
    [InlineData("ac.e", "!(a|b)c.!(d|e)", false)]
    [InlineData("bc.e", "!(a|b)c.!(d|e)", false)]
    [InlineData("cc.e", "!(a|b)c.!(d|e)", false)]
    [InlineData("ac.f", "!(a|b)c.!(d|e)", false)]
    [InlineData("bc.f", "!(a|b)c.!(d|e)", false)]
    [InlineData("cc.f", "!(a|b)c.!(d|e)", true)]
    [InlineData("dc.g", "!(a|b)c.!(d|e)", true)]
    public void ShouldSupportMultipleLogicalOrsNegationExtglobs(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData(".md", "@(a|b).md", false)]
    [InlineData("a.js", "@(a|b).md", false)]
    [InlineData("c.md", "@(a|b).md", false)]
    [InlineData("a.md", "@(a|b).md", true)]
    [InlineData("b.md", "@(a|b).md", true)]
    public void ShouldSupportMatchingFileExtensionsWithAt(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData(".md", "+(a|b).md", false)]
    [InlineData("a.js", "+(a|b).md", false)]
    [InlineData("c.md", "+(a|b).md", false)]
    [InlineData("a.md", "+(a|b).md", true)]
    [InlineData("aa.md", "+(a|b).md", true)]
    [InlineData("ab.md", "+(a|b).md", true)]
    [InlineData("b.md", "+(a|b).md", true)]
    [InlineData("bb.md", "+(a|b).md", true)]
    public void ShouldSupportMatchingFileExtensionsWithPlus(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a.js", "*(a|b).md", false)]
    [InlineData("c.md", "*(a|b).md", false)]
    [InlineData(".md", "*(a|b).md", true)]
    [InlineData("a.md", "*(a|b).md", true)]
    [InlineData("aa.md", "*(a|b).md", true)]
    [InlineData("ab.md", "*(a|b).md", true)]
    [InlineData("b.md", "*(a|b).md", true)]
    [InlineData("bb.md", "*(a|b).md", true)]
    public void ShouldSupportMatchingFileExtensionsWithStar(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a.js", "?(a|b).md", false)]
    [InlineData("bb.md", "?(a|b).md", false)]
    [InlineData("c.md", "?(a|b).md", false)]
    [InlineData(".md", "?(a|b).md", true)]
    [InlineData("a.md", "?(a|ab|b).md", true)]
    [InlineData("a.md", "?(a|b).md", true)]
    [InlineData("aa.md", "?(a|aa|b).md", true)]
    [InlineData("ab.md", "?(a|ab|b).md", true)]
    [InlineData("b.md", "?(a|ab|b).md", true)]
    public void ShouldSupportMatchingFileExtensionsWithQuestion(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("ab", "+(a)?(b)", true)]
    [InlineData("aab", "+(a)?(b)", true)]
    [InlineData("aa", "+(a)?(b)", true)]
    [InlineData("a", "+(a)?(b)", true)]
    public void ShouldSupportCombinedExtglobs(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a", "a*(z)", true)]
    [InlineData("az", "a*(z)", true)]
    [InlineData("azz", "a*(z)", true)]
    [InlineData("azzz", "a*(z)", true)]
    [InlineData("abz", "a*(z)", false)]
    [InlineData("cz", "a*(z)", false)]
    [InlineData("a/a", "*(b/a)", false)]
    [InlineData("a/b", "*(b/a)", false)]
    [InlineData("a/c", "*(b/a)", false)]
    [InlineData("b/a", "*(b/a)", true)]
    [InlineData("b/b", "*(b/a)", false)]
    [InlineData("b/c", "*(b/a)", false)]
    [InlineData("cz", "a**(z)", false)]
    [InlineData("abz", "a**(z)", true)]
    [InlineData("az", "a**(z)", true)]
    [InlineData("c/z/v", "*(z)", false)]
    [InlineData("z", "*(z)", true)]
    [InlineData("zf", "*(z)", false)]
    [InlineData("fz", "*(z)", false)]
    [InlineData("c/a/v", "c/*(z)/v", false)]
    [InlineData("c/z/v", "c/*(z)/v", true)]
    [InlineData("a.md.js", "*.*(js).js", false)]
    [InlineData("a.js.js", "*.*(js).js", true)]
    public void ShouldSupportStarExtglobs(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a", "a+(z)", false)]
    [InlineData("az", "a+(z)", true)]
    [InlineData("cz", "a+(z)", false)]
    [InlineData("abz", "a+(z)", false)]
    [InlineData("a+z", "a+(z)", false)]
    [InlineData("a+z", "a++(z)", true)]
    [InlineData("c+z", "a+(z)", false)]
    [InlineData("a+bz", "a+(z)", false)]
    [InlineData("az", "+(z)", false)]
    [InlineData("cz", "+(z)", false)]
    [InlineData("abz", "+(z)", false)]
    [InlineData("fz", "+(z)", false)]
    [InlineData("z", "+(z)", true)]
    [InlineData("zz", "+(z)", true)]
    [InlineData("c/z/v", "c/+(z)/v", true)]
    [InlineData("c/zz/v", "c/+(z)/v", true)]
    [InlineData("c/a/v", "c/+(z)/v", false)]
    public void ShouldSupportPlusExtglobs(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a?z", "a??(z)", true)]
    [InlineData("a.z", "a??(z)", true)]
    [InlineData("a/z", "a??(z)", false)]
    [InlineData("a?", "a??(z)", true)]
    [InlineData("ab", "a??(z)", true)]
    [InlineData("a/", "a??(z)", false)]
    [InlineData("a?z", "a?(z)", false)]
    [InlineData("abz", "a?(z)", false)]
    [InlineData("z", "a?(z)", false)]
    [InlineData("a", "a?(z)", true)]
    [InlineData("az", "a?(z)", true)]
    [InlineData("abz", "?(z)", false)]
    [InlineData("az", "?(z)", false)]
    [InlineData("cz", "?(z)", false)]
    [InlineData("fz", "?(z)", false)]
    [InlineData("zz", "?(z)", false)]
    [InlineData("z", "?(z)", true)]
    [InlineData("c/a/v", "c/?(z)/v", false)]
    [InlineData("c/zz/v", "c/?(z)/v", false)]
    [InlineData("c/z/v", "c/?(z)/v", true)]
    public void ShouldSupportQuestionExtglobs(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("c/z/v", "c/@(z)/v", true)]
    [InlineData("c/a/v", "c/@(z)/v", false)]
    [InlineData("moo.cow", "@(*.*)", true)]
    [InlineData("cz", "a*@(z)", false)]
    [InlineData("abz", "a*@(z)", true)]
    [InlineData("az", "a*@(z)", true)]
    [InlineData("cz", "a@(z)", false)]
    [InlineData("abz", "a@(z)", false)]
    [InlineData("az", "a@(z)", true)]
    public void ShouldSupportAtExtglobs(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("aa.aa", "(b|a).(a)", false)]
    [InlineData("a.bb", "(b|a).(a)", false)]
    [InlineData("a.aa.a", "(b|a).(a)", false)]
    [InlineData("cc.a", "(b|a).(a)", false)]
    [InlineData("a.a", "(b|a).(a)", true)]
    [InlineData("c.a", "(b|a).(a)", false)]
    [InlineData("dd.aa.d", "(b|a).(a)", false)]
    [InlineData("b.a", "(b|a).(a)", true)]
    public void ShouldMatchExactlyOneOfTheGivenPattern(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("aa.aa", "@(b|a).@(a)", false)]
    [InlineData("a.bb", "@(b|a).@(a)", false)]
    [InlineData("a.aa.a", "@(b|a).@(a)", false)]
    [InlineData("cc.a", "@(b|a).@(a)", false)]
    [InlineData("a.a", "@(b|a).@(a)", true)]
    [InlineData("c.a", "@(b|a).@(a)", false)]
    [InlineData("dd.aa.d", "@(b|a).@(a)", false)]
    [InlineData("b.a", "@(b|a).@(a)", true)]
    public void ShouldMatchExactlyOneOfTheGivenPatternWithAt(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("ax", "a?(b*)", false)]
    [InlineData("ax", "?(a*|b)", true)]
    public void ShouldSupportQuestionExtglobsEndingWithStatechar(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("ax", "a*(b*)", false)]
    [InlineData("ax", "*(a*|b)", true)]
    public void ShouldSupportStarExtglobsEndingWithStatechar(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("ax", "a@(b*)", false)]
    [InlineData("ax", "@(a*|b)", true)]
    public void ShouldSupportAtExtglobsEndingWithStatechar(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("ax", "a!(b*)", true)]
    [InlineData("ax", "!(a*|b)", false)]
    public void ShouldSupportNegationExtglobsEndingWithStatechar(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("137577991", "*(0|1|3|5|7|9)", true)]
    [InlineData("2468", "*(0|1|3|5|7|9)", false)]
    [InlineData("file.c", "*.c?(c)", true)]
    [InlineData("file.C", "*.c?(c)", false)]
    [InlineData("file.cc", "*.c?(c)", true)]
    [InlineData("file.ccc", "*.c?(c)", false)]
    [InlineData("VMS.FILE;", "*\\;[1-9]*([0-9])", false)]
    [InlineData("VMS.FILE;0", "*\\;[1-9]*([0-9])", false)]
    [InlineData("VMS.FILE;1", "*\\;[1-9]*([0-9])", true)]
    [InlineData("VMS.FILE;139", "*\\;[1-9]*([0-9])", true)]
    [InlineData("VMS.FILE;1N", "*\\;[1-9]*([0-9])", false)]
    public void TestsFromRosenblattKornShellBook(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("parse.y", "!(*.c|*.h|Makefile.in|config*|README)", true)]
    [InlineData("shell.c", "!(*.c|*.h|Makefile.in|config*|README)", false)]
    [InlineData("Makefile", "!(*.c|*.h|Makefile.in|config*|README)", true)]
    [InlineData("Makefile.in", "!(*.c|*.h|Makefile.in|config*|README)", false)]
    public void TestsFromRosenblattKornShellBookNegation(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("abcx", "!([*)*", true)]
    [InlineData("abcz", "!([*)*", true)]
    [InlineData("bbc", "!([*)*", true)]
    [InlineData("abcx", "!([[*])*", true)]
    [InlineData("abcz", "!([[*])*", true)]
    [InlineData("bbc", "!([[*])*", true)]
    public void TestsDerivedFromPdKshTestSuiteNegation(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("abcx", "+(a|b\\[)*", true)]
    [InlineData("abcz", "+(a|b\\[)*", true)]
    [InlineData("bbc", "+(a|b\\[)*", false)]
    [InlineData("abcx", "+(a|b[)*", true)]
    [InlineData("abcz", "+(a|b[)*", true)]
    [InlineData("bbc", "+(a|b[)*", false)]
    public void TestsDerivedFromPdKshTestSuitePlus(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("abcx", "[a*(]*z", false)]
    [InlineData("abcz", "[a*(]*z", true)]
    [InlineData("bbc", "[a*(]*z", false)]
    [InlineData("aaz", "[a*(]*z", true)]
    [InlineData("aaaz", "[a*(]*z", true)]
    public void TestsDerivedFromPdKshTestSuiteBrackets(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("abcx", "[a*(]*)z", false)]
    [InlineData("abcz", "[a*(]*)z", false)]
    [InlineData("bbc", "[a*(]*)z", false)]
    public void TestsDerivedFromPdKshTestSuiteBracketsWithCloseParen(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("abc", "+()c", false)]
    [InlineData("abc", "+()x", false)]
    [InlineData("abc", "+(*)c", true)]
    [InlineData("abc", "+(*)x", false)]
    [InlineData("abc", "no-file+(a|b)stuff", false)]
    [InlineData("abc", "no-file+(a*(c)|b)stuff", false)]
    public void TestsDerivedFromPdKshTestSuiteEmptyAndNoMatch(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("abd", "a+(b|c)d", true)]
    [InlineData("acd", "a+(b|c)d", true)]
    [InlineData("abc", "a+(b|c)d", false)]
    public void TestsDerivedFromPdKshTestSuitePlusWithOr(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("abd", "a!(b|B)", true)]
    [InlineData("acd", "a!(@(b|B))", true)]
    [InlineData("ac", "a!(@(b|B))", true)]
    [InlineData("ab", "a!(@(b|B))", false)]
    public void TestsDerivedFromPdKshTestSuiteNegationWithOr(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("abc", "a!(@(b|B))d", false)]
    [InlineData("abd", "a!(@(b|B))d", false)]
    [InlineData("acd", "a!(@(b|B))d", true)]
    public void TestsDerivedFromPdKshTestSuiteNegationWithOrAndSuffix(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("abd", "a[b*(foo|bar)]d", true)]
    [InlineData("abc", "a[b*(foo|bar)]d", false)]
    [InlineData("acd", "a[b*(foo|bar)]d", false)]
    public void TestsDerivedFromPdKshTestSuiteBracketWithExtglob(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("para", "para+([0-9])", false)]
    [InlineData("para381", "para?([345]|99)1", false)]
    [InlineData("paragraph", "para*([0-9])", false)]
    [InlineData("paramour", "para@(chute|graph)", false)]
    [InlineData("para", "para*([0-9])", true)]
    [InlineData("para.38", "para!(*.[0-9])", true)]
    [InlineData("para.38", "para!(*.[00-09])", true)]
    [InlineData("para.graph", "para!(*.[0-9])", true)]
    [InlineData("para13829383746592", "para*([0-9])", true)]
    [InlineData("para39", "para!(*.[0-9])", true)]
    [InlineData("para987346523", "para+([0-9])", true)]
    [InlineData("para991", "para?([345]|99)1", true)]
    [InlineData("paragraph", "para!(*.[0-9])", true)]
    [InlineData("paragraph", "para@(chute|graph)", true)]
    public void StuffFromKornsBook(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("foo", "*(a|b[)", false)]
    [InlineData("(", "*(a|b[)", false)]
    [InlineData(")", "*(a|b[)", false)]
    [InlineData("|", "*(a|b[)", false)]
    [InlineData("a", "*(a|b)", true)]
    [InlineData("b", "*(a|b)", true)]
    [InlineData("b[", "*(a|b\\[)", true)]
    [InlineData("ab[", "+(a|b\\[)", true)]
    [InlineData("ab[cde", "+(a|b\\[)", false)]
    [InlineData("ab[cde", "+(a|b\\[)*", true)]
    [InlineData("foo", "*(a|b|f)*", true)]
    [InlineData("foo", "*(a|b|o)*", true)]
    [InlineData("foo", "*(a|b|f|o)", true)]
    [InlineData("*(a|b[)", "\\*\\(a\\|b\\[\\)", true)]
    [InlineData("foo", "*(a|b)", false)]
    [InlineData("foo", "*(a|b\\[)", false)]
    [InlineData("foo", "*(a|b\\[)|f*", true)]
    public void SimpleKleeneStarTests(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("moo.cow", "@(*).@(*)", true)]
    [InlineData("a.a", "*.@(a|b|@(ab|a*@(b))*@(c)d)", true)]
    [InlineData("a.b", "*.@(a|b|@(ab|a*@(b))*@(c)d)", true)]
    [InlineData("a.c", "*.@(a|b|@(ab|a*@(b))*@(c)d)", false)]
    [InlineData("a.c.d", "*.@(a|b|@(ab|a*@(b))*@(c)d)", false)]
    [InlineData("c.c", "*.@(a|b|@(ab|a*@(b))*@(c)d)", false)]
    [InlineData("a.", "*.@(a|b|@(ab|a*@(b))*@(c)d)", false)]
    [InlineData("d.d", "*.@(a|b|@(ab|a*@(b))*@(c)d)", false)]
    [InlineData("e.e", "*.@(a|b|@(ab|a*@(b))*@(c)d)", false)]
    [InlineData("f.f", "*.@(a|b|@(ab|a*@(b))*@(c)d)", false)]
    [InlineData("a.abcd", "*.@(a|b|@(ab|a*@(b))*@(c)d)", true)]
    public void ShouldSupportMultipleExtglobs(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a.a", "!(*.a|*.b|*.c)", false)]
    [InlineData("a.b", "!(*.a|*.b|*.c)", false)]
    [InlineData("a.c", "!(*.a|*.b|*.c)", false)]
    [InlineData("a.c.d", "!(*.a|*.b|*.c)", true)]
    [InlineData("c.c", "!(*.a|*.b|*.c)", false)]
    [InlineData("a.", "!(*.a|*.b|*.c)", true)]
    [InlineData("d.d", "!(*.a|*.b|*.c)", true)]
    [InlineData("e.e", "!(*.a|*.b|*.c)", true)]
    [InlineData("f.f", "!(*.a|*.b|*.c)", true)]
    [InlineData("a.abcd", "!(*.a|*.b|*.c)", true)]
    public void ShouldSupportMultipleExtglobsWithNegation(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a.a", "!(*.[^a-c])", true)]
    [InlineData("a.b", "!(*.[^a-c])", true)]
    [InlineData("a.c", "!(*.[^a-c])", true)]
    [InlineData("a.c.d", "!(*.[^a-c])", false)]
    [InlineData("c.c", "!(*.[^a-c])", true)]
    [InlineData("a.", "!(*.[^a-c])", true)]
    [InlineData("d.d", "!(*.[^a-c])", false)]
    [InlineData("e.e", "!(*.[^a-c])", false)]
    [InlineData("f.f", "!(*.[^a-c])", false)]
    [InlineData("a.abcd", "!(*.[^a-c])", true)]
    public void ShouldSupportNegatedBracketNotRange(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a.a", "!(*.[a-c])", false)]
    [InlineData("a.b", "!(*.[a-c])", false)]
    [InlineData("a.c", "!(*.[a-c])", false)]
    [InlineData("a.c.d", "!(*.[a-c])", true)]
    [InlineData("c.c", "!(*.[a-c])", false)]
    [InlineData("a.", "!(*.[a-c])", true)]
    [InlineData("d.d", "!(*.[a-c])", true)]
    [InlineData("e.e", "!(*.[a-c])", true)]
    [InlineData("f.f", "!(*.[a-c])", true)]
    [InlineData("a.abcd", "!(*.[a-c])", true)]
    public void ShouldSupportNegatedBracketRange(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a.a", "!(*.[a-c]*)", false)]
    [InlineData("a.b", "!(*.[a-c]*)", false)]
    [InlineData("a.c", "!(*.[a-c]*)", false)]
    [InlineData("a.c.d", "!(*.[a-c]*)", false)]
    [InlineData("c.c", "!(*.[a-c]*)", false)]
    [InlineData("a.", "!(*.[a-c]*)", true)]
    [InlineData("d.d", "!(*.[a-c]*)", true)]
    [InlineData("e.e", "!(*.[a-c]*)", true)]
    [InlineData("f.f", "!(*.[a-c]*)", true)]
    [InlineData("a.abcd", "!(*.[a-c]*)", false)]
    public void ShouldSupportNegatedBracketRangeWithStar(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a.a", "*.!(a|b|c)", false)]
    [InlineData("a.b", "*.!(a|b|c)", false)]
    [InlineData("a.c", "*.!(a|b|c)", false)]
    [InlineData("a.c.d", "*.!(a|b|c)", true)]
    [InlineData("c.c", "*.!(a|b|c)", false)]
    [InlineData("a.", "*.!(a|b|c)", true)]
    [InlineData("d.d", "*.!(a|b|c)", true)]
    [InlineData("e.e", "*.!(a|b|c)", true)]
    [InlineData("f.f", "*.!(a|b|c)", true)]
    [InlineData("a.abcd", "*.!(a|b|c)", true)]
    public void ShouldSupportExtglobNegationAfterDot(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a.a", "*!(.a|.b|.c)", true)]
    [InlineData("a.b", "*!(.a|.b|.c)", true)]
    [InlineData("a.c", "*!(.a|.b|.c)", true)]
    [InlineData("a.c.d", "*!(.a|.b|.c)", true)]
    [InlineData("c.c", "*!(.a|.b|.c)", true)]
    [InlineData("a.", "*!(.a|.b|.c)", true)]
    [InlineData("d.d", "*!(.a|.b|.c)", true)]
    [InlineData("e.e", "*!(.a|.b|.c)", true)]
    [InlineData("f.f", "*!(.a|.b|.c)", true)]
    [InlineData("a.abcd", "*!(.a|.b|.c)", true)]
    public void ShouldSupportStarExtglobNegationWithDots(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a.a", "!(*.[a-c])*", false)]
    [InlineData("a.b", "!(*.[a-c])*", false)]
    [InlineData("a.c", "!(*.[a-c])*", false)]
    [InlineData("a.c.d", "!(*.[a-c])*", false)]
    [InlineData("c.c", "!(*.[a-c])*", false)]
    [InlineData("a.", "!(*.[a-c])*", true)]
    [InlineData("d.d", "!(*.[a-c])*", true)]
    [InlineData("e.e", "!(*.[a-c])*", true)]
    [InlineData("f.f", "!(*.[a-c])*", true)]
    [InlineData("a.abcd", "!(*.[a-c])*", false)]
    public void ShouldSupportNegationExtglobWithBracketRangeStar(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a.a", "*!(.a|.b|.c)*", true)]
    [InlineData("a.b", "*!(.a|.b|.c)*", true)]
    [InlineData("a.c", "*!(.a|.b|.c)*", true)]
    [InlineData("a.c.d", "*!(.a|.b|.c)*", true)]
    [InlineData("c.c", "*!(.a|.b|.c)*", true)]
    [InlineData("a.", "*!(.a|.b|.c)*", true)]
    [InlineData("d.d", "*!(.a|.b|.c)*", true)]
    [InlineData("e.e", "*!(.a|.b|.c)*", true)]
    [InlineData("f.f", "*!(.a|.b|.c)*", true)]
    [InlineData("a.abcd", "*!(.a|.b|.c)*", true)]
    public void ShouldSupportStarExtglobNegationWithDotsStar(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a.a", "*.!(a|b|c)*", false)]
    [InlineData("a.b", "*.!(a|b|c)*", false)]
    [InlineData("a.c", "*.!(a|b|c)*", false)]
    [InlineData("a.c.d", "*.!(a|b|c)*", true)]
    [InlineData("c.c", "*.!(a|b|c)*", false)]
    [InlineData("a.", "*.!(a|b|c)*", true)]
    [InlineData("d.d", "*.!(a|b|c)*", true)]
    [InlineData("e.e", "*.!(a|b|c)*", true)]
    [InlineData("f.f", "*.!(a|b|c)*", true)]
    [InlineData("a.abcd", "*.!(a|b|c)*", false)]
    public void ShouldSupportExtglobNegationAfterDotStar(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("def", "@()ef", false)]
    [InlineData("ef", "@()ef", true)]
    [InlineData("def", "()ef", false)]
    [InlineData("ef", "()ef", true)]
    public void ShouldCorrectlyMatchEmptyParens(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a(b", "a(b", true)]
    [InlineData("a(b", "a\\(b", true)]
    [InlineData("a((b", "a(b", false)]
    [InlineData("a((((b", "a(b", false)]
    [InlineData("ab", "a(b", false)]
    [InlineData("a(b", "a(*b", true)]
    [InlineData("a(ab", "a\\(*b", true)]
    [InlineData("a((b", "a(*b", true)]
    [InlineData("a((((b", "a(*b", true)]
    [InlineData("ab", "a(*b", false)]
    public void ShouldMatchEscapedParens(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a", "!(a/**)", true)]
    [InlineData("a/", "!(a/**)", false)]
    [InlineData("a/b", "!(a/**)", false)]
    [InlineData("a/b/c", "!(a/**)", false)]
    [InlineData("b", "!(a/**)", true)]
    [InlineData("b/c", "!(a/**)", true)]
    public void ShouldMatchNestedDirectoriesWithNegationExtglobs(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a/a", "a/!(b*)", true)]
    [InlineData("a/b", "a/!(b*)", false)]
    [InlineData("a/b/c", "a/!(b/*)")]
    [InlineData("a/b/c", "a/!(b*)", false)]
    [InlineData("a/c", "a/!(b*)", true)]
    public void ShouldMatchNestedDirectoriesWithNegationExtglobsAndSlashes(string input, string pattern, bool expected = false)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a/a/", "a/!(b*)/**", true)]
    [InlineData("a/a", "a/!(b*)", true)]
    [InlineData("a/a", "a/!(b*)/**", true)]
    [InlineData("a/b", "a/!(b*)/**", false)]
    [InlineData("a/b/c", "a/!(b*)/**", false)]
    [InlineData("a/c", "a/!(b*)/**", true)]
    [InlineData("a/c", "a/!(b*)", true)]
    [InlineData("a/c/", "a/!(b*)/**", true)]
    public void ShouldMatchNestedDirectoriesWithNegationExtglobsAndGlobstars(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

}
