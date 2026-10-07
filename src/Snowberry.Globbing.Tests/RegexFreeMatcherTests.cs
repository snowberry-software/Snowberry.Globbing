using Snowberry.Globbing.Compilation;

namespace Snowberry.Globbing.Tests;

public class RegexFreeMatcherTests
{
    private static readonly string[] s_Inputs =
    [
        "", "a", ".a", "a.js", ".js", "a.js/", "a.js//", "x/a.js", "x/.a.js", ".x/a.js", "x//a.js", "/a.js", "x\\a.js", "x/y/a.ts", "a.jsx",
        "src", "src/", "src/a", "src/.a", "src/a/b.cs", "src/.a/b.cs", "src//b.cs", "srcx/a", "x/src/a", "node_modules", "x/node_modules",
        "x/node_modules/y", ".x/node_modules/y", "x/node_modules/.y", "node_modules/node_modules/a", "a\nb.js", "\n.js", "a.js\n",
        new string((char)0x2028, 1) + "a.js", "/", "//", "a/", "a/b", "a/.b", "a/..", "a/./b", "a\\.b", "src\\a.js", "src\\.a",
        "x\\y\\node_modules\\z", "a/b/c", "a/b\\c", "a\\b\\c",
    ];

    public static TheoryData<string, GlobPathStyle> FastPatterns()
    {
        var data = new TheoryData<string, GlobPathStyle>();
        string[] patterns = ["*", "**", "*.js", "**/*.js", "*.{js,ts}", "**/*.{js,jsx}", "src/**", "src/**/*.cs", "**/node_modules/**", "src/a.js", "**/bin", "!*.md", "**/y/node_modules/**"];
        foreach (var style in new[] { GlobPathStyle.Posix, GlobPathStyle.Windows })
        {
            foreach (string pattern in patterns)
                data.Add(pattern, style);
        }

        return data;
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
    [InlineData("**/*.[jt]s")]
    [InlineData("src/**/!(*.test).js")]
    [InlineData("a?c")]
    [InlineData("*.js")]
    public void OtherShapesAndOptions_KeepTheRegex(string pattern)
    {
        var options = new GlobOptions { PathStyle = GlobPathStyle.Posix, MatchDotFiles = pattern == "*.js" };
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