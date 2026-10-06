namespace Snowberry.Globbing.Tests;

/// <summary>
/// Tests for regex features in picomatch.
/// Ported from: https://github.com/micromatch/picomatch/blob/master/test/regex-features.js
/// </summary>
public class RegexFeaturesTests
{
    [Theory]
    [InlineData("Foo", "foo", true)]
    [InlineData("FoO", "fOo", true)]
    public void Regex_CaseInsensitive(string input, string pattern, bool expected)
    {
        var options = new GlobOptions { IgnoreCase = true };
        Glob.IsMatch(input, pattern, options).Should().Be(expected);
    }

    [Theory]
    [InlineData("FOO", "foo", false)]
    [InlineData("foo", "FOO", false)]
    public void Regex_CaseSensitive_Default(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("d", "[^abc]", true)]
    [InlineData("a", "[^abc]", false)]
    [InlineData("0", "[a-z]", false)]
    [InlineData("0", "[0-9]", true)]
    [InlineData("9", "[0-9]", true)]
    [InlineData("a", "[a-zA-Z]", true)]
    [InlineData("Z", "[a-zA-Z]", true)]
    [InlineData("5", "[a-zA-Z]", false)]
    public void Regex_CharacterClasses(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("foo.txt", "*.js", false)]
    [InlineData("a/b/foo.js", "**/*.js", true)]
    [InlineData("foo.js", "**/*.js", true)]
    [InlineData("src/components/Button.tsx", "src/**/*.tsx", true)]
    [InlineData("src/utils/helpers.ts", "src/**/*.tsx", false)]
    [InlineData("test/Button.tsx", "src/**/*.tsx", false)]
    [InlineData("README.md", "*.md", true)]
    [InlineData("docs/README.md", "docs/*.md", true)]
    [InlineData("src/README.md", "docs/*.md", false)]
    public void Regex_CommonFilePatterns(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a", ".", false)]
    [InlineData("ab", "..", false)]
    [InlineData(".", ".", true)]
    [InlineData("..", "..", true)]
    public void Regex_DotCharacter(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a", "@(a)", true)]
    [InlineData("b", "@(a|b)", true)]
    [InlineData("c", "@(a|b)", false)]
    [InlineData("a", "+(a)", true)]
    [InlineData("aaa", "+(a)", true)]
    [InlineData("", "+(a)", false)]
    [InlineData("b", "+(a)", false)]
    [InlineData("a", "*(a)", true)]
    [InlineData("aaa", "*(a)", true)]
    [InlineData("b", "*(a)", false)]
    [InlineData("a", "?(a)", true)]
    [InlineData("c", "!(a)", true)]
    public void Regex_ExtglobQuantifiers(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a/c", "a/**/c", true)]
    [InlineData("a/x/y/z/c", "a/**/c", true)]
    [InlineData("a/b", "a/**/c", false)]
    [InlineData("c.txt", "**/c.txt", true)]
    [InlineData("x/y/c.txt", "**/c.txt", true)]
    [InlineData("a/b/d.txt", "**/c.txt", false)]
    [InlineData("a/x", "a/**", true)]
    [InlineData("b/c", "a/**", false)]
    public void Regex_Globstar(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("foobar", "foo", false)]
    [InlineData("barfoo", "foo", false)]
    [InlineData("foo ", "foo", false)]
    [InlineData("foo bar", "foo bar", true)]
    [InlineData("", "a", false)]
    [InlineData("/", "/", true)]
    public void Regex_LiteralMatching(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("abc", "[[:alpha:]]+", true)]
    [InlineData("a", "[[:alpha:]]+", true)]
    [InlineData("a1", "[[:alpha:]]+", false)]
    public void Regex_PosixClassWithPlusQuantifier(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern, new GlobOptions { PosixClasses = true }).Should().Be(expected);
    }

    [Theory]
    [InlineData("foobar", "foo*", true)]
    [InlineData("foo", "foo*", true)]
    [InlineData("barfoo", "foo*", false)]
    [InlineData("barfoo", "*foo", true)]
    [InlineData("foo", "*foo", true)]
    [InlineData("foobar", "*foo", false)]
    public void Regex_Star(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }
}