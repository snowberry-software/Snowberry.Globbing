using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Snowberry.Globbing.IntegrationTests.Infrastructure;

namespace Snowberry.Globbing.IntegrationTests.Conformance;

public class PostgreSqlConformanceTests(ConformanceFixture fixture, PostgreSqlFixture postgres) : IAsyncLifetime
{
    [Theory]
    [MemberData(nameof(ConformanceData.OptionSets), MemberType = typeof(ConformanceData))]
    public async Task GeneratedRegex_ShouldMatchLikePicomatch_WhenTranslatedByNpgsql(string optionSet)
    {
        var cases = fixture.CasesFor(optionSet);
        await using var context = postgres.CreateContext();

        var actual = new List<(IReadOnlySet<int>?, string?)>(cases.Count);
        foreach (var c in cases)
        {
            string source = c.Source;
            try
            {
                var ids = await context.Samples
                    .Where(s => Regex.IsMatch(s.Value, source, RegexOptions.Singleline))
                    .Select(s => s.Id)
                    .ToListAsync(TestContext.Current.CancellationToken);
                actual.Add((ids.ToHashSet(), null));
            }
            catch (PostgresException e)
            {
                actual.Add((null, e.MessageText));
            }
        }

        fixture.AssertConforms("PostgreSQL 17", cases, actual);
    }

    public async ValueTask InitializeAsync()
    {
        await using var context = postgres.CreateContext();
        if (!await context.Database.EnsureCreatedAsync())
            return;

        context.Samples.AddRange(fixture.Inputs.Select((value, id) => new Sample { Id = id, Value = value }));
        await context.SaveChangesAsync();
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}