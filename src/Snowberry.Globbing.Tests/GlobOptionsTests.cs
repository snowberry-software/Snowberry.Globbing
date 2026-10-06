namespace Snowberry.Globbing.Tests;

public class GlobOptionsTests
{
    private static readonly string[] s_CompiledInputs =
    [
        "app.js",
        "src/components/Button.tsx",
        "src/utils/helper.js",
        "src/utils/helper.test.js",
        "README.md",
        "test-3a.txt",
        "a/x/y/b/main.js",
        "foo/bar/qux.js",
        "foo/bar/baz.js",
        "node_modules/pkg/index.js",
        "b/deep/nested/file.cs",
        "alpha.log",
        "bc_d.txt",
        ".hidden"
    ];

    private static readonly string[] s_CompiledPatterns =
    [
        "*.js",
        "**/*.{js,ts,jsx,tsx}",
        "src/**/!(*.test|*.spec).{js,jsx,ts,tsx}",
        "!(*.md)",
        "test-[0-9][a-z].txt",
        "a/**/b/*.@(js|ts)",
        "+(foo|bar)/baz",
        "**/node_modules/**",
        "{a,b,c}/**/*.cs",
        "[[:alpha:]]*.log",
        "foo/**/bar/!(qux).js",
        "?(a|b)c*d.txt",
        "!*.md",
        "*"
    ];

    private static readonly GlobOptions s_Posix = new() { PathStyle = GlobPathStyle.Posix };

    public static TheoryData<string> CompiledPatterns()
    {
        var data = new TheoryData<string>();
        foreach (string p in s_CompiledPatterns)
            data.Add(p);
        return data;
    }

    [Theory]
    [InlineData("a", "a", true)]
    [InlineData("abc", "a*c", true)]
    [InlineData("test.js", "*", true)]
    [InlineData("abd", "a*c", false)]
    public void BashCompatibility_MatchesPlainGlobs(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern, new GlobOptions { BashCompatibility = true }).Should().Be(expected);
    }

    [Theory]
    [InlineData(true, "test.js", true)]
    [InlineData(true, "test.ts", true)]
    [InlineData(true, "test.{js,ts}", false)]
    [InlineData(false, "test.{js,ts}", true)]
    [InlineData(false, "test.js", false)]
    public void BraceExpansion_ControlsBraceSyntax(bool braceExpansion, string input, bool expected)
    {
        new Glob("*.{js,ts}", new GlobOptions { BraceExpansion = braceExpansion }).IsMatch(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("a1b", true)]
    [InlineData("a2b", true)]
    [InlineData("a3b", true)]
    [InlineData("a4b", false)]
    public void BraceRangeExpander_ReplacesBuiltInRangeConversion(string input, bool expected)
    {
        var options = new GlobOptions
        {
            BraceRangeExpander = range =>
            {
                var values = new List<string>();
                for (int i = int.Parse(range[0]); i <= int.Parse(range[1]); i++)
                    values.Add(i.ToString());
                return $"({string.Join("|", values)})";
            }
        };

        Glob.IsMatch(input, "a{1..3}b", options).Should().Be(expected);
    }

    [Theory]
    [InlineData(true, "a.txt", true)]
    [InlineData(true, "b.txt", true)]
    [InlineData(false, "[abc].txt", true)]
    [InlineData(false, "a.txt", false)]
    public void BracketExpressions_ControlsBracketSyntax(bool bracketExpressions, string input, bool expected)
    {
        new Glob("[abc].txt", new GlobOptions { BracketExpressions = bracketExpressions }).IsMatch(input).Should().Be(expected);
    }

    [Theory]
    [InlineData(GlobBracketMode.Literal, "[abc].js", true)]
    [InlineData(GlobBracketMode.Literal, "a.js", false)]
    [InlineData(GlobBracketMode.CharacterClass, "a.js", true)]
    [InlineData(GlobBracketMode.CharacterClass, "b.js", true)]
    // An input equal to the pattern always matches, as in picomatch.
    [InlineData(GlobBracketMode.CharacterClass, "[abc].js", true)]
    public void BracketMode_ControlsBracketInterpretation(GlobBracketMode bracketMode, string input, bool expected)
    {
        new Glob("[abc].js", new GlobOptions { BracketMode = bracketMode }).IsMatch(input).Should().Be(expected);
    }

    [Fact]
    public void Default_HasDocumentedValues()
    {
        foreach (var options in new[] { new GlobOptions(), GlobOptions.Default })
        {
            options.IgnoreCase.Should().BeFalse();
            options.MatchDotFiles.Should().BeFalse();
            options.MatchFileNameOnly.Should().BeFalse();
            options.MatchSubstring.Should().BeFalse();
            options.PathStyle.Should().Be(GlobPathStyle.Auto);
            options.BraceExpansion.Should().BeTrue();
            options.BracketExpressions.Should().BeTrue();
            options.Extglobs.Should().BeTrue();
            options.Globstar.Should().BeTrue();
            options.Negation.Should().BeTrue();
            options.PosixClasses.Should().BeTrue();
            options.BracketMode.Should().Be(GlobBracketMode.Auto);
            options.StrictSlashes.Should().BeFalse();
            options.StrictBrackets.Should().BeFalse();
            options.BashCompatibility.Should().BeFalse();
            options.RegexQuantifiers.Should().BeFalse();
            options.KeepQuotes.Should().BeFalse();
            options.Unescape.Should().BeFalse();
            options.CaptureGroups.Should().BeFalse();
            options.RegexOptions.Should().Be(RegexOptions.None);
            options.MaxPatternLength.Should().Be(65536);
            options.IgnorePatterns.Should().BeEmpty();
            options.InputNormalizer.Should().BeNull();
            options.BraceRangeExpander.Should().BeNull();
        }
    }

    [Theory]
    [InlineData(true, "a", "+(a)", true)]
    [InlineData(true, "aa", "+(a)", true)]
    [InlineData(false, "a", "+(a)", false)]
    [InlineData(false, "@(a|b)", "@(a|b)", true)]
    [InlineData(false, "a", "@(a|b)", false)]
    [InlineData(false, "b", "@(a|b)", false)]
    [InlineData(false, "a*(b)", "a*(b)", true)]
    // The star stays a wildcard and the parentheses become a plain group, as in picomatch.
    [InlineData(false, "ab", "a*(b)", true)]
    [InlineData(false, "a+(b)", "a+(b)", true)]
    [InlineData(false, "a?(b)", "a?(b)", true)]
    [InlineData(false, "a@(b)", "a@(b)", true)]
    [InlineData(false, "a!(b)", "a!(b)", true)]
    public void Extglobs_ControlsExtglobSyntax(bool extglobs, string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern, new GlobOptions { Extglobs = extglobs }).Should().Be(expected);
    }

    [Theory]
    [InlineData("foo", "**", true)]
    [InlineData("a", "**", true)]
    [InlineData("foo/bar", "**", false)]
    [InlineData("a/b/c", "**", false)]
    [InlineData("a/b/c", "**/c", false)]
    [InlineData("a/b/c", "a/**", false)]
    [InlineData("a/b/c", "a/**/c", true)]
    [InlineData("b", "**/b", false)]
    [InlineData("**", "**", true)]
    [InlineData("foo", "*", true)]
    [InlineData("foo.txt", "*.txt", true)]
    [InlineData("a/foo", "a/*", true)]
    [InlineData("foo", "@(foo|bar)", true)]
    [InlineData("foo", "!(baz)", true)]
    [InlineData("foo", "f??", true)]
    [InlineData("foo", "[f]oo", true)]
    public void Globstar_Disabled_TreatsDoubleStarAsSingleStar(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern, new GlobOptions { Globstar = false }).Should().Be(expected);
    }

    [Theory]
    [InlineData(false, "a", "A", false)]
    [InlineData(false, "ABC", "abc", false)]
    [InlineData(false, "test.JS", "*.js", false)]
    [InlineData(false, "test.js", "*.js", true)]
    [InlineData(true, "a", "A", true)]
    [InlineData(true, "A", "a", true)]
    [InlineData(true, "abc", "ABC", true)]
    [InlineData(true, "aBc", "AbC", true)]
    [InlineData(true, "test.JS", "*.js", true)]
    [InlineData(true, "test.jS", "*.JS", true)]
    [InlineData(true, "A.txt", "*.TXT", true)]
    [InlineData(true, "SRC/file.js", "src/*", true)]
    [InlineData(true, "src/file.js", "SRC/*", true)]
    public void IgnoreCase_ControlsCaseSensitivity(bool ignoreCase, string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern, new GlobOptions { IgnoreCase = ignoreCase }).Should().Be(expected);
    }

    [Theory]
    [InlineData("SRC/file.js", true)]
    [InlineData("src/FILE.JS", true)]
    [InlineData("SRC/file.md", false)]
    public void IgnoreCase_WithMatchFileNameOnly_MatchesFileNameInAnyCase(string input, bool expected)
    {
        var options = new GlobOptions { MatchFileNameOnly = true, IgnoreCase = true };

        Glob.IsMatch(input, "*.js", options).Should().Be(expected);
    }

    [Fact]
    public void IgnorePatterns_AreCopiedWhenSet()
    {
        var source = new List<string> { "*.md" };
        var options = s_Posix with { IgnorePatterns = source };

        source.Add("*.js");
        source[0] = "*.txt";

        options.IgnorePatterns.Should().Equal(["*.md"]);
        new Glob("*", options).IsMatch("a.js").Should().BeTrue();
        new Glob("*", options).IsMatch("a.md").Should().BeFalse();
    }

    [Theory]
    [InlineData(new[] { "*.test.js", "*.spec.js" }, "*.js", "app.js", true)]
    [InlineData(new[] { "*.test.js", "*.spec.js" }, "*.js", "app.test.js", false)]
    [InlineData(new[] { "*.test.js", "*.spec.js" }, "*.js", "app.spec.js", false)]
    [InlineData(new[] { "b" }, "*", "a", true)]
    [InlineData(new[] { "b" }, "*", "b", false)]
    [InlineData(new[] { "b", "c" }, "*", "c", false)]
    [InlineData(new[] { "b", "c" }, "*", "d", true)]
    [InlineData(new[] { "*.txt" }, "*", "foo.txt", false)]
    [InlineData(new[] { "*.txt" }, "*", "foo.js", true)]
    [InlineData(new[] { "*.txt" }, "*.txt", "a.txt", false)]
    [InlineData(new[] { "**/node_modules/**" }, "**/*.js", "node_modules/pkg/file.js", false)]
    [InlineData(new[] { "**/node_modules/**" }, "**/*.js", "src/file.js", true)]
    [InlineData(new[] { "**/test/**" }, "**/*.js", "test/app.js", false)]
    [InlineData(new[] { "**/test/**" }, "**/*.js", "src/test/app.js", false)]
    [InlineData(new[] { "node_modules/**", "dist/**" }, "**/*.js", "dist/a.js", false)]
    [InlineData(new[] { "node_modules/**", "dist/**" }, "**/*.js", "src/a.js", true)]
    // A negated ignore pattern ignores everything it does not match.
    [InlineData(new[] { "!*.js" }, "*", "foo.js", true)]
    [InlineData(new string[0], "*", "anything", true)]
    public void IgnorePatterns_ExcludeMatchingInputs(string[] ignorePatterns, string pattern, string input, bool expected)
    {
        Glob.IsMatch(input, pattern, new GlobOptions { IgnorePatterns = ignorePatterns }).Should().Be(expected);
    }

    [Fact]
    public void IgnorePatterns_Null_ThrowsArgumentNullException()
    {
        FluentActions.Invoking(() => new GlobOptions { IgnorePatterns = null! }).Should().ThrowExactly<ArgumentNullException>();
    }

    [Fact]
    public void InputNormalizer_ReceivesOriginalInput()
    {
        string? received = null;
        var options = new GlobOptions
        {
            InputNormalizer = s =>
            {
                received = s;
                return s;
            }
        };

        Glob.IsMatch("test-input", "test-input", options).Should().BeTrue();
        received.Should().Be("test-input");
    }

    [Fact]
    public void InputNormalizer_TransformsInputBeforeMatching()
    {
        var stripDashes = new GlobOptions { InputNormalizer = s => s.Replace("-", "") };
        var lowerCase = new GlobOptions { InputNormalizer = s => s.ToLowerInvariant() };
        var underscoreToSlash = new GlobOptions { InputNormalizer = s => s.Replace("_", "/") };

        Glob.IsMatch("a-b-c", "abc", stripDashes).Should().BeTrue();
        Glob.IsMatch("a--b--c", "abc", stripDashes).Should().BeTrue();
        Glob.IsMatch("AbC", "abc", lowerCase).Should().BeTrue();
        Glob.IsMatch("FOO", "@(foo|bar)", lowerCase).Should().BeTrue();
        Glob.IsMatch("FOO", "!(baz)", lowerCase).Should().BeTrue();
        Glob.IsMatch("foo_bar", "foo/bar", underscoreToSlash).Should().BeTrue();
        Glob.IsMatch("foo_bar_baz", "foo/*/baz", underscoreToSlash).Should().BeTrue();
        Glob.IsMatch("foo_bar_baz", "**/baz", underscoreToSlash).Should().BeTrue();
        Glob.IsMatch("a-b-c", "a-*", stripDashes).Should().BeFalse();
    }

    [Fact]
    public void IsMatch_StaticWithPatternsAndOptions_AppliesOptions()
    {
        string[] patterns = ["*.md", "*.js"];

        Glob.IsMatch("A.JS", patterns, s_Posix with { IgnoreCase = true }).Should().BeTrue();
        Glob.IsMatch("A.JS", patterns, s_Posix).Should().BeFalse();
    }

    [Theory]
    [InlineData("a*b", false, true)]
    [InlineData("axb", false, false)]
    [InlineData("a*b", true, false)]
    [InlineData("\"a*\"b", true, true)]
    public void KeepQuotes_KeepsQuotesAsLiteralText(string input, bool keepQuotes, bool expected)
    {
        Glob.IsMatch(input, "\"a*\"b", s_Posix with { KeepQuotes = keepQuotes }).Should().Be(expected);
    }

    [Theory]
    [InlineData(true, ".gitignore", "*", true)]
    [InlineData(true, "regular.txt", "*", true)]
    [InlineData(false, ".gitignore", "*", false)]
    [InlineData(false, "regular.txt", "*", true)]
    [InlineData(true, ".hidden/file.js", "**/*.js", true)]
    public void MatchDotFiles_ControlsDotfileMatching(bool matchDotFiles, string input, string pattern, bool expected)
    {
        new Glob(pattern, new GlobOptions { MatchDotFiles = matchDotFiles }).IsMatch(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("**", "", false)]
    [InlineData("*", "", false)]
    [InlineData("**", "/", true)]
    [InlineData("*", "a/", true)]
    public void MatchFileNameOnly_EmptyInput_NeverMatches(string pattern, string input, bool expected)
    {
        Glob.IsMatch(input, pattern, s_Posix with { MatchFileNameOnly = true }).Should().Be(expected);
    }

    [Theory]
    [InlineData(false, "a/b/c.js", "*.js", false)]
    [InlineData(false, "a/b/c/d.js", "*.js", false)]
    [InlineData(true, "a/b/c.js", "*.js", true)]
    [InlineData(true, "a/b/c/d/e/test.js", "*.js", true)]
    [InlineData(true, "test.js", "*.js", true)]
    [InlineData(true, "foo/bar.md", "*.js", false)]
    [InlineData(true, "x/y/z/file.md", "*.md", true)]
    [InlineData(true, "a/b/c.js", "c.js", true)]
    [InlineData(true, "a/b/foo.txt", "foo.txt", true)]
    [InlineData(true, "a/b/c.js", "b/*.js", false)]
    [InlineData(true, "src/components/Button.tsx", "src/*.tsx", false)]
    public void MatchFileNameOnly_MatchesAgainstFileName(bool matchFileNameOnly, string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern, new GlobOptions { MatchFileNameOnly = matchFileNameOnly }).Should().Be(expected);
    }

    [Fact]
    public void MatchFileNameOnly_WithPosixPathStyle_TreatsBackslashAsOrdinaryCharacter()
    {
        var options = s_Posix with { MatchFileNameOnly = true };

        Glob.IsMatch("dir/file.js", "file.js", options).Should().BeTrue();
        Glob.IsMatch(@"dir\file.js", "file.js", options).Should().BeFalse();
    }

    [Theory]
    [InlineData(@"dir\file.js", "file.js")]
    [InlineData(@"foo\bar.js", "*.js")]
    public void MatchFileNameOnly_WithWindowsPathStyle_TreatsBackslashAsSeparator(string input, string pattern)
    {
        var options = new GlobOptions { PathStyle = GlobPathStyle.Windows, MatchFileNameOnly = true };

        Glob.IsMatch(input, pattern, options).Should().BeTrue();
    }

    [Theory]
    [InlineData(false, "foobar", "bar", false)]
    [InlineData(false, "a/b/c", "b", false)]
    [InlineData(false, "my-test-file.js", "test", false)]
    [InlineData(false, "bar", "bar", true)]
    [InlineData(true, "foobar", "bar", true)]
    [InlineData(true, "barbaz", "bar", true)]
    [InlineData(true, "foobarbaz", "bar", true)]
    [InlineData(true, "foo", "bar", false)]
    [InlineData(true, "my-test-file.js", "test", true)]
    [InlineData(true, "pretest", "test", true)]
    [InlineData(true, "test", "test", true)]
    [InlineData(true, "file.js", "test", false)]
    [InlineData(true, "a/b/c", "a", true)]
    [InlineData(true, "a/b/c", "b", true)]
    [InlineData(true, "a/b/c", "c", true)]
    [InlineData(true, "abc/def/ghi", "def", true)]
    [InlineData(true, "foo/bar/baz", "ar/ba", true)]
    [InlineData(true, "foo/bar/baz", "qux", false)]
    public void MatchSubstring_MatchesPatternAnywhereInInput(bool matchSubstring, string input, string pattern, bool expected)
    {
        var glob = new Glob(pattern, new GlobOptions { MatchSubstring = matchSubstring });

        glob.IsMatch(input).Should().Be(expected);
        glob.ToRegex().IsMatch(input).Should().Be(expected);
    }

    [Fact]
    public void MaxPatternLength_BelowOne_ThrowsArgumentOutOfRangeException()
    {
        FluentActions.Invoking(() => new GlobOptions { MaxPatternLength = 0 }).Should().ThrowExactly<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void MaxPatternLength_PatternAtTheLimit_Compiles()
    {
        new Glob("abcde", new GlobOptions { MaxPatternLength = 5 }).IsMatch("abcde").Should().BeTrue();
    }

    [Theory]
    [InlineData(true, "test.md", false)]
    [InlineData(true, "test.js", true)]
    [InlineData(false, "!test.md", true)]
    [InlineData(false, "test.md", false)]
    public void Negation_ControlsLeadingExclamationMark(bool negation, string input, bool expected)
    {
        new Glob("!*.md", new GlobOptions { Negation = negation }).IsMatch(input).Should().Be(expected);
    }

    [Fact]
    public void Options_AreNotModifiedByMatching()
    {
        var options = s_Posix with { Extglobs = false };

        _ = new Glob("+(a)", options);
        _ = TestHelpers.Parse("*.js", options);

        options.Should().Be(s_Posix with { Extglobs = false });
    }

    [Fact]
    public void PathStyle_Auto_FollowsPlatformSeparator()
    {
        Glob.IsMatch("a/b", "a/**").Should().BeTrue();
        Glob.IsMatch(@"a\b", "a/**").Should().Be(Path.DirectorySeparatorChar == '\\');
    }

    [Theory]
    [InlineData(GlobPathStyle.Posix, "a/b", "a/**", true)]
    [InlineData(GlobPathStyle.Posix, @"a\b", "a/**", false)]
    [InlineData(GlobPathStyle.Posix, "src/lib/app.js", "src/**/*.js", true)]
    [InlineData(GlobPathStyle.Windows, "a/b", "a/**", true)]
    [InlineData(GlobPathStyle.Windows, @"a\b", "a/**", true)]
    [InlineData(GlobPathStyle.Windows, "src/lib/app.js", "src/**/*.js", true)]
    [InlineData(GlobPathStyle.Windows, @"src\lib\app.js", "src/**/*.js", true)]
    public void PathStyle_ControlsWhetherBackslashIsSeparator(GlobPathStyle pathStyle, string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern, new GlobOptions { PathStyle = pathStyle }).Should().Be(expected);
    }

    [Fact]
    public void PosixClasses_WhenEnabled_MatchesCharacterClass()
    {
        var glob = new Glob("[[:alnum:]]*", new GlobOptions { PosixClasses = true });

        glob.IsMatch("abc123").Should().BeTrue();
        glob.IsMatch("!!!").Should().BeFalse();
    }

    [Fact]
    public void RegexOptions_AreAppliedToRegex()
    {
        var regex = new Glob("test", new GlobOptions { RegexOptions = RegexOptions.IgnoreCase }).ToRegex();

        "TEST".Should().MatchRegex(regex);
    }

    [Theory]
    [MemberData(nameof(CompiledPatterns))]
    public void RegexOptions_Compiled_DoesNotChangeResults(string pattern)
    {
        var compiled = new Glob(pattern, new GlobOptions { RegexOptions = RegexOptions.Compiled });
        var interpreted = new Glob(pattern, new GlobOptions { RegexOptions = RegexOptions.None });

        compiled.ToRegex().ToString().Should().Be(interpreted.ToRegex().ToString());
        foreach (string input in s_CompiledInputs)
            compiled.IsMatch(input).Should().Be(interpreted.IsMatch(input));
    }

    [Theory]
    [InlineData("(ab)*c", "ababc", false, true)]
    [InlineData("(ab)*c", "c", false, false)]
    [InlineData("(ab)*c", "abxc", false, true)]
    [InlineData("(ab)*c", "ababc", true, true)]
    [InlineData("(ab)*c", "c", true, true)]
    [InlineData("(ab)*c", "abxc", true, false)]
    public void RegexQuantifiers_StarAfterGroupRepeatsTheGroup(string pattern, string input, bool regexQuantifiers, bool expected)
    {
        Glob.IsMatch(input, pattern, s_Posix with { RegexQuantifiers = regexQuantifiers }).Should().Be(expected);
    }

    [Fact]
    public void StrictBrackets_WithBalancedBrackets_Compiles()
    {
        var glob = new Glob("[abc]", new GlobOptions { StrictBrackets = true });

        glob.IsMatch("a").Should().BeTrue();
    }

    [Theory]
    [InlineData("a", "a/", false)]
    [InlineData("a/", "a/", true)]
    public void StrictSlashes_RequiresTrailingSlashToMatch(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern, new GlobOptions { StrictSlashes = true }).Should().Be(expected);
    }
}