using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace Snowberry.Globbing.Tests;

/// <summary>
/// Stresses the shared <c>Token</c> pool (activated by token recycling in the internal
/// compile pipeline) under concurrency. If recycling ever cross-contaminated tokens
/// between parses, concurrently generated regex sources / match results would diverge
/// from the single-threaded oracle.
/// </summary>
public class ConcurrencyTests
{
    // Patterns that exercise the full parse path (tokens, not the fast path): globstar,
    // braces, extglobs, brackets, negation, posix, nested combinations.
    private static readonly string[] s_Patterns =
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
        "?(a|b)c*d.txt"
    ];

    private static readonly (string Input, string Pattern, bool Expected)[] s_MatchCases = BuildMatchCases();

    private static (string Input, string Pattern, bool Expected)[] BuildMatchCases()
    {
        string[] inputs =
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
            "alpha.log"
        ];

        // The expected value is the single-threaded result; the test only asserts the
        // concurrent result equals this oracle (it does not hardcode true/false).
        return
        [
            .. inputs.SelectMany(input => s_Patterns.Select(pattern =>
                (input, pattern, GlobMatcher.IsMatch(input, pattern))))
        ];
    }

    [Fact]
    public void ParallelGenerateRegex_MatchesSequentialSource()
    {
        // Sequential oracle: the canonical regex source for each pattern.
        string[] expected = [.. s_Patterns.Select(p => GlobMatcher.GenerateRegex(p))];

        var mismatches = new ConcurrentBag<string>();

        Parallel.For(0, 20_000, i =>
        {
            int idx = i % s_Patterns.Length;
            string actual = GlobMatcher.GenerateRegex(s_Patterns[idx]);
            if (actual != expected[idx])
                mismatches.Add($"pattern='{s_Patterns[idx]}' expected='{expected[idx]}' actual='{actual}'");
        });

        Assert.Empty(mismatches);
    }

    [Fact]
    public void ParallelMakeRe_MatchesSequentialSource()
    {
        string[] expected = [.. s_Patterns.Select(p => GlobMatcher.MakeRe(p).ToString())];

        var mismatches = new ConcurrentBag<string>();

        Parallel.For(0, 20_000, i =>
        {
            int idx = i % s_Patterns.Length;
            string actual = GlobMatcher.MakeRe(s_Patterns[idx]).ToString();
            if (actual != expected[idx])
                mismatches.Add($"pattern='{s_Patterns[idx]}' expected='{expected[idx]}' actual='{actual}'");
        });

        Assert.Empty(mismatches);
    }

    [Fact]
    public void ParallelIsMatch_MatchesSequentialResult()
    {
        var failures = new ConcurrentBag<string>();

        Parallel.For(0, 50_000, i =>
        {
            var (input, pattern, expected) = s_MatchCases[i % s_MatchCases.Length];
            bool actual = GlobMatcher.IsMatch(input, pattern);
            if (actual != expected)
                failures.Add($"input='{input}' pattern='{pattern}' expected={expected} actual={actual}");
        });

        Assert.Empty(failures);
    }
}
