namespace Snowberry.Globbing.Tests;

/// <summary>
/// Tests for regex features in picomatch.
/// Ported from: https://github.com/micromatch/picomatch/blob/master/test/regex-features.js
/// </summary>
public class RegexFeaturesTests
{

    [Theory]
    [InlineData("a", "a", true)]
    [InlineData("a", "b", false)]
    [InlineData("ab", "ab", true)]
    [InlineData("ab", "a", false)]
    public void Regex_LiteralMatching(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a", ".", false)]   // JS: . is literal, doesn't match 'a'
    [InlineData("b", ".", false)]   // JS: . is literal, doesn't match 'b'
    [InlineData("ab", "..", false)] // JS: . is literal, doesn't match 'ab'
    [InlineData(".", ".", true)]    // Literal . matches .
    [InlineData("..", "..", true)]  // Literal .. matches ..
    public void Regex_DotCharacter(string input, string pattern, bool expected)
    {
        // Note: In glob patterns, '.' is a literal character
        // This tests that . in globs is escaped properly
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a", "[abc]", true)]
    [InlineData("b", "[abc]", true)]
    [InlineData("c", "[abc]", true)]
    [InlineData("d", "[abc]", false)]
    public void Regex_CharacterClass(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("d", "[^abc]", true)]
    [InlineData("e", "[^abc]", true)]
    [InlineData("a", "[^abc]", false)]
    [InlineData("b", "[^abc]", false)]
    public void Regex_NegatedCharacterClass(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a", "[a-z]", true)]
    [InlineData("m", "[a-z]", true)]
    [InlineData("z", "[a-z]", true)]
    [InlineData("A", "[a-z]", false)]
    [InlineData("0", "[a-z]", false)]
    public void Regex_CharacterRange(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("5", "[0-9]", true)]
    [InlineData("0", "[0-9]", true)]
    [InlineData("9", "[0-9]", true)]
    [InlineData("a", "[0-9]", false)]
    public void Regex_NumericRange(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a", "[a-zA-Z]", true)]
    [InlineData("Z", "[a-zA-Z]", true)]
    [InlineData("M", "[a-zA-Z]", true)]
    [InlineData("5", "[a-zA-Z]", false)]
    public void Regex_MixedRange(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a.b", "a.b", true)]
    [InlineData("a.b", "a?b", true)]
    [InlineData("axb", "a.b", false)]
    [InlineData("axb", "a?b", true)]
    public void Regex_DotInPattern(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a*b", "a\\*b", true)]
    [InlineData("ab", "a\\*b", false)]
    [InlineData("axxb", "a\\*b", false)]
    public void Regex_EscapedStar(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a?b", "a\\?b", true)]
    [InlineData("axb", "a\\?b", false)]
    [InlineData("ab", "a\\?b", false)]
    public void Regex_EscapedQuestion(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a[b", "a\\[b", true)]
    [InlineData("a]b", "a\\]b", true)]
    [InlineData("ab", "a\\[b", false)]
    public void Regex_EscapedBrackets(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("foo", "foo", true)]
    [InlineData("foobar", "foo", false)]
    [InlineData("barfoo", "foo", false)]
    public void Regex_FullMatchAnchoring(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("foo", "*", true)]
    [InlineData("foobar", "*", true)]
    [InlineData("a/b", "*", false)]
    public void Regex_StarAnchoring(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("foobar", "foo*", true)]
    [InlineData("foo", "foo*", true)]
    [InlineData("barfoo", "foo*", false)]
    public void Regex_StarSuffix(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("barfoo", "*foo", true)]
    [InlineData("foo", "*foo", true)]
    [InlineData("foobar", "*foo", false)]
    public void Regex_StarPrefix(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a", "**", true)]
    [InlineData("a/b", "**", true)]
    [InlineData("a/b/c", "**", true)]
    [InlineData("a/b/c/d", "**", true)]
    public void Regex_Globstar(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a/b/c", "a/**/c", true)]
    [InlineData("a/c", "a/**/c", true)]
    [InlineData("a/x/y/z/c", "a/**/c", true)]
    [InlineData("a/b", "a/**/c", false)]
    public void Regex_GlobstarBetweenSegments(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a/b/c.txt", "**/c.txt", true)]
    [InlineData("c.txt", "**/c.txt", true)]
    [InlineData("x/y/c.txt", "**/c.txt", true)]
    [InlineData("a/b/d.txt", "**/c.txt", false)]
    public void Regex_GlobstarPrefix(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a/b/c", "a/**", true)]
    [InlineData("a/x", "a/**", true)]
    [InlineData("a", "a/**", true)]
    [InlineData("b/c", "a/**", false)]
    public void Regex_GlobstarSuffix(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("FOO", "foo", false)]
    [InlineData("foo", "FOO", false)]
    [InlineData("Foo", "foo", false)]
    public void Regex_CaseSensitive_Default(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("FOO", "foo", true)]
    [InlineData("foo", "FOO", true)]
    [InlineData("Foo", "foo", true)]
    [InlineData("FoO", "fOo", true)]
    public void Regex_CaseInsensitive(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { NoCase = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("a", "@(a)", true)]
    [InlineData("a", "@(a|b)", true)]
    [InlineData("b", "@(a|b)", true)]
    [InlineData("c", "@(a|b)", false)]
    public void Regex_ExactlyOne(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a", "+(a)", true)]
    [InlineData("aa", "+(a)", true)]
    [InlineData("aaa", "+(a)", true)]
    [InlineData("", "+(a)", false)]
    [InlineData("b", "+(a)", false)]
    public void Regex_OneOrMore(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("", "*(a)", false)]  // JS returns false
    [InlineData("a", "*(a)", true)]
    [InlineData("aa", "*(a)", true)]
    [InlineData("aaa", "*(a)", true)]
    [InlineData("b", "*(a)", false)]
    public void Regex_ZeroOrMore(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("", "?(a)", false)]  // JS returns false
    [InlineData("a", "?(a)", true)]
    [InlineData("aa", "?(a)", false)]
    public void Regex_ZeroOrOne(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("b", "!(a)", true)]
    [InlineData("c", "!(a)", true)]
    [InlineData("a", "!(a)", false)]
    public void Regex_Negation(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("foo.js", "*.js", true)]
    [InlineData("foo.txt", "*.js", false)]
    [InlineData("a/b/foo.js", "**/*.js", true)]
    [InlineData("foo.js", "**/*.js", true)]
    public void Regex_FileExtension(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("src/components/Button.tsx", "src/**/*.tsx", true)]
    [InlineData("src/utils/helpers.ts", "src/**/*.tsx", false)]
    [InlineData("test/Button.tsx", "src/**/*.tsx", false)]
    public void Regex_DirectoryPattern(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("README.md", "*.md", true)]
    [InlineData("docs/README.md", "**/*.md", true)]
    [InlineData("docs/README.md", "docs/*.md", true)]
    [InlineData("src/README.md", "docs/*.md", false)]
    public void Regex_MarkdownFiles(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    // Note: Empty pattern throws exception in C# library (by design)
    // [InlineData("", "", true)]
    // [InlineData("a", "", false)]
    [Theory]
    [InlineData("", "a", false)]
    public void Regex_EmptyStrings(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("foo", "foo", true)]
    [InlineData("foo ", "foo", false)]
    [InlineData(" foo", "foo", false)]
    [InlineData("foo bar", "foo bar", true)]
    public void Regex_Whitespace(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("/", "/", true)]
    [InlineData("a/", "a/", true)]
    [InlineData("/a", "/a", true)]
    public void Regex_Slashes(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

}
