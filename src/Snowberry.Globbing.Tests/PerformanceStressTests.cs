namespace Snowberry.Globbing.Tests;

public class PerformanceStressTests
{
    private static readonly GlobOptions s_Posix = new() { PathStyle = GlobPathStyle.Posix };

    private static string Nest(string open, string close, int depth)
    {
        return string.Concat(Enumerable.Repeat(open, depth)) + "b" + string.Concat(Enumerable.Repeat(close, depth));
    }

    [Theory]
    [InlineData("dir", "file.js", true)]
    [InlineData("dir", "file.ts", false)]
    [InlineData("dir", ".file.js", false)]
    [InlineData(".git", "file.js", false)]
    public void Globstar_OnThousandSegmentPath_DecidesBySegment(string middle, string fileName, bool expected)
    {
        string half = string.Join("/", Enumerable.Repeat("dir", 500));
        string path = half + "/" + middle + "/" + half + "/" + fileName;

        new Glob("**/*.js", s_Posix).IsMatch(path).Should().Be(expected);
    }

    [Fact]
    public void ManyPatterns_InOneGlob_ReportTheMatchingPattern()
    {
        var glob = new Glob(Enumerable.Range(0, 500).Select(i => $"**/*.{i}.js"), s_Posix);

        glob.Match("src/app.250.js").Pattern.Should().Be("**/*.250.js");
        glob.IsMatch("src/app.500.js").Should().BeFalse();
    }

    [Theory]
    [InlineData("(", ")")]
    [InlineData("@(", ")")]
    [InlineData("{a,", "}")]
    public void Nesting_IsLimitedTo256Levels(string open, string close)
    {
        new Glob(Nest(open, close, 256), s_Posix).IsMatch("b").Should().BeTrue();

        var e = FluentActions.Invoking(() => new Glob(Nest(open, close, 257), s_Posix)).Should().ThrowExactly<GlobParseException>().Which;
        e.Error.Should().Be(GlobParseError.NestingTooDeep);
    }

    [Theory]
    [InlineData("v{1..4096}", "v4096", true)]
    [InlineData("v{1..4096}", "v4097", false)]
    // Past 4096 values a range is not expanded and matches as literal text.
    [InlineData("*v{1..100000}", "xv{1..100000}", true)]
    [InlineData("*v{1..100000}", "xv5", false)]
    public void NumericRange_IsCappedInsteadOfExpanded(string pattern, string input, bool expected)
    {
        new Glob(pattern, s_Posix).IsMatch(input).Should().Be(expected);
    }

    [Theory]
    [InlineData(".js", true)]
    [InlineData(".ts", false)]
    [InlineData("/b.js", false)]
    public void Star_OnVeryLongSegment_Completes(string suffix, bool expected)
    {
        string input = new string('a', 100_000) + suffix;

        new Glob("*.js", s_Posix).IsMatch(input).Should().Be(expected);
    }
}