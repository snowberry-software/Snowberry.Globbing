using System.Text.Json;

namespace Snowberry.Globbing.IntegrationTests.Conformance;

/// <summary>
/// One pattern compiled under one option set, with the input indices picomatch matches.
/// </summary>
/// <param name="Pattern">The glob pattern.</param>
/// <param name="OptionSet">The name of the option set the pattern was compiled with.</param>
/// <param name="Source">The regex source Snowberry generates for the pattern.</param>
/// <param name="Expected">The indices into <see cref="ConformanceFixture.Inputs"/> that picomatch matches.</param>
public sealed record ConformanceCase(string Pattern, string OptionSet, string Source, IReadOnlySet<int> Expected);

/// <summary>
/// Loads the picomatch oracle (<c>picomatch-cases.json</c>) and the regex Snowberry generates for every case.
/// </summary>
public sealed class ConformanceFixture
{
    private const string c_LiteralBraces = "picomatch emits NoBrace braces unescaped, so they act as regex quantifiers; Snowberry matches them literally.";

    private static readonly Dictionary<(string Pattern, string OptionSet), string> s_KnownDeviations = new()
    {
        [("foo{1,2}bar", "nobrace")] = c_LiteralBraces,
        [("a{1}", "nobrace")] = c_LiteralBraces,
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="ConformanceFixture"/> class.
    /// </summary>
    public ConformanceFixture()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Conformance", "picomatch-cases.json")));
        var root = document.RootElement;

        PicomatchCommit = root.GetProperty("picomatch").GetString()!;
        Inputs = [.. root.GetProperty("inputs").EnumerateArray().Select(i => i.GetString()!)];

        var optionSets = root.GetProperty("optionSets").EnumerateObject().ToDictionary(o => o.Name, o => o.Value);
        OptionSets = [.. optionSets.Keys];

        var cases = new List<ConformanceCase>();
        foreach (var c in root.GetProperty("cases").EnumerateArray())
        {
            string pattern = c.GetProperty("pattern").GetString()!;
            string optionSet = c.GetProperty("optionSet").GetString()!;
            if (s_KnownDeviations.ContainsKey((pattern, optionSet)))
                continue;

            var options = ToGlobOptions(optionSets[optionSet]);
            var expected = c.GetProperty("matches").EnumerateArray().Select(m => m.GetInt32()).ToHashSet();
            cases.Add(new ConformanceCase(pattern, optionSet, new Glob(pattern, options).ToRegexString(), expected));
        }

        Cases = cases;
    }

    /// <summary>
    /// Gets the picomatch commit the expected results were generated from.
    /// </summary>
    public string PicomatchCommit { get; }

    /// <summary>
    /// Gets the inputs every case is matched against.
    /// </summary>
    public IReadOnlyList<string> Inputs { get; }

    /// <summary>
    /// Gets the names of the option sets in the fixture.
    /// </summary>
    public IReadOnlyList<string> OptionSets { get; }

    /// <summary>
    /// Gets every case in the fixture.
    /// </summary>
    public IReadOnlyList<ConformanceCase> Cases { get; }

    /// <summary>
    /// Gets the cases compiled with the specified option set.
    /// </summary>
    /// <param name="optionSet">The option set name.</param>
    /// <returns>The cases of <paramref name="optionSet"/>.</returns>
    public IReadOnlyList<ConformanceCase> CasesFor(string optionSet)
    {
        return [.. Cases.Where(c => c.OptionSet == optionSet)];
    }

    /// <summary>
    /// Asserts that every case matched exactly the inputs picomatch matches.
    /// </summary>
    /// <param name="engine">The engine name, used in the failure message.</param>
    /// <param name="cases">The cases that were evaluated.</param>
    /// <param name="actual">The matched input indices per case, or an error message when the engine rejected the regex.</param>
    public void AssertConforms(string engine, IReadOnlyList<ConformanceCase> cases, IReadOnlyList<(IReadOnlySet<int>? Matches, string? Error)> actual)
    {
        var failures = new List<string>();
        for (int i = 0; i < cases.Count; i++)
        {
            var c = cases[i];
            var (matches, error) = actual[i];

            if (error != null)
            {
                failures.Add($"{c.Pattern} [{c.OptionSet}] {c.Source}: {engine} rejected the regex: {error}");
                continue;
            }

            var missing = c.Expected.Except(matches!).Select(Describe).ToList();
            var unexpected = matches!.Except(c.Expected).Select(Describe).ToList();
            if (missing.Count > 0 || unexpected.Count > 0)
                failures.Add($"{c.Pattern} [{c.OptionSet}] {c.Source}: missing [{string.Join(", ", missing)}] unexpected [{string.Join(", ", unexpected)}]");
        }

        failures.Should().BeEmpty($"{failures.Count} case(s) differ from picomatch {PicomatchCommit} on {engine}:{Environment.NewLine}{string.Join(Environment.NewLine, failures.Take(50))}");
    }

    private string Describe(int index)
    {
        return JsonSerializer.Serialize(Inputs[index]);
    }

    private static GlobOptions ToGlobOptions(JsonElement options)
    {
        var result = new GlobOptions { PathStyle = GlobPathStyle.Posix };
        foreach (var option in options.EnumerateObject())
        {
            bool value = option.Value.GetBoolean();
            result = option.Name switch
            {
                "dot" => result with { MatchDotFiles = value },
                "strictSlashes" => result with { StrictSlashes = value },
                "noglobstar" => result with { Globstar = !value },
                "noextglob" => result with { Extglobs = !value },
                "nobrace" => result with { BraceExpansion = !value },
                _ => throw new NotSupportedException($"Unmapped picomatch option '{option.Name}'."),
            };
        }

        return result;
    }
}