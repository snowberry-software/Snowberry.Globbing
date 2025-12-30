using System.Diagnostics;
using Xunit.Abstractions;

namespace Snowberry.Globbing.Tests;

/// <summary>
/// Performance and stress tests to ensure the library handles high load
/// </summary>
public class PerformanceStressTests
{
    private readonly ITestOutputHelper _output;

    public PerformanceStressTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void LargeNumberOfPatterns_ShouldCompileQuickly()
    {
        string[] patterns = [.. Enumerable.Range(0, 1000).Select(i => $"**/*.{i}.js")];

        var sw = Stopwatch.StartNew();
        foreach (string? pattern in patterns)
        {
            _ = GlobMatcher.MakeRe(pattern);
        }

        sw.Stop();

        _output.WriteLine($"Compiled 1000 patterns in {sw.ElapsedMilliseconds}ms");
        Assert.True(sw.ElapsedMilliseconds < 5000); // Should complete in under 5 seconds
    }

    [Fact]
    public void LargeNumberOfMatches_ShouldExecuteQuickly()
    {
        var matcher = GlobMatcher.Create("**/*.js");
        string[] testPaths = [.. Enumerable.Range(0, 10000).Select(i => $"path/to/file{i}.js")];

        var sw = Stopwatch.StartNew();
        foreach (string? path in testPaths)
        {
            _ = matcher(path);
        }

        sw.Stop();

        _output.WriteLine($"Matched 10000 paths in {sw.ElapsedMilliseconds}ms");
        Assert.True(sw.ElapsedMilliseconds < 1000); // Should complete in under 1 second
    }

    [Fact]
    public void ComplexPatternCompilation_MeasurePerformance()
    {
        string[] complexPatterns =
        [
            "**/*.{js,jsx,ts,tsx,vue,svelte}",
            "src/**/!(*.test|*.spec).{js,ts}",
            "{src,test,lib}/**/@(index|main).{js,ts}",
            "**/{node_modules,dist,build}/**",
            "+(a|b|c|d|e|f|g|h|i|j)/**/*.js"
        ];

        var sw = Stopwatch.StartNew();
        foreach (string? pattern in complexPatterns)
        {
            _ = GlobMatcher.MakeRe(pattern);
        }

        sw.Stop();

        _output.WriteLine($"Compiled {complexPatterns.Length} complex patterns in {sw.ElapsedMilliseconds}ms");
        Assert.True(sw.ElapsedMilliseconds < 500);
    }

    [Fact]
    public void DeepDirectoryStructure_ShouldMatchQuickly()
    {
        var matcher = GlobMatcher.Create("**/*.js");
        string deepPath = string.Join("/", Enumerable.Repeat("dir", 100)) + "/test.js";

        var sw = Stopwatch.StartNew();
        bool result = matcher(deepPath);
        sw.Stop();

        _output.WriteLine($"Matched deep path in {sw.ElapsedTicks} ticks");
        Assert.True(result);
        Assert.True(sw.ElapsedMilliseconds < 100);
    }

    [Fact]
    public void ScanLargePattern_PerformanceTest()
    {
        string pattern = "src/**/!(*.test|*.spec).{js,jsx,ts,tsx,vue,svelte,astro}";

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < 100; i++)
        {
            _ = GlobMatcher.Scan(pattern);
        }

        sw.Stop();

        _output.WriteLine($"Scanned pattern 100 times in {sw.ElapsedMilliseconds}ms");
        Assert.True(sw.ElapsedMilliseconds < 500);
    }

    [Fact]
    public void ParseLargePattern_PerformanceTest()
    {
        string pattern = "{src,lib,test,config,tools}/**/!(*.min|*.map).{js,ts,json,yaml,yml}";

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < 100; i++)
        {
            _ = GlobMatcher.Parse(pattern);
        }

        sw.Stop();

        _output.WriteLine($"Parsed pattern 100 times in {sw.ElapsedMilliseconds}ms");
        Assert.True(sw.ElapsedMilliseconds < 1000);
    }

    [Fact]
    public void StressTest_ManyPatternsAndPaths()
    {
        string[] patterns =
        [
            "**/*.js",
            "**/*.ts",
            "src/**/*.{js,ts}",
            "!**/node_modules/**",
            "test/**/*.spec.js"
        ];

        var matchers = patterns.Select(p => GlobMatcher.Create(p)).ToArray();
        string[] testPaths = [.. Enumerable.Range(0, 1000)
            .SelectMany(i => new[]
            {
                $"src/file{i}.js",
                $"test/file{i}.spec.js",
                $"node_modules/lib{i}/index.js",
                $"lib/util{i}.ts"
            })];

        var sw = Stopwatch.StartNew();
        int totalMatches = 0;
        foreach (string? path in testPaths)
        {
            foreach (var matcher in matchers)
            {
                if (matcher(path))
                    totalMatches++;
            }
        }

        sw.Stop();

        _output.WriteLine($"Tested {testPaths.Length} paths against {patterns.Length} patterns in {sw.ElapsedMilliseconds}ms");
        _output.WriteLine($"Total matches: {totalMatches}");
        Assert.True(sw.ElapsedMilliseconds < 2000);
    }

    [Fact]
    public void MemoryPressure_CreateManyMatchers()
    {
        long initialMemory = GC.GetTotalMemory(true);
        var matchers = new System.Collections.Generic.List<MatcherHandler>();

        for (int i = 0; i < 1000; i++)
        {
            matchers.Add(GlobMatcher.Create($"**/*.{i}.js"));
        }

        long afterCreation = GC.GetTotalMemory(false);
        double allocated = (afterCreation - initialMemory) / 1024.0 / 1024.0; // MB

        _output.WriteLine($"Created 1000 matchers, allocated ~{allocated:F2} MB");

        // Use the matchers to prevent optimization
        Assert.True(matchers[0]("test.0.js"));
        Assert.Equal(1000, matchers.Count);
    }

    [Fact]
    public void FastPathOptimization_VsFullParsing()
    {
        string simplePattern = "*.js";
        var options = new GlobbingOptions { FastPaths = true };
        var optionsNoFastpath = new GlobbingOptions { FastPaths = false };

        var sw1 = Stopwatch.StartNew();
        for (int i = 0; i < 10000; i++)
        {
            _ = GlobMatcher.MakeRe(simplePattern, options);
        }

        sw1.Stop();

        var sw2 = Stopwatch.StartNew();
        for (int i = 0; i < 10000; i++)
        {
            _ = GlobMatcher.MakeRe(simplePattern, optionsNoFastpath);
        }

        sw2.Stop();

        _output.WriteLine($"With fastpaths: {sw1.ElapsedMilliseconds}ms");
        _output.WriteLine($"Without fastpaths: {sw2.ElapsedMilliseconds}ms");

        // Both should complete in reasonable time
        Assert.True(sw1.ElapsedMilliseconds < 2000);
        Assert.True(sw2.ElapsedMilliseconds < 2000);
    }

    [Fact]
    public void RegexCompilation_VsInterpretation()
    {
        string pattern = "**/*.{js,ts,jsx,tsx}";
        var matcher = GlobMatcher.Create(pattern);
        string[] testPaths = [.. Enumerable.Range(0, 1000).Select(i => $"src/components/Component{i}.tsx")];

        // Warm up
        foreach (string? path in testPaths.Take(10))
        {
            _ = matcher(path);
        }

        var sw = Stopwatch.StartNew();
        foreach (string? path in testPaths)
        {
            _ = matcher(path);
        }

        sw.Stop();

        _output.WriteLine($"Matched 1000 paths with compiled regex in {sw.ElapsedMilliseconds}ms");
        Assert.True(sw.ElapsedMilliseconds < 100);
    }

    [Fact]
    public void CacheEfficiency_RepeatedPatterns()
    {
        string[] patterns = ["*.js", "*.ts", "*.md"];

        var sw1 = Stopwatch.StartNew();
        for (int i = 0; i < 1000; i++)
        {
            foreach (string? pattern in patterns)
            {
                _ = GlobMatcher.MakeRe(pattern);
            }
        }

        sw1.Stop();

        _output.WriteLine($"Created regex 3000 times (1000 iterations x 3 patterns) in {sw1.ElapsedMilliseconds}ms");

        // Should complete quickly even without explicit caching due to regex compilation
        Assert.True(sw1.ElapsedMilliseconds < 3000);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    public void ScalabilityTest_VaryingPathCounts(int pathCount)
    {
        var matcher = GlobMatcher.Create("**/*.js");
        string[] paths = [.. Enumerable.Range(0, pathCount).Select(i => $"path/to/file{i}.js")];

        var sw = Stopwatch.StartNew();
        foreach (string? path in paths)
        {
            _ = matcher(path);
        }

        sw.Stop();

        _output.WriteLine($"Matched {pathCount} paths in {sw.ElapsedMilliseconds}ms ({sw.ElapsedMilliseconds * 1000.0 / pathCount:F2} �s/path)");

        // Should scale linearly - very generous timing
        int expectedMaxMs = Math.Max(100, pathCount); // At least 100ms budget, or 1ms per path
        Assert.True(sw.ElapsedMilliseconds < expectedMaxMs, $"Expected < {expectedMaxMs}ms but was {sw.ElapsedMilliseconds}ms");
    }
}
