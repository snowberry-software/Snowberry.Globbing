namespace Snowberry.Globbing.Tests;

/// <summary>
/// Tests for options.expandRange.
/// Ported from: https://github.com/micromatch/picomatch/blob/master/test/options.expandRange.js
/// </summary>
public class OptionsExpandRangeTests
{
    [Theory]
    [InlineData("a", "a", true)]
    [InlineData("b", "a", false)]
    [InlineData("a", "{a,b,c}", true)]
    [InlineData("b", "{a,b,c}", true)]
    [InlineData("c", "{a,b,c}", true)]
    [InlineData("d", "{a,b,c}", false)]
    public void ExpandRange_BasicBraces(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Fact]
    public void ExpandRange_CustomExpander()
    {
        var options = new GlobbingOptions
        {
            ExpandRange = (range, opts) =>
            {
                // Custom logic to expand ranges
                var result = new List<string>();
                if (range.Length >= 2 &&
                    int.TryParse(range[0], out int startNum) &&
                    int.TryParse(range[1], out int endNum))
                {
                    for (int i = startNum; i <= endNum; i++)
                    {
                        result.Add(i.ToString());
                    }
                }

                return $"({string.Join("|", result)})";
            }
        };

        // Basic test with custom expander
        Assert.True(GlobMatcher.IsMatch("a1b", "a{1..3}b", options));
        Assert.True(GlobMatcher.IsMatch("a2b", "a{1..3}b", options));
        Assert.True(GlobMatcher.IsMatch("a3b", "a{1..3}b", options));
        Assert.False(GlobMatcher.IsMatch("a4b", "a{1..3}b", options));
    }
}
