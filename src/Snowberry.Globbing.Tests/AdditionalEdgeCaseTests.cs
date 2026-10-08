namespace Snowberry.Globbing.Tests;

/// <summary>
/// Additional edge case tests ported from picomatch JavaScript library.
/// </summary>
public class AdditionalEdgeCaseTests
{
    [Theory]
    [InlineData("a {1,2}", "a \\{1,2\\}", true)]
    [InlineData("a {a..b}", "a \\{a..b\\}", true)]
    public void Braces_ShouldMatchLiteralBracesWhenEscaped(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("aa", "{a,b,c}", false)]
    [InlineData("cc", "{a,b,c}", false)]
    public void Braces_ShouldMatchSingleBraces(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a/c", "a/{a,b}", false)]
    [InlineData("b/b", "a/{a,b,c}", false)]
    [InlineData("b/b", "a/{a,b}", false)]
    [InlineData("a/a", "a/{a,b}", true)]
    [InlineData("a/b", "a/{a,b}", true)]
    [InlineData("a/c", "a/{a,b,c}", true)]
    public void Braces_ShouldMatchUsingBracePatterns(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("foo{}baz", "foo[{a,b}]+baz", true)]
    [InlineData("{a}{b}{c}", "[abc{}]+", true)]
    public void Braces_ShouldNotConvertBracesInsideBrackets(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a/a", "a/{a..c}", true)]
    [InlineData("a/c", "a/{a..c}", true)]
    public void Braces_ShouldSupportBraceRanges(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a", "{/,}a/**", true)]
    [InlineData("aa.txt", "a{a,b/}*.txt", true)]
    [InlineData("ab/.txt", "a{a,b/}*.txt", true)]
    [InlineData("ab/a.txt", "a{a,b/}*.txt", true)]
    [InlineData("a/", "a/**{/,}", true)]
    [InlineData("a/a", "a/**{/,}", true)]
    [InlineData("a/a/", "a/**{/,}", true)]
    public void Braces_ShouldSupportBracesContainingSlashes(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a/b/cd/xyz.md", "a/b/**/c{d,e}/**/xyz.md", true)]
    [InlineData("a/b/c/xyz.md", "a/b/**/{c,d,e}/**/xyz.md", true)]
    [InlineData("a/b/d/xyz.md", "a/b/**/{c,d,e}/**/xyz.md", true)]
    public void Braces_ShouldSupportBracesInPatternsWithGlobstars(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("abc.txt", "a{,b}.txt", false)]
    [InlineData("abc.txt", "a{a,b,}.txt", false)]
    [InlineData("abc.txt", "a{b,}.txt", false)]
    [InlineData("a.txt", "a{,b}.txt", true)]
    [InlineData("a.txt", "a{b,}.txt", true)]
    [InlineData("aa.txt", "a{a,b,}.txt", true)]
    [InlineData("ab.txt", "a{,b}.txt", true)]
    [InlineData("ab.txt", "a{b,}.txt", true)]
    public void Braces_ShouldSupportBracesWithEmptyElements(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a/b/foo/bar/baz.qux", "a/b{,/**}/bar{,/**}/*.*", true)]
    [InlineData("a/b/bar/baz.qux", "a/b{,/**}/bar{,/**}/*.*", true)]
    public void Braces_ShouldSupportBracesWithGlobstarsAndEmptyElements(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a.txt", "a{,/**/}*.txt", true)]
    [InlineData("a/b.txt", "a{,/**/,/}*.txt", true)]
    [InlineData("a/x/y.txt", "a{,/**/}*.txt", true)]
    [InlineData("a/x/y/z", "a{,/**/}*.txt", false)]
    public void Braces_ShouldSupportBracesWithGlobstarsSlashesAndEmptyElements(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a.txt", "a{,/}*.txt", true)]
    [InlineData("a/b.txt", "a{,/}*.txt", true)]
    [InlineData("a/ab.txt", "a{,/}*.txt", true)]
    public void Braces_ShouldSupportBracesWithSlashesAndEmptyElements(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("ab", "{ab,c}+", true)]
    [InlineData("c", "{ab,c}+", true)]
    [InlineData("cab", "{ab,c}+", true)]
    [InlineData("ccc", "{a,b,c}+", true)]
    [InlineData("a", "{a,b,c}+", true)]
    [InlineData("aab", "{a,b,c}+", true)]
    public void Braces_ShouldSupportKleenePlus(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("ab", "{ab,c}*", true)]
    [InlineData("c", "{ab,c}*", true)]
    [InlineData("cab", "{ab,c}*", true)]
    [InlineData("abcab", "{ab,c}*", true)]
    [InlineData("ccc", "{ab,c}*", true)]
    public void Braces_ShouldSupportKleeneStars(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a {abc} b", "a {abc} b", true)]
    [InlineData("a {a.c} b", "a {a.c} b", true)]
    public void Braces_ShouldTreatSingleSetBracesAsLiterals(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("foo/bar - 1", "**/*\\[1\\]", false)]
    [InlineData("foo/bar - copy (1)", "**/*\\[1\\]", false)]
    [InlineData("foo/bar (1)", "**/*\\[1\\]", false)]
    [InlineData("foo/bar - copy [1]", "**/*\\[1\\]", true)]
    [InlineData("foo/bar - foo + bar - copy [1]", "**/*\\[1\\]", true)]
    public void Brackets_ShouldMatchEscapedBracketLiteralsWithGlobstar(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("foo/bar - 1", "*/*\\[*\\]", false)]
    [InlineData("foo/bar - copy (1)", "*/*\\[*\\]", false)]
    [InlineData("foo/bar (1)", "*/*\\[*\\]", false)]
    [InlineData("foo/bar - copy [1]", "*/*\\[*\\]", true)]
    [InlineData("foo/bar - foo + bar - copy [1]", "*/*\\[*\\]", true)]
    public void Brackets_ShouldMatchEscapedBracketLiteralsWithSingleStar(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("foo/bar", "*/*", true)]
    [InlineData("foo/bar/baz", "*/*", false)]
    [InlineData("foo/bar/baz", "*/*/*", true)]
    public void PathChars_ShouldMatchAnyCharacterZeroOrMoreTimesExceptForSlash(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("//C://user\\docs\\Letter.txt", "**", true)]
    [InlineData("//C:\\\\user/docs/Letter.txt", "**", true)]
    [InlineData(":\\", "*{,/}", true)]
    [InlineData(":\\", ":*{,/}", true)]
    [InlineData("\\\\foo/bar", "**", true)]
    [InlineData("\\\\foo/bar", "//*/*", true)]
    [InlineData("\\\\unc\\admin$", "**", true)]
    [InlineData("\\\\unc\\admin$", "//*/*$", true)]
    [InlineData("\\\\unc\\admin$\\system32", "//*/*$/*32", true)]
    [InlineData("\\\\unc\\share\\foo", "//u*/s*/f*", true)]
    [InlineData("foo\\bar\\baz", "f*/*/*", true)]
    public void PathChars_ShouldMatchMixedSlashesOnWindows(string input, string pattern, bool expected)
    {
        var options = TestOptions.Windows;
        Glob.IsMatch(input, pattern, options).Should().Be(expected);
    }

    [Theory]
    [InlineData("C:\\foo\\bar.txt", "C:/**/*.txt", true)]
    [InlineData("C:\\foo\\bar.txt", "C:/**/*.md", false)]
    [InlineData("//server/file", "//*", false)]
    [InlineData("//server/file", "/**", true)]
    [InlineData("//server/file", "//**", true)]
    [InlineData("//server/file", "**", true)]
    [InlineData("//UNC//Server01//user//docs//Letter.txt", "**", true)]
    [InlineData("/foo", "**", true)]
    [InlineData("/home/foo/..", "**/..", true)]
    [InlineData("directory\\directory", "**", true)]
    [InlineData("a/b/c.js", "**", true)]
    public void PathChars_ShouldMatchMixedSlashesWithWindowsOption(string input, string pattern, bool expected)
    {
        var options = TestOptions.Windows;
        Glob.IsMatch(input, pattern, options).Should().Be(expected);
    }

    [Theory]
    [InlineData("c:\\", "**", true)]
    [InlineData("C:\\Users\\", "**", true)]
    [InlineData("C:cwd/another", "**", true)]
    [InlineData("C:cwd\\another", "**", true)]
    public void PathChars_ShouldMatchWindowsDrivesWithGlobstars(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("c:\\", "*{,/}", true)]
    [InlineData("C:\\Users\\", "*", false)]
    [InlineData("C:cwd\\another", "*", false)]
    public void PathChars_ShouldNotMatchMultipleWindowsDirectoriesWithSingleStar(string input, string pattern, bool expected)
    {
        var options = TestOptions.Windows;
        Glob.IsMatch(input, pattern, options).Should().Be(expected);
    }

    [Theory]
    [InlineData("a", "*??", false)]
    [InlineData("aaa", "*???", true)]
    [InlineData("aa", "*****???", false)]
    [InlineData("aaa", "*****???", true)]
    public void Qmarks_ShouldEnforceOneCharacterPerQmarkEvenWhenPrecededByStars(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a/bbb/abcd.md", "a/*/ab??.md", true)]
    [InlineData("a/bbb/abcd.md", "a/bbb/ab???md", true)]
    public void Qmarks_ShouldMatchCharactersPrecedingADot(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("aaa.bbb", "aaa?bbb", true)]
    public void Qmarks_ShouldMatchNonLeadingDots(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a/bb/c/dd/e.md", "a/??/c/??/e.md", true)]
    [InlineData("a/b/c/d/e.md", "a/?/c/???/e.md", false)]
    [InlineData("a/b/c/zzz/e.md", "a/?/c/???/e.md", true)]
    public void Qmarks_ShouldMatchOneCharacterPerQmarkInPath(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("aaa/bbb", "aaa?bbb", false)]
    [InlineData("acb/", "a?b/", true)]
    [InlineData("acdb/", "a??b/", true)]
    [InlineData("/acb", "/a?b", true)]
    [InlineData("acb/", "a\\?b/", false)]
    [InlineData("acdb/", "a\\?\\?b/", false)]
    [InlineData("/acb", "/a\\?b", false)]
    public void Qmarks_ShouldMatchOtherNonSlashCharacters(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData(".aaa/bbb", "?aaa/bbb", false)]
    [InlineData("aaa/.bbb", "aaa/?bbb", false)]
    public void Qmarks_ShouldNotMatchLeadingDots(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a/b/c/d/e.md", "a/?/c/?/*/e.md", false)]
    [InlineData("a/b/c/d/e/e.md", "a/?/c/?/*/e.md", true)]
    [InlineData("a/b/c/d/efghijk/e.md", "a/?/c/?/*/e.md", true)]
    [InlineData("a/b/c/d/efghijk/e.md", "a/?/**/e.md", true)]
    [InlineData("a/bb/e.md", "a/?/e.md", false)]
    [InlineData("a/bb/e.md", "a/??/e.md", true)]
    [InlineData("a/bb/e.md", "a/?/**/e.md", false)]
    [InlineData("a/b/ccc/e.md", "a/?/**/e.md", true)]
    [InlineData("a/b/c/d/efghijk/e.md", "a/*/?/**/e.md", true)]
    [InlineData("a/b.bb/c/d/efgh.ijk/e.md", "a/*/?/**/e.md", true)]
    [InlineData("a/bbb/c/d/efgh.ijk/e.md", "a/*/?/**/e.md", true)]
    public void Qmarks_ShouldSupportQmarksStarsAndSlashes(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("1/2", "(*)/\\1", false)]
    [InlineData("1/1", "(*)/\\1", true)]
    [InlineData("1/1/1/1", "(*)/\\1/\\1/\\1", true)]
    [InlineData("1/11/111/1111", "(*)/\\1/\\1/\\1", false)]
    [InlineData("1/11/111/1111", "(*)/(\\1)+/(\\1)+/(\\1)+", true)]
    [InlineData("1/2/1/1", "(*)/\\1/\\1/\\1", false)]
    [InlineData("1/1/1/1", "(*)/\\1/(*)/\\2", true)]
    [InlineData("1/1/2/1", "(*)/\\1/(*)/\\2", false)]
    [InlineData("1/1/2/2", "(*)/\\1/(*)/\\2", true)]
    public void RegexBackRef_ShouldSupportRegexBackreferences(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a/bb/c/dd/e.md", "a/??/?/(dd)/e.md", true)]
    [InlineData("a/b/c/d/e.md", "a/?/c/?/(e|f).md", true)]
    [InlineData("a/b/c/d/f.md", "a/?/c/?/(e|f).md", true)]
    public void RegexCapture_ShouldSupportRegexCaptureGroups(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("foo/bar", "**/!([a-k])*", false)]
    [InlineData("foo/jar", "**/!([a-k])*", false)]
    [InlineData("foo/bar", "**/!([a-i])*", false)]
    [InlineData("foo/bar", "**/!([c-i])*", true)]
    [InlineData("foo/jar", "**/!([a-i])*", true)]
    public void RegexCapture_ShouldSupportRegexCharacterClassesInsideExtglobs(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a/bb/c/dd/e.md", "a/**/(?:dd)/e.md", true)]
    [InlineData("a/b/c/d/e.md", "a/?/c/?/(?:e|f).md", true)]
    [InlineData("a/b/c/d/f.md", "a/?/c/?/(?:e|f).md", true)]
    public void RegexCapture_ShouldSupportRegexNonCaptureGroups(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("0123e45g78", "[\\de]+", false)]
    [InlineData("0123e456", "[\\de]+", true)]
    [InlineData("01234", "[\\de]+", true)]
    public void RegexCharClass_ShouldMatchAlphaNumericCharacters(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a]c", "a[]]c", true)]
    [InlineData("a]c", "a]c", true)]
    [InlineData("a]", "a]", true)]
    [InlineData("a[c", "a[\\[]c", true)]
    [InlineData("a[c", "a[c", true)]
    [InlineData("a[", "a[", true)]
    public void RegexCharClass_ShouldMatchBracketLiterals(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("abc", "a[b-d]e", false)]
    [InlineData("abe", "a[b-d]e", true)]
    [InlineData("ac", "a[b-d]e", false)]
    [InlineData("abc", "a[b-d]", false)]
    [InlineData("abd", "a[b-d]+", true)]
    [InlineData("abe", "a[b-d]", false)]
    [InlineData("ac", "a[b-d]", true)]
    public void RegexCharClass_ShouldMatchCharacterClassAlphabeticalRanges(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("abc", "a[bc]d", false)]
    [InlineData("abd", "a[bc]d", true)]
    public void RegexCharClass_ShouldMatchCharacterClasses(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("abc", "a[-c]", false)]
    [InlineData("ac", "a[-c]", true)]
    [InlineData("a-", "a[-c]", true)]
    public void RegexCharClass_ShouldMatchCharacterClassesWithLeadingDashes(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("abc", "a[c-]", false)]
    [InlineData("ac", "a[c-]", true)]
    [InlineData("a-", "a[c-]", true)]
    public void RegexCharClass_ShouldMatchCharacterClassesWithTrailingDashes(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("abc", "a[^-b]c", false)]
    [InlineData("adc", "a[^-b]c", true)]
    [InlineData("a-c", "a[^-b]c", false)]
    public void RegexCharClass_ShouldMatchNegatedDashes(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("foo/bar", "**/[jkl]*", false)]
    [InlineData("foo/jar", "**/[jkl]*", true)]
    [InlineData("foo/bar", "**/[^jkl]*", true)]
    [InlineData("foo/jar", "**/[^jkl]*", false)]
    [InlineData("foo/bar", "**/[abc]ar", true)]
    [InlineData("foo/jar", "**/[abc]ar", false)]
    public void RegexCharClass_ShouldMatchWithCharacterClassesByDefault(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a/a", "a/[a-z]", false)]
    public void RegexCharClass_ShouldNotMatchWithCharacterClassesWhenDisabled(string input, string pattern, bool expected)
    {
        var options = new GlobOptions { BracketExpressions = false };
        Glob.IsMatch(input, pattern, options).Should().Be(expected);
    }

    [Theory]
    [InlineData("a]", "a[^bc]d", false)]
    [InlineData("acd", "a[^bc]d", false)]
    [InlineData("aed", "a[^bc]d", true)]
    public void RegexCharClass_ShouldSupportNegatedCharacterClasses(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a/z", "a/[b-c]", false)]
    [InlineData("a/b", "[a-z]/[a-z]", true)]
    [InlineData("z/z", "[a-z]/[a-z]", true)]
    [InlineData("a.a", "[a-b].[a-b]", true)]
    [InlineData("a.a.a", "[a-b].[a-b]", false)]
    [InlineData("c.a", "[a-b].[a-b]", false)]
    public void RegexCharClass_ShouldSupportValidRegexRanges(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a.a", "[a-d].[a-b]", true)]
    [InlineData("a.a.a", "[a-d].[a-b]", false)]
    [InlineData("c.a", "[a-d].[a-b]", true)]
    [InlineData("a.bb", "[a-d].[a-b]", false)]
    [InlineData("a.b", "[a-d]*.[a-b]", true)]
    [InlineData("a.a.a", "[a-d]*.[a-b]", true)]
    [InlineData("d.a.d", "[a-d]*.[a-b]", false)]
    [InlineData("a.ccc", "[a-d]*.[a-b]", false)]
    public void RegexCharClass_ShouldSupportValidRegexRangesExtended(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a.a", "*.[^a-b]", false)]
    [InlineData("a.a.a", "*.[^a-b]", false)]
    [InlineData("d.a.d", "*.[^a-b]", true)]
    [InlineData("a.bb", "*.[^a-b]", false)]
    [InlineData("a.ccc", "*.[^a-b]", false)]
    public void RegexGlobNegation_ShouldSupportValidRegexRangesInNegatedCharacterClasses(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a.a", "a.[^a-b]*", false)]
    [InlineData("a.a.a", "a.[^a-b]*", false)]
    [InlineData("d.a.d", "a.[^a-b]*", false)]
    [InlineData("a.bb", "a.[^a-b]*", false)]
    [InlineData("a.ccc", "a.[^a-b]*", true)]
    public void RegexGlobNegation_ShouldSupportValidRegexRangesInNegatedCharacterClassesStar(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a.a", "!*.[a-b]", false)]
    [InlineData("a.a.a", "!*.[a-b]", false)]
    [InlineData("d.a.d", "!*.[a-b]", true)]
    [InlineData("a.bb", "!*.[a-b]", true)]
    [InlineData("a.ccc", "!*.[a-b]", true)]
    public void RegexGlobNegation_ShouldSupportValidRegexRangesWithGlobNegationPatterns(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a.a", "!*.[a-b]*", false)]
    [InlineData("a.a.a", "!*.[a-b]*", false)]
    [InlineData("d.a.d", "!*.[a-b]*", false)]
    [InlineData("a.bb", "!*.[a-b]*", false)]
    [InlineData("a.ccc", "!*.[a-b]*", true)]
    public void RegexGlobNegation_ShouldSupportValidRegexRangesWithGlobNegationStar(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a.a", "![a-b].[a-b]", false)]
    [InlineData("a.a.a", "![a-b].[a-b]", true)]
    [InlineData("d.a.d", "![a-b].[a-b]", true)]
    [InlineData("a.bb", "![a-b].[a-b]", true)]
    [InlineData("a.ccc", "![a-b].[a-b]", true)]
    public void RegexGlobNegation_ShouldSupportValidRegexRangesWithNegatedBracket(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a.a", "![a-b]+.[a-b]+", false)]
    [InlineData("a.a.a", "![a-b]+.[a-b]+", true)]
    [InlineData("d.a.d", "![a-b]+.[a-b]+", true)]
    [InlineData("a.bb", "![a-b]+.[a-b]+", false)]
    [InlineData("a.ccc", "![a-b]+.[a-b]+", true)]
    public void RegexGlobNegation_ShouldSupportValidRegexRangesWithNegatedBracketPlus(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("foo.o", "*.@(a|b|f|o)", true)]
    [InlineData("bar.a", "*.@(a|b|f|o)", true)]
    [InlineData("test.x", "*.@(a|b|f|o)", false)]
    public void RegexWord_ShouldMatchExtglobAfterDot(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a", "a\\b", true)]
    [InlineData("a", "(a\\b)", true)]
    public void WordBoundaries_ShouldSupportWordBoundaries(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }
}