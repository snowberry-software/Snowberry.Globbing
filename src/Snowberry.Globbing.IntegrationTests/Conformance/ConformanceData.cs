using System.Text.Json;

namespace Snowberry.Globbing.IntegrationTests.Conformance;

/// <summary>
/// Theory data read from the picomatch oracle fixture.
/// </summary>
public static class ConformanceData
{
    /// <summary>
    /// Gets the option set names of the fixture, one theory row per option set.
    /// </summary>
    public static TheoryData<string> OptionSets
    {
        get
        {
            using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Conformance", "picomatch-cases.json")));
            return [.. document.RootElement.GetProperty("optionSets").EnumerateObject().Select(o => o.Name)];
        }
    }
}