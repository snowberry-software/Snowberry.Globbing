namespace Snowberry.Globbing.Tests;

/// <summary>
/// Tests for negation patterns ported from picomatch.
/// </summary>
public class NegationTests
{

    [Theory]
    [InlineData("abc", "!*", false)]
    [InlineData("abc", "!abc", false)]
    [InlineData("bar.md", "*!.md", false)]
    [InlineData("bar.md", "foo!.md", false)]
    [InlineData("foo!.md", "\\!*!*.md", false)]
    [InlineData("foo!bar.md", "\\!*!*.md", false)]
    [InlineData("!foo!.md", "*!*.md", true)]
    [InlineData("!foo!.md", "\\!*!*.md", true)]
    [InlineData("abc", "!*foo", true)]
    [InlineData("abc", "!foo*", true)]
    [InlineData("abc", "!xyz", true)]
    [InlineData("ba!r.js", "*!*.*", true)]
    [InlineData("bar.md", "*.md", true)]
    [InlineData("foo!.md", "*!*.*", true)]
    [InlineData("foo!.md", "*!*.md", true)]
    [InlineData("foo!.md", "*!.md", true)]
    [InlineData("foo!.md", "*.md", true)]
    [InlineData("foo!.md", "foo!.md", true)]
    [InlineData("foo!bar.md", "*!*.md", true)]
    [InlineData("foobar.md", "*b*.md", true)]
    public void ShouldPatternsWithLeadingExclamationAsNegatedInvertedGlobs(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a", "a!!b", false)]
    [InlineData("aa", "a!!b", false)]
    [InlineData("a/b", "a!!b", false)]
    [InlineData("a!b", "a!!b", false)]
    [InlineData("a!!b", "a!!b", true)]
    [InlineData("a/!!/b", "a!!b", false)]
    public void ShouldTreatNonLeadingExclamationAsLiteralCharacters(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a/b", "!a/b", false)]
    [InlineData("a", "!a/b", true)]
    [InlineData("a.b", "!a/b", true)]
    [InlineData("a/a", "!a/b", true)]
    [InlineData("a/c", "!a/b", true)]
    [InlineData("b/a", "!a/b", true)]
    [InlineData("b/b", "!a/b", true)]
    [InlineData("b/c", "!a/b", true)]
    public void ShouldSupportNegationInGlobsThatHaveNoOtherSpecialCharacters(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    // Note: Pattern "!a" negates "a", so it matches everything except "a"
    // "!a" (the input) does not equal "a", so "!a" matches
    [Theory]
    [InlineData("abc", "!abc", false)]
    [InlineData("abc", "!!abc", true)]
    [InlineData("abc", "!!!abc", false)]
    [InlineData("abc", "!!!!abc", true)]
    [InlineData("abc", "!!!!!abc", false)]
    [InlineData("abc", "!!!!!!abc", true)]
    [InlineData("abc", "!!!!!!!abc", false)]
    [InlineData("abc", "!!!!!!!!abc", true)]
    public void ShouldSupportMultipleLeadingExclamationsToToggleNegation(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a", "!a", false)]
    [InlineData("!a", "!a", true)]   // "!a" is not "a", so negation matches
    [InlineData("a", "!!a", true)]
    [InlineData("aa", "!!a", false)]
    [InlineData("!a", "!!a", false)]
    [InlineData("a", "!!!a", false)]
    [InlineData("!a", "!!!a", true)]
    [InlineData("a", "!!!!a", true)]
    [InlineData("!a", "!!!!a", false)]
    public void ShouldSupportPatternsThatStartWithExclamations(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a/a", "!a/a", false)]
    [InlineData("a/b", "!a/a", true)]
    [InlineData("a/c", "!a/a", true)]
    [InlineData("b/a", "!a/a", true)]
    [InlineData("b/b", "!a/a", true)]
    [InlineData("b/c", "!a/a", true)]
    public void ShouldSupportPatternsThatStartWithExclamationsInPath(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a", "!*", false)]
    [InlineData("a.b", "!*", false)]
    [InlineData("a/a", "!*", true)]
    [InlineData("a/b", "!*", true)]
    [InlineData("a/c", "!*", true)]
    [InlineData("b/a", "!*", true)]
    [InlineData("b/b", "!*", true)]
    [InlineData("b/c", "!*", true)]
    public void ShouldNegateWithStar(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a/a", "!*/*", false)]
    [InlineData("a/b", "!*/*", false)]
    [InlineData("a/c", "!*/*", false)]
    [InlineData("b/a", "!*/*", false)]
    [InlineData("b/b", "!*/*", false)]
    [InlineData("b/c", "!*/*", false)]
    [InlineData("a", "!*/*", true)]
    [InlineData("a.b", "!*/*", true)]
    public void ShouldNegatePathWithStarSlashStar(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a/a", "!(*/*)")]
    [InlineData("a/b", "!(*/*)")]
    [InlineData("a/c", "!(*/*)")]
    [InlineData("b/a", "!(*/*)")]
    [InlineData("b/b", "!(*/*)")]
    [InlineData("b/c", "!(*/*)")]
    [InlineData("a", "!(*/*)", true)]
    [InlineData("a.b", "!(*/*)", true)]
    public void ShouldNegateWithExtglobStarSlashStar(string input, string pattern, bool expected = false)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a/b", "!(*/b)")]
    [InlineData("b/b", "!(*/b)")]
    [InlineData("a", "!(*/b)", true)]
    [InlineData("a.b", "!(*/b)", true)]
    [InlineData("a/a", "!(*/b)", true)]
    [InlineData("a/c", "!(*/b)", true)]
    [InlineData("b/a", "!(*/b)", true)]
    [InlineData("b/c", "!(*/b)", true)]
    public void ShouldNegateWithExtglobStarSlashB(string input, string pattern, bool expected = false)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a/b", "!(a/b)")]
    [InlineData("a", "!(a/b)", true)]
    [InlineData("a.b", "!(a/b)", true)]
    [InlineData("a/a", "!(a/b)", true)]
    [InlineData("a/c", "!(a/b)", true)]
    [InlineData("b/a", "!(a/b)", true)]
    [InlineData("b/b", "!(a/b)", true)]
    [InlineData("b/c", "!(a/b)", true)]
    public void ShouldNegateWithExtglobASlashB(string input, string pattern, bool expected = false)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a/b", "!*/b")]
    [InlineData("b/b", "!*/b")]
    [InlineData("a/c", "!*/c")]
    [InlineData("b/c", "!*/c")]
    [InlineData("a", "!*/b", true)]
    [InlineData("a.b", "!*/b", true)]
    [InlineData("a/a", "!*/b", true)]
    [InlineData("a/c", "!*/b", true)]
    [InlineData("b/a", "!*/b", true)]
    [InlineData("b/c", "!*/b", true)]
    [InlineData("a", "!*/c", true)]
    [InlineData("a.b", "!*/c", true)]
    [InlineData("a/a", "!*/c", true)]
    [InlineData("a/b", "!*/c", true)]
    [InlineData("b/a", "!*/c", true)]
    [InlineData("b/b", "!*/c", true)]
    public void ShouldNegateWithStarSlashB(string input, string pattern, bool expected = false)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("bar", "!*a*")]
    [InlineData("fab", "!*a*")]
    [InlineData("foo", "!*a*", true)]
    public void ShouldNegateWithStarAStar(string input, string pattern, bool expected = false)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a/a", "!a/(*)")]
    [InlineData("a/b", "!a/(*)")]
    [InlineData("a/c", "!a/(*)")]
    [InlineData("a", "!a/(*)", true)]
    [InlineData("a.b", "!a/(*)", true)]
    [InlineData("b/a", "!a/(*)", true)]
    [InlineData("b/b", "!a/(*)", true)]
    [InlineData("b/c", "!a/(*)", true)]
    public void ShouldNegateASlashStar(string input, string pattern, bool expected = false)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a/b", "!a/(b)")]
    [InlineData("a", "!a/(b)", true)]
    [InlineData("a.b", "!a/(b)", true)]
    [InlineData("a/a", "!a/(b)", true)]
    [InlineData("a/c", "!a/(b)", true)]
    [InlineData("b/a", "!a/(b)", true)]
    [InlineData("b/b", "!a/(b)", true)]
    [InlineData("b/c", "!a/(b)", true)]
    public void ShouldNegateASlashParenB(string input, string pattern, bool expected = false)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a/a", "!a/*")]
    [InlineData("a/b", "!a/*")]
    [InlineData("a/c", "!a/*")]
    [InlineData("a", "!a/*", true)]
    [InlineData("a.b", "!a/*", true)]
    [InlineData("b/a", "!a/*", true)]
    [InlineData("b/b", "!a/*", true)]
    [InlineData("b/c", "!a/*", true)]
    public void ShouldNegateASlashStarPattern(string input, string pattern, bool expected = false)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("fab", "!f*b")]
    [InlineData("bar", "!f*b", true)]
    [InlineData("foo", "!f*b", true)]
    public void ShouldNegateFStarB(string input, string pattern, bool expected = false)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a", "!**", false)]
    [InlineData("a.b", "!**", false)]
    public void ShouldNegateWithGlobstar(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a/a", "!**/a", false)]
    [InlineData("a/b", "!**/a", true)]
    [InlineData("a/c", "!**/a", true)]
    [InlineData("b/a", "!**/a", false)]
    [InlineData("b/b", "!**/a", true)]
    [InlineData("b/c", "!**/a", true)]
    public void ShouldNegatePathWithGlobstarSlashA(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a/a", "!a/**", false)]
    [InlineData("a/b", "!a/**", false)]
    [InlineData("a/c", "!a/**", false)]
    [InlineData("b/a", "!a/**", true)]
    [InlineData("b/b", "!a/**", true)]
    [InlineData("b/c", "!a/**", true)]
    public void ShouldNegatePathWithASlashGlobstar(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a/a", "!a/**/a", false)]
    [InlineData("a/b", "!a/**/a", true)]
    [InlineData("a/c", "!a/**/a", true)]
    [InlineData("b/a", "!a/**/a", true)]
    [InlineData("b/b", "!a/**/a", true)]
    [InlineData("b/c", "!a/**/a", true)]
    public void ShouldNegatePathWithGlobstarInMiddle(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a/a/a", "!a/**/a", false)]
    [InlineData("a/b/a", "!a/**/a", false)]
    [InlineData("a/c/a", "!a/**/a", false)]
    [InlineData("a/a/b", "!a/**/a", true)]
    [InlineData("a/b/b", "!a/**/a", true)]
    [InlineData("b/a/a", "!a/**/a", true)]
    [InlineData("b/b/a", "!a/**/a", true)]
    public void ShouldNegateNestedPathWithGlobstar(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData(".md", "!.md", false)]
    [InlineData("a.js", "!**/*.md", true)]
    [InlineData("b.md", "!**/*.md", false)]
    [InlineData("c.txt", "!**/*.md", true)]
    [InlineData("a.js", "!*.md", true)]
    [InlineData("b.md", "!*.md", false)]
    [InlineData("c.txt", "!*.md", true)]
    [InlineData("abc.md", "!*.md", false)]
    [InlineData("abc.txt", "!*.md", true)]
    [InlineData("foo.md", "!*.md", false)]
    [InlineData("foo.md", "!.md", true)]
    public void ShouldNegateFilesWithExtensions(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a.js", "!*.md", true)]
    [InlineData("b.txt", "!*.md", true)]
    [InlineData("c.md", "!*.md", false)]
    public void ShouldSupportNegatedSingleStars(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a/a/a.js", "!a/*/a.js", false)]
    [InlineData("a/b/a.js", "!a/*/a.js", false)]
    [InlineData("a/c/a.js", "!a/*/a.js", false)]
    [InlineData("a/a/a/a.js", "!a/*/*/a.js", false)]
    [InlineData("b/a/b/a.js", "!a/*/*/a.js", true)]
    [InlineData("c/a/c/a.js", "!a/*/*/a.js", true)]
    [InlineData("a/a.txt", "!a/a*.txt", false)]
    [InlineData("a/b.txt", "!a/a*.txt", true)]
    [InlineData("a/c.txt", "!a/a*.txt", true)]
    [InlineData("a.a.txt", "!a.a*.txt", false)]
    [InlineData("a.b.txt", "!a.a*.txt", true)]
    [InlineData("a.c.txt", "!a.a*.txt", true)]
    [InlineData("a/a.txt", "!a/*.txt", false)]
    [InlineData("a/b.txt", "!a/*.txt", false)]
    [InlineData("a/c.txt", "!a/*.txt", false)]
    public void ShouldSupportNegatedSingleStarsWithPaths(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a/a/a.js", "!**/a.js", false)]
    [InlineData("a/b/a.js", "!**/a.js", false)]
    [InlineData("a/c/a.js", "!**/a.js", false)]
    [InlineData("a/a/b.js", "!**/a.js", true)]
    [InlineData("a/a/a/a.js", "!a/**/a.js", false)]
    [InlineData("b/a/b/a.js", "!a/**/a.js", true)]
    [InlineData("c/a/c/a.js", "!a/**/a.js", true)]
    public void ShouldSupportNegatedGlobstars(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a/b.js", "!**/*.md", true)]
    [InlineData("a.js", "!**/*.md", true)]
    [InlineData("a/b.md", "!**/*.md", false)]
    [InlineData("a.md", "!**/*.md", false)]
    [InlineData("a/b.js", "**/*.md", false)]
    [InlineData("a.js", "**/*.md", false)]
    [InlineData("a/b.md", "**/*.md", true)]
    [InlineData("a.md", "**/*.md", true)]
    public void ShouldSupportGlobstarsWithExtensions(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a/b.js", "!*.md", true)]
    [InlineData("a.js", "!*.md", true)]
    [InlineData("a/b.md", "!*.md", true)]
    [InlineData("a.md", "!*.md", false)]
    public void ShouldNotMatchSlashesWithSingleStar(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData(".dotfile.md", "!.*.md", false)]
    [InlineData(".dotfile.md", "!*.md", true)]
    [InlineData(".dotfile.txt", "!*.md", true)]
    [InlineData("a/b/.dotfile", "!*.md", true)]
    [InlineData(".gitignore", "!.gitignore", false)]
    [InlineData("a", "!.gitignore", true)]
    [InlineData("b", "!.gitignore", true)]
    public void ShouldNegateDotfiles(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("foo/bar.md", "!*.md", true)]
    [InlineData("foo.md", "!*.md", false)]
    public void ShouldNotMatchSlashesWithSingleStarNegate(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a", "!a/**", false)]
    [InlineData("a/", "!a/**", false)]
    [InlineData("a/b", "!a/**", false)]
    [InlineData("a/b/c", "!a/**", false)]
    [InlineData("b", "!a/**", true)]
    [InlineData("b/c", "!a/**", true)]
    public void ShouldMatchNestedDirectoriesWithGlobstarsNegate(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a", "a!b", false)]
    [InlineData("aa", "a!b", false)]
    [InlineData("ab", "a!b", false)]
    [InlineData("a!b", "a!b", true)]
    public void ShouldNotNegateWhenExclamationIsNotAtStart(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a!b", "a\\!b", true)]
    [InlineData("a", "a\\!b", false)]
    [InlineData("aa", "a\\!b", false)]
    [InlineData("ab", "a\\!b", false)]
    public void ShouldSupportEscapedExclamation(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("!a", "\\!a", true)]
    [InlineData("a", "\\!a", false)]
    public void ShouldMatchEscapedExclamationAtStart(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("!!a", "\\!\\!a", true)]
    [InlineData("!a", "\\!\\!a", false)]
    [InlineData("a", "\\!\\!a", false)]
    public void ShouldMatchMultipleEscapedExclamations(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("abc.txt", "\\!abc.txt", false)]
    [InlineData("!abc.txt", "\\!abc.txt", true)]
    public void ShouldMatchEscapedExclamationInFilename(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    // Note: In picomatch, [!...] does NOT work as negation - use [^...] for bracket negation
    // This matches the JavaScript picomatch behavior
    private static readonly GlobbingOptions PosixOptions = new() { Posix = true };

    // Use [^...] syntax which works correctly
    [Theory]
    [InlineData("a", "[^a]", false)]
    [InlineData("b", "[^a]", true)]
    [InlineData("c", "[^a]", true)]
    public void ShouldNegateCharacterClassWithExclamation(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, PosixOptions));
    }

    [Theory]
    [InlineData("a", "[^a]", false)]
    [InlineData("b", "[^a]", true)]
    [InlineData("c", "[^a]", true)]
    public void ShouldNegateCharacterClassWithCaret(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, PosixOptions));
    }

    [Theory]
    [InlineData("a", "[^abc]", false)]
    [InlineData("b", "[^abc]", false)]
    [InlineData("c", "[^abc]", false)]
    [InlineData("d", "[^abc]", true)]
    [InlineData("e", "[^abc]", true)]
    public void ShouldNegateMultipleCharsInBrackets(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, PosixOptions));
    }

    [Theory]
    [InlineData("a", "[^a-c]", false)]
    [InlineData("b", "[^a-c]", false)]
    [InlineData("c", "[^a-c]", false)]
    [InlineData("d", "[^a-c]", true)]
    [InlineData("e", "[^a-c]", true)]
    public void ShouldNegateRangeInBrackets(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, PosixOptions));
    }

    [Theory]
    [InlineData("foobar", "!foo*", false)]
    [InlineData("foo", "!foo*", false)]
    [InlineData("bar", "!foo*", true)]
    [InlineData("baz", "!foo*", true)]
    public void ShouldNegatePatternWithWildcard(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("foo.js", "!*.js", false)]
    [InlineData("bar.js", "!*.js", false)]
    [InlineData("foo.txt", "!*.js", true)]
    [InlineData("bar.txt", "!*.js", true)]
    public void ShouldNegateFileExtension(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("foo", "!f?o", false)]
    [InlineData("fao", "!f?o", false)]
    [InlineData("fbo", "!f?o", false)]
    [InlineData("bar", "!f?o", true)]
    [InlineData("baz", "!f?o", true)]
    public void ShouldNegatePatternWithQuestionMark(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    // Note: With nonegate option, ! is treated as a literal character
    // So "!a" pattern only matches the literal string "!a", not "a"
    [Theory]
    [InlineData("a", "!a", false)]      // "a" does not match literal "!a"
    [InlineData("b", "!a", false)]      // "b" does not match literal "!a"
    [InlineData("!a", "!a", true)]      // "!a" matches literal "!a"
    public void WithNonegateOptionExclamationShouldMatchLiterally(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { NoNegate = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("abc", "!abc", false)]  // "abc" does not match literal "!abc"
    [InlineData("def", "!abc", false)]
    [InlineData("!abc", "!abc", true)]  // "!abc" matches literal "!abc"
    public void WithNonegateExactMatchShouldWork(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { NoNegate = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("!abc", "!!abc", false)]  // "!abc" does not match literal "!!abc"
    [InlineData("!!abc", "!!abc", true)]  // "!!abc" matches literal "!!abc"
    [InlineData("abc", "!!abc", false)]
    public void WithNonegateDoubleExclamationShouldMatchLiterally(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { NoNegate = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("!foo.js", "!*.js", true)]
    [InlineData("foo.js", "!*.js", false)]
    [InlineData("!foo.txt", "!*.js", false)]
    public void WithNonegatePatternWithWildcardShouldMatchLiterally(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { NoNegate = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("a/b.js", "!a/*.js", false)]
    [InlineData("a/c.txt", "!a/*.js", true)]
    [InlineData("b/b.js", "!a/*.js", true)]
    public void ShouldNegateNestedPathWithWildcard(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a/b/c.js", "!a/**/*.js", false)]
    [InlineData("a/b/c/d.js", "!a/**/*.js", false)]
    [InlineData("a/b/c.txt", "!a/**/*.js", true)]
    [InlineData("b/c.js", "!a/**/*.js", true)]
    public void ShouldNegateNestedPathWithGlobstarAndWildcard(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

}
