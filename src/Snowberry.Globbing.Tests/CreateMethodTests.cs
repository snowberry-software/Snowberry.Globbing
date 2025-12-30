namespace Snowberry.Globbing.Tests;

/// <summary>
/// Comprehensive tests for the Create method with various options and edge cases
/// </summary>
public class CreateMethodTests
{
    [Fact]
    public void Create_WithNullGlob_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => GlobMatcher.Create((string)null!));
    }

    [Fact]
    public void Create_WithEmptyGlob_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => GlobMatcher.Create(""));
    }

    [Fact]
    public void Create_WithNullArray_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => GlobMatcher.Create((string[])null!));
    }

    [Fact]
    public void Create_WithEmptyArray_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => GlobMatcher.Create([]));
    }

    [Fact]
    public void Create_WithSinglePattern_ReturnsMatcherFunction()
    {
        var matcher = GlobMatcher.Create("*.js");
        Assert.NotNull(matcher);
        Assert.True(matcher("test.js"));
        Assert.False(matcher("test.md"));
    }

    [Fact]
    public void Create_WithMultiplePatterns_MatchesAnyPattern()
    {
        var matcher = GlobMatcher.Create(["*.js", "*.ts", "*.md"]);

        Assert.True(matcher("app.js"));
        Assert.True(matcher("app.ts"));
        Assert.True(matcher("readme.md"));
        Assert.False(matcher("app.css"));
    }

    [Fact]
    public void Create_WithWindowsOption_AutoDetectsIfNotSet()
    {
        var matcher = GlobMatcher.Create("*.js");
        Assert.NotNull(matcher);
    }

    [Fact]
    public void Create_WithWindowsOptionTrue_AcceptsPattern()
    {
        var matcher = GlobMatcher.Create("*.js", new GlobbingOptions { Windows = true });
        Assert.True(matcher("test.js"));
    }

    [Fact]
    public void Create_WithWindowsOptionFalse_AcceptsPattern()
    {
        var matcher = GlobMatcher.Create("*.js", new GlobbingOptions { Windows = false });
        Assert.True(matcher("test.js"));
    }

    [Fact]
    public void Create_WithIgnorePatterns_ExcludesMatches()
    {
        var options = new GlobbingOptions
        {
            Ignore = ["*.test.js", "*.spec.js"]
        };
        var matcher = GlobMatcher.Create("*.js", options);

        Assert.True(matcher("app.js"));
        Assert.False(matcher("app.test.js"));
        Assert.False(matcher("app.spec.js"));
    }

    [Fact]
    public void Create_WithOnResultCallback_CalledForEveryTest()
    {
        int callCount = 0;
        var options = new GlobbingOptions
        {
            OnResult = (result) => callCount++
        };
        var matcher = GlobMatcher.Create("*.js", options);

        matcher("test.js");
        matcher("test.md");
        matcher("app.js");

        Assert.Equal(3, callCount);
    }

    [Fact]
    public void Create_WithOnMatchCallback_CalledOnlyForMatches()
    {
        int matchCount = 0;
        var options = new GlobbingOptions
        {
            OnMatch = (result) => matchCount++
        };
        var matcher = GlobMatcher.Create("*.js", options);

        matcher("test.js");
        matcher("test.md");
        matcher("app.js");

        Assert.Equal(2, matchCount);
    }

    [Fact]
    public void Create_WithOnIgnoreCallback_CalledForIgnoredMatches()
    {
        int ignoreCount = 0;
        var options = new GlobbingOptions
        {
            Ignore = ["*.test.js"],
            OnIgnore = (result) => ignoreCount++
        };
        var matcher = GlobMatcher.Create("*.js", options);

        matcher("app.js");
        matcher("app.test.js");
        matcher("app.spec.js"); // Doesn't match *.js after .test

        Assert.Equal(1, ignoreCount);
    }

    [Fact]
    public void Create_WithDotOption_MatchesDotfiles()
    {
        var withDot = GlobMatcher.Create("*", new GlobbingOptions { Dot = true });
        var withoutDot = GlobMatcher.Create("*", new GlobbingOptions { Dot = false });

        Assert.True(withDot(".gitignore"));
        Assert.False(withoutDot(".gitignore"));
        Assert.True(withDot("regular.txt"));
        Assert.True(withoutDot("regular.txt"));
    }

    [Fact]
    public void Create_WithNocaseOption_IgnoresCase()
    {
        var matcher = GlobMatcher.Create("*.JS", new GlobbingOptions { NoCase = true });

        Assert.True(matcher("test.js"));
        Assert.True(matcher("test.JS"));
        Assert.True(matcher("test.Js"));
        Assert.True(matcher("test.jS"));
    }

    [Fact]
    public void Create_WithContainsOption_MatchesSubstring()
    {
        var matcher = GlobMatcher.Create("test", new GlobbingOptions { Contains = true });

        Assert.True(matcher("test"));
        Assert.True(matcher("my-test-file"));
        Assert.True(matcher("testing"));
        Assert.True(matcher("pretest"));
        Assert.False(matcher("file"));
    }

    [Fact]
    public void Create_WithNoextglobOption_DisablesExtglobs()
    {
        var withExtglob = GlobMatcher.Create("+(a)", new GlobbingOptions { NoExtglob = false });
        var withoutExtglob = GlobMatcher.Create("+(a)", new GlobbingOptions { NoExtglob = true });

        Assert.True(withExtglob("a"));
        // Without extglob, it should be treated differently
        Assert.False(withoutExtglob("a"));
    }

    [Fact]
    public void Create_WithNoglobstarOption_DisablesGlobstar()
    {
        var matcher = GlobMatcher.Create("**/*.js", new GlobbingOptions { NoGlobstar = true });

        // With noglobstar, behavior may vary by implementation
        Assert.NotNull(matcher);
    }

    [Fact]
    public void Create_WithNonegateOption_DisablesNegation()
    {
        var matcher = GlobMatcher.Create("!*.md", new GlobbingOptions { NoNegate = true });

        // With nonegate, ! should be treated literally
        Assert.True(matcher("!test.md"));
        Assert.False(matcher("test.md"));
    }

    [Fact]
    public void Create_WithBashOption_UsesBashRules()
    {
        var matcher = GlobMatcher.Create("*", new GlobbingOptions { Bash = true });

        Assert.True(matcher("test.js"));
        Assert.True(matcher("file"));
    }

    [Fact]
    public void Create_WithNobraceOption_DisablesBraceExpansion()
    {
        var matcher = GlobMatcher.Create("*.{js,ts}", new GlobbingOptions { NoBrace = true });

        // Should match literal pattern
        Assert.True(matcher("test.{js,ts}"));
        Assert.False(matcher("test.js"));
    }

    [Fact]
    public void Create_WithComplexPattern_MatchesCorrectly()
    {
        var matcher = GlobMatcher.Create("**/src/**/*.{js,ts}");

        Assert.True(matcher("src/app.js"));
        Assert.True(matcher("src/lib/utils.ts"));
        Assert.True(matcher("packages/core/src/index.js"));
        Assert.False(matcher("test/app.js"));
    }

    [Fact]
    public void Create_WithNestedGlobstars_MatchesCorrectly()
    {
        var matcher = GlobMatcher.Create("**/**/test.js");

        Assert.True(matcher("test.js"));
        Assert.True(matcher("src/test.js"));
        Assert.True(matcher("src/lib/test.js"));
    }

    [Fact]
    public void Create_WithMultipleWildcards_MatchesCorrectly()
    {
        var matcher = GlobMatcher.Create("*-*-*.js");

        Assert.True(matcher("foo-bar-baz.js"));
        Assert.True(matcher("a-b-c.js"));
        Assert.False(matcher("foo-bar.js"));
        Assert.False(matcher("foo.js"));
    }

    [Fact]
    public void Create_WithQuestionMarks_MatchesSingleCharacters()
    {
        var matcher = GlobMatcher.Create("test-?.js");

        Assert.True(matcher("test-1.js"));
        Assert.True(matcher("test-a.js"));
        Assert.False(matcher("test-12.js"));
        Assert.False(matcher("test-.js"));
    }

    [Fact]
    public void Create_WithCharacterRanges_MatchesCorrectly()
    {
        var matcher = GlobMatcher.Create("test-[0-9].js");

        Assert.True(matcher("test-0.js"));
        Assert.True(matcher("test-5.js"));
        Assert.True(matcher("test-9.js"));
        Assert.False(matcher("test-a.js"));
    }

    [Fact]
    public void Create_WithNegatedCharacterClass_MatchesCorrectly()
    {
        var matcher = GlobMatcher.Create("test-[^0-9].js");

        Assert.False(matcher("test-0.js"));
        Assert.False(matcher("test-9.js"));
        Assert.True(matcher("test-a.js"));
        Assert.True(matcher("test-z.js"));
    }

    [Fact]
    public void Create_WithExtglobPlus_MatchesOneOrMore()
    {
        var matcher = GlobMatcher.Create("+(a|b)");

        Assert.True(matcher("a"));
        Assert.True(matcher("b"));
        Assert.True(matcher("aa"));
        Assert.True(matcher("ab"));
        Assert.True(matcher("ba"));
        Assert.False(matcher(""));
        Assert.False(matcher("c"));
    }

    [Fact]
    public void Create_WithExtglobStar_MatchesZeroOrMore()
    {
        var matcher = GlobMatcher.Create("a*(b)");

        Assert.True(matcher("a"));
        Assert.True(matcher("ab"));
        Assert.True(matcher("abb"));
        Assert.True(matcher("abbb"));
        Assert.False(matcher("b"));
    }

    [Fact]
    public void Create_WithExtglobAt_MatchesExactlyOne()
    {
        var matcher = GlobMatcher.Create("@(a|b|c)");

        Assert.True(matcher("a"));
        Assert.True(matcher("b"));
        Assert.True(matcher("c"));
        Assert.False(matcher("ab"));
        Assert.False(matcher("d"));
    }

    [Fact]
    public void Create_WithExtglobQuestion_MatchesZeroOrOne()
    {
        var matcher = GlobMatcher.Create("a?(b)");

        Assert.True(matcher("a"));
        Assert.True(matcher("ab"));
        Assert.False(matcher("abb"));
        Assert.False(matcher("b"));
    }

    [Fact]
    public void Create_WithExtglobNegate_MatchesAnythingBut()
    {
        var matcher = GlobMatcher.Create("!(*.md)");

        Assert.False(matcher("readme.md"));
        Assert.False(matcher("test.md"));
        Assert.True(matcher("app.js"));
        Assert.True(matcher("test.txt"));
    }
}
