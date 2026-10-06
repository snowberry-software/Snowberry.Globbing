using System.Text.RegularExpressions;

namespace Snowberry.Globbing.IntegrationTests.Conformance;

public class DotNetConformanceTests(ConformanceFixture fixture)
{
    [Theory]
    [MemberData(nameof(ConformanceData.OptionSets), MemberType = typeof(ConformanceData))]
    public void GeneratedRegex_ShouldMatchLikePicomatch(string optionSet)
    {
        var cases = fixture.CasesFor(optionSet);

        var actual = cases.Select(c =>
        {
            try
            {
                var regex = new Regex(c.Source);
                IReadOnlySet<int> matches = Enumerable.Range(0, fixture.Inputs.Count).Where(i => regex.IsMatch(fixture.Inputs[i])).ToHashSet();
                return (matches, (string?)null);
            }
            catch (ArgumentException e)
            {
                return ((IReadOnlySet<int>?)null, e.Message);
            }
        }).ToList();

        fixture.AssertConforms(".NET", cases, actual);
    }
}