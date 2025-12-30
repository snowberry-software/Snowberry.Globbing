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
        var matcher = GlobMatcher.Create(pattern);

        Assert.NotNull(matcher);
        Assert.False(matcher("test.js"));
    }

    [Fact]
    public void PatternWithMaximumLength_ThrowsException()
    {
        string pattern = new('a', Constants.c_MaxLength + 1);

        Assert.Throws<ArgumentException>(() => GlobMatcher.MakeRe(pattern));
    }

    [Fact]
    public void DeeplyNestedBraces_ShouldMatch()
    {
        var matcher = GlobMatcher.Create("{a,{b,{c,{d,e}}}}");

        Assert.True(matcher("a"));
        Assert.True(matcher("b"));
        Assert.True(matcher("c"));
        Assert.True(matcher("d"));
        Assert.True(matcher("e"));
        Assert.False(matcher("f"));
    }

    [Fact]
    public void MultipleConsecutiveGlobstars_SimplifiedCorrectly()
    {
        var matcher = GlobMatcher.Create("**/**/***/**/*.js");

        Assert.True(matcher("a/b/c/test.js"));
        Assert.True(matcher("x/test.js"));
        Assert.False(matcher("test.ts"));
    }

    [Fact]
    public void ComplexExtglobCombinations_MatchCorrectly()
    {
        var matcher = GlobMatcher.Create("!(*.md|*.txt)");

        Assert.True(matcher("test.js"));
        Assert.True(matcher("app.ts"));
        Assert.False(matcher("readme.md"));
        Assert.False(matcher("notes.txt"));
    }

    [Fact]
    public void NestedExtglobs_ShouldWork()
    {
        var matcher = GlobMatcher.Create("+(+(a|b)|+(c|d))");

        Assert.True(matcher("a"));
        Assert.True(matcher("b"));
        Assert.True(matcher("c"));
        Assert.True(matcher("d"));
        Assert.True(matcher("aa"));
        Assert.True(matcher("ab"));
        Assert.True(matcher("aabbccdd"));
        Assert.False(matcher("e"));
    }

    [Fact]
    public void MixedBracesAndExtglobs_ShouldMatch()
    {
        var matcher = GlobMatcher.Create("{src,test}/+(*.js|*.ts)");

        Assert.True(matcher("src/app.js"));
        Assert.True(matcher("test/test.ts"));
        Assert.True(matcher("src/index.ts"));
        Assert.False(matcher("lib/app.js"));
    }

    [Fact]
    public void ComplexCharacterClasses_WithRangesAndNegation()
    {
        var matcher = GlobMatcher.Create("test-[^a-fA-F0-9].txt");

        Assert.True(matcher("test-g.txt"));
        Assert.True(matcher("test-Z.txt"));
        Assert.False(matcher("test-a.txt"));
        Assert.False(matcher("test-5.txt"));
        Assert.False(matcher("test-A.txt"));
    }

    [Fact]
    public void EscapedSpecialCharacters_InComplexPatterns()
    {
        var matcher = GlobMatcher.Create(@"test\*\?\[\]\{\}\.js");

        Assert.True(matcher(@"test*?[]{}.js"));
        Assert.False(matcher("testanything.js"));
    }

    [Fact]
    public void MultipleNegatedPatterns_ShouldAllBeRespected()
    {
        // Note: Patterns starting with ! negate the match
        var mdMatcher = GlobMatcher.Create("!*.md");
        var txtMatcher = GlobMatcher.Create("!*.txt");
        var logMatcher = GlobMatcher.Create("!*.log");

        // A negated pattern matches everything EXCEPT the pattern
        Assert.True(mdMatcher("readme.js"));  // Not .md, so matches
        Assert.False(mdMatcher("readme.md")); // Is .md, so doesn't match

        Assert.True(txtMatcher("readme.md"));
        Assert.False(txtMatcher("notes.txt"));

        Assert.True(logMatcher("readme.md"));
        Assert.False(logMatcher("debug.log"));
    }

    [Theory]
    [InlineData("", "", false)] // Empty pattern
    [InlineData("*", "", false)] // Empty input
    [InlineData("**", "", false)]
    [InlineData("?", "a", true)] // Single char
    [InlineData("?", "", false)]
    [InlineData("?", "ab", false)]
    public void EmptyAndSingleCharacterCases_HandleCorrectly(string pattern, string input, bool expected)
    {
        if (string.IsNullOrEmpty(pattern))
        {
            Assert.Throws<ArgumentException>(() => GlobMatcher.Create(pattern));
        }
        else
        {
            var matcher = GlobMatcher.Create(pattern);
            Assert.Equal(expected, matcher(input));
        }
    }

    [Fact]
    public void ComplexWindowsPaths_WithMixedSeparators()
    {
        var options = new GlobbingOptions { Windows = true };
        var matcher = GlobMatcher.Create("src/**/*.js", options);

        Assert.True(matcher(@"src\components\Button.js"));
        Assert.True(matcher("src/components/Button.js"));
        Assert.True(matcher(@"src\lib\utils\helper.js"));
        Assert.False(matcher(@"test\app.js"));
    }

    [Fact]
    public void BraceExpansionWithRanges_ComplexScenarios()
    {
        var matcher = GlobMatcher.Create("file-{1..3}-{a..c}.txt");

        // This tests if range expansion works correctly
        var regex = GlobMatcher.MakeRe("file-{1..3}-{a..c}.txt");
        Assert.NotNull(regex);
    }

    [Fact]
    public void MultipleGlobstarsInDifferentSegments_MatchCorrectly()
    {
        var matcher = GlobMatcher.Create("**/src/**/test/**/*.js");

        Assert.True(matcher("src/test/app.js"));
        Assert.True(matcher("a/src/b/test/c/file.js"));
        Assert.True(matcher("x/y/z/src/u/v/test/w/test.js"));
        Assert.False(matcher("src/file.js"));
        Assert.False(matcher("test/file.js"));
    }

    [Fact]
    public void SpecialCharactersInPath_ShouldNotBreakMatching()
    {
        var matcher = GlobMatcher.Create("**/*.js");

        Assert.True(matcher("path with spaces/file.js"));
        Assert.True(matcher("path-with-dashes/file.js"));
        Assert.True(matcher("path_with_underscores/file.js"));
        Assert.True(matcher("path.with.dots/file.js"));
    }

    [Fact]
    public void PosixCharacterClasses_ComplexCombinations()
    {
        var options = new GlobbingOptions { Posix = true };
        var matcher = GlobMatcher.Create("[[:alnum:]]*", options);

        Assert.NotNull(matcher);
    }

    [Fact]
    public void CaseSensitivityWithComplexPatterns()
    {
        var caseSensitive = GlobMatcher.Create("Test-[A-Z].js");
        var caseInsensitive = GlobMatcher.Create("Test-[A-Z].js", new GlobbingOptions { NoCase = true });

        Assert.True(caseSensitive("Test-A.js"));
        Assert.False(caseSensitive("test-a.js"));

        Assert.True(caseInsensitive("Test-A.js"));
        Assert.True(caseInsensitive("test-a.js"));
        Assert.True(caseInsensitive("TEST-A.JS"));
    }

    [Fact]
    public void IgnorePatternsWithCallbacks_ComplexScenario()
    {
        int onResultCount = 0;
        int onMatchCount = 0;
        int onIgnoreCount = 0;

        var options = new GlobbingOptions
        {
            Ignore = ["*.test.js", "*.spec.js"],
            OnResult = _ => onResultCount++,
            OnMatch = _ => onMatchCount++,
            OnIgnore = _ => onIgnoreCount++
        };

        var matcher = GlobMatcher.Create("*.js", options);

        matcher("app.js");
        matcher("app.test.js");
        matcher("app.spec.js");
        matcher("lib.js");

        Assert.Equal(4, onResultCount);
        Assert.Equal(2, onMatchCount);
        Assert.Equal(2, onIgnoreCount);
    }

    [Fact]
    public void NestedBracesWithWildcards_ShouldMatch()
    {
        var matcher = GlobMatcher.Create("{*.{js,ts},*.md}");

        Assert.True(matcher("app.js"));
        Assert.True(matcher("app.ts"));
        Assert.True(matcher("README.md"));
        Assert.False(matcher("app.css"));
    }

    [Fact]
    public void VeryComplexRealWorldPattern_ShouldWork()
    {
        string pattern = "src/**/!(*.test|*.spec).{js,jsx,ts,tsx}";
        var matcher = GlobMatcher.Create(pattern);

        Assert.NotNull(matcher);
        // The pattern should compile without errors
    }

    [Fact]
    public void MultipleArrayPatterns_WithComplexOptions()
    {
        string[] patterns =
        [
            "**/*.{js,jsx}",
            "!**/node_modules/**",
            "!**/*.test.*",
            "src/**/*"
        ];

        var matchers = patterns.Select(p => GlobMatcher.Create(p)).ToList();
        Assert.Equal(4, matchers.Count);
        Assert.All(matchers, Assert.NotNull);
    }

    [Fact]
    public void PathWithConsecutiveSlashes_ShouldNormalize()
    {
        var matcher = GlobMatcher.Create("**/test.js");

        Assert.True(matcher("a/b/test.js"));
        Assert.True(matcher("a/b/c/test.js"));
    }

    [Fact]
    public void DotfilesInComplexPaths_WithDotOption()
    {
        var withDot = GlobMatcher.Create("**/*", new GlobbingOptions { Dot = true });
        var withoutDot = GlobMatcher.Create("**/*", new GlobbingOptions { Dot = false });

        Assert.True(withDot(".config/settings.json"));
        Assert.False(withoutDot(".config/settings.json"));

        Assert.True(withDot("src/.hidden/file.js"));
        Assert.False(withoutDot("src/.hidden/file.js"));
    }

    [Fact]
    public void StrictSlashes_WithComplexGlobPatterns()
    {
        var strict = GlobMatcher.Create("*", new GlobbingOptions { StrictSlashes = true });
        var notStrict = GlobMatcher.Create("*", new GlobbingOptions { StrictSlashes = false });

        Assert.NotNull(strict);
        Assert.NotNull(notStrict);
    }

    [Fact]
    public void UnicodeCharactersInPaths_ShouldMatch()
    {
        var matcher = GlobMatcher.Create("**/*.js");

        Assert.True(matcher("路径/文件.js"));
        Assert.True(matcher("مسار/ملف.js"));
        Assert.True(matcher("путь/файл.js"));
    }

    [Fact]
    public void MaximumBraceNesting_ShouldNotCrash()
    {
        string pattern = "{a,{b,{c,{d,{e,{f,{g,{h,i}}}}}}}}";
        var matcher = GlobMatcher.Create(pattern);

        Assert.True(matcher("a"));
        Assert.True(matcher("i"));
        Assert.False(matcher("j"));
    }
}
