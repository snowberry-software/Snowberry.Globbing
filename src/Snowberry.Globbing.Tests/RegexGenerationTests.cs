using Snowberry.Globbing.Compilation;

namespace Snowberry.Globbing.Tests;

public class RegexGenerationTests
{
    private static readonly GlobOptions s_Posix = new() { PathStyle = GlobPathStyle.Posix };

    [Theory]
    [InlineData("test.js", true)]
    [InlineData("test.md", false)]
    public void CompileRegexSource_FastPaths_DoNotChangeResults(string input, bool expected)
    {
        var withFastPaths = new Regex(GlobCompiler.CompileRegexSource("*.js", GlobOptions.Default, fastPaths: true));
        var withoutFastPaths = new Regex(GlobCompiler.CompileRegexSource("*.js", GlobOptions.Default, fastPaths: false));

        withFastPaths.IsMatch(input).Should().Be(expected);
        withoutFastPaths.IsMatch(input).Should().Be(expected);
    }

    [Fact]
    public void CompileRegexSource_WithFastPathsDisabled_DoesNotAllowTrailingSlash()
    {
        var regex = new Regex(GlobCompiler.CompileRegexSource("*.js", s_Posix, fastPaths: false));

        "a.js/".Should().NotMatchRegex(regex);
    }

    [Fact]
    public void CompileRegexSource_WithMatchDotFiles_OmitsDotLookahead()
    {
        TestHelpers.Parse("*.js", new GlobOptions { MatchDotFiles = true }).Output.Should().NotContain("(?!\\.)");
        TestHelpers.Parse("*.js").Output.Should().Contain("(?!\\.)");
    }

    [Fact]
    public void ToRegexString_NegatedPattern_WrapsBodyInNegativeLookahead()
    {
        string source = new Glob("!*.md").ToRegexString();
        var regex = new Regex(source);

        source.Should().Contain("(?!");
        source.Should().EndWith(RegexSyntax.c_AnyNonLineTerminator + "*" + RegexSyntax.c_EndOfInput);
        "test.js".Should().MatchRegex(regex);
        "app.txt".Should().MatchRegex(regex);
        "readme.md".Should().NotMatchRegex(regex);
    }

    [Fact]
    public void ToRegexString_StartsWithAnchor()
    {
        new Glob("*.js").ToRegexString().Should().StartWith("^");
    }

    [Theory]
    [InlineData("foo")]
    [InlineData("*.js")]
    [InlineData("**")]
    [InlineData("a/**/b")]
    [InlineData("**/.*")]
    [InlineData("!*.md")]
    [InlineData("!(*.md)")]
    public void ToRegexString_UsesPortableAnchors(string pattern)
    {
        string source = new Glob(pattern, s_Posix).ToRegexString();

        source.Should().EndWith(RegexSyntax.c_EndOfInput);
        source.Should().NotContain(@"\z");
        source.Should().NotContain(@"\A");
    }

    [Fact]
    public void ToRegexString_WithMatchSubstring_OmitsAnchors()
    {
        string source = new Glob("bar", new GlobOptions { MatchSubstring = true }).ToRegexString();

        source.Should().NotContain("^");
        source.Should().NotContain("$");
    }

    [Theory]
    [InlineData("a.js/", "*.js", true)]
    [InlineData("a.js/", "./*.js", true)]
    [InlineData("a/b.js/", "**/*.js", true)]
    [InlineData("a.js.map/", "*.js.map", true)]
    [InlineData(".a.js/", ".*.js", true)]
    [InlineData("a/b.js/", "a/*.js", false)]
    [InlineData("a.js//", "*.js", false)]
    public void ToRegex_FastPathExtensionPattern_AllowsTrailingSlash(string input, string pattern, bool expected)
    {
        new Glob(pattern, s_Posix).ToRegex().IsMatch(input).Should().Be(expected);
    }

    [Fact]
    public void ToRegex_FastPathExtensionPattern_WithStrictSlashes_DoesNotAllowTrailingSlash()
    {
        var regex = new Glob("*.js", s_Posix with { StrictSlashes = true }).ToRegex();

        "a.js/".Should().NotMatchRegex(regex);
    }

    [Theory]
    [InlineData("foo\n", "foo", false)]
    [InlineData("a.js\n", "*.js", false)]
    [InlineData("a/b.js\n", "**/*.js", false)]
    [InlineData("a\n", "[a-z]", false)]
    [InlineData("a/b\n", "a/**/b", false)]
    [InlineData("foo", "foo", true)]
    [InlineData("a.js", "*.js", true)]
    public void ToRegex_TrailingLineBreak_DoesNotMatch(string input, string pattern, bool expected)
    {
        new Glob(pattern, s_Posix).ToRegex().IsMatch(input).Should().Be(expected);
    }
}