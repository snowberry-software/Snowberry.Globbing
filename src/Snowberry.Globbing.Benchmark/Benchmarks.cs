using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Microsoft.VSDiagnostics;

namespace Snowberry.Globbing.Benchmark;

/// <summary>
/// Shared test datasets for the benchmark classes. Generated once per benchmark
/// class in its <c>[GlobalSetup]</c> so each <c>[Params]</c> dimension stays isolated
/// to its own class (a single class holding all params would run every benchmark
/// against the full cartesian product of params).
/// </summary>
internal static class BenchmarkData
{
    public static string[] GenerateTestPaths()
    {
        var paths = new List<string>();

        // Simple files
        for (int i = 0; i < 100; i++)
        {
            paths.Add($"file{i}.js");
            paths.Add($"file{i}.ts");
            paths.Add($"document{i}.md");
        }

        // Nested paths
        for (int i = 0; i < 100; i++)
        {
            paths.Add($"src/component{i}.js");
            paths.Add($"test/test{i}.spec.js");
            paths.Add($"lib/utils/util{i}.ts");
        }

        return [.. paths];
    }

    public static string[] GenerateJsFiles()
    {
        return [.. Enumerable.Range(0, 500).Select(i => $"app{i}.js")];
    }

    public static string[] GenerateDeepPaths()
    {
        var paths = new List<string>();
        for (int depth = 1; depth <= 5; depth++)
        {
            string segments = string.Join("/", Enumerable.Range(0, depth).Select(i => $"level{i}"));
            for (int i = 0; i < 50; i++)
            {
                paths.Add($"{segments}/file{i}.js");
                paths.Add($"{segments}/component{i}.tsx");
                paths.Add($"{segments}/test{i}.spec.js");
            }
        }

        return [.. paths];
    }

    public static string[] GenerateMixedExtensions()
    {
        string[] extensions = new[] { "js", "ts", "jsx", "tsx", "css", "scss", "html", "json", "md", "txt" };
        return [.. Enumerable.Range(0, 1000).Select(i => $"file{i}.{extensions[i % extensions.Length]}")];
    }

    public static string[] GenerateCharacterClassFiles()
    {
        return [.. Enumerable.Range(0, 100).SelectMany(i => new[] { $"test-{i % 10}a.txt", $"test-{i % 10}z.txt", $"test-{i % 10}X.txt" })];
    }

    public static string[] GenerateRealWorldPaths()
    {
        return
        [
            "src/components/Button/Button.tsx",
            "src/components/Button/Button.test.tsx",
            "src/components/Input/Input.jsx",
            "src/utils/helpers/string.js",
            "src/utils/helpers/array.ts",
            "src/services/api/client.ts",
            "src/services/api/client.test.ts",
            "test/unit/components/Button.spec.js",
            "test/integration/api.test.js",
            "lib/vendor/external.min.js",
            "dist/bundle.js",
            "dist/styles/main.css",
            "node_modules/package/index.js",
            "docs/README.md",
            "docs/api/reference.md",
            ".config/settings.json",
            ".github/workflows/ci.yml"
        ];
    }
}

/// <summary>
/// Throughput of pre-compiled matchers against representative datasets.
/// One result row per <see cref="MatchScenario"/>.
/// </summary>
[MemoryDiagnoser]
[CPUUsageDiagnoser]
[HtmlExporter]
[MarkdownExporterAttribute.GitHub]
public class MatchBenchmarks
{
    /// <summary>Pre-compiled matcher + dataset pairings exercised by <see cref="Match"/>.</summary>
    public enum MatchScenario
    {
        SimpleWildcard,
        Globstar,
        BraceExpansion,
        Extglob,
        ComplexNested,
        MultiplePatterns,
        Negation,
        CharacterClass
    }

    private Dictionary<MatchScenario, (MatcherHandler Matcher, string[] Data)> _scenarios;

    [ParamsAllValues]
    public MatchScenario Scenario { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        string[] jsFiles = BenchmarkData.GenerateJsFiles();
        string[] deepPaths = BenchmarkData.GenerateDeepPaths();
        string[] mixedExtensions = BenchmarkData.GenerateMixedExtensions();
        string[] realWorldPaths = BenchmarkData.GenerateRealWorldPaths();
        string[] characterClassFiles = BenchmarkData.GenerateCharacterClassFiles();

        _scenarios = new Dictionary<MatchScenario, (MatcherHandler, string[])>
        {
            [MatchScenario.SimpleWildcard] = (GlobMatcher.Create("*.js"), jsFiles),
            [MatchScenario.Globstar] = (GlobMatcher.Create("**/*.js"), deepPaths),
            [MatchScenario.BraceExpansion] = (GlobMatcher.Create("*.{js,ts,jsx,tsx}"), mixedExtensions),
            [MatchScenario.Extglob] = (GlobMatcher.Create("!(*.test|*.spec).{js,ts}"), deepPaths),
            [MatchScenario.ComplexNested] = (GlobMatcher.Create("src/**/!(*.test|*.spec).{js,jsx,ts,tsx}"), deepPaths),
            [MatchScenario.MultiplePatterns] = (GlobMatcher.Create(["**/*.js", "**/*.ts", "!**/node_modules/**"]), realWorldPaths),
            [MatchScenario.Negation] = (GlobMatcher.Create("!*.md"), mixedExtensions),
            [MatchScenario.CharacterClass] = (GlobMatcher.Create("test-[0-9][a-z].txt"), characterClassFiles),
        };
    }

    [Benchmark]
    public int Match()
    {
        var (matcher, data) = _scenarios[Scenario];
        int count = 0;
        foreach (string path in data)
        {
            if (matcher(path))
                count++;
        }

        return count;
    }
}

/// <summary>
/// Compile-then-filter cost for real-world include/exclude patterns.
/// One result row per <see cref="RealWorldPattern"/>.
/// </summary>
[MemoryDiagnoser]
[CPUUsageDiagnoser]
[HtmlExporter]
[MarkdownExporterAttribute.GitHub]
public class RealWorldBenchmarks
{
    private string[] _realWorldPaths;

    // Filter JS, filter source, exclude tests, find all tests.
    [Params("**/*.{js,jsx}", "src/**/*.{js,ts,jsx,tsx}", "!(*.test|*.spec).*", "**/*.{test,spec}.{js,ts,jsx,tsx}")]
    public string RealWorldPattern { get; set; }

    [GlobalSetup]
    public void Setup() => _realWorldPaths = BenchmarkData.GenerateRealWorldPaths();

    [Benchmark]
    public int RealWorld()
    {
        var matcher = GlobMatcher.Create(RealWorldPattern);
        int count = 0;
        foreach (string path in _realWorldPaths)
        {
            if (matcher(path))
                count++;
        }

        return count;
    }
}

/// <summary>
/// Pattern-processing primitives (Parse / Scan / MakeRe) over a simple and a complex pattern.
/// Two result rows per benchmark, one per <see cref="PatternComplexity"/>.
/// </summary>
[MemoryDiagnoser]
[CPUUsageDiagnoser]
[HtmlExporter]
[MarkdownExporterAttribute.GitHub]
public class OperationBenchmarks
{
    public enum Complexity
    {
        Simple,
        Complex
    }

    [ParamsAllValues]
    public Complexity PatternComplexity { get; set; }

    private string Pattern => PatternComplexity == Complexity.Simple
        ? "*.js"
        : "src/**/!(*.test|*.spec).{js,jsx,ts,tsx}";

    [Benchmark]
    public object Parse() => GlobMatcher.Parse(Pattern);

    [Benchmark]
    public object Scan() => GlobMatcher.Scan(Pattern);

    [Benchmark]
    public object MakeRe() => GlobMatcher.MakeRe(Pattern);
}

/// <summary>
/// One-shot compilation, one-shot <see cref="GlobMatcher.IsMatch(string, string, GlobbingOptions)"/>,
/// and batch-over-many-matchers benchmarks that don't fit a single parameter axis.
/// </summary>
[MemoryDiagnoser]
[CPUUsageDiagnoser]
[HtmlExporter]
[MarkdownExporterAttribute.GitHub]
public class CompilationBenchmarks
{
    private string[] _testPaths;

    [GlobalSetup]
    public void Setup() => _testPaths = BenchmarkData.GenerateTestPaths();

    // ===== Pattern Compilation =====

    [Benchmark]
    public MatcherHandler CreateSimpleWildcard() => GlobMatcher.Create("*.js");

    [Benchmark]
    public MatcherHandler CreateGlobstar() => GlobMatcher.Create("**/*.{js,ts}");

    [Benchmark]
    public MatcherHandler CreateComplexPattern() => GlobMatcher.Create("src/**/!(*.test|*.spec).{js,jsx,ts,tsx}");

    [Benchmark]
    public MatcherHandler CreateMultiplePatterns() => GlobMatcher.Create(["**/*.js", "**/*.ts", "!**/node_modules/**"]);

    // ===== One-shot IsMatch =====

    [Benchmark]
    public bool IsMatch_Simple() => GlobMatcher.IsMatch("app.js", "*.js");

    [Benchmark]
    public bool IsMatch_Globstar() => GlobMatcher.IsMatch("src/components/Button.tsx", "**/*.tsx");

    [Benchmark]
    public bool IsMatch_Complex() => GlobMatcher.IsMatch("src/utils/helper.js", "src/**/!(*.test).{js,ts}");

    [Benchmark]
    public bool IsMatch_MultiplePatterns() => GlobMatcher.IsMatch("src/app.js", ["**/*.js", "!**/test/**"]);

    // ===== Batch Processing =====

    [Benchmark]
    public int BatchProcess_LargeDataset()
    {
        var matchers = new[]
        {
            GlobMatcher.Create("**/*.js"),
            GlobMatcher.Create("**/*.ts"),
            GlobMatcher.Create("**/*.jsx"),
            GlobMatcher.Create("**/*.tsx")
        };

        int count = 0;
        foreach (string path in _testPaths)
        {
            foreach (var matcher in matchers)
            {
                if (matcher(path))
                {
                    count++;
                    break;
                }
            }
        }

        return count;
    }
}
