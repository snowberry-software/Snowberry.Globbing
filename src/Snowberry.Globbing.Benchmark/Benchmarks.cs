using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Snowberry.Globbing.Compilation;

namespace Snowberry.Globbing.Benchmark;

/// <summary>
/// Single self-contained benchmark file for Snowberry.Globbing.
///
/// The baseline it produces is auto-generated: BenchmarkDotNet's GitHub markdown
/// exporter writes <c>BenchmarkDotNet.Artifacts/results/*-report-github.md</c> on every
/// run, so there is no hand-maintained results document to keep in sync.
///
/// Two complementary views:
///   <see cref="PipelineBenchmarks"/>: the per-phase baseline. Decomposes the glob
///     lifecycle into compile to regex source (lex + parse + emit) -> construct regex -> run the
///     regex <see cref="Glob.ToRegex"/> returns, plus end-to-end <see cref="Glob"/> construction.
///   <see cref="MatchBenchmarks"/>: end-to-end match throughput of a pre-compiled
///     <see cref="Glob"/> over a
///     representative dataset, the cost a consumer actually pays per path.
/// </summary>
internal static class Program
{
    private static void Main(string[] args)
    {
        BenchmarkRunner.Run(typeof(Program).Assembly, args: args);
    }
}

/// <summary>
/// Glob patterns and the datasets they are exercised against. Centralised here so the
/// pipeline and throughput benchmarks measure the exact same workloads. Patterns live
/// as <c>const</c> strings to keep the two benchmark classes from drifting apart.
/// </summary>
public static class Workloads
{
    public const string c_BraceExpansion = "*.{js,ts,jsx,tsx}";
    public const string c_CharacterClass = "test-[0-9][a-z].txt";
    public const string c_ComplexNested = "src/**/!(*.test|*.spec).{js,jsx,ts,tsx}";
    public const string c_Extglob = "!(*.test|*.spec).{js,ts}";
    public const string c_Globstar = "**/*.js";
    public const string c_Negation = "!*.md";
    public const string c_RealWorld = "**/*.{js,jsx}";
    public const string c_SimpleWildcard = "*.js";

    public static readonly string[] s_JsFiles =
        [.. Enumerable.Range(0, 500).Select(i => $"app{i}.js")];

    public static readonly string[] s_MixedExtensions = BuildMixedExtensions();

    public static readonly string[] s_CharacterClassFiles =
        [.. Enumerable.Range(0, 100).SelectMany(i => new[] { $"test-{i % 10}a.txt", $"test-{i % 10}z.txt", $"test-{i % 10}X.txt" })];

    public static readonly string[] s_DeepPaths = BuildDeepPaths();

    // Datasets are built once and cached; generators stay deterministic so runs are comparable.


    public static readonly string[] s_RealWorldPaths =
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

    /// <summary>Maps a scenario to the dataset its pattern is run against.</summary>
    public static string[] DataFor(Scenario scenario)
    {
        return scenario switch
        {
            Scenario.SimpleWildcard => s_JsFiles,
            Scenario.Globstar => s_DeepPaths,
            Scenario.BraceExpansion => s_MixedExtensions,
            Scenario.Extglob => s_DeepPaths,
            Scenario.ComplexNested => s_DeepPaths,
            Scenario.CharacterClass => s_CharacterClassFiles,
            Scenario.RealWorld => s_RealWorldPaths,
            Scenario.Negation => s_MixedExtensions,
            _ => s_JsFiles
        };
    }

    /// <summary>Maps a scenario to its glob pattern.</summary>
    public static string PatternFor(Scenario scenario)
    {
        return scenario switch
        {
            Scenario.SimpleWildcard => c_SimpleWildcard,
            Scenario.Globstar => c_Globstar,
            Scenario.BraceExpansion => c_BraceExpansion,
            Scenario.Extglob => c_Extglob,
            Scenario.ComplexNested => c_ComplexNested,
            Scenario.CharacterClass => c_CharacterClass,
            Scenario.RealWorld => c_RealWorld,
            Scenario.Negation => c_Negation,
            _ => c_SimpleWildcard
        };
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

    private static string[] BuildMixedExtensions()
    {
        string[] extensions = ["js", "ts", "jsx", "tsx", "css", "scss", "html", "json", "md", "txt"];
        return [.. Enumerable.Range(0, 1000).Select(i => $"file{i}.{extensions[i % extensions.Length]}")];
    }

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
}

/// <summary>
/// Per-phase baseline of the glob compilation lifecycle. Each scenario yields one row per
/// phase so the cost of regex-source compilation, regex construction and running the regex
/// can be read independently. <see cref="Create"/> is end-to-end construction, which may
/// skip building the regex, so it is not the sum of the phases.
/// </summary>
[MemoryDiagnoser]
public class PipelineBenchmarks
{
    private Regex _compiled = null!;
    private string[] _data = [];

    private string _pattern = "";
    private string _regexSource = "";

    /// <summary>Structural scan of the pattern (no regex compilation).</summary>
    [Benchmark]
    public GlobInfo Analyze()
    {
        return Glob.Analyze(_pattern);
    }

    /// <summary>Lex, parse and emit the glob pattern as a regex source.</summary>
    [Benchmark]
    public string CompileSource()
    {
        return GlobCompiler.CompileRegexSource(_pattern, GlobOptions.Default);
    }

    /// <summary>Construct a <see cref="Regex"/> from an already-compiled regex source.</summary>
    [Benchmark]
    public Regex ConstructRegex()
    {
        return new Regex(_regexSource);
    }

    /// <summary>End-to-end construction of a <see cref="Glob"/>, which may skip building the regex.</summary>
    [Benchmark]
    public Glob Create()
    {
        return new Glob(_pattern);
    }

    /// <summary>Run the regex <see cref="Glob.ToRegex"/> returns over the scenario's dataset; <see cref="Glob.IsMatch(string)"/> uses it only when no regex-free matcher applies.</summary>
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

    [GlobalSetup]
    public void Setup()
    {
        _pattern = Workloads.PatternFor(Scenario);
        _data = Workloads.DataFor(Scenario);

        // Pre-stage each phase's input so the benchmarked call measures only that phase.
        _regexSource = GlobCompiler.CompileRegexSource(_pattern, GlobOptions.Default);
        _compiled = new Glob(_pattern).ToRegex();
    }

    [ParamsAllValues]
    public Workloads.Scenario Scenario { get; set; }
}

/// <summary>
/// End-to-end match throughput of a pre-compiled matcher over a representative dataset:
/// the cost a consumer pays per path once a matcher has been created. Includes a
/// multi-pattern (OR) scenario that the single-pattern <see cref="PipelineBenchmarks"/>
/// cannot decompose.
/// </summary>
[MemoryDiagnoser]
public class MatchBenchmarks
{
    private Dictionary<MatchScenario, (Glob Matcher, string[] Data)> _scenarios = null!;

    [Benchmark]
    public int Match()
    {
        var (matcher, data) = _scenarios[Scenario];
        int count = 0;
        foreach (string path in data)
        {
            if (matcher.IsMatch(path))
                count++;
        }

        return count;
    }

    [GlobalSetup]
    public void Setup()
    {
        _scenarios = new Dictionary<MatchScenario, (Glob, string[])>
        {
            [MatchScenario.SimpleWildcard] = (new Glob(Workloads.c_SimpleWildcard), Workloads.s_JsFiles),
            [MatchScenario.Globstar] = (new Glob(Workloads.c_Globstar), Workloads.s_DeepPaths),
            [MatchScenario.BraceExpansion] = (new Glob(Workloads.c_BraceExpansion), Workloads.s_MixedExtensions),
            [MatchScenario.Extglob] = (new Glob(Workloads.c_Extglob), Workloads.s_DeepPaths),
            [MatchScenario.ComplexNested] = (new Glob(Workloads.c_ComplexNested), Workloads.s_DeepPaths),
            [MatchScenario.Negation] = (new Glob(Workloads.c_Negation), Workloads.s_MixedExtensions),
            [MatchScenario.CharacterClass] = (new Glob(Workloads.c_CharacterClass), Workloads.s_CharacterClassFiles),
            [MatchScenario.MultiplePatterns] = (new Glob(new[] { "**/*.js", "**/*.ts", "!**/node_modules/**" }), Workloads.s_RealWorldPaths),
        };
    }

    [ParamsAllValues]
    public MatchScenario Scenario { get; set; }

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
}