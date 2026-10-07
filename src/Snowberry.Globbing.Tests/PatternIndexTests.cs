namespace Snowberry.Globbing.Tests;

public class PatternIndexTests
{
    private static readonly string[] s_Patterns =
    [
        "**/node_modules/**", "**/*.log", "**/bin/**", "**/obj/**", "**/[Bb]in/*", "**/[Dd]ebug/**", "**/[Rr]elease/**",
        "**/[._]s[a-rt-v][a-z]", "**/*.[1-9][0-9]", "**/*.[1-9][0-9][0-9]R/**", "**/[Aa][Rr][Mm]/**", "**/*~", "**/*.a",
        "!**/keep/**", "src/**/*.ts", "packages/*/src/**/*.tsx", "docs/**", "**/.DS_Store", "**/__pycache__/**",
        "**/*.py[cod]", "**/*.{cs,vb}proj", "\\*.js", "\"quoted name\".txt", "*.tar.gz", "**/coverage*.xml",
        "**/[a-c-e]x/**", "**/[!a]bc.txt", "**/[^a]bc.txt", "**/[-ab]yz", "**/[ab-]yz", "**/[_-a]q", "@(foo|bar).md",
        "**/target/**", "build/**", "dist/**", "**/*.min.js", "**/*.map", "**/Thumbs.db", "**/*.swp", "**/*.tmp",
        "**/[[:alpha:]]zz", "**/[.]hidden", "**/x[abc]y[def]z", "**/out/**", "**/*.egg-info/**",
    ];

    private static readonly string[] s_Inputs =
    [
        "node_modules/a/b.js", "src/app/main.ts", "src/app/main.log", "a/bin/x.dll", "a/Bin/x.dll", "a/bin/x/y.dll", "Debug/x",
        "a/debug/x", "x/.swp", "x/_sxy", "x/.ssa", "a/b.10", "a/b.99", "a/b.9", "a/b.100R", "a/b.123R/c", "x/ARM/y",
        "x/arm/y", "x/[Aa][Rr][Mm]/y", "x/[Aa]rm/y", "x/[Bb]in/z", "a/b~", "a/b.a", "keep/a.txt", "a/keep/b", "other.txt",
        "packages/p1/src/a/b.tsx", "docs/readme.md", "a/.DS_Store", "a/__pycache__/b.pyc", "a/b.pyc", "a/b.pyd",
        "a/app.csproj", "a/app.vbproj", "*.js", "\\*.js", "quoted name.txt", "\"quoted name\".txt", "a.tar.gz",
        "coverage-final.xml", "a/coverage.xml", "q/cx/r", "q/ax/r", "zbc.txt", "abc.txt", "q/-yz", "q/byz", "q/`q", "q/_q",
        "foo.md", "bar.md", "baz.md", "a/target/b", "build/x", "dist/x.js", "a/b.min.js", "a/b.js.map", "Thumbs.db",
        "a/Thumbs.db", "x.tmp", "q/azz", "q/1zz", "q/.hidden", "q/xaydz", "q/xbyfz", "q/xdyfz", "a\\bin\\x.dll",
        "node_modules\\a\\b.js", "a/b.log/", "a/b.10/", "", "/", "a", ".", "x/ARM", "x/[Aa][Rr][Mm]",
    ];

    public static TheoryData<string> OptionNames => ["posix", "windows", "dot", "fileName", "literalBrackets", "classBrackets", "substring"];

    private static GlobOptions OptionsFor(string name)
    {
        var posix = new GlobOptions { PathStyle = GlobPathStyle.Posix };
        return name switch
        {
            "posix" => posix,
            "windows" => posix with { PathStyle = GlobPathStyle.Windows },
            "dot" => posix with { MatchDotFiles = true },
            "fileName" => posix with { MatchFileNameOnly = true },
            "literalBrackets" => posix with { BracketMode = GlobBracketMode.Literal },
            "classBrackets" => posix with { BracketMode = GlobBracketMode.CharacterClass },
            "substring" => posix with { MatchSubstring = true },
            _ => throw new ArgumentOutOfRangeException(nameof(name)),
        };
    }

    [Theory]
    [MemberData(nameof(OptionNames))]
    public void ManyPatterns_ReportTheFirstPatternThatMatchesOnItsOwn(string optionName)
    {
        var options = OptionsFor(optionName);
        var glob = new Glob(s_Patterns, options);

        foreach (string input in s_Inputs)
        {
            string? expected = s_Patterns.FirstOrDefault(p => Glob.IsMatch(input, p, options));
            var match = glob.Match(input);

            match.Pattern.Should().Be(expected, "the first pattern matching \"{0}\" on its own is reported", input);
            glob.IsMatch(input).Should().Be(expected != null, input);
            glob.IsMatch(input.AsSpan()).Should().Be(expected != null, input);
        }
    }

    [Theory]
    [InlineData("x/ARM/y", "**/[Aa][Rr][Mm]/**")]
    [InlineData("x/[Aa][Rr][Mm]/y", "**/[Aa][Rr][Mm]/**")]
    [InlineData("x/[Aa]Rm/y", "**/[Aa][Rr][Mm]/**")]
    [InlineData("a/b.42", "**/*.[1-9][0-9]")]
    [InlineData("x/.svz", "**/[._]s[a-rt-v][a-z]")]
    public void BracketOnlyPatterns_StillMatchAmongManyPatterns(string input, string pattern)
    {
        var glob = new Glob([.. Enumerable.Range(0, 50).Select(i => $"dir{i}/**/*.txt"), pattern], new GlobOptions { PathStyle = GlobPathStyle.Posix });

        glob.Match(input).Pattern.Should().Be(pattern);
    }

    [Fact]
    public void InputEqualToAPattern_MatchesThatPatternAmongManyPatterns()
    {
        string[] patterns = [.. Enumerable.Range(0, 50).Select(i => $"dir{i}/**/*.txt"), "[ab]c", "\\*.js"];
        var glob = new Glob(patterns, new GlobOptions { PathStyle = GlobPathStyle.Posix });

        glob.Match("[ab]c").Pattern.Should().Be("[ab]c");
        glob.Match("\\*.js").Pattern.Should().Be("\\*.js");
        glob.IsMatch("\\*.js".AsSpan()).Should().BeTrue();
    }

    [Fact]
    public void ThousandsOfPatterns_KeepTheFirstMatchingPattern()
    {
        string[] patterns = [.. Enumerable.Range(0, 3000).Select(i => $"dir{i}/**"), "**/target.txt", "dir2999/**/*.txt"];
        var glob = new Glob(patterns, new GlobOptions { PathStyle = GlobPathStyle.Posix });

        glob.Match("dir2999/a/target.txt").Pattern.Should().Be("dir2999/**");
        glob.Match("dir5/a/target.txt").Pattern.Should().Be("dir5/**");
        glob.Match("other/target.txt").Pattern.Should().Be("**/target.txt");
        glob.IsMatch("other/x.txt").Should().BeFalse();
    }

    [Fact]
    public void ManyPatterns_WithEveryTrigramInTheInput_ReportTheMatchingPattern()
    {
        // The input holds the trigrams of all 300 patterns, so many hash slots are hit before the matching one.
        string[] words = [.. Enumerable.Range(0, 300).Select(i => string.Concat((char)('a' + (i / 26 % 26)), (char)('a' + (i % 26)), "q"))];
        string[] patterns = [.. words.Select(w => $"**/*_{w}")];
        var glob = new Glob(patterns, new GlobOptions { PathStyle = GlobPathStyle.Posix });
        string all = string.Concat(words.Select(w => "_" + w));

        foreach (string word in words)
            glob.Match($"{all}_{word}").Pattern.Should().Be($"**/*_{word}");
    }

    [Fact]
    public void LongPatternEqualToTheInput_MatchesAmongManyPatterns()
    {
        // Longer than the 1024 characters whose lengths are indexed one by one; "[xy]" does not match its own text.
        string pattern = string.Concat(Enumerable.Repeat("[xy]", 300));
        var glob = new Glob([.. Enumerable.Range(0, 50).Select(i => $"dir{i}/**/*.txt"), pattern], new GlobOptions { PathStyle = GlobPathStyle.Posix, BracketMode = GlobBracketMode.CharacterClass });

        glob.Match(pattern).Pattern.Should().Be(pattern);
        glob.IsMatch(pattern.AsSpan()).Should().BeTrue();
    }

    [Fact]
    public void ManyIgnorePatterns_ExcludeInputs()
    {
        var options = new GlobOptions { PathStyle = GlobPathStyle.Posix, IgnorePatterns = [.. s_Patterns] };
        var glob = new Glob("**", options with { MatchDotFiles = true });

        glob.Match("node_modules/a/b.js").Should().Match<GlobMatch>(m => m.IsIgnored && m.Pattern == "**");
        glob.IsMatch("src/app/main.cs").Should().BeFalse("the negated ignore pattern !**/keep/** excludes it");
        glob.IsMatch("keep/a.cs").Should().BeTrue();
    }
}
