using System.Collections.Concurrent;
using Snowberry.Globbing.Compilation;

namespace Snowberry.Globbing.Tests;

public class ConcurrencyTests
{
    private static readonly GlobOptions s_Posix = new() { PathStyle = GlobPathStyle.Posix };

    private static readonly string[] s_Inputs =
    [
        "app.js",
        "README.md",
        "docs/guide.md",
        ".config.js",
        "src/components/Button.tsx",
        "src/utils/helper.js",
        "src/utils/helper.test.js",
        "src/.cache/chunk.js",
        "node_modules/pkg/index.js",
        "a\nb.md",
        "test-3a.txt"
    ];

    [Theory]
    [InlineData("!**/*.md")]
    [InlineData("!(*.md)")]
    [InlineData("**/*.{js,ts,tsx}")]
    [InlineData("src/**/!(*.test|*.spec).{js,ts}")]
    [InlineData("test-[0-9][a-z].txt")]
    public void SharedGlob_MatchedConcurrently_AgreesWithSequentialResults(string pattern)
    {
        AssertConcurrentMatchesAgree(() => new Glob(pattern, s_Posix));
    }

    [Fact]
    public void SharedGlobWithIgnorePatterns_MatchedConcurrently_AgreesWithSequentialResults()
    {
        AssertConcurrentMatchesAgree(() => new Glob(
            ["**/*.js", "!**/*.{js,tsx}"],
            s_Posix with { IgnorePatterns = ["**/node_modules/**", "!src/**"] }));
    }

    [Fact]
    public void StaticIsMatch_PastCacheCapacity_AgreesWithUncachedResults()
    {
        GlobOptions[] variants = [s_Posix, s_Posix with { MatchDotFiles = true }];
        var cases = new List<(string Input, string Pattern, GlobOptions Options, bool Expected)>();
        for (int i = 0; i < 300; i++)
        {
            string pattern = i % 2 == 0 ? $"**/*.{i}.js" : $"!**/*.{i}.md";
            foreach (var options in variants)
            {
                var oracle = new Glob(pattern, options);
                foreach (string input in new[] { $"src/a.{i}.js", $".hidden/a.{i}.js", $"src/a.{i}.md" })
                    cases.Add((input, pattern, options, oracle.IsMatch(input)));
            }
        }

        var failures = new ConcurrentBag<string>();
        Parallel.For(0, cases.Count * 2, i =>
        {
            var (input, pattern, options, expected) = cases[i % cases.Count];
            bool actual = Glob.IsMatch(input, pattern, options);
            if (actual != expected)
                failures.Add($"'{input}' / '{pattern}': expected {expected}, got {actual}");
        });

        failures.Should().BeEmpty();
    }

    [Fact]
    public void ConcurrentCompilation_ProducesSequentialRegexSources()
    {
        string longPattern = string.Join("/", Enumerable.Range(0, 40).Select(i => $"{{a{i},b{i}}}*.@(x|y)"));
        string[] patterns = ["*.js", "**/*.{js,ts,jsx,tsx}", "src/**/!(*.test|*.spec).{js,ts}", "!(*.md)", "[[:alpha:]]*.log", "{1..300}", longPattern];
        GlobOptions[] variants =
        [
            s_Posix,
            new() { PathStyle = GlobPathStyle.Windows },
            s_Posix with { MatchDotFiles = true },
            s_Posix with { CaptureGroups = true },
            s_Posix with { BashCompatibility = true }
        ];
        var cases = (from pattern in patterns
                     from options in variants
                     select (Pattern: pattern, Options: options, Expected: GlobCompiler.Compile(pattern, options).Source)).ToArray();

        var failures = new ConcurrentBag<string>();
        Parallel.For(0, cases.Length * 300, i =>
        {
            var (pattern, options, expected) = cases[i % cases.Length];
            string actual = GlobCompiler.Compile(pattern, options).Source;
            if (actual != expected)
                failures.Add($"'{pattern}': expected '{expected}', got '{actual}'");
        });

        failures.Should().BeEmpty();
    }

    private static void AssertConcurrentMatchesAgree(Func<Glob> create)
    {
        var oracle = create();
        bool[] expected = [.. s_Inputs.Select(input => oracle.IsMatch(input))];

        // A fresh instance, so its lazily built regexes are first used concurrently.
        var shared = create();
        var failures = new ConcurrentBag<string>();
        Parallel.For(0, s_Inputs.Length * 200, i =>
        {
            int index = i % s_Inputs.Length;
            string input = s_Inputs[index];
            bool actual = i % 2 == 0 ? shared.IsMatch(input) : shared.IsMatch(input.AsSpan());
            if (actual != expected[index])
                failures.Add($"'{input}': expected {expected[index]}, got {actual}");
        });

        failures.Should().BeEmpty();
    }
}