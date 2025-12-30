using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Microsoft.VSDiagnostics;

namespace Snowberry.Globbing.Benchmark;

/// <summary>
/// Comprehensive benchmarks for glob pattern matching performance.
/// Tests various pattern complexities and matching scenarios.
/// </summary>
[MemoryDiagnoser]
[CPUUsageDiagnoser]
[HtmlExporter]
[MarkdownExporterAttribute.GitHub]
public class Benchmarks
{
    private MatcherHandler _simpleWildcardMatcher;
    private MatcherHandler _globstarMatcher;
    private MatcherHandler _braceExpansionMatcher;
    private MatcherHandler _extglobMatcher;
    private MatcherHandler _complexNestedMatcher;
    private MatcherHandler _multiplePatternsMatcher;
    private MatcherHandler _negationMatcher;
    private MatcherHandler _characterClassMatcher;

    private string[] _testPaths;
    private string[] _jsFiles;
    private string[] _deepPaths;
    private string[] _mixedExtensions;
    private string[] _realWorldPaths;

    [GlobalSetup]
    public void Setup()
    {
        // Initialize matchers with various pattern complexities
        _simpleWildcardMatcher = GlobMatcher.Create("*.js");
        _globstarMatcher = GlobMatcher.Create("**/*.js");
        _braceExpansionMatcher = GlobMatcher.Create("*.{js,ts,jsx,tsx}");
        _extglobMatcher = GlobMatcher.Create("!(*.test|*.spec).{js,ts}");
        _complexNestedMatcher = GlobMatcher.Create("src/**/!(*.test|*.spec).{js,jsx,ts,tsx}");
        _multiplePatternsMatcher = GlobMatcher.Create(["**/*.js", "**/*.ts", "!**/node_modules/**"]);
        _negationMatcher = GlobMatcher.Create("!*.md");
        _characterClassMatcher = GlobMatcher.Create("test-[0-9][a-z].txt");

        // Generate test data sets
        _testPaths = GenerateTestPaths();
        _jsFiles = GenerateJsFiles();
        _deepPaths = GenerateDeepPaths();
        _mixedExtensions = GenerateMixedExtensions();
        _realWorldPaths = GenerateRealWorldPaths();
    }

    private string[] GenerateTestPaths()
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

    private string[] GenerateJsFiles()
    {
        return [.. Enumerable.Range(0, 500).Select(i => $"app{i}.js")];
    }

    private string[] GenerateDeepPaths()
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

    private string[] GenerateMixedExtensions()
    {
        string[] extensions = new[] { "js", "ts", "jsx", "tsx", "css", "scss", "html", "json", "md", "txt" };
        return [.. Enumerable.Range(0, 1000).Select(i => $"file{i}.{extensions[i % extensions.Length]}")];
    }

    private string[] GenerateRealWorldPaths()
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

    // ===== Pattern Compilation Benchmarks =====

    [Benchmark]
    public MatcherHandler CreateSimpleWildcard()
    {
        return GlobMatcher.Create("*.js");
    }

    [Benchmark]
    public MatcherHandler CreateGlobstar()
    {
        return GlobMatcher.Create("**/*.{js,ts}");
    }

    [Benchmark]
    public MatcherHandler CreateComplexPattern()
    {
        return GlobMatcher.Create("src/**/!(*.test|*.spec).{js,jsx,ts,tsx}");
    }

    [Benchmark]
    public MatcherHandler CreateMultiplePatterns()
    {
        return GlobMatcher.Create(["**/*.js", "**/*.ts", "!**/node_modules/**"]);
    }

    // ===== Matching Performance Benchmarks =====

    [Benchmark]
    public int SimpleWildcardMatching()
    {
        int count = 0;
        foreach (string path in _jsFiles)
        {
            if (_simpleWildcardMatcher(path))
                count++;
        }

        return count;
    }

    [Benchmark]
    public int GlobstarMatching()
    {
        int count = 0;
        foreach (string path in _deepPaths)
        {
            if (_globstarMatcher(path))
                count++;
        }

        return count;
    }

    [Benchmark]
    public int BraceExpansionMatching()
    {
        int count = 0;
        foreach (string path in _mixedExtensions)
        {
            if (_braceExpansionMatcher(path))
                count++;
        }

        return count;
    }

    [Benchmark]
    public int ExtglobMatching()
    {
        int count = 0;
        foreach (string path in _deepPaths)
        {
            if (_extglobMatcher(path))
                count++;
        }

        return count;
    }

    [Benchmark]
    public int ComplexNestedMatching()
    {
        int count = 0;
        foreach (string path in _deepPaths)
        {
            if (_complexNestedMatcher(path))
                count++;
        }

        return count;
    }

    [Benchmark]
    public int MultiplePatternsMatching()
    {
        int count = 0;
        foreach (string path in _realWorldPaths)
        {
            if (_multiplePatternsMatcher(path))
                count++;
        }

        return count;
    }

    // ===== IsMatch Method Benchmarks =====

    [Benchmark]
    public bool IsMatch_Simple()
    {
        return GlobMatcher.IsMatch("app.js", "*.js");
    }

    [Benchmark]
    public bool IsMatch_Globstar()
    {
        return GlobMatcher.IsMatch("src/components/Button.tsx", "**/*.tsx");
    }

    [Benchmark]
    public bool IsMatch_Complex()
    {
        return GlobMatcher.IsMatch("src/utils/helper.js", "src/**/!(*.test).{js,ts}");
    }

    [Benchmark]
    public bool IsMatch_MultiplePatterns()
    {
        return GlobMatcher.IsMatch("src/app.js", ["**/*.js", "!**/test/**"]);
    }

    // ===== Real-World Scenarios =====

    [Benchmark]
    public int RealWorld_FilterJavaScriptFiles()
    {
        var matcher = GlobMatcher.Create("**/*.{js,jsx}");
        int count = 0;
        foreach (string path in _realWorldPaths)
        {
            if (matcher(path))
                count++;
        }

        return count;
    }

    [Benchmark]
    public int RealWorld_FilterSourceFiles()
    {
        var matcher = GlobMatcher.Create("src/**/*.{js,ts,jsx,tsx}");
        int count = 0;
        foreach (string path in _realWorldPaths)
        {
            if (matcher(path))
                count++;
        }

        return count;
    }

    [Benchmark]
    public int RealWorld_ExcludeTests()
    {
        var matcher = GlobMatcher.Create("!(*.test|*.spec).*");
        int count = 0;
        foreach (string path in _realWorldPaths)
        {
            if (matcher(path))
                count++;
        }

        return count;
    }

    [Benchmark]
    public int RealWorld_FindAllTests()
    {
        var matcher = GlobMatcher.Create("**/*.{test,spec}.{js,ts,jsx,tsx}");
        int count = 0;
        foreach (string path in _realWorldPaths)
        {
            if (matcher(path))
                count++;
        }

        return count;
    }

    // ===== Character Class Benchmarks =====

    [Benchmark]
    public int CharacterClassMatching()
    {
        string[] testFiles = [.. Enumerable.Range(0, 100).SelectMany(i => new[] { $"test-{i % 10}a.txt", $"test-{i % 10}z.txt", $"test-{i % 10}X.txt" })];

        int count = 0;
        foreach (string path in testFiles)
        {
            if (_characterClassMatcher(path))
                count++;
        }

        return count;
    }

    // ===== Negation Pattern Benchmarks =====

    [Benchmark]
    public int NegationMatching()
    {
        int count = 0;
        foreach (string path in _mixedExtensions)
        {
            if (_negationMatcher(path))
                count++;
        }

        return count;
    }

    // ===== Parse and Scan Benchmarks =====

    [Benchmark]
    public object Parse_SimplePattern()
    {
        return GlobMatcher.Parse("*.js");
    }

    [Benchmark]
    public object Parse_ComplexPattern()
    {
        return GlobMatcher.Parse("src/**/!(*.test|*.spec).{js,jsx,ts,tsx}");
    }

    [Benchmark]
    public object Scan_SimplePattern()
    {
        return GlobMatcher.Scan("*.js");
    }

    [Benchmark]
    public object Scan_ComplexPattern()
    {
        return GlobMatcher.Scan("src/**/!(*.test|*.spec).{js,jsx,ts,tsx}");
    }

    // ===== MakeRe Benchmarks =====

    [Benchmark]
    public object MakeRe_SimplePattern()
    {
        return GlobMatcher.MakeRe("*.js");
    }

    [Benchmark]
    public object MakeRe_ComplexPattern()
    {
        return GlobMatcher.MakeRe("src/**/!(*.test|*.spec).{js,jsx,ts,tsx}");
    }

    // ===== Batch Processing Benchmark =====

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
