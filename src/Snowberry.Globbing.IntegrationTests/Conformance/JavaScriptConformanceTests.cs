using Snowberry.Globbing.IntegrationTests.Infrastructure;

namespace Snowberry.Globbing.IntegrationTests.Conformance;

public class JavaScriptConformanceTests(ConformanceFixture fixture, NodeFixture node)
{
    [Theory]
    [MemberData(nameof(ConformanceData.OptionSets), MemberType = typeof(ConformanceData))]
    public async Task GeneratedRegex_ShouldMatchLikePicomatch(string optionSet)
    {
        var cases = fixture.CasesFor(optionSet);

        var actual = await node.EvaluateAsync([.. cases.Select(c => c.Source)], fixture.Inputs);

        fixture.AssertConforms("JavaScript (Node 24)", cases, actual);
    }
}