using Snowberry.Globbing.Compilation;

namespace Snowberry.Globbing.Tests;

public class RegexFreeMatcherTests
{
    private static readonly string[] s_Inputs =
    [
        "", "a", ".a", "a.js", ".js", "a.js/", "a.js//", "x/a.js", "x/.a.js", ".x/a.js", "x//a.js", "/a.js", "x\\a.js", "x/y/a.ts", "a.jsx",
        "src", "src/", "src/a", "src/.a", "src/a/b.cs", "src/.a/b.cs", "src//b.cs", "srcx/a", "x/src/a", "node_modules", "x/node_modules",
        "x/node_modules/y", ".x/node_modules/y", "x/node_modules/.y", "node_modules/node_modules/a", "a\nb.js", "\n.js", "a.js\n",
        new string((char)0x2028, 1) + "a.js", "/", "//", "a/", "a/b", "a/.b", "a/..", "a/./b",
        "a.min.b.js", "a.min.js", ".a.min.b.js", "x/a.min.min.js", "a.minx.js", "a.min.b.ts", "a.min..js", "min.js", "a.min.b/c.js",
        "x/.y/a.min.b.js", "src/a/b.x.c", "src/a/.b.x.c", "src/b.x", "a.min.b","a\\.b", "src\\a.js", "src\\.a",
        "x\\y\\node_modules\\z", "a/b/c", "a/b\\c", "a\\b\\c",
        ".", "..", "./", "../", "..\\", "..a.js", "a.c", "a.h", "a.[ch]", "x/a.[ch]", "a.ts", "a.as", "a.bs", "a.és", "a-b.js", "-.js", "a--b.js",
        "a.", "a./", "a.\n", "a.\n.b", "\n.b", "a.b.c", "test-1a.txt", "test-a1.txt", "test-[0-9][a-z].txt", "a_b.js", "ab", "a/b.c",
    ];

    public static TheoryData<string, GlobPathStyle> FastPatterns()
    {
        var data = new TheoryData<string, GlobPathStyle>();
        string[] patterns = ["*", "**", "*.js", "**/*.js", "*.{js,ts}", "**/*.{js,jsx}", "src/**", "src/**/*.cs", "**/node_modules/**", "src/a.js", "**/bin", "!*.md", "**/y/node_modules/**", "*.min.*.js", "**/*.min.*.js", "*.min.*", "src/**/*.x.*", "*.min.*.{js,ts}"];
        foreach (var style in new[] { GlobPathStyle.Posix, GlobPathStyle.Windows })
        {
            foreach (string pattern in patterns)
                data.Add(pattern, style);
        }

        return data;
    }

    public static TheoryData<string, GlobOptions> FastPatternsWithOptions()
    {
        var data = new TheoryData<string, GlobOptions>();
        string[] patterns = ["*.[ch]", "**/*.[ch]", "src/**/*.[jt]s", "test-[0-9][a-z].txt", "*.[!a]s", "?", "?.js", "a?c", "*-*.js", "*_*.js", "*-*-*.{js,ts}", "**/*-*.js", "*.*", "**/*.*", "*.js", "*.{js,ts}", "*", "*.min.*.js"];
        foreach (var style in new[] { GlobPathStyle.Posix, GlobPathStyle.Windows })
        {
            foreach (var options in new[]
            {
                new GlobOptions { PathStyle = style },
                new GlobOptions { PathStyle = style, MatchDotFiles = true },
                new GlobOptions { PathStyle = style, BracketMode = GlobBracketMode.Literal },
                new GlobOptions { PathStyle = style, BracketMode = GlobBracketMode.CharacterClass },
            })
            {
                foreach (string pattern in patterns)
                    data.Add(pattern, options);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(FastPatternsWithOptions))]
    public void BracketDotFileAndBoundedStarShapes_AgreeWithTheRegex(string pattern, GlobOptions options)
    {
        string source = GlobCompiler.Compile(pattern, options).Source;
        var regex = new Regex(source);

        var matcher = RegexFreeMatcher.TryCreate(source, RegexOptions.None);

        foreach (string input in s_Inputs)
            matcher?.IsMatch(input.AsSpan()).Should().Be(regex.IsMatch(input), "input \"{0}\"", input);
    }

    [Theory]
    [InlineData("*.[jt]s", false)]
    [InlineData("**/*.[ch]", false)]
    [InlineData("test-[0-9][a-z].txt", false)]
    [InlineData("*.[!a]s", false)]
    [InlineData("?.js", false)]
    [InlineData("*-*.js", false)]
    [InlineData("*.*", false)]
    [InlineData("*.js", true)]
    [InlineData("*.{js,ts}", true)]
    public void BracketDotFileAndBoundedStarShapes_AreMatchedWithoutRegex(string pattern, bool matchDotFiles)
    {
        var options = new GlobOptions { PathStyle = GlobPathStyle.Posix, MatchDotFiles = matchDotFiles };

        new CompiledPattern(pattern, options, RegexOptions.None).IsRegexFree.Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(FastPatterns))]
    public void CommonShapes_AreMatchedWithoutRegex_AndAgreeWithIt(string pattern, GlobPathStyle style)
    {
        var options = new GlobOptions { PathStyle = style };
        var compilation = GlobCompiler.Compile(pattern, options);
        string source = compilation.PositiveSource ?? compilation.Source;
        var regex = new Regex(source);

        var matcher = RegexFreeMatcher.TryCreate(source, RegexOptions.None);

        matcher.Should().NotBeNull();
        foreach (string input in s_Inputs)
            matcher!.IsMatch(input.AsSpan()).Should().Be(regex.IsMatch(input), "input \"{0}\"", input);
    }

    [Theory]
    [InlineData(@"^(?:a\/b[\\/]c)$(?!\n)")]
    [InlineData(@"^(?:src(?:\/(?:(?!\.)[^/]*\/)*|$(?!\n)))$(?!\n)")]
    [InlineData(@"^(?:[^./\n\r\u2028\u2029][^/]*\.[a-[b]]s)$(?!\n)")]
    [InlineData(@"^(?:[^./\n\r\u2028\u2029][^/]*\.[/a]s)$(?!\n)")]
    [InlineData(@"^(?:[^./\n\r\u2028\u2029][^/]*\.[\d]s)$(?!\n)")]
    [InlineData(@"^(?:(?=[^.\n\r\u2028\u2029])[^/\-]*-\.js)$(?!\n)")]
    [InlineData(@"^(?:(?=[^.\n\r\u2028\u2029])[^/a]*ab[^/]*c)$(?!\n)")]
    [InlineData(@"^(?:(?!\.)[^/]*\.(?=[^\n\r\u2028\u2029])[^/]*\.(?=[^\n\r\u2028\u2029])[^/]*)$(?!\n)")]
    public void TryCreate_EdgeShapes_AgreesWithTheRegexIfCreated(string source)
    {
        // An escaped separator beside a Windows separator class, and a middle globstar with nothing after it.
        var regex = new Regex(source);

        var matcher = RegexFreeMatcher.TryCreate(source, RegexOptions.None);

        foreach (string input in s_Inputs)
            matcher?.IsMatch(input.AsSpan()).Should().Be(regex.IsMatch(input), "input \"{0}\"", input);
    }

    [Theory]
    [InlineData(RegexOptions.IgnoreCase)]
    [InlineData(RegexOptions.Multiline)]
    [InlineData(RegexOptions.RightToLeft)]
    [InlineData(RegexOptions.IgnorePatternWhitespace)]
    [InlineData(RegexOptions.ECMAScript)]
    public void OptionsThatChangeTheRegex_DisableTheMatcher(RegexOptions regexOptions)
    {
        string source = GlobCompiler.Compile("**/*.js", GlobOptions.Default).Source;

        RegexFreeMatcher.TryCreate(source, regexOptions).Should().BeNull();
    }

    [Fact]
    public void MatchTimeout_KeepsTheMatcher()
    {
        // The matcher is linear, so a timeout, including the process-wide default the test module sets, does not disable it.
        var options = new GlobOptions { PathStyle = GlobPathStyle.Posix, MatchTimeout = TimeSpan.FromSeconds(1) };

        new CompiledPattern("**/*.js", options, RegexOptions.None).IsRegexFree.Should().BeTrue();
        new CompiledPattern("**/*.js", options with { MatchTimeout = null }, RegexOptions.None).IsRegexFree.Should().BeTrue();
    }

    [Theory]
    [InlineData(256, true)]
    [InlineData(257, false)]
    public void LongLiteral_KeepsTheRegex(int length, bool regexFree)
    {
        var options = new GlobOptions { PathStyle = GlobPathStyle.Posix };

        new CompiledPattern("**/" + new string('a', length) + "/**", options, RegexOptions.None).IsRegexFree.Should().Be(regexFree);
    }

    [Theory]
    [InlineData("[[:alpha:]]*.log")]
    [InlineData("src/**/!(*.test).js")]
    [InlineData("*.[jt]s?(x)")]
    [InlineData("**/*.js")]
    public void OtherShapesAndOptions_KeepTheRegex(string pattern)
    {
        var options = new GlobOptions { PathStyle = GlobPathStyle.Posix, MatchDotFiles = pattern == "**/*.js" };
        string source = GlobCompiler.Compile(pattern, options).Source;

        RegexFreeMatcher.TryCreate(source, RegexOptions.None).Should().BeNull();
    }

    [Theory]
    [InlineData("x\\y\\a.js", true)]
    [InlineData("x\\.y\\a.js", false)]
    [InlineData("x\\y\\.a.js", false)]
    public void WindowsInputs_MatchWithoutAllocating(string input, bool expected)
    {
        var glob = new Glob("**/*.js", new GlobOptions { PathStyle = GlobPathStyle.Windows, IgnorePatterns = ["**/node_modules/**"] });

        glob.IsMatch(input).Should().Be(expected);
        glob.IsMatch(input.AsSpan()).Should().Be(expected);
#if NET
        glob.IsMatch(input);
        long before = GC.GetAllocatedBytesForCurrentThread();
        glob.IsMatch(input);
        glob.IsMatch(input.AsSpan());
        (GC.GetAllocatedBytesForCurrentThread() - before).Should().Be(0);
#endif
    }
}