using Snowberry.Globbing.Compilation;

namespace Snowberry.Globbing.Tests;

public class RegexGenerationTests
{
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
        var regex = new Regex(GlobCompiler.CompileRegexSource("*.js", TestOptions.Posix, fastPaths: false));

        "a.js/".Should().NotMatchRegex(regex);
    }

    [Fact]
    public void CompileRegexSource_WithMatchDotFiles_OmitsDotLookahead()
    {
        GlobCompiler.CompileRegexSource("**", new GlobOptions { MatchDotFiles = true }, fastPaths: false).Should().NotContain("(?!\\.)");
        GlobCompiler.CompileRegexSource("**", GlobOptions.Default, fastPaths: false).Should().Contain("(?!\\.)");
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
        string source = new Glob(pattern, TestOptions.Posix).ToRegexString();

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
        new Glob(pattern, TestOptions.Posix).ToRegex().IsMatch(input).Should().Be(expected);
    }

    [Fact]
    public void ToRegex_FastPathExtensionPattern_WithStrictSlashes_DoesNotAllowTrailingSlash()
    {
        var regex = new Glob("*.js", TestOptions.Posix with { StrictSlashes = true }).ToRegex();

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
        new Glob(pattern, TestOptions.Posix).ToRegex().IsMatch(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("*a*a*b", @"[^/a]*a[^/a]*a[^/]*b")]
    [InlineData("*-*.js", @"[^/\-]*-[^/]*\.js")]
    [InlineData("*ab*ab*c", @"[^/a]*(?:a(?!b)[^/a]*)*ab[^/a]*(?:a(?!b)[^/a]*)*ab[^/]*c")]
    [InlineData("*.test.*", @"[^/]*\.test\.[^/]*")]
    public void ToRegexString_StarBeforeLiteralAndStar_StopsAtFirstOccurrence(string pattern, string expected)
    {
        new Glob(pattern, TestOptions.Posix).ToRegexString().Should().Contain(expected);
    }

    [Theory]
    [InlineData("*a*a*b")]
    [InlineData("(*a)*b")]
    [InlineData("(*a*a*b)")]
    public void ToRegexString_StarsObservableThroughCaptures_StayGreedy(string pattern)
    {
        new Glob(pattern, TestOptions.Posix with { CaptureGroups = pattern[0] != '(' }).ToRegexString().Should().NotContain("[^/a]");
    }

    [Theory]
    [InlineData("*aab*", "aaab", true)]
    [InlineData("*aab*", "aaba", true)]
    [InlineData("*ab*ab*c", "ababc", true)]
    [InlineData("*ab*ab*c", "aabab_c", true)]
    [InlineData("*ab*ab*c", "abac", false)]
    [InlineData("*ab*ab*ab*c", "abababc", true)]
    [InlineData("*ab*ab*ab*c", "ababaabc", true)]
    [InlineData("*ab*ab*ab*c", "ababac", false)]
    [InlineData("*a*a*b", "aab", true)]
    [InlineData("*a*a*b", "ab", false)]
    [InlineData("*a*a*b", "a/ab", false)]
    [InlineData("x*.*.*", "x..", true)]
    [InlineData("x*.*.*", "x.", false)]
    [InlineData("*A*a*b", "aAab", true)]
    public void ToRegex_StarBeforeLiteralAndStar_MatchesAsBefore(string pattern, string input, bool expected)
    {
        new Glob(pattern, TestOptions.Posix).ToRegex().IsMatch(input).Should().Be(expected);
        Glob.IsMatch(input.ToUpperInvariant(), pattern, TestOptions.Posix with { IgnoreCase = true }).Should().Be(expected);
    }

    [Fact]
    public void ToRegex_LongStarChain_MatchesInLinearTime()
    {
        // Twelve plain stars backtrack for hours on this input; each bounded star has one way to reach each split point.
        var regex = new Regex(new Glob("*a*a*a*a*a*a*a*a*a*a*a*a*b", TestOptions.Posix).ToRegexString(), RegexOptions.None, TimeSpan.FromSeconds(5));

        regex.IsMatch(new string('a', 100_000) + "/b").Should().BeFalse();
        regex.IsMatch(new string('a', 100_000) + "b").Should().BeTrue();
    }
}