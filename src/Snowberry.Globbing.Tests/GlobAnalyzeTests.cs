namespace Snowberry.Globbing.Tests;

public class GlobAnalyzeTests
{
    private static readonly GlobOptions s_Posix = new() { PathStyle = GlobPathStyle.Posix };

    [Fact]
    public void Analyze_WithNullPattern_ThrowsArgumentNullException()
    {
        FluentActions.Invoking(() => Glob.Analyze(null!)).Should().ThrowExactly<ArgumentNullException>();
    }

    [Fact]
    public void Analyze_DescribesPatternStructure()
    {
        var info = Glob.Analyze("src/lib/**/*.{cs,csproj}");

        info.Pattern.Should().Be("src/lib/**/*.{cs,csproj}");
        info.BasePath.Should().Be("src/lib");
        info.GlobPart.Should().Be("**/*.{cs,csproj}");
        info.IsGlob.Should().BeTrue();
        info.HasGlobstar.Should().BeTrue();
        info.HasBraces.Should().BeTrue();
        info.HasBrackets.Should().BeFalse();
        info.HasExtglob.Should().BeFalse();
        info.IsNegated.Should().BeFalse();
        info.Segments.Should().Equal(["src", "lib", "**", "*.{cs,csproj}"]);
    }

    [Theory]
    [InlineData("!./foo/*.js", "!./", "foo", "*.js", "foo|*.js")]
    [InlineData("./foo/*.js", "./", "foo", "*.js", "foo|*.js")]
    [InlineData("foo", "", "foo", "", "foo")]
    [InlineData("/abs/x", "", "/abs/x", "", "|abs|x")]
    [InlineData("a/", "", "a/", "", "a|")]
    [InlineData("!(a)/b", "", "", "!(a)/b", "!(a)|b")]
    public void Analyze_MatchesPicomatchScan(string pattern, string prefix, string basePath, string globPart, string segments)
    {
        var info = Glob.Analyze(pattern);

        info.Prefix.Should().Be(prefix);
        info.BasePath.Should().Be(basePath);
        info.GlobPart.Should().Be(globPart);
        info.Segments.Should().Equal(segments.Split('|'));
    }

    [Theory]
    [InlineData("foo/bar/baz", "foo|bar|baz")]
    [InlineData("foo/*/bar", "foo|*|bar")]
    [InlineData("foo/**/bar", "foo|**|bar")]
    public void Analyze_Segments_SplitsAtSeparators(string pattern, string segments)
    {
        Glob.Analyze(pattern).Segments.Should().Equal(segments.Split('|'));
    }

    [Theory]
    [InlineData("*")]
    [InlineData("foo/bar/baz.js")]
    [InlineData("!./foo/*.js")]
    [InlineData(@"foo\bar")]
    [InlineData(@"foo\*")]
    [InlineData(@"foo\**\bar")]
    public void Analyze_Pattern_IsOriginalPattern(string pattern)
    {
        Glob.Analyze(pattern).Pattern.Should().Be(pattern);
    }

    [Theory]
    [InlineData("./foo", "./")]
    [InlineData("./foo/bar", "./")]
    [InlineData("/foo", "")]
    [InlineData("/foo/bar", "")]
    [InlineData("/foo/bar/*", "")]
    [InlineData("*", "")]
    [InlineData("foo/*", "")]
    [InlineData("foo/bar/*", "")]
    public void Analyze_Prefix_IsLeadingNegationOrDotSlash(string pattern, string expectedPrefix)
    {
        Glob.Analyze(pattern).Prefix.Should().Be(expectedPrefix);
    }

    [Theory]
    [InlineData("foo", "foo", "")]
    [InlineData("foo/bar", "foo/bar", "")]
    [InlineData("*", "", "*")]
    [InlineData("**", "", "**")]
    [InlineData("foo/*", "foo", "*")]
    [InlineData("foo/**", "foo", "**")]
    [InlineData("foo/bar/*", "foo/bar", "*")]
    [InlineData("foo/bar/**", "foo/bar", "**")]
    [InlineData("foo/bar/*.js", "foo/bar", "*.js")]
    [InlineData("a/b/c/*.txt", "a/b/c", "*.txt")]
    [InlineData("a/**/b", "a", "**/b")]
    [InlineData("a/**/b/*.txt", "a", "**/b/*.txt")]
    public void Analyze_SplitsBasePathFromGlobPart(string pattern, string expectedBasePath, string expectedGlobPart)
    {
        var info = Glob.Analyze(pattern);

        info.BasePath.Should().Be(expectedBasePath);
        info.GlobPart.Should().Be(expectedGlobPart);
    }

    [Theory]
    [InlineData("*", true)]
    [InlineData("*.js", true)]
    [InlineData("**/*.js", true)]
    [InlineData("foo?bar", true)]
    [InlineData("[abc]", true)]
    [InlineData("{a,b}", true)]
    [InlineData("+(a|b)", true)]
    [InlineData("!*.md", true)]
    [InlineData("!./foo/*.js", true)]
    [InlineData("foo", false)]
    [InlineData("test.js", false)]
    [InlineData("foo/bar.js", false)]
    [InlineData("foo/bar/baz.js", false)]
    public void Analyze_IsGlob_DetectsGlobSyntax(string pattern, bool expected)
    {
        Glob.Analyze(pattern).IsGlob.Should().Be(expected);
    }

    [Theory]
    [InlineData("{a,b}", true)]
    [InlineData("{a,b,c}", true)]
    [InlineData("foo/{a,b}/bar", true)]
    [InlineData("*.{js,ts}", true)]
    [InlineData("foo", false)]
    [InlineData("*", false)]
    [InlineData("**", false)]
    [InlineData("[abc]", false)]
    public void Analyze_HasBraces_DetectsBraces(string pattern, bool expected)
    {
        Glob.Analyze(pattern).HasBraces.Should().Be(expected);
    }

    [Theory]
    [InlineData("[abc]", true)]
    [InlineData("[a-z]", true)]
    [InlineData("[!abc]", true)]
    [InlineData("foo/[abc]/bar", true)]
    [InlineData("*.[ch]", true)]
    [InlineData("[abc].js", true)]
    [InlineData("foo", false)]
    [InlineData("*", false)]
    [InlineData("**", false)]
    [InlineData("{a,b}", false)]
    public void Analyze_HasBrackets_DetectsBrackets(string pattern, bool expected)
    {
        Glob.Analyze(pattern).HasBrackets.Should().Be(expected);
    }

    [Theory]
    [InlineData("**", true)]
    [InlineData("a/**", true)]
    [InlineData("a/**/b", true)]
    [InlineData("**/*.txt", true)]
    [InlineData("foo/**/bar/**/baz", true)]
    [InlineData("*", false)]
    [InlineData("foo", false)]
    [InlineData("foo/*", false)]
    [InlineData("foo/*/bar", false)]
    public void Analyze_HasGlobstar_DetectsGlobstar(string pattern, bool expected)
    {
        Glob.Analyze(pattern).HasGlobstar.Should().Be(expected);
    }

    [Theory]
    [InlineData("!(foo)", true)]
    [InlineData("@(foo)", true)]
    [InlineData("*(foo)", true)]
    [InlineData("+(foo)", true)]
    [InlineData("?(foo)", true)]
    [InlineData("!(a|b)", true)]
    [InlineData("@(a|b)", true)]
    [InlineData("*(a|b)", true)]
    [InlineData("foo/!(bar)/baz", true)]
    [InlineData("foo", false)]
    [InlineData("*", false)]
    [InlineData("**", false)]
    [InlineData("[abc]", false)]
    [InlineData("{a,b}", false)]
    public void Analyze_HasExtglob_DetectsExtglob(string pattern, bool expected)
    {
        Glob.Analyze(pattern).HasExtglob.Should().Be(expected);
    }

    [Theory]
    [InlineData("!foo", true)]
    [InlineData("!*", true)]
    [InlineData("!**", true)]
    [InlineData("!*.md", true)]
    [InlineData("!foo/bar", true)]
    [InlineData("!foo/**/bar", true)]
    [InlineData("!./foo/*.js", true)]
    [InlineData("foo", false)]
    [InlineData("*", false)]
    [InlineData("**", false)]
    [InlineData("foo/bar", false)]
    public void Analyze_IsNegated_DetectsLeadingExclamationMark(string pattern, bool expected)
    {
        Glob.Analyze(pattern).IsNegated.Should().Be(expected);
    }

    [Fact]
    public void Analyze_DoubleNegation_IsNotNegated()
    {
        var info = Glob.Analyze("!!a/*.js");

        info.IsNegated.Should().BeFalse();
        info.Prefix.Should().Be("!!");
    }

    [Fact]
    public void Analyze_NegatedPattern_ReportsBasePathWithoutNegation()
    {
        var info = Glob.Analyze("!src/**/*.test.js");

        info.IsGlob.Should().BeTrue();
        info.HasGlobstar.Should().BeTrue();
        info.IsNegated.Should().BeTrue();
        info.BasePath.Should().Be("src");
    }

    [Theory]
    [InlineData("!(foo)", true)]
    [InlineData("!(a|b)", true)]
    [InlineData("!(a)/!(b)", true)]
    [InlineData("@(foo)", false)]
    [InlineData("*(foo)", false)]
    [InlineData("+(foo)", false)]
    [InlineData("?(foo)", false)]
    [InlineData("foo", false)]
    [InlineData("!foo", false)]
    // Only a negated extglob at the start of the pattern counts.
    [InlineData("foo/!(bar)/baz", false)]
    public void Analyze_IsNegatedExtglob_DetectsLeadingNegatedExtglob(string pattern, bool expected)
    {
        Glob.Analyze(pattern).IsNegatedExtglob.Should().Be(expected);
    }

    [Fact]
    public void Analyze_ExtglobWithBraces_ReportsAllFeatures()
    {
        var info = Glob.Analyze("src/!(test)/**/*.{js,ts}");

        info.IsGlob.Should().BeTrue();
        info.HasGlobstar.Should().BeTrue();
        info.HasBraces.Should().BeTrue();
        info.HasExtglob.Should().BeTrue();
        info.IsNegatedExtglob.Should().BeFalse();
    }

    [Fact]
    public void Analyze_WithStrictBrackets_DoesNotThrow()
    {
        var info = Glob.Analyze("a/[b", new GlobOptions { StrictBrackets = true });

        info.IsGlob.Should().BeFalse();
        info.BasePath.Should().Be("a/[b");
    }

    [Theory]
    [InlineData("a/{b,c}", false)]
    [InlineData("a/[bc]", false)]
    [InlineData("a/+(b)", true)]
    public void Analyze_WithSyntaxDisabled_DoesNotReportIt(string pattern, bool expectedIsGlob)
    {
        var options = new GlobOptions { BraceExpansion = false, BracketExpressions = false, Extglobs = false };

        var info = Glob.Analyze(pattern, options);

        info.IsGlob.Should().Be(expectedIsGlob);
        info.HasBraces.Should().BeFalse();
        info.HasBrackets.Should().BeFalse();
        info.HasExtglob.Should().BeFalse();
    }

    [Theory]
    [InlineData(@"a/\*/b")]
    [InlineData("a/(b")]
    [InlineData("a/{b")]
    [InlineData("a/[b")]
    public void Analyze_EscapedOrUnbalancedSyntax_IsNotAGlob(string pattern)
    {
        var info = Glob.Analyze(pattern, s_Posix);

        info.IsGlob.Should().BeFalse();
        info.BasePath.Should().Be(pattern);
        info.GlobPart.Should().Be("");
    }

    [Fact]
    public void Analyze_SeparatorsInsideBraces_DoNotSplitSegments()
    {
        var info = Glob.Analyze("src/{a/b,c}/*.cs", s_Posix);

        info.Segments.Should().Equal(["src", "{a/b,c}", "*.cs"]);
        info.BasePath.Should().Be("src");
        info.GlobPart.Should().Be("{a/b,c}/*.cs");
    }
}
