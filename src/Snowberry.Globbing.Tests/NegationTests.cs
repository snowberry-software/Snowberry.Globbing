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
    [InlineData("!foo!.md", "*!*.md", true)]
    [InlineData("!foo!.md", "\\!*!*.md", true)]
    [InlineData("abc", "!foo*", true)]
    [InlineData("abc", "!xyz", true)]
    [InlineData("ba!r.js", "*!*.*", true)]
    [InlineData("foo!.md", "*!*.md", true)]
    [InlineData("foo!.md", "*!.md", true)]
    [InlineData("foo!.md", "*.md", true)]
    [InlineData("foo!.md", "foo!.md", true)]
    [InlineData("foo!bar.md", "*!*.md", true)]
    public void ShouldPatternsWithLeadingExclamationAsNegatedInvertedGlobs(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a", "a!!b", false)]
    [InlineData("a!b", "a!!b", false)]
    [InlineData("a!!b", "a!!b", true)]
    [InlineData("a/!!/b", "a!!b", false)]
    public void ShouldTreatNonLeadingExclamationAsLiteralCharacters(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a", "!a/b", true)]
    [InlineData("a.b", "!a/b", true)]
    [InlineData("a/b", "!a/b", false)]
    public void ShouldSupportNegationInGlobsThatHaveNoOtherSpecialCharacters(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("abc", "!!abc", true)]
    [InlineData("abc", "!!!abc", false)]
    [InlineData("abc", "!!!!!!!!abc", true)]
    public void ShouldSupportMultipleLeadingExclamationsToToggleNegation(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a", "!a", false)]
    [InlineData("!a", "!a", true)]   // "!a" is not "a", so negation matches
    [InlineData("a", "!!a", true)]
    [InlineData("aa", "!!a", false)]
    [InlineData("!a", "!!a", false)]
    [InlineData("a", "!!!a", false)]
    [InlineData("!a", "!!!a", true)]
    public void ShouldSupportPatternsThatStartWithExclamations(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a", "!*", false)]
    [InlineData("a.b", "!*", false)]
    [InlineData("a/a", "!*", true)]
    public void ShouldNegateWithStar(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a/a", "!*/*", false)]
    [InlineData("a", "!*/*", true)]
    [InlineData("a.b", "!*/*", true)]
    public void ShouldNegatePathWithStarSlashStar(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a/a", "!(*/*)", false)]
    [InlineData("a", "!(*/*)", true)]
    [InlineData("a.b", "!(*/*)", true)]
    public void ShouldNegateWithExtglobStarSlashStar(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a/b", "!(*/b)", false)]
    [InlineData("a", "!(*/b)", true)]
    [InlineData("a/a", "!(*/b)", true)]
    public void ShouldNegateWithExtglobStarSlashB(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a/b", "!(a/b)", false)]
    [InlineData("a", "!(a/b)", true)]
    [InlineData("a/a", "!(a/b)", true)]
    [InlineData("b/b", "!(a/b)", true)]
    public void ShouldNegateWithExtglobASlashB(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a/b", "!*/b", false)]
    [InlineData("a/c", "!*/c", false)]
    [InlineData("a", "!*/b", true)]
    [InlineData("a/a", "!*/b", true)]
    [InlineData("a/b", "!*/c", true)]
    public void ShouldNegateWithStarSlashB(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("bar", "!*a*", false)]
    [InlineData("fab", "!*a*", false)]
    [InlineData("foo", "!*a*", true)]
    public void ShouldNegateWithStarAStar(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a/a", "!a/(*)", false)]
    [InlineData("a", "!a/(*)", true)]
    [InlineData("b/b", "!a/(*)", true)]
    public void ShouldNegateASlashStar(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a/b", "!a/(b)", false)]
    [InlineData("a", "!a/(b)", true)]
    [InlineData("a/a", "!a/(b)", true)]
    [InlineData("b/b", "!a/(b)", true)]
    public void ShouldNegateASlashParenB(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a/a", "!a/*", false)]
    [InlineData("a", "!a/*", true)]
    [InlineData("b/b", "!a/*", true)]
    public void ShouldNegateASlashStarPattern(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("fab", "!f*b", false)]
    [InlineData("bar", "!f*b", true)]
    [InlineData("foo", "!f*b", true)]
    public void ShouldNegateFStarB(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a", "!**", false)]
    public void ShouldNegateWithGlobstar(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a/a", "!**/a", false)]
    [InlineData("a/b", "!**/a", true)]
    [InlineData("b/a", "!**/a", false)]
    public void ShouldNegatePathWithGlobstarSlashA(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a/b", "!a/**", false)]
    [InlineData("b/c", "!a/**", true)]
    public void ShouldNegatePathWithASlashGlobstar(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a/a", "!a/**/a", false)]
    [InlineData("a/b", "!a/**/a", true)]
    [InlineData("b/a", "!a/**/a", true)]
    public void ShouldNegatePathWithGlobstarInMiddle(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a/a/a", "!a/**/a", false)]
    [InlineData("a/b/a", "!a/**/a", false)]
    [InlineData("a/a/b", "!a/**/a", true)]
    [InlineData("b/a/a", "!a/**/a", true)]
    public void ShouldNegateNestedPathWithGlobstar(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData(".md", "!.md", false)]
    [InlineData("a.js", "!**/*.md", true)]
    [InlineData("b.md", "!**/*.md", false)]
    [InlineData("a.js", "!*.md", true)]
    [InlineData("foo.md", "!*.md", false)]
    [InlineData("foo.md", "!.md", true)]
    public void ShouldNegateFilesWithExtensions(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a/a/a.js", "!a/*/a.js", false)]
    [InlineData("a/a/a/a.js", "!a/*/*/a.js", false)]
    [InlineData("b/a/b/a.js", "!a/*/*/a.js", true)]
    [InlineData("a/a.txt", "!a/a*.txt", false)]
    [InlineData("a/b.txt", "!a/a*.txt", true)]
    [InlineData("a.a.txt", "!a.a*.txt", false)]
    [InlineData("a.b.txt", "!a.a*.txt", true)]
    [InlineData("a/a.txt", "!a/*.txt", false)]
    public void ShouldSupportNegatedSingleStarsWithPaths(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a/a/a.js", "!**/a.js", false)]
    [InlineData("a/a/b.js", "!**/a.js", true)]
    [InlineData("a/a/a/a.js", "!a/**/a.js", false)]
    [InlineData("b/a/b/a.js", "!a/**/a.js", true)]
    public void ShouldSupportNegatedGlobstars(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a/b.js", "!**/*.md", true)]
    [InlineData("a/b.md", "!**/*.md", false)]
    [InlineData("a/b.js", "**/*.md", false)]
    public void ShouldSupportGlobstarsWithExtensions(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a/b.md", "!*.md", true)]
    [InlineData("b.md", "!*.md", false)]
    public void ShouldNotMatchSlashesWithSingleStar(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData(".dotfile.md", "!.*.md", false)]
    [InlineData(".dotfile.md", "!*.md", true)]
    [InlineData(".dotfile.txt", "!*.md", true)]
    [InlineData("a/b/.dotfile", "!*.md", true)]
    [InlineData(".gitignore", "!.gitignore", false)]
    [InlineData("a", "!.gitignore", true)]
    public void ShouldNegateDotfiles(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a", "!a/**", false)]
    [InlineData("a/", "!a/**", false)]
    [InlineData("a/b/c", "!a/**", false)]
    [InlineData("b", "!a/**", true)]
    public void ShouldMatchNestedDirectoriesWithGlobstarsNegate(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a", "a!b", false)]
    [InlineData("ab", "a!b", false)]
    [InlineData("a!b", "a!b", true)]
    public void ShouldNotNegateWhenExclamationIsNotAtStart(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a!b", "a\\!b", true)]
    [InlineData("ab", "a\\!b", false)]
    public void ShouldSupportEscapedExclamation(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("!a", "\\!a", true)]
    [InlineData("a", "\\!a", false)]
    public void ShouldMatchEscapedExclamationAtStart(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("!!a", "\\!\\!a", true)]
    [InlineData("!a", "\\!\\!a", false)]
    public void ShouldMatchMultipleEscapedExclamations(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    // [!...] is not bracket negation (picomatch parity); use [^...]
    private static readonly GlobOptions s_PosixOptions = new() { PosixClasses = true };

    [Theory]
    [InlineData("a", "[^a]", false)]
    [InlineData("b", "[^a]", true)]
    [InlineData("c", "[^a]", true)]
    public void ShouldNegateCharacterClassWithExclamation(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern, s_PosixOptions).Should().Be(expected);
    }

    [Theory]
    [InlineData("a", "[^a-c]", false)]
    [InlineData("c", "[^a-c]", false)]
    [InlineData("d", "[^a-c]", true)]
    public void ShouldNegateRangeInBrackets(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern, s_PosixOptions).Should().Be(expected);
    }

    [Theory]
    [InlineData("foobar", "!foo*", false)]
    [InlineData("foo", "!foo*", false)]
    [InlineData("bar", "!foo*", true)]
    public void ShouldNegatePatternWithWildcard(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("foo", "!f?o", false)]
    [InlineData("bar", "!f?o", true)]
    public void ShouldNegatePatternWithQuestionMark(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    // With Negation = false, ! is a literal character
    [Theory]
    [InlineData("a", "!a", false)]
    [InlineData("!a", "!a", true)]
    public void WithNonegateOptionExclamationShouldMatchLiterally(string input, string pattern, bool expected)
    {
        var options = new GlobOptions { Negation = false };
        Glob.IsMatch(input, pattern, options).Should().Be(expected);
    }

    [Theory]
    [InlineData("!abc", "!!abc", false)]
    [InlineData("!!abc", "!!abc", true)]
    [InlineData("abc", "!!abc", false)]
    public void WithNonegateDoubleExclamationShouldMatchLiterally(string input, string pattern, bool expected)
    {
        var options = new GlobOptions { Negation = false };
        Glob.IsMatch(input, pattern, options).Should().Be(expected);
    }

    [Theory]
    [InlineData("!foo.js", "!*.js", true)]
    [InlineData("foo.js", "!*.js", false)]
    [InlineData("!foo.txt", "!*.js", false)]
    public void WithNonegatePatternWithWildcardShouldMatchLiterally(string input, string pattern, bool expected)
    {
        var options = new GlobOptions { Negation = false };
        Glob.IsMatch(input, pattern, options).Should().Be(expected);
    }

    [Theory]
    [InlineData("a/b.js", "!a/*.js", false)]
    [InlineData("a/c.txt", "!a/*.js", true)]
    [InlineData("b/b.js", "!a/*.js", true)]
    public void ShouldNegateNestedPathWithWildcard(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a/b/c.js", "!a/**/*.js", false)]
    [InlineData("a/b/c/d.js", "!a/**/*.js", false)]
    [InlineData("a/b/c.txt", "!a/**/*.js", true)]
    [InlineData("b/c.js", "!a/**/*.js", true)]
    public void ShouldNegateNestedPathWithGlobstarAndWildcard(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

}