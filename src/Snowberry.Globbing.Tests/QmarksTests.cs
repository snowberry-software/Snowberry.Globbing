namespace Snowberry.Globbing.Tests;

/// <summary>
/// Tests for question mark (?) pattern ported from picomatch.
/// </summary>
public class QmarksTests
{

    [Theory]
    [InlineData("a", "?", true)]
    [InlineData("b", "?", true)]
    [InlineData("c", "?", true)]
    [InlineData("aa", "?", false)]
    [InlineData("ab", "?", false)]
    [InlineData("abc", "?", false)]
    public void SingleQmarkShouldMatchSingleCharacter(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("aa", "??", true)]
    [InlineData("ab", "??", true)]
    [InlineData("zz", "??", true)]
    [InlineData("a", "??", false)]
    [InlineData("abc", "??", false)]
    [InlineData("abcd", "??", false)]
    public void DoubleQmarkShouldMatchTwoCharacters(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("abc", "???", true)]
    [InlineData("def", "???", true)]
    [InlineData("xyz", "???", true)]
    [InlineData("ab", "???", false)]
    [InlineData("abcd", "???", false)]
    public void TripleQmarkShouldMatchThreeCharacters(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("aa", "a?", true)]
    [InlineData("ab", "a?", true)]
    [InlineData("az", "a?", true)]
    [InlineData("a", "a?", false)]
    [InlineData("abc", "a?", false)]
    [InlineData("ba", "a?", false)]
    public void QmarkWithPrefixShouldMatch(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("aa", "?a", true)]
    [InlineData("ba", "?a", true)]
    [InlineData("za", "?a", true)]
    [InlineData("a", "?a", false)]
    [InlineData("ab", "?a", false)]
    [InlineData("abc", "?a", false)]
    public void QmarkWithSuffixShouldMatch(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("aXb", "a?b", true)]
    [InlineData("aYb", "a?b", true)]
    [InlineData("aab", "a?b", true)]
    [InlineData("ab", "a?b", false)]
    [InlineData("aXXb", "a?b", false)]
    public void QmarkBetweenCharsShouldMatch(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("ab", "?*", true)]
    [InlineData("abc", "?*", true)]
    [InlineData("abcd", "?*", true)]
    [InlineData("a", "?*", true)]
    [InlineData("", "?*", false)]
    public void QmarkWithStarShouldMatchOneOrMoreChars(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a.txt", "?.txt", true)]
    [InlineData("b.txt", "?.txt", true)]
    [InlineData("ab.txt", "?.txt", false)]
    [InlineData(".txt", "?.txt", false)]
    public void QmarkInFileNameShouldMatch(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a.t", "?.*", true)]
    [InlineData("b.txt", "?.*", true)]
    [InlineData("c.json", "?.*", true)]
    [InlineData("ab.txt", "?.*", false)]
    [InlineData("a", "?.*", false)]
    public void QmarkWithDotStarShouldMatch(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("abc.txt", "*?.txt", true)]
    [InlineData("ab.txt", "*?.txt", true)]
    [InlineData("a.txt", "*?.txt", true)]
    [InlineData(".txt", "*?.txt", false)]
    public void StarWithQmarkInExtensionShouldMatch(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a/b", "a?b", false)]
    [InlineData("a/b", "?/?", true)]
    [InlineData("a/b/c", "?/?/?", true)]
    public void QmarkShouldNotMatchSlash(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a/b", "???", false)]
    [InlineData("a/", "??", false)]
    [InlineData("/b", "??", false)]
    public void MultipleQmarksShouldNotMatchSlash(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a/b", "?/b", true)]
    [InlineData("x/b", "?/b", true)]
    [InlineData("ab/b", "?/b", false)]
    [InlineData("a/c", "?/b", false)]
    public void QmarkInPathSegmentShouldMatch(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a/b/c", "?/b/?", true)]
    [InlineData("x/b/y", "?/b/?", true)]
    [InlineData("a/b/cd", "?/b/?", false)]
    [InlineData("ab/b/c", "?/b/?", false)]
    public void QmarksInMultiplePathSegmentsShouldMatch(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a/bc/d", "?/??/?", true)]
    [InlineData("x/ab/y", "?/??/?", true)]
    [InlineData("a/b/c", "?/??/?", false)]
    [InlineData("ab/bc/d", "?/??/?", false)]
    public void MixedQmarksInPathShouldMatch(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData(".a", "?a", false)]
    [InlineData(".a", "?*", false)]
    [InlineData(".abc", "????", false)]
    public void QmarkShouldNotMatchDotAtStartByDefault(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData(".a", "?a", true)]
    [InlineData(".a", "?*", true)]
    [InlineData(".abc", "????", true)]
    public void QmarkShouldMatchDotAtStartWithDotOption(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { Dot = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("a/.b", "a/?b", false)]
    [InlineData("a/.bc", "a/???", false)]
    public void QmarkShouldNotMatchDotAtStartOfPathSegmentByDefault(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a/.b", "a/?b", true)]
    [InlineData("a/.bc", "a/???", true)]
    public void QmarkShouldMatchDotAtStartOfPathSegmentWithDotOption(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { Dot = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("?", "\\?", true)]
    [InlineData("a", "\\?", false)]
    [InlineData("??", "\\?\\?", true)]
    [InlineData("ab", "\\?\\?", false)]
    public void EscapedQmarkShouldMatchLiteralQuestionMark(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a?b", "a\\?b", true)]
    [InlineData("aXb", "a\\?b", false)]
    [InlineData("ab", "a\\?b", false)]
    public void EscapedQmarkMixedWithLiteralsShouldMatch(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a?", "?\\?", true)]
    [InlineData("b?", "?\\?", true)]
    [InlineData("aa", "?\\?", false)]
    [InlineData("a", "?\\?", false)]
    public void MixedQmarkAndEscapedQmarkShouldMatch(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("?", "?", true)]
    [InlineData("??", "?", false)]
    [InlineData("???", "?", false)]
    [InlineData("?", "??", false)]
    [InlineData("??", "??", true)]
    [InlineData("???", "??", false)]
    [InlineData("?", "???", false)]
    [InlineData("??", "???", false)]
    [InlineData("???", "???", true)]
    public void ShouldMatchQuestionMarksWithQuestionMarks(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("?", "?*", true)]
    [InlineData("??", "?*", true)]
    [InlineData("???", "?*", true)]
    [InlineData("?", "*?", true)]
    [InlineData("??", "*?", true)]
    [InlineData("???", "*?", true)]
    [InlineData("?", "?*?", false)]
    [InlineData("??", "?*?", true)]
    [InlineData("???", "?*?", true)]
    [InlineData("?*", "?*", true)]
    [InlineData("?*?", "?*", true)]
    [InlineData("?*?*?", "?*", true)]
    [InlineData("?*", "*?", true)]
    [InlineData("?*?", "*?", true)]
    [InlineData("?*?*?", "*?", true)]
    [InlineData("?*", "?*?", true)]
    [InlineData("?*?", "?*?", true)]
    [InlineData("?*?*?", "?*?", true)]
    public void ShouldMatchQuestionMarksAndStarsWithQuestionMarksAndStars(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("aaa", "a*?c", false)]
    [InlineData("aac", "a*?c", true)]
    [InlineData("abc", "a*?c", true)]
    [InlineData("abc", "a**?c", true)]
    [InlineData("abb", "a**?c", false)]
    [InlineData("acc", "a**?c", true)]
    [InlineData("abc", "a*****?c", true)]
    [InlineData("aaaabbbbbbccccc", "a*****?c", true)]
    [InlineData("a", "*****?", true)]
    [InlineData("ab", "*****?", true)]
    [InlineData("abc", "*****?", true)]
    [InlineData("abcd", "*****?", true)]
    [InlineData("a", "*****??", false)]
    [InlineData("ab", "*****??", true)]
    [InlineData("abc", "*****??", true)]
    [InlineData("abcd", "*****??", true)]
    [InlineData("a", "?*****??", false)]
    [InlineData("ab", "?*****??", false)]
    [InlineData("abc", "?*****??", true)]
    [InlineData("abcd", "?*****??", true)]
    [InlineData("abc", "?*****?c", true)]
    [InlineData("abb", "?*****?c", false)]
    [InlineData("zzz", "?*****?c", false)]
    [InlineData("abc", "?***?****?", true)]
    [InlineData("bbb", "?***?****?", true)]
    [InlineData("zzz", "?***?****?", true)]
    [InlineData("abc", "?***?****c", true)]
    [InlineData("bbb", "?***?****c", false)]
    [InlineData("zzz", "?***?****c", false)]
    [InlineData("abc", "*******?", true)]
    [InlineData("abc", "*******c", true)]
    [InlineData("abc", "?***?****", true)]
    [InlineData("abcdecdhjk", "a****c**?**??*****", true)]
    [InlineData("abcdecdhjk", "a**?**cd**?**??***k", true)]
    [InlineData("abcdecdhjk", "a**?**cd**?**??***k**", true)]
    [InlineData("abcdecdhjk", "a**?**cd**?**??k", true)]
    [InlineData("abcdecdhjk", "a**?**cd**?**??k***", true)]
    [InlineData("abcdecdhjk", "a*cd**?**??k", true)]
    public void ShouldSupportConsecutiveStarsAndQuestionMarks(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a", "?", true)]
    [InlineData("aa", "?", false)]
    [InlineData("ab", "?", false)]
    [InlineData("aaa", "?", false)]
    [InlineData("abcdefg", "?", false)]
    [InlineData("a", "??", false)]
    [InlineData("aa", "??", true)]
    [InlineData("ab", "??", true)]
    [InlineData("aaa", "??", false)]
    [InlineData("abcdefg", "??", false)]
    [InlineData("a", "???", false)]
    [InlineData("aa", "???", false)]
    [InlineData("ab", "???", false)]
    [InlineData("aaa", "???", true)]
    [InlineData("abcdefg", "???", false)]
    [InlineData("a/", "??", false)]
    [InlineData("/a/", "??", false)]
    [InlineData("/a/b/", "??", false)]
    [InlineData("/a/b/c/", "??", false)]
    [InlineData("/a/b/c/d/", "??", false)]
    [InlineData("a/b/c.md", "a/?/c.md", true)]
    [InlineData("a/bb/c.md", "a/?/c.md", false)]
    [InlineData("a/bb/c.md", "a/??/c.md", true)]
    [InlineData("a/bbb/c.md", "a/??/c.md", false)]
    [InlineData("a/bbb/c.md", "a/???/c.md", true)]
    [InlineData("a/bbbb/c.md", "a/????/c.md", true)]
    public void ShouldMatchOneCharacterPerQuestionMark(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("//", "/?", false)]
    [InlineData("a/", "/?", false)]
    [InlineData("/a", "/?", true)]
    [InlineData("/a/", "/?", false)]
    [InlineData("aa", "/?", false)]
    [InlineData("/aa", "/?", false)]
    [InlineData("a/a", "/?", false)]
    [InlineData("aaa", "/?", false)]
    [InlineData("/aaa", "/?", false)]
    [InlineData("//", "/??", false)]
    [InlineData("a/", "/??", false)]
    [InlineData("/a", "/??", false)]
    [InlineData("/a/", "/??", false)]
    [InlineData("aa", "/??", false)]
    [InlineData("/aa", "/??", true)]
    [InlineData("a/a", "/??", false)]
    [InlineData("aaa", "/??", false)]
    [InlineData("/aaa", "/??", false)]
    [InlineData("//", "/???", false)]
    [InlineData("a/", "/???", false)]
    [InlineData("/a", "/???", false)]
    [InlineData("/a/", "/???", false)]
    [InlineData("aa", "/???", false)]
    [InlineData("/aa", "/???", false)]
    [InlineData("a/a", "/???", false)]
    [InlineData("aaa", "/???", false)]
    [InlineData("/aaa", "/???", true)]
    [InlineData("//", "/?/", false)]
    [InlineData("a/", "/?/", false)]
    [InlineData("/a", "/?/", false)]
    [InlineData("/a/", "/?/", true)]
    [InlineData("aa", "/?/", false)]
    [InlineData("/aa", "/?/", false)]
    [InlineData("a/a", "/?/", false)]
    [InlineData("aaa", "/?/", false)]
    [InlineData("/aaa", "/?/", false)]
    [InlineData("//", "??", false)]
    [InlineData("a/", "??", false)]
    [InlineData("/a", "??", false)]
    [InlineData("/a/", "??", false)]
    [InlineData("aa", "??", true)]
    [InlineData("/aa", "??", false)]
    [InlineData("a/a", "??", false)]
    [InlineData("aaa", "??", false)]
    [InlineData("/aaa", "??", false)]
    [InlineData("//", "?/?", false)]
    [InlineData("a/", "?/?", false)]
    [InlineData("/a", "?/?", false)]
    [InlineData("/a/", "?/?", false)]
    [InlineData("aa", "?/?", false)]
    [InlineData("/aa", "?/?", false)]
    [InlineData("a/a", "?/?", true)]
    [InlineData("aaa", "?/?", false)]
    [InlineData("/aaa", "?/?", false)]
    [InlineData("//", "???", false)]
    [InlineData("a/", "???", false)]
    [InlineData("/a", "???", false)]
    [InlineData("/a/", "???", false)]
    [InlineData("aa", "???", false)]
    [InlineData("/aa", "???", false)]
    [InlineData("a/a", "???", false)]
    [InlineData("aaa", "???", true)]
    [InlineData("/aaa", "???", false)]
    [InlineData("//", "a?a", false)]
    [InlineData("a/", "a?a", false)]
    [InlineData("/a", "a?a", false)]
    [InlineData("/a/", "a?a", false)]
    [InlineData("aa", "a?a", false)]
    [InlineData("/aa", "a?a", false)]
    [InlineData("a/a", "a?a", false)]
    [InlineData("aaa", "a?a", true)]
    [InlineData("/aaa", "a?a", false)]
    [InlineData("//", "aa?", false)]
    [InlineData("a/", "aa?", false)]
    [InlineData("/a", "aa?", false)]
    [InlineData("/a/", "aa?", false)]
    [InlineData("aa", "aa?", false)]
    [InlineData("/aa", "aa?", false)]
    [InlineData("a/a", "aa?", false)]
    [InlineData("aaa", "aa?", true)]
    [InlineData("/aaa", "aa?", false)]
    [InlineData("//", "?aa", false)]
    [InlineData("a/", "?aa", false)]
    [InlineData("/a", "?aa", false)]
    [InlineData("/a/", "?aa", false)]
    [InlineData("aa", "?aa", false)]
    [InlineData("/aa", "?aa", false)]
    [InlineData("a/a", "?aa", false)]
    [InlineData("aaa", "?aa", true)]
    [InlineData("/aaa", "?aa", false)]
    public void ShouldNotMatchSlashesQuestionMarks(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a/b.bb/c/d/efgh.ijk/e", "a/*/?/**/e", true)]
    [InlineData("a/b/c/d/e", "a/?/c/?/*/e", false)]
    [InlineData("a/b/c/d/e/e", "a/?/c/?/*/e", true)]
    [InlineData("a/b/c/d/efgh.ijk/e", "a/*/?/**/e", true)]
    [InlineData("a/b/c/d/efghijk/e", "a/*/?/**/e", true)]
    [InlineData("a/b/c/d/efghijk/e", "a/?/**/e", true)]
    [InlineData("a/b/c/d/efghijk/e", "a/?/c/?/*/e", true)]
    [InlineData("a/bb/e", "a/?/**/e", false)]
    [InlineData("a/bb/e", "a/?/e", false)]
    [InlineData("a/bbb/c/d/efgh.ijk/e", "a/*/?/**/e", true)]
    public void ShouldSupportQuestionMarksAndStarsBetweenSlashes(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a/a", "?/?", true)]
    [InlineData("a/a/a", "?/?", false)]
    [InlineData("a/aa/a", "?/?", false)]
    [InlineData("a/aaa/a", "?/?", false)]
    [InlineData("a/aaaa/a", "?/?", false)]
    [InlineData("a/aaaaa/a", "?/?", false)]
    [InlineData("a/a", "?/???/?", false)]
    [InlineData("a/a/a", "?/???/?", false)]
    [InlineData("a/aa/a", "?/???/?", false)]
    [InlineData("a/aaa/a", "?/???/?", true)]
    [InlineData("a/aaaa/a", "?/???/?", false)]
    [InlineData("a/aaaaa/a", "?/???/?", false)]
    [InlineData("a/a", "?/????/?", false)]
    [InlineData("a/a/a", "?/????/?", false)]
    [InlineData("a/aa/a", "?/????/?", false)]
    [InlineData("a/aaa/a", "?/????/?", false)]
    [InlineData("a/aaaa/a", "?/????/?", true)]
    [InlineData("a/aaaaa/a", "?/????/?", false)]
    [InlineData("a/a", "?/?????/?", false)]
    [InlineData("a/a/a", "?/?????/?", false)]
    [InlineData("a/aa/a", "?/?????/?", false)]
    [InlineData("a/aaa/a", "?/?????/?", false)]
    [InlineData("a/aaaa/a", "?/?????/?", false)]
    [InlineData("a/aaaaa/a", "?/?????/?", true)]
    [InlineData("a/a", "a/?", true)]
    [InlineData("a/a/a", "a/?", false)]
    [InlineData("a/aa/a", "a/?", false)]
    [InlineData("a/aaa/a", "a/?", false)]
    [InlineData("a/aaaa/a", "a/?", false)]
    [InlineData("a/aaaaa/a", "a/?", false)]
    [InlineData("a/a", "a/?/a", false)]
    [InlineData("a/a/a", "a/?/a", true)]
    [InlineData("a/aa/a", "a/?/a", false)]
    [InlineData("a/aaa/a", "a/?/a", false)]
    [InlineData("a/aaaa/a", "a/?/a", false)]
    [InlineData("a/aaaaa/a", "a/?/a", false)]
    [InlineData("a/a", "a/??/a", false)]
    [InlineData("a/a/a", "a/??/a", false)]
    [InlineData("a/aa/a", "a/??/a", true)]
    [InlineData("a/aaa/a", "a/??/a", false)]
    [InlineData("a/aaaa/a", "a/??/a", false)]
    [InlineData("a/aaaaa/a", "a/??/a", false)]
    [InlineData("a/a", "a/???/a", false)]
    [InlineData("a/a/a", "a/???/a", false)]
    [InlineData("a/aa/a", "a/???/a", false)]
    [InlineData("a/aaa/a", "a/???/a", true)]
    [InlineData("a/aaaa/a", "a/???/a", false)]
    [InlineData("a/aaaaa/a", "a/???/a", false)]
    [InlineData("a/a", "a/????/a", false)]
    [InlineData("a/a/a", "a/????/a", false)]
    [InlineData("a/aa/a", "a/????/a", false)]
    [InlineData("a/aaa/a", "a/????/a", false)]
    [InlineData("a/aaaa/a", "a/????/a", true)]
    [InlineData("a/aaaaa/a", "a/????/a", false)]
    [InlineData("a/a", "a/????a/a", false)]
    [InlineData("a/a/a", "a/????a/a", false)]
    [InlineData("a/aa/a", "a/????a/a", false)]
    [InlineData("a/aaa/a", "a/????a/a", false)]
    [InlineData("a/aaaa/a", "a/????a/a", false)]
    [InlineData("a/aaaaa/a", "a/????a/a", true)]
    public void ShouldMatchNoMoreThanOneCharacterBetweenSlashes(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData(".", "?", false)]
    [InlineData(".a", "?", false)]
    [InlineData("a", "?", true)]
    [InlineData("aa", "?", false)]
    [InlineData("a.a", "?", false)]
    [InlineData("aa.a", "?", false)]
    [InlineData("aaa", "?", false)]
    [InlineData("aaa.a", "?", false)]
    [InlineData("aaaa.a", "?", false)]
    [InlineData("aaaaa", "?", false)]
    [InlineData(".", ".?", false)]
    [InlineData(".a", ".?", true)]
    [InlineData("a", ".?", false)]
    [InlineData("aa", ".?", false)]
    [InlineData("a.a", ".?", false)]
    [InlineData("aa.a", ".?", false)]
    [InlineData("aaa", ".?", false)]
    [InlineData("aaa.a", ".?", false)]
    [InlineData("aaaa.a", ".?", false)]
    [InlineData("aaaaa", ".?", false)]
    [InlineData(".", "?a", false)]
    [InlineData(".a", "?a", false)]
    [InlineData("a", "?a", false)]
    [InlineData("aa", "?a", true)]
    [InlineData("a.a", "?a", false)]
    [InlineData("aa.a", "?a", false)]
    [InlineData("aaa", "?a", false)]
    [InlineData("aaa.a", "?a", false)]
    [InlineData("aaaa.a", "?a", false)]
    [InlineData("aaaaa", "?a", false)]
    [InlineData(".", "??", false)]
    [InlineData(".a", "??", false)]
    [InlineData("a", "??", false)]
    [InlineData("aa", "??", true)]
    [InlineData("a.a", "??", false)]
    [InlineData("aa.a", "??", false)]
    [InlineData("aaa", "??", false)]
    [InlineData("aaa.a", "??", false)]
    [InlineData("aaaa.a", "??", false)]
    [InlineData("aaaaa", "??", false)]
    [InlineData(".", "?a?", false)]
    [InlineData(".a", "?a?", false)]
    [InlineData("a", "?a?", false)]
    [InlineData("aa", "?a?", false)]
    [InlineData("a.a", "?a?", false)]
    [InlineData("aa.a", "?a?", false)]
    [InlineData("aaa", "?a?", true)]
    [InlineData("aaa.a", "?a?", false)]
    [InlineData("aaaa.a", "?a?", false)]
    [InlineData("aaaaa", "?a?", false)]
    [InlineData(".", "aaa?a", false)]
    [InlineData(".a", "aaa?a", false)]
    [InlineData("a", "aaa?a", false)]
    [InlineData("aa", "aaa?a", false)]
    [InlineData("a.a", "aaa?a", false)]
    [InlineData("aa.a", "aaa?a", false)]
    [InlineData("aaa", "aaa?a", false)]
    [InlineData("aaa.a", "aaa?a", true)]
    [InlineData("aaaa.a", "aaa?a", false)]
    [InlineData("aaaaa", "aaa?a", true)]
    [InlineData(".", "a?a?a", false)]
    [InlineData(".a", "a?a?a", false)]
    [InlineData("a", "a?a?a", false)]
    [InlineData("aa", "a?a?a", false)]
    [InlineData("a.a", "a?a?a", false)]
    [InlineData("aa.a", "a?a?a", false)]
    [InlineData("aaa", "a?a?a", false)]
    [InlineData("aaa.a", "a?a?a", true)]
    [InlineData("aaaa.a", "a?a?a", false)]
    [InlineData("aaaaa", "a?a?a", true)]
    [InlineData(".", "a???a", false)]
    [InlineData(".a", "a???a", false)]
    [InlineData("a", "a???a", false)]
    [InlineData("aa", "a???a", false)]
    [InlineData("a.a", "a???a", false)]
    [InlineData("aa.a", "a???a", false)]
    [InlineData("aaa", "a???a", false)]
    [InlineData("aaa.a", "a???a", true)]
    [InlineData("aaaa.a", "a???a", false)]
    [InlineData("aaaaa", "a???a", true)]
    [InlineData(".", "a?????", false)]
    [InlineData(".a", "a?????", false)]
    [InlineData("a", "a?????", false)]
    [InlineData("aa", "a?????", false)]
    [InlineData("a.a", "a?????", false)]
    [InlineData("aa.a", "a?????", false)]
    [InlineData("aaa", "a?????", false)]
    [InlineData("aaa.a", "a?????", false)]
    [InlineData("aaaa.a", "a?????", true)]
    [InlineData("aaaaa", "a?????", false)]
    public void ShouldNotMatchNonLeadingDotsWithQuestionMarks(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData(".", "?", true)]
    [InlineData(".a", "?", false)]
    [InlineData("a", "?", true)]
    [InlineData("aa", "?", false)]
    [InlineData("a.a", "?", false)]
    [InlineData("aa.a", "?", false)]
    [InlineData(".aa", "?", false)]
    [InlineData("aaa.a", "?", false)]
    [InlineData("aaaa.a", "?", false)]
    [InlineData("aaaaa", "?", false)]
    [InlineData(".", ".?", false)]
    [InlineData(".a", ".?", true)]
    [InlineData("a", ".?", false)]
    [InlineData("aa", ".?", false)]
    [InlineData("a.a", ".?", false)]
    [InlineData("aa.a", ".?", false)]
    [InlineData(".aa", ".?", false)]
    [InlineData("aaa.a", ".?", false)]
    [InlineData("aaaa.a", ".?", false)]
    [InlineData("aaaaa", ".?", false)]
    [InlineData(".", "?a", false)]
    [InlineData(".a", "?a", true)]
    [InlineData("a", "?a", false)]
    [InlineData("aa", "?a", true)]
    [InlineData("a.a", "?a", false)]
    [InlineData("aa.a", "?a", false)]
    [InlineData(".aa", "?a", false)]
    [InlineData("aaa.a", "?a", false)]
    [InlineData("aaaa.a", "?a", false)]
    [InlineData("aaaaa", "?a", false)]
    [InlineData(".", "??", false)]
    [InlineData(".a", "??", true)]
    [InlineData("a", "??", false)]
    [InlineData("aa", "??", true)]
    [InlineData("a.a", "??", false)]
    [InlineData("aa.a", "??", false)]
    [InlineData(".aa", "??", false)]
    [InlineData("aaa.a", "??", false)]
    [InlineData("aaaa.a", "??", false)]
    [InlineData("aaaaa", "??", false)]
    [InlineData(".", "?a?", false)]
    [InlineData(".a", "?a?", false)]
    [InlineData("a", "?a?", false)]
    [InlineData("aa", "?a?", false)]
    [InlineData("a.a", "?a?", false)]
    [InlineData("aa.a", "?a?", false)]
    [InlineData(".aa", "?a?", true)]
    [InlineData("aaa.a", "?a?", false)]
    [InlineData("aaaa.a", "?a?", false)]
    [InlineData("aaaaa", "?a?", false)]
    public void ShouldMatchNonLeadingDotsWithQuestionMarksWhenDotOptionTrue(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { Dot = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("a.js", "?.??", true)]
    [InlineData("b.ts", "?.??", true)]
    [InlineData("c.md", "?.??", true)]
    [InlineData("ab.js", "?.??", false)]
    [InlineData("a.txt", "?.??", false)]
    public void QmarksShouldMatchFileExtension(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a.txt", "?.???", true)]
    [InlineData("b.doc", "?.???", true)]
    [InlineData("a.js", "?.???", false)]
    [InlineData("ab.txt", "?.???", false)]
    public void QmarksShouldMatchThreeCharExtension(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

}
