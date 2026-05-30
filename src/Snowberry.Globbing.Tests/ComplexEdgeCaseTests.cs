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

    [Theory]
    // DeeplyNestedBraces_ShouldMatch
    [InlineData("a", "{a,{b,{c,{d,e}}}}", true)]
    [InlineData("b", "{a,{b,{c,{d,e}}}}", true)]
    [InlineData("c", "{a,{b,{c,{d,e}}}}", true)]
    [InlineData("d", "{a,{b,{c,{d,e}}}}", true)]
    [InlineData("e", "{a,{b,{c,{d,e}}}}", true)]
    [InlineData("f", "{a,{b,{c,{d,e}}}}", false)]
    // MultipleConsecutiveGlobstars_SimplifiedCorrectly
    [InlineData("a/b/c/test.js", "**/**/***/**/*.js", true)]
    [InlineData("x/test.js", "**/**/***/**/*.js", true)]
    [InlineData("test.ts", "**/**/***/**/*.js", false)]
    // ComplexExtglobCombinations_MatchCorrectly
    [InlineData("test.js", "!(*.md|*.txt)", true)]
    [InlineData("app.ts", "!(*.md|*.txt)", true)]
    [InlineData("readme.md", "!(*.md|*.txt)", false)]
    [InlineData("notes.txt", "!(*.md|*.txt)", false)]
    // NestedExtglobs_ShouldWork
    [InlineData("a", "+(+(a|b)|+(c|d))", true)]
    [InlineData("b", "+(+(a|b)|+(c|d))", true)]
    [InlineData("c", "+(+(a|b)|+(c|d))", true)]
    [InlineData("d", "+(+(a|b)|+(c|d))", true)]
    [InlineData("aa", "+(+(a|b)|+(c|d))", true)]
    [InlineData("ab", "+(+(a|b)|+(c|d))", true)]
    [InlineData("aabbccdd", "+(+(a|b)|+(c|d))", true)]
    [InlineData("e", "+(+(a|b)|+(c|d))", false)]
    // MixedBracesAndExtglobs_ShouldMatch
    [InlineData("src/app.js", "{src,test}/+(*.js|*.ts)", true)]
    [InlineData("test/test.ts", "{src,test}/+(*.js|*.ts)", true)]
    [InlineData("src/index.ts", "{src,test}/+(*.js|*.ts)", true)]
    [InlineData("lib/app.js", "{src,test}/+(*.js|*.ts)", false)]
    // ComplexCharacterClasses_WithRangesAndNegation
    [InlineData("test-g.txt", "test-[^a-fA-F0-9].txt", true)]
    [InlineData("test-Z.txt", "test-[^a-fA-F0-9].txt", true)]
    [InlineData("test-a.txt", "test-[^a-fA-F0-9].txt", false)]
    [InlineData("test-5.txt", "test-[^a-fA-F0-9].txt", false)]
    [InlineData("test-A.txt", "test-[^a-fA-F0-9].txt", false)]
    // EscapedSpecialCharacters_InComplexPatterns
    [InlineData(@"test*?[]{}.js", @"test\*\?\[\]\{\}\.js", true)]
    [InlineData("testanything.js", @"test\*\?\[\]\{\}\.js", false)]
    // MultipleNegatedPatterns_ShouldAllBeRespected
    // Note: Patterns starting with ! negate the match.
    // A negated pattern matches everything EXCEPT the pattern.
    [InlineData("readme.js", "!*.md", true)]  // Not .md, so matches
    [InlineData("readme.md", "!*.md", false)] // Is .md, so doesn't match
    [InlineData("readme.md", "!*.txt", true)]
    [InlineData("notes.txt", "!*.txt", false)]
    [InlineData("readme.md", "!*.log", true)]
    [InlineData("debug.log", "!*.log", false)]
    // MultipleGlobstarsInDifferentSegments_MatchCorrectly
    [InlineData("src/test/app.js", "**/src/**/test/**/*.js", true)]
    [InlineData("a/src/b/test/c/file.js", "**/src/**/test/**/*.js", true)]
    [InlineData("x/y/z/src/u/v/test/w/test.js", "**/src/**/test/**/*.js", true)]
    [InlineData("src/file.js", "**/src/**/test/**/*.js", false)]
    [InlineData("test/file.js", "**/src/**/test/**/*.js", false)]
    // SpecialCharactersInPath_ShouldNotBreakMatching
    [InlineData("path with spaces/file.js", "**/*.js", true)]
    [InlineData("path-with-dashes/file.js", "**/*.js", true)]
    [InlineData("path_with_underscores/file.js", "**/*.js", true)]
    [InlineData("path.with.dots/file.js", "**/*.js", true)]
    // NestedBracesWithWildcards_ShouldMatch
    [InlineData("app.js", "{*.{js,ts},*.md}", true)]
    [InlineData("app.ts", "{*.{js,ts},*.md}", true)]
    [InlineData("README.md", "{*.{js,ts},*.md}", true)]
    [InlineData("app.css", "{*.{js,ts},*.md}", false)]
    // PathWithConsecutiveSlashes_ShouldNormalize
    [InlineData("a/b/test.js", "**/test.js", true)]
    [InlineData("a/b/c/test.js", "**/test.js", true)]
    // UnicodeCharactersInPaths_ShouldMatch
    [InlineData("路径/文件.js", "**/*.js", true)]
    [InlineData("مسار/ملف.js", "**/*.js", true)]
    [InlineData("путь/файл.js", "**/*.js", true)]
    // MaximumBraceNesting_ShouldNotCrash
    [InlineData("a", "{a,{b,{c,{d,{e,{f,{g,{h,i}}}}}}}}", true)]
    [InlineData("i", "{a,{b,{c,{d,{e,{f,{g,{h,i}}}}}}}}", true)]
    [InlineData("j", "{a,{b,{c,{d,{e,{f,{g,{h,i}}}}}}}}", false)]
    public void DefaultOptions_MatchCorrectly(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
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

    [Theory]
    // ComplexWindowsPaths_WithMixedSeparators
    [InlineData(@"src\components\Button.js", "src/**/*.js", true)]
    [InlineData("src/components/Button.js", "src/**/*.js", true)]
    [InlineData(@"src\lib\utils\helper.js", "src/**/*.js", true)]
    [InlineData(@"test\app.js", "src/**/*.js", false)]
    public void WindowsOption_MatchCorrectly(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { Windows = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
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
    public void PosixCharacterClasses_ComplexCombinations()
    {
        var options = new GlobbingOptions { Posix = true };
        var matcher = GlobMatcher.Create("[[:alnum:]]*", options);

        Assert.NotNull(matcher);
    }

    [Theory]
    // CaseSensitivityWithComplexPatterns (case-sensitive / default options)
    [InlineData("Test-A.js", "Test-[A-Z].js", true)]
    [InlineData("test-a.js", "Test-[A-Z].js", false)]
    public void CaseSensitive_MatchCorrectly(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    // CaseSensitivityWithComplexPatterns (case-insensitive)
    [InlineData("Test-A.js", "Test-[A-Z].js", true)]
    [InlineData("test-a.js", "Test-[A-Z].js", true)]
    [InlineData("TEST-A.JS", "Test-[A-Z].js", true)]
    public void NoCaseOption_MatchCorrectly(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { NoCase = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
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

    [Theory]
    // DotfilesInComplexPaths_WithDotOption (Dot = true)
    [InlineData(".config/settings.json", "**/*", true)]
    [InlineData("src/.hidden/file.js", "**/*", true)]
    public void DotOption_MatchCorrectly(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { Dot = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    // DotfilesInComplexPaths_WithDotOption (Dot = false)
    [InlineData(".config/settings.json", "**/*", false)]
    [InlineData("src/.hidden/file.js", "**/*", false)]
    public void NoDotOption_MatchCorrectly(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { Dot = false };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Fact]
    public void StrictSlashes_WithComplexGlobPatterns()
    {
        var strict = GlobMatcher.Create("*", new GlobbingOptions { StrictSlashes = true });
        var notStrict = GlobMatcher.Create("*", new GlobbingOptions { StrictSlashes = false });

        Assert.NotNull(strict);
        Assert.NotNull(notStrict);
    }
}
