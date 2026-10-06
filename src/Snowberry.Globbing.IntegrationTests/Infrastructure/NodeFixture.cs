using System.Text;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace Snowberry.Globbing.IntegrationTests.Infrastructure;

/// <summary>
/// Starts a Node.js container shared by all tests in the assembly, used to evaluate regexes with the JavaScript engine.
/// </summary>
public sealed class NodeFixture : IAsyncLifetime
{
    private const string c_Script = """
        const fs = require('fs');
        const { regexes, inputs } = JSON.parse(fs.readFileSync('/work/input.json', 'utf8'));
        const results = regexes.map(source => {
          try {
            const regex = new RegExp(source);
            return { matches: inputs.flatMap((input, index) => regex.test(input) ? [index] : []) };
          } catch (e) {
            return { error: e.message };
          }
        });
        fs.writeFileSync('/work/output.json', JSON.stringify(results));
        """;

    private readonly IContainer _container = new ContainerBuilder("node:24-alpine")
        .WithEntrypoint("sleep")
        .WithCommand("infinity")
        .Build();

    /// <summary>
    /// Evaluates every regex against every input with JavaScript's <c>RegExp</c>, without flags.
    /// </summary>
    /// <param name="regexes">The regex sources.</param>
    /// <param name="inputs">The inputs to match.</param>
    /// <returns>The matched input indices per regex, or the error message when JavaScript rejected the regex.</returns>
    public async Task<IReadOnlyList<(IReadOnlySet<int>? Matches, string? Error)>> EvaluateAsync(IReadOnlyList<string> regexes, IReadOnlyList<string> inputs)
    {
        string payload = System.Text.Json.JsonSerializer.Serialize(new { regexes, inputs });
        await _container.CopyAsync(Encoding.UTF8.GetBytes(payload), "/work/input.json");
        await _container.CopyAsync(Encoding.UTF8.GetBytes(c_Script), "/work/run.js");

        var result = await _container.ExecAsync(["node", "/work/run.js"]);
        result.ExitCode.Should().Be(0, $"node exited with {result.ExitCode}: {result.Stderr}");

        using var output = System.Text.Json.JsonDocument.Parse(await _container.ReadFileAsync("/work/output.json"));
        return [.. output.RootElement.EnumerateArray().Select(r => r.TryGetProperty("error", out var error)
            ? ((IReadOnlySet<int>?)null, error.GetString())
            : (r.GetProperty("matches").EnumerateArray().Select(m => m.GetInt32()).ToHashSet(), null))];
    }

    /// <inheritdoc/>
    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}