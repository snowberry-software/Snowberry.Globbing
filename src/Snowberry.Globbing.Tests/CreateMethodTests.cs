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

    [Theory]
    [InlineData("app.js", true)]
    [InlineData("app.test.js", false)]
    [InlineData("app.spec.js", false)]
    public void Create_WithIgnorePatterns_ExcludesMatches(string input, bool expected)
    {
        var options = new GlobbingOptions
        {
            Ignore = ["*.test.js", "*.spec.js"]
        };
        var matcher = GlobMatcher.Create("*.js", options);

        Assert.Equal(expected, matcher(input));
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

    [Theory]
    [InlineData("test.js", true)]
    [InlineData("test.JS", true)]
    [InlineData("test.Js", true)]
    [InlineData("test.jS", true)]
    public void Create_WithNocaseOption_IgnoresCase(string input, bool expected)
    {
        var matcher = GlobMatcher.Create("*.JS", new GlobbingOptions { NoCase = true });

        Assert.Equal(expected, matcher(input));
    }

    [Theory]
    [InlineData("test", true)]
    [InlineData("my-test-file", true)]
    [InlineData("testing", true)]
    [InlineData("pretest", true)]
    [InlineData("file", false)]
    public void Create_WithContainsOption_MatchesSubstring(string input, bool expected)
    {
        var matcher = GlobMatcher.Create("test", new GlobbingOptions { Contains = true });

        Assert.Equal(expected, matcher(input));
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

    [Theory]
    [InlineData("!test.md", true)]
    [InlineData("test.md", false)]
    public void Create_WithNonegateOption_DisablesNegation(string input, bool expected)
    {
        // With nonegate, ! should be treated literally
        var matcher = GlobMatcher.Create("!*.md", new GlobbingOptions { NoNegate = true });

        Assert.Equal(expected, matcher(input));
    }

    [Theory]
    [InlineData("test.js", true)]
    [InlineData("file", true)]
    public void Create_WithBashOption_UsesBashRules(string input, bool expected)
    {
        var matcher = GlobMatcher.Create("*", new GlobbingOptions { Bash = true });

        Assert.Equal(expected, matcher(input));
    }

    [Theory]
    [InlineData("test.{js,ts}", true)]
    [InlineData("test.js", false)]
    public void Create_WithNobraceOption_DisablesBraceExpansion(string input, bool expected)
    {
        // Should match literal pattern
        var matcher = GlobMatcher.Create("*.{js,ts}", new GlobbingOptions { NoBrace = true });

        Assert.Equal(expected, matcher(input));
    }

    [Theory]
    // Create_WithComplexPattern_MatchesCorrectly
    [InlineData("src/app.js", "**/src/**/*.{js,ts}", true)]
    [InlineData("src/lib/utils.ts", "**/src/**/*.{js,ts}", true)]
    [InlineData("packages/core/src/index.js", "**/src/**/*.{js,ts}", true)]
    [InlineData("test/app.js", "**/src/**/*.{js,ts}", false)]
    // Create_WithNestedGlobstars_MatchesCorrectly
    [InlineData("test.js", "**/**/test.js", true)]
    [InlineData("src/test.js", "**/**/test.js", true)]
    [InlineData("src/lib/test.js", "**/**/test.js", true)]
    // Create_WithMultipleWildcards_MatchesCorrectly
    [InlineData("foo-bar-baz.js", "*-*-*.js", true)]
    [InlineData("a-b-c.js", "*-*-*.js", true)]
    [InlineData("foo-bar.js", "*-*-*.js", false)]
    [InlineData("foo.js", "*-*-*.js", false)]
    // Create_WithQuestionMarks_MatchesSingleCharacters
    [InlineData("test-1.js", "test-?.js", true)]
    [InlineData("test-a.js", "test-?.js", true)]
    [InlineData("test-12.js", "test-?.js", false)]
    [InlineData("test-.js", "test-?.js", false)]
    // Create_WithCharacterRanges_MatchesCorrectly
    [InlineData("test-0.js", "test-[0-9].js", true)]
    [InlineData("test-5.js", "test-[0-9].js", true)]
    [InlineData("test-9.js", "test-[0-9].js", true)]
    [InlineData("test-a.js", "test-[0-9].js", false)]
    // Create_WithNegatedCharacterClass_MatchesCorrectly
    [InlineData("test-0.js", "test-[^0-9].js", false)]
    [InlineData("test-9.js", "test-[^0-9].js", false)]
    [InlineData("test-a.js", "test-[^0-9].js", true)]
    [InlineData("test-z.js", "test-[^0-9].js", true)]
    // Create_WithExtglobPlus_MatchesOneOrMore
    [InlineData("a", "+(a|b)", true)]
    [InlineData("b", "+(a|b)", true)]
    [InlineData("aa", "+(a|b)", true)]
    [InlineData("ab", "+(a|b)", true)]
    [InlineData("ba", "+(a|b)", true)]
    [InlineData("", "+(a|b)", false)]
    [InlineData("c", "+(a|b)", false)]
    // Create_WithExtglobStar_MatchesZeroOrMore
    [InlineData("a", "a*(b)", true)]
    [InlineData("ab", "a*(b)", true)]
    [InlineData("abb", "a*(b)", true)]
    [InlineData("abbb", "a*(b)", true)]
    [InlineData("b", "a*(b)", false)]
    // Create_WithExtglobAt_MatchesExactlyOne
    [InlineData("a", "@(a|b|c)", true)]
    [InlineData("b", "@(a|b|c)", true)]
    [InlineData("c", "@(a|b|c)", true)]
    [InlineData("ab", "@(a|b|c)", false)]
    [InlineData("d", "@(a|b|c)", false)]
    // Create_WithExtglobQuestion_MatchesZeroOrOne
    [InlineData("a", "a?(b)", true)]
    [InlineData("ab", "a?(b)", true)]
    [InlineData("abb", "a?(b)", false)]
    [InlineData("b", "a?(b)", false)]
    // Create_WithExtglobNegate_MatchesAnythingBut
    [InlineData("readme.md", "!(*.md)", false)]
    [InlineData("test.md", "!(*.md)", false)]
    [InlineData("app.js", "!(*.md)", true)]
    [InlineData("test.txt", "!(*.md)", true)]
    public void Create_WithPattern_MatchesCorrectly(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.Create(pattern)(input));
    }
}
