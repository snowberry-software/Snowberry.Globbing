namespace Snowberry.Globbing.Tests;

/// <summary>
/// Tests for complex edge cases and boundary conditions
/// </summary>
public class ComplexEdgeCaseTests
{
    [Fact]
    public void VeryLongPattern_ShouldHandleCorrectly()
    {
        string pattern = string.Join("/", Enumerable.Repeat("*", 100)) + "/*.js";
        var matcher = new Glob(pattern);

        matcher.Should().NotBeNull();
        matcher.IsMatch("test.js").Should().BeFalse();
    }

    [Theory]
    // DeeplyNestedBraces_ShouldMatch
    [InlineData("a", "{a,{b,{c,{d,e}}}}", true)]
    [InlineData("e", "{a,{b,{c,{d,e}}}}", true)]
    [InlineData("f", "{a,{b,{c,{d,e}}}}", false)]
    // MultipleConsecutiveGlobstars_SimplifiedCorrectly
    [InlineData("a/b/c/test.js", "**/**/***/**/*.js", true)]
    [InlineData("x/test.js", "**/**/***/**/*.js", true)]
    [InlineData("test.ts", "**/**/***/**/*.js", false)]
    // ComplexExtglobCombinations_MatchCorrectly
    [InlineData("test.js", "!(*.md|*.txt)", true)]
    [InlineData("readme.md", "!(*.md|*.txt)", false)]
    [InlineData("notes.txt", "!(*.md|*.txt)", false)]
    // NestedExtglobs_ShouldWork
    [InlineData("a", "+(+(a|b)|+(c|d))", true)]
    [InlineData("c", "+(+(a|b)|+(c|d))", true)]
    [InlineData("aabbccdd", "+(+(a|b)|+(c|d))", true)]
    [InlineData("e", "+(+(a|b)|+(c|d))", false)]
    // MixedBracesAndExtglobs_ShouldMatch
    [InlineData("src/app.js", "{src,test}/+(*.js|*.ts)", true)]
    [InlineData("test/test.ts", "{src,test}/+(*.js|*.ts)", true)]
    [InlineData("src/index.ts", "{src,test}/+(*.js|*.ts)", true)]
    [InlineData("lib/app.js", "{src,test}/+(*.js|*.ts)", false)]
    // ComplexCharacterClasses_WithRangesAndNegation
    [InlineData("test-g.txt", "test-[^a-fA-F0-9].txt", true)]
    [InlineData("test-a.txt", "test-[^a-fA-F0-9].txt", false)]
    [InlineData("test-5.txt", "test-[^a-fA-F0-9].txt", false)]
    [InlineData("test-A.txt", "test-[^a-fA-F0-9].txt", false)]
    // EscapedSpecialCharacters_InComplexPatterns
    [InlineData(@"test*?[]{}.js", @"test\*\?\[\]\{\}\.js", true)]
    [InlineData("testanything.js", @"test\*\?\[\]\{\}\.js", false)]
    // MultipleNegatedPatterns_ShouldAllBeRespected
    [InlineData("readme.js", "!*.md", true)]  // Not .md, so matches
    [InlineData("readme.md", "!*.md", false)] // Is .md, so doesn't match
    // MultipleGlobstarsInDifferentSegments_MatchCorrectly
    [InlineData("src/test/app.js", "**/src/**/test/**/*.js", true)]
    [InlineData("a/src/b/test/c/file.js", "**/src/**/test/**/*.js", true)]
    [InlineData("src/file.js", "**/src/**/test/**/*.js", false)]
    // SpecialCharactersInPath_ShouldNotBreakMatching
    [InlineData("path with spaces/file.js", "**/*.js", true)]
    [InlineData("path.with.dots/file.js", "**/*.js", true)]
    // NestedBracesWithWildcards_ShouldMatch
    [InlineData("app.js", "{*.{js,ts},*.md}", true)]
    [InlineData("README.md", "{*.{js,ts},*.md}", true)]
    [InlineData("app.css", "{*.{js,ts},*.md}", false)]
    // UnicodeCharactersInPaths_ShouldMatch
    [InlineData("路径/文件.js", "**/*.js", true)]
    [InlineData("مسار/ملف.js", "**/*.js", true)]
    // /!(*.d).{ts,tsx}
    [InlineData("/file.d.ts", "/!(*.d).{ts,tsx}", false)]
    [InlineData("/file.ts", "/!(*.d).{ts,tsx}", true)]
    [InlineData("/file.d.ts", "/!(*.d).@(ts)", false)]
    [InlineData("/file.ts", "/!(*.d).@(ts)", true)]
    // MaximumBraceNesting_ShouldNotCrash
    [InlineData("a", "{a,{b,{c,{d,{e,{f,{g,{h,i}}}}}}}}", true)]
    [InlineData("i", "{a,{b,{c,{d,{e,{f,{g,{h,i}}}}}}}}", true)]
    [InlineData("j", "{a,{b,{c,{d,{e,{f,{g,{h,i}}}}}}}}", false)]
    public void DefaultOptions_MatchCorrectly(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("*", "", false)]
    [InlineData("**", "", false)]
    [InlineData("?", "", false)]
    [InlineData("!a", "", false)]
    [InlineData("*", "a", true)]
    public void EmptyInput_NeverMatches(string pattern, string input, bool expected)
    {
        new Glob(pattern).IsMatch(input).Should().Be(expected);
    }

    [Theory]
    // ComplexWindowsPaths_WithMixedSeparators
    [InlineData(@"src\components\Button.js", "src/**/*.js", true)]
    [InlineData("src/components/Button.js", "src/**/*.js", true)]
    [InlineData(@"test\app.js", "src/**/*.js", false)]
    public void WindowsOption_MatchCorrectly(string input, string pattern, bool expected)
    {
        var options = new GlobOptions { PathStyle = GlobPathStyle.Windows };
        Glob.IsMatch(input, pattern, options).Should().Be(expected);
    }

    [Theory]
    // CaseSensitivityWithComplexPatterns (case-sensitive / default options)
    [InlineData("Test-A.js", "Test-[A-Z].js", true)]
    [InlineData("test-a.js", "Test-[A-Z].js", false)]
    public void CaseSensitive_MatchCorrectly(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    // CaseSensitivityWithComplexPatterns (case-insensitive)
    [InlineData("test-a.js", "Test-[A-Z].js", true)]
    [InlineData("TEST-A.JS", "Test-[A-Z].js", true)]
    public void NoCaseOption_MatchCorrectly(string input, string pattern, bool expected)
    {
        var options = new GlobOptions { IgnoreCase = true };
        Glob.IsMatch(input, pattern, options).Should().Be(expected);
    }

    [Fact]
    public void IgnorePatternsWithMatchResults_ComplexScenario()
    {
        var options = new GlobOptions
        {
            IgnorePatterns = ["*.test.js", "*.spec.js"]
        };

        var matcher = new Glob("*.js", options);

        var results = new[] { "app.js", "app.test.js", "app.spec.js", "lib.js", "app.md" }.Select(matcher.Match).ToList();

        results.Count(r => r.Success).Should().Be(2);
        results.Count(r => r.IsIgnored).Should().Be(2);
        results.Where(r => r.IsIgnored).Should().AllSatisfy(r => r.Success.Should().BeFalse());
        results[4].IsIgnored.Should().BeFalse();
    }

    [Theory]
    [InlineData(".config/settings.json", "**/*", true, true)]
    [InlineData("src/.hidden/file.js", "**/*", true, true)]
    [InlineData(".config/settings.json", "**/*", false, false)]
    [InlineData("src/.hidden/file.js", "**/*", false, false)]
    public void DotOption_MatchCorrectly(string input, string pattern, bool matchDotFiles, bool expected)
    {
        var options = new GlobOptions { MatchDotFiles = matchDotFiles };
        Glob.IsMatch(input, pattern, options).Should().Be(expected);
    }
}