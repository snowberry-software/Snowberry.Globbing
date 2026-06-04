using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Snowberry.Globbing.Models;

namespace Snowberry.Globbing.Benchmark;

/// <summary>
/// Single self-contained benchmark file for Snowberry.Globbing.
///
/// The baseline it produces is auto-generated: BenchmarkDotNet's GitHub markdown
/// exporter writes <c>BenchmarkDotNet.Artifacts/results/*-report-github.md</c> on every
/// run, so there is no hand-maintained results document to keep in sync.
///
/// Two complementary views:
///   <see cref="PipelineBenchmarks"/> — the per-phase baseline. Decomposes the glob
///     lifecycle into read/parse -> generate regex source -> compile regex -> run the
///     final generated regex, plus end-to-end <c>MakeRe</c> (parse+generate+compile).
///   <see cref="MatchBenchmarks"/> — end-to-end match throughput of a pre-compiled
///     matcher (via <see cref="GlobMatcher.Create(string, GlobbingOptions)"/>) over a
///     representative dataset, the cost a consumer actually pays per path.
/// </summary>
internal static class Program
{
    private static void Main(string[] args)
        => BenchmarkRunner.Run(typeof(Program).Assembly, args: args);
}

/// <summary>
/// Glob patterns and the datasets they are exercised against. Centralised here so the
/// pipeline and throughput benchmarks measure the exact same workloads — patterns live
/// as <c>const</c> strings to keep the two benchmark classes from drifting apart.
/// </summary>
public static class Workloads
{
    public const string SimpleWildcard = "*.js";
    public const string Globstar = "**/*.js";
    public const string BraceExpansion = "*.{js,ts,jsx,tsx}";
    public const string Extglob = "!(*.test|*.spec).{js,ts}";
    public const string ComplexNested = "src/**/!(*.test|*.spec).{js,jsx,ts,tsx}";
    public const string CharacterClass = "test-[0-9][a-z].txt";
    public const string RealWorld = "**/*.{js,jsx}";
    public const string Negation = "!*.md";

    /// <summary>The single-pattern scenarios exercised by both benchmark classes.</summary>
    public enum Scenario
    {
        SimpleWildcard,
        Globstar,
        BraceExpansion,
        Extglob,
        ComplexNested,
        CharacterClass,
        RealWorld,
        Negation
    }

    /// <summary>Maps a scenario to its glob pattern.</summary>
    public static string PatternFor(Scenario scenario) => scenario switch
    {
        Scenario.SimpleWildcard => SimpleWildcard,
        Scenario.Globstar => Globstar,
        Scenario.BraceExpansion => BraceExpansion,
        Scenario.Extglob => Extglob,
        Scenario.ComplexNested => ComplexNested,
        Scenario.CharacterClass => CharacterClass,
        Scenario.RealWorld => RealWorld,
        Scenario.Negation => Negation,
        _ => SimpleWildcard
    };

    /// <summary>Maps a scenario to the dataset its pattern is run against.</summary>
    public static string[] DataFor(Scenario scenario) => scenario switch
    {
        Scenario.SimpleWildcard => JsFiles,
        Scenario.Globstar => DeepPaths,
        Scenario.BraceExpansion => MixedExtensions,
        Scenario.Extglob => DeepPaths,
        Scenario.ComplexNested => DeepPaths,
        Scenario.CharacterClass => CharacterClassFiles,
        Scenario.RealWorld => RealWorldPaths,
        Scenario.Negation => MixedExtensions,
        _ => JsFiles
    };

    // Datasets are built once and cached; generators stay deterministic so runs are comparable.

    public static readonly string[] JsFiles =
        [.. Enumerable.Range(0, 500).Select(i => $"app{i}.js")];

    public static readonly string[] MixedExtensions = BuildMixedExtensions();

    public static readonly string[] CharacterClassFiles =
        [.. Enumerable.Range(0, 100).SelectMany(i => new[] { $"test-{i % 10}a.txt", $"test-{i % 10}z.txt", $"test-{i % 10}X.txt" })];

    public static readonly string[] DeepPaths = BuildDeepPaths();

    public static readonly string[] RealWorldPaths =
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

    private static string[] BuildMixedExtensions()
    {
        string[] extensions = ["js", "ts", "jsx", "tsx", "css", "scss", "html", "json", "md", "txt"];
        return [.. Enumerable.Range(0, 1000).Select(i => $"file{i}.{extensions[i % extensions.Length]}")];
    }

    private static string[] BuildDeepPaths()
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
}

/// <summary>
/// Per-phase baseline of the glob compilation lifecycle. Each scenario yields one row per
/// phase so the cost of read/parse, regex-source generation, regex compilation and running
/// the final generated regex can be read independently and summed against the end-to-end
/// <see cref="MakeRe"/> figure.
/// </summary>
[MemoryDiagnoser]
public class PipelineBenchmarks
{
    [ParamsAllValues]
    public Workloads.Scenario Scenario { get; set; }

    private string _pattern = "";
    private ParseState _parsed = null!;
    private string _regexSource = "";
    private Regex _compiled = null!;
    private string[] _data = [];

    [GlobalSetup]
    public void Setup()
    {
        _pattern = Workloads.PatternFor(Scenario);
        _data = Workloads.DataFor(Scenario);

        // Pre-stage each phase's input so the benchmarked call measures only that phase.
        _parsed = GlobMatcher.Parse(_pattern);
        _regexSource = GlobMatcher.GenerateRegex(_parsed);
        _compiled = GlobMatcher.MakeRe(_pattern);
    }

    /// <summary>Read + parse the glob pattern into its <see cref="ParseState"/>.</summary>
    [Benchmark]
    public ParseState Parse() => GlobMatcher.Parse(_pattern);

    /// <summary>Structural scan of the pattern (no regex compilation).</summary>
    [Benchmark]
    public ScanResult Scan() => GlobMatcher.Scan(_pattern);

    /// <summary>Generate the regex source string from an already-parsed state.</summary>
    [Benchmark]
    public string GenerateRegex() => GlobMatcher.GenerateRegex(_parsed);

    /// <summary>Compile an already-generated regex source string into a <see cref="Regex"/>.</summary>
    [Benchmark]
    public Regex Compile() => GlobMatcher.ToRegex(_regexSource);

    /// <summary>End-to-end compile: parse + generate + compile (sum reference for the phases above).</summary>
    [Benchmark]
    public Regex MakeRe() => GlobMatcher.MakeRe(_pattern);

    /// <summary>Run the final generated regex over the scenario's dataset.</summary>
    [Benchmark]
    public int RunRegex()
    {
        int count = 0;
        foreach (string path in _data)
        {
            if (_compiled.IsMatch(path))
                count++;
        }

        return count;
    }
}

/// <summary>
/// End-to-end match throughput of a pre-compiled matcher over a representative dataset —
/// the cost a consumer pays per path once a matcher has been created. Includes a
/// multi-pattern (OR) scenario that the single-pattern <see cref="PipelineBenchmarks"/>
/// cannot decompose.
/// </summary>
[MemoryDiagnoser]
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
        Negation,
        CharacterClass,
        MultiplePatterns
    }

    private Dictionary<MatchScenario, (MatcherHandler Matcher, string[] Data)> _scenarios = null!;

    [ParamsAllValues]
    public MatchScenario Scenario { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _scenarios = new Dictionary<MatchScenario, (MatcherHandler, string[])>
        {
            [MatchScenario.SimpleWildcard] = (GlobMatcher.Create(Workloads.SimpleWildcard), Workloads.JsFiles),
            [MatchScenario.Globstar] = (GlobMatcher.Create(Workloads.Globstar), Workloads.DeepPaths),
            [MatchScenario.BraceExpansion] = (GlobMatcher.Create(Workloads.BraceExpansion), Workloads.MixedExtensions),
            [MatchScenario.Extglob] = (GlobMatcher.Create(Workloads.Extglob), Workloads.DeepPaths),
            [MatchScenario.ComplexNested] = (GlobMatcher.Create(Workloads.ComplexNested), Workloads.DeepPaths),
            [MatchScenario.Negation] = (GlobMatcher.Create(Workloads.Negation), Workloads.MixedExtensions),
            [MatchScenario.CharacterClass] = (GlobMatcher.Create(Workloads.CharacterClass), Workloads.CharacterClassFiles),
            [MatchScenario.MultiplePatterns] = (GlobMatcher.Create(["**/*.js", "**/*.ts", "!**/node_modules/**"]), Workloads.RealWorldPaths),
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
