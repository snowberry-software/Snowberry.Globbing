using System.Collections.Concurrent;
using Snowberry.Globbing.Models;

namespace Snowberry.Globbing.Tests;

/// <summary>
/// Tests for multi-threaded scenarios to ensure thread-safety
/// </summary>
public class MultiThreadingTests
{
    [Fact]
    public void Create_CalledConcurrently_ShouldBeThreadSafe()
    {
        const int threadCount = 50;
        const int iterationsPerThread = 100;
        string[] patterns = ["*.js", "**/*.ts", "src/**/*.{js,ts}", "test-[0-9].txt"];
        var errors = new ConcurrentBag<Exception>();

        Parallel.For(0, threadCount, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, i =>
        {
            try
            {
                for (int j = 0; j < iterationsPerThread; j++)
                {
                    string pattern = patterns[j % patterns.Length];
                    var matcher = GlobMatcher.Create(pattern);
                    string testFile = j % 2 == 0 ? "test.js" : "test.ts";
                    _ = matcher(testFile);
                }
            }
            catch (Exception ex)
            {
                errors.Add(ex);
            }
        });

        Assert.Empty(errors);
    }

    [Fact]
    public void MakeRe_CalledConcurrently_ProducesConsistentResults()
    {
        const int iterations = 1000;
        string pattern = "**/*.{js,ts,tsx}";
        var results = new ConcurrentBag<string>();

        Parallel.For(0, iterations, _ =>
        {
            var regex = GlobMatcher.MakeRe(pattern);
            results.Add(regex.ToString());
        });

        var uniqueResults = results.Distinct().ToList();
        Assert.Single(uniqueResults);
    }

    [Fact]
    public void IsMatch_WithSharedMatcher_IsThreadSafe()
    {
        var matcher = GlobMatcher.Create("**/*.js");
        string[] testPaths = [.. Enumerable.Range(0, 1000).Select(i => $"path/to/file{i}.js")];

        var results = new ConcurrentBag<bool>();

        Parallel.ForEach(testPaths, path =>
        {
            results.Add(matcher(path));
        });

        Assert.Equal(testPaths.Length, results.Count);
        Assert.All(results, Assert.True);
    }

    [Fact]
    public void Parse_CalledConcurrentlyWithDifferentPatterns_ReturnsCorrectResults()
    {
        var patterns = new Dictionary<string, int>
        {
            ["*.js"] = 0,
            ["**/*.ts"] = 0,
            ["{a,b,c}/*.txt"] = 0,
            ["test-[0-9].log"] = 0
        };

        var locks = patterns.Keys.ToDictionary(k => k, _ => new object());
        var results = new ConcurrentDictionary<string, List<ParseState>>(
            patterns.Keys.Select(k => new KeyValuePair<string, List<ParseState>>(k, []))
        );

        Parallel.For(0, 100, _ =>
        {
            foreach (string pattern in patterns.Keys)
            {
                var state = GlobMatcher.Parse(pattern);
                lock (locks[pattern])
                {
                    results[pattern].Add(state);
                }
            }
        });

        foreach (string pattern in patterns.Keys)
        {
            Assert.Equal(100, results[pattern].Count);
            var outputs = results[pattern].Select(s => s.Output).Distinct().ToList();
            Assert.Single(outputs);
        }
    }

    [Fact]
    public void Scan_CalledConcurrently_ReturnsConsistentResults()
    {
        string pattern = "src/**/*.{js,ts}";
        var scanResults = new ConcurrentBag<ScanResult>();

        Parallel.For(0, 500, _ =>
        {
            var result = GlobMatcher.Scan(pattern);
            scanResults.Add(result);
        });

        bool allEqual = scanResults.All(r =>
            r.IsGlob == scanResults.First().IsGlob &&
            r.IsBrace == scanResults.First().IsBrace &&
            r.Glob == scanResults.First().Glob
        );

        Assert.True(allEqual);
    }

    [Fact]
    public async Task Create_WithMultipleAsyncTasks_CompletesSuccessfully()
    {
        const int taskCount = 100;
        var tasks = new List<Task<bool>>();

        for (int i = 0; i < taskCount; i++)
        {
            int index = i;
            tasks.Add(Task.Run(() =>
            {
                string pattern = index % 2 == 0 ? "*.js" : "**/*.ts";
                var matcher = GlobMatcher.Create(pattern);
                string testFile = index % 2 == 0 ? "test.js" : "path/test.ts";
                return matcher(testFile);
            }));
        }

        bool[] results = await Task.WhenAll(tasks);
        Assert.All(results, Assert.True);
    }

    [Fact]
    public void MultiplePatternsMatching_UnderConcurrency_ProducesCorrectResults()
    {
        string[] patterns = ["*.js", "*.ts", "*.md"];
        var testFiles = new[]
        {
            ("test.js", new[] { true, false, false }),
            ("test.ts", [false, true, false]),
            ("README.md", [false, false, true]),
            ("file.txt", [false, false, false])
        };

        var errors = new ConcurrentBag<string>();

        Parallel.ForEach(testFiles, testCase =>
        {
            for (int i = 0; i < patterns.Length; i++)
            {
                var matcher = GlobMatcher.Create(patterns[i]);
                bool result = matcher(testCase.Item1);
                if (result != testCase.Item2[i])
                {
                    errors.Add($"Pattern {patterns[i]} failed for {testCase.Item1}");
                }
            }
        });

        Assert.Empty(errors);
    }

    [Fact]
    public void ConcurrentOptionsModification_DoesNotAffectOtherThreads()
    {
        var results = new ConcurrentBag<bool>();

        Parallel.For(0, 100, i =>
        {
            var options = new GlobbingOptions
            {
                NoCase = i % 2 == 0,
                Dot = i % 3 == 0
            };

            var matcher = GlobMatcher.Create("*.JS", options);
            bool result = matcher("test.js");
            results.Add(result);
        });

        Assert.Equal(100, results.Count);
    }

    [Fact]
    public void StringBuilderPool_UnderConcurrency_DoesNotCauseCorruption()
    {
        var errors = new ConcurrentBag<Exception>();
        const int iterations = 1000;

        Parallel.For(0, iterations, _ =>
        {
            try
            {
                string[] patterns = ["**/*.js", "src/**/*.{ts,tsx}", "test-[0-9].log"];
                foreach (string? pattern in patterns)
                {
                    var compiledRegex = GlobMatcher.MakeRe(pattern);
                    string regexString = compiledRegex.ToString();
                }
            }
            catch (Exception ex)
            {
                errors.Add(ex);
            }
        });

        Assert.Empty(errors);
    }
}
