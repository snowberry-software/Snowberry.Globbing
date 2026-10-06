namespace Snowberry.Globbing.Tests;

/// <summary>
/// Tests for question mark (?) pattern ported from picomatch.
/// </summary>
public class QmarksTests
{

    [Theory]
    [InlineData("a", "?", true)]
    [InlineData("aa", "?", false)]
    [InlineData("abc", "?", false)]
    public void SingleQmarkShouldMatchSingleCharacter(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("aa", "??", true)]
    [InlineData("a", "??", false)]
    [InlineData("abc", "??", false)]
    public void DoubleQmarkShouldMatchTwoCharacters(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("abc", "???", true)]
    [InlineData("ab", "???", false)]
    [InlineData("abcd", "???", false)]
    public void TripleQmarkShouldMatchThreeCharacters(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("aa", "a?", true)]
    [InlineData("a", "a?", false)]
    [InlineData("ba", "a?", false)]
    public void QmarkWithPrefixShouldMatch(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("aa", "?a", true)]
    [InlineData("a", "?a", false)]
    [InlineData("ab", "?a", false)]
    public void QmarkWithSuffixShouldMatch(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("aXb", "a?b", true)]
    [InlineData("ab", "a?b", false)]
    [InlineData("aXXb", "a?b", false)]
    public void QmarkBetweenCharsShouldMatch(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("abc", "?*", true)]
    [InlineData("a", "?*", true)]
    [InlineData("", "?*", false)]
    public void QmarkWithStarShouldMatchOneOrMoreChars(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a.txt", "?.txt", true)]
    [InlineData("ab.txt", "?.txt", false)]
    [InlineData(".txt", "?.txt", false)]
    public void QmarkInFileNameShouldMatch(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a.t", "?.*", true)]
    [InlineData("ab.txt", "?.*", false)]
    [InlineData("a", "?.*", false)]
    public void QmarkWithDotStarShouldMatch(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("abc.txt", "*?.txt", true)]
    [InlineData("a.txt", "*?.txt", true)]
    [InlineData(".txt", "*?.txt", false)]
    public void StarWithQmarkInExtensionShouldMatch(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a/b", "a?b", false)]
    [InlineData("a/b/c", "?/?/?", true)]
    public void QmarkShouldNotMatchSlash(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a/b", "???", false)]
    [InlineData("a/", "??", false)]
    [InlineData("/b", "??", false)]
    public void MultipleQmarksShouldNotMatchSlash(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a/b", "?/b", true)]
    [InlineData("ab/b", "?/b", false)]
    [InlineData("a/c", "?/b", false)]
    public void QmarkInPathSegmentShouldMatch(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a/b/c", "?/b/?", true)]
    [InlineData("a/b/cd", "?/b/?", false)]
    [InlineData("ab/b/c", "?/b/?", false)]
    public void QmarksInMultiplePathSegmentsShouldMatch(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a/bc/d", "?/??/?", true)]
    [InlineData("a/b/c", "?/??/?", false)]
    [InlineData("ab/bc/d", "?/??/?", false)]
    public void MixedQmarksInPathShouldMatch(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData(".a", "?a", false)]
    [InlineData(".a", "?*", false)]
    [InlineData(".abc", "????", false)]
    public void QmarkShouldNotMatchDotAtStartByDefault(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData(".a", "?a", true)]
    [InlineData(".a", "?*", true)]
    [InlineData(".abc", "????", true)]
    public void QmarkShouldMatchDotAtStartWithDotOption(string input, string pattern, bool expected)
    {
        var options = new GlobOptions { MatchDotFiles = true };
        Glob.IsMatch(input, pattern, options).Should().Be(expected);
    }

    [Theory]
    [InlineData("a/.b", "a/?b", false)]
    [InlineData("a/.bc", "a/???", false)]
    public void QmarkShouldNotMatchDotAtStartOfPathSegmentByDefault(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a/.b", "a/?b", true)]
    [InlineData("a/.bc", "a/???", true)]
    public void QmarkShouldMatchDotAtStartOfPathSegmentWithDotOption(string input, string pattern, bool expected)
    {
        var options = new GlobOptions { MatchDotFiles = true };
        Glob.IsMatch(input, pattern, options).Should().Be(expected);
    }

    [Theory]
    [InlineData("?", "\\?", true)]
    [InlineData("a", "\\?", false)]
    [InlineData("??", "\\?\\?", true)]
    public void EscapedQmarkShouldMatchLiteralQuestionMark(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a?b", "a\\?b", true)]
    [InlineData("aXb", "a\\?b", false)]
    public void EscapedQmarkMixedWithLiteralsShouldMatch(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a?", "?\\?", true)]
    [InlineData("aa", "?\\?", false)]
    public void MixedQmarkAndEscapedQmarkShouldMatch(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("?", "?", true)]
    [InlineData("??", "?", false)]
    [InlineData("?", "??", false)]
    [InlineData("??", "??", true)]
    public void ShouldMatchQuestionMarksWithQuestionMarks(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("?", "?*", true)]
    [InlineData("?", "*?", true)]
    [InlineData("?", "?*?", false)]
    [InlineData("??", "?*?", true)]
    [InlineData("?*", "?*", true)]
    [InlineData("?*?*?", "?*", true)]
    [InlineData("?*?", "*?", true)]
    [InlineData("?*?*?", "?*?", true)]
    public void ShouldMatchQuestionMarksAndStarsWithQuestionMarksAndStars(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("aaa", "a*?c", false)]
    [InlineData("aac", "a*?c", true)]
    [InlineData("abc", "a*?c", true)]
    [InlineData("abc", "a**?c", true)]
    [InlineData("abb", "a**?c", false)]
    [InlineData("abc", "a*****?c", true)]
    [InlineData("a", "*****?", true)]
    [InlineData("abc", "*****?", true)]
    [InlineData("a", "*****??", false)]
    [InlineData("ab", "*****??", true)]
    [InlineData("a", "?*****??", false)]
    [InlineData("ab", "?*****??", false)]
    [InlineData("abc", "?*****??", true)]
    [InlineData("abc", "?*****?c", true)]
    [InlineData("abb", "?*****?c", false)]
    [InlineData("abc", "?***?****?", true)]
    [InlineData("abc", "?***?****c", true)]
    [InlineData("abc", "*******?", true)]
    [InlineData("abc", "*******c", true)]
    [InlineData("abc", "?***?****", true)]
    [InlineData("abcdecdhjk", "a****c**?**??*****", true)]
    [InlineData("abcdecdhjk", "a**?**cd**?**??***k", true)]
    [InlineData("abcdecdhjk", "a**?**cd**?**??k", true)]
    [InlineData("abcdecdhjk", "a*cd**?**??k", true)]
    public void ShouldSupportConsecutiveStarsAndQuestionMarks(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("aaa", "?", false)]
    [InlineData("aaa", "??", false)]
    [InlineData("aa", "???", false)]
    [InlineData("aaa", "???", true)]
    [InlineData("/a/", "??", false)]
    [InlineData("a/b/c.md", "a/?/c.md", true)]
    [InlineData("a/bb/c.md", "a/?/c.md", false)]
    [InlineData("a/bb/c.md", "a/??/c.md", true)]
    [InlineData("a/bbb/c.md", "a/??/c.md", false)]
    public void ShouldMatchOneCharacterPerQuestionMark(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("//", "/?", false)]
    [InlineData("/a", "/?", true)]
    [InlineData("/a/", "/?", false)]
    [InlineData("/aa", "/?", false)]
    [InlineData("/a", "/??", false)]
    [InlineData("/aa", "/??", true)]
    [InlineData("/aaa", "/??", false)]
    [InlineData("/aa", "/???", false)]
    [InlineData("/aaa", "/???", true)]
    [InlineData("//", "/?/", false)]
    [InlineData("/a", "/?/", false)]
    [InlineData("/a/", "/?/", true)]
    [InlineData("/a", "??", false)]
    [InlineData("/aa", "??", false)]
    [InlineData("a/a", "??", false)]
    [InlineData("//", "?/?", false)]
    [InlineData("a/", "?/?", false)]
    [InlineData("/a", "?/?", false)]
    [InlineData("a/a", "?/?", true)]
    [InlineData("aaa", "?/?", false)]
    [InlineData("//", "???", false)]
    [InlineData("/a/", "???", false)]
    [InlineData("/aa", "???", false)]
    [InlineData("a/a", "???", false)]
    [InlineData("/a/", "a?a", false)]
    [InlineData("a/a", "a?a", false)]
    [InlineData("aaa", "a?a", true)]
    [InlineData("aa", "aa?", false)]
    [InlineData("/aa", "aa?", false)]
    [InlineData("aaa", "aa?", true)]
    [InlineData("/aa", "?aa", false)]
    [InlineData("a/a", "?aa", false)]
    [InlineData("aaa", "?aa", true)]
    public void ShouldNotMatchSlashesQuestionMarks(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a/b.bb/c/d/efgh.ijk/e", "a/*/?/**/e", true)]
    [InlineData("a/b/c/d/e", "a/?/c/?/*/e", false)]
    [InlineData("a/b/c/d/e/e", "a/?/c/?/*/e", true)]
    [InlineData("a/b/c/d/efghijk/e", "a/*/?/**/e", true)]
    [InlineData("a/b/c/d/efghijk/e", "a/?/**/e", true)]
    [InlineData("a/b/c/d/efghijk/e", "a/?/c/?/*/e", true)]
    [InlineData("a/bb/e", "a/?/**/e", false)]
    public void ShouldSupportQuestionMarksAndStarsBetweenSlashes(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a/a/a", "?/?", false)]
    [InlineData("a/aa/a", "?/?", false)]
    [InlineData("a/a", "?/???/?", false)]
    [InlineData("a/aa/a", "?/???/?", false)]
    [InlineData("a/aaa/a", "?/???/?", true)]
    [InlineData("a/aaaa/a", "?/???/?", false)]
    [InlineData("a/aaa/a", "?/????/?", false)]
    [InlineData("a/aaaa/a", "?/????/?", true)]
    [InlineData("a/aaaa/a", "?/?????/?", false)]
    [InlineData("a/aaaaa/a", "?/?????/?", true)]
    [InlineData("a/a", "a/?", true)]
    [InlineData("a/a/a", "a/?", false)]
    [InlineData("a/a", "a/?/a", false)]
    [InlineData("a/a/a", "a/?/a", true)]
    [InlineData("a/aa/a", "a/?/a", false)]
    [InlineData("a/a/a", "a/??/a", false)]
    [InlineData("a/aa/a", "a/??/a", true)]
    [InlineData("a/aaa/a", "a/??/a", false)]
    [InlineData("a/aa/a", "a/???/a", false)]
    [InlineData("a/aaa/a", "a/???/a", true)]
    [InlineData("a/aaa/a", "a/????/a", false)]
    [InlineData("a/aaaa/a", "a/????/a", true)]
    [InlineData("a/aaaa/a", "a/????a/a", false)]
    [InlineData("a/aaaaa/a", "a/????a/a", true)]
    public void ShouldMatchNoMoreThanOneCharacterBetweenSlashes(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData(".", "?", false)]
    [InlineData(".a", "?", false)]
    [InlineData("a.a", "?", false)]
    [InlineData(".", ".?", false)]
    [InlineData(".a", ".?", true)]
    [InlineData("a.a", ".?", false)]
    [InlineData(".", "?a", false)]
    [InlineData("a.a", "?a", false)]
    [InlineData(".a", "??", false)]
    [InlineData("a.a", "??", false)]
    [InlineData(".a", "?a?", false)]
    [InlineData("a.a", "?a?", false)]
    [InlineData("aaa", "?a?", true)]
    [InlineData("aaa.a", "aaa?a", true)]
    [InlineData("aaaa.a", "aaa?a", false)]
    [InlineData("aaaaa", "aaa?a", true)]
    [InlineData("aa.a", "a?a?a", false)]
    [InlineData("aaa.a", "a?a?a", true)]
    [InlineData("aaaaa", "a?a?a", true)]
    [InlineData("a.a", "a???a", false)]
    [InlineData("aaa.a", "a???a", true)]
    [InlineData("aaaaa", "a???a", true)]
    [InlineData("aaa.a", "a?????", false)]
    [InlineData("aaaa.a", "a?????", true)]
    [InlineData("aaaaa", "a?????", false)]
    public void ShouldNotMatchNonLeadingDotsWithQuestionMarks(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData(".", "?", true)]
    [InlineData(".a", "?", false)]
    [InlineData("a", "?", true)]
    [InlineData(".", ".?", false)]
    [InlineData(".a", ".?", true)]
    [InlineData("a.a", ".?", false)]
    [InlineData(".", "?a", false)]
    [InlineData("aa", "?a", true)]
    [InlineData(".aa", "?a", false)]
    [InlineData(".", "??", false)]
    [InlineData(".a", "??", true)]
    [InlineData("aa", "??", true)]
    [InlineData(".aa", "??", false)]
    [InlineData(".a", "?a?", false)]
    [InlineData("a.a", "?a?", false)]
    [InlineData(".aa", "?a?", true)]
    public void ShouldMatchNonLeadingDotsWithQuestionMarksWhenDotOptionTrue(string input, string pattern, bool expected)
    {
        var options = new GlobOptions { MatchDotFiles = true };
        Glob.IsMatch(input, pattern, options).Should().Be(expected);
    }

    [Theory]
    [InlineData("a.js", "?.??", true)]
    [InlineData("b.ts", "?.??", true)]
    [InlineData("c.md", "?.??", true)]
    [InlineData("ab.js", "?.??", false)]
    [InlineData("a.txt", "?.??", false)]
    public void QmarksShouldMatchFileExtension(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a.txt", "?.???", true)]
    [InlineData("b.doc", "?.???", true)]
    [InlineData("a.js", "?.???", false)]
    [InlineData("ab.txt", "?.???", false)]
    public void QmarksShouldMatchThreeCharExtension(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

}