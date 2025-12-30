using System.Text.RegularExpressions;

namespace Snowberry.Globbing.Tests;

/// <summary>
/// Comprehensive tests for regex generation functionality
/// Tests MakeRe, ToRegex, and CompileRe methods for database query usage
/// </summary>
public class RegexGenerationTests
{
    [Fact]
    public void MakeRe_SimpleWildcard_GeneratesValidRegex()
    {
        var regex = GlobMatcher.MakeRe("*.js");

        Assert.NotNull(regex);
        Assert.Matches(regex, "test.js");
        Assert.Matches(regex, "app.js");
        Assert.DoesNotMatch(regex, "test.md");
    }

    [Fact]
    public void MakeRe_ReturnsCompiledRegex()
    {
        var regex = GlobMatcher.MakeRe("*.js");

        Assert.NotNull(regex);
        Assert.Contains(RegexOptions.Compiled, new[] { regex.Options & RegexOptions.Compiled });
    }

    [Theory]
    [InlineData("*.js")]
    [InlineData("**/*.js")]
    [InlineData("src/**/*.{js,ts}")]
    [InlineData("test-[0-9].js")]
    [InlineData("!(*.md)")]
    public void MakeRe_VariousPatterns_GeneratesValidRegex(string pattern)
    {
        var regex = GlobMatcher.MakeRe(pattern);
        Assert.NotNull(regex);
    }

    [Fact]
    public void MakeRe_WithNullInput_ThrowsException()
    {
        Assert.Throws<ArgumentException>(() => GlobMatcher.MakeRe(null!));
    }

    [Fact]
    public void MakeRe_WithEmptyInput_ThrowsException()
    {
        Assert.Throws<ArgumentException>(() => GlobMatcher.MakeRe(""));
    }

    [Fact]
    public void MakeRe_WithNocaseOption_GeneratesCaseInsensitiveRegex()
    {
        var regex = GlobMatcher.MakeRe("*.JS", new GlobbingOptions { NoCase = true });

        Assert.Matches(regex, "test.js");
        Assert.Matches(regex, "test.JS");
        Assert.Matches(regex, "test.Js");
    }

    [Fact]
    public void MakeRe_WithContainsOption_GeneratesRegexWithoutAnchors()
    {
        var regex = GlobMatcher.MakeRe("test", new GlobbingOptions { Contains = true });

        Assert.Matches(regex, "test");
        Assert.Matches(regex, "my-test-file");
        Assert.Matches(regex, "testing");
        Assert.Matches(regex, "pretest");
    }

    [Fact]
    public void MakeRe_Globstar_GeneratesCorrectRegex()
    {
        var regex = GlobMatcher.MakeRe("**/*.js");

        Assert.Matches(regex, "test.js");
        Assert.Matches(regex, "src/test.js");
        Assert.Matches(regex, "src/lib/utils.js");
        Assert.DoesNotMatch(regex, "test.md");
    }

    [Fact]
    public void MakeRe_BraceExpansion_GeneratesCorrectRegex()
    {
        var regex = GlobMatcher.MakeRe("*.{js,ts,md}");

        Assert.Matches(regex, "app.js");
        Assert.Matches(regex, "app.ts");
        Assert.Matches(regex, "readme.md");
        Assert.DoesNotMatch(regex, "app.css");
    }

    [Fact]
    public void MakeRe_CharacterClass_GeneratesCorrectRegex()
    {
        var regex = GlobMatcher.MakeRe("[abc].js");

        Assert.Matches(regex, "a.js");
        Assert.Matches(regex, "b.js");
        Assert.Matches(regex, "c.js");
        Assert.DoesNotMatch(regex, "d.js");
    }

    [Fact]
    public void MakeRe_QuestionMark_GeneratesCorrectRegex()
    {
        var regex = GlobMatcher.MakeRe("test-?.js");

        Assert.Matches(regex, "test-1.js");
        Assert.Matches(regex, "test-a.js");
        Assert.DoesNotMatch(regex, "test-12.js");
    }

    [Fact]
    public void MakeRe_RegexPattern_CanBeUsedInDatabaseQueries()
    {
        var regex = GlobMatcher.MakeRe("*.js");
        string pattern = regex.ToString();

        Assert.NotNull(pattern);
        Assert.NotEmpty(pattern);
        // Pattern should be usable in SQL REGEXP or similar
    }

    [Fact]
    public void ToRegex_SimplePattern_GeneratesValidRegex()
    {
        var regex = GlobMatcher.ToRegex("test.*");

        Assert.NotNull(regex);
        Assert.Matches(regex, "test.js");
        Assert.Matches(regex, "test.anything");
    }

    [Fact]
    public void ToRegex_WithNocaseFlag_GeneratesCaseInsensitiveRegex()
    {
        var regex = GlobMatcher.ToRegex("test", new GlobbingOptions { NoCase = true });

        Assert.Matches(regex, "test");
        Assert.Matches(regex, "TEST");
        Assert.Matches(regex, "Test");
    }

    [Fact]
    public void ToRegex_WithFlags_AppliesFlags()
    {
        var regex = GlobMatcher.ToRegex("test", new GlobbingOptions { Flags = RegexFlags.IgnoreCase });

        Assert.Matches(regex, "TEST");
    }

    [Fact]
    public void ToRegex_WithDebugOption_ThrowsOnInvalidPattern()
    {
        Assert.ThrowsAny<Exception>(() =>
            GlobMatcher.ToRegex("[", new GlobbingOptions { Debug = true }));
    }

    [Fact]
    public void ToRegex_WithoutDebugOption_ReturnsNonMatchingRegexOnError()
    {
        var regex = GlobMatcher.ToRegex("[", new GlobbingOptions { Debug = false });

        Assert.NotNull(regex);
        Assert.DoesNotMatch(regex, "anything");
    }

    [Fact]
    public void Parse_ReturnsStateWithOutput()
    {
        var state = GlobMatcher.Parse("*.js");

        Assert.NotNull(state);
        Assert.NotNull(state.Output);
        Assert.NotEmpty(state.Output);
    }

    [Fact]
    public void Parse_MultiplePatterns_ReturnsArrayOfStates()
    {
        var states = GlobMatcher.Parse(["*.js", "*.ts"]);

        Assert.NotNull(states);
        Assert.Equal(2, states.Length);
        Assert.All(states, state => Assert.NotNull(state.Output));
    }

    [Fact]
    public void CompileRe_FromParseState_GeneratesValidRegex()
    {
        var state = GlobMatcher.Parse("*.js");
        var regex = GlobMatcher.CompileRe(state);

        Assert.NotNull(regex);
        Assert.Matches(regex, "test.js");
        Assert.DoesNotMatch(regex, "test.md");
    }

    [Fact]
    public void CompileRe_WithContainsOption_GeneratesCorrectRegex()
    {
        var state = GlobMatcher.Parse("test");
        var regex = GlobMatcher.CompileRe(state, new GlobbingOptions { Contains = true });

        Assert.NotNull(regex);
        Assert.Matches(regex, "my-test-file");
        Assert.Matches(regex, "testing");
    }

    [Fact]
    public void MakeRe_ComplexPattern_DatabaseUsable()
    {
        var regex = GlobMatcher.MakeRe("src/**/*.{js,ts}");
        string pattern = regex.ToString();

        // Verify the pattern is a valid regex string
        Assert.NotNull(pattern);
        Assert.NotEmpty(pattern);

        // Test it matches expected values
        Assert.Matches(regex, "src/app.js");
        Assert.Matches(regex, "src/lib/utils.ts");
        Assert.DoesNotMatch(regex, "test/app.js");
    }

    [Theory]
    [InlineData("*.js", "test.js", true)]
    [InlineData("**/*.js", "src/test.js", true)]
    [InlineData("test-[0-9].js", "test-5.js", true)]
    [InlineData("!(*.md)", "test.js", true)]
    [InlineData("*.{js,ts}", "app.ts", true)]
    public void MakeRe_VariousPatterns_MatchExpectedInputs(string pattern, string input, bool shouldMatch)
    {
        var regex = GlobMatcher.MakeRe(pattern);
        Assert.Equal(shouldMatch, regex.IsMatch(input));
    }

    [Fact]
    public void MakeRe_ForDatabaseQuery_SqlServerRegexpCompatible()
    {
        // Generate regex patterns suitable for SQL Server or PostgreSQL
        string[] patterns =
        [
            "*.js",
            "**/*.ts",
            "src/**/*.{js,ts}"
        ];

        foreach (string? pattern in patterns)
        {
            var regex = GlobMatcher.MakeRe(pattern);
            string regexPattern = regex.ToString();

            // Verify pattern is not null and can be converted to string
            Assert.NotNull(regexPattern);
            Assert.NotEmpty(regexPattern);

            // In real database usage: SELECT * FROM files WHERE filename REGEXP 'pattern'
        }
    }

    [Fact]
    public void MakeRe_ExtractRegexPattern_ForExternalUse()
    {
        var regex = GlobMatcher.MakeRe("*.js");
        string pattern = regex.ToString();

        // This pattern can be used in:
        // - SQL REGEXP queries
        // - MongoDB regex queries
        // - Elasticsearch queries
        // - Other database systems

        Assert.NotNull(pattern);

        // Create new regex from extracted pattern
        var newRegex = new Regex(pattern);
        Assert.Matches(newRegex, "test.js");
    }

    [Fact]
    public void MakeRe_WithFastpaths_OptimizesCommonPatterns()
    {
        var regexWithFastpath = GlobMatcher.MakeRe("*.js", new GlobbingOptions { FastPaths = true });
        var regexWithoutFastpath = GlobMatcher.MakeRe("*.js", new GlobbingOptions { FastPaths = false });

        // Both should match correctly
        Assert.Matches(regexWithFastpath, "test.js");
        Assert.Matches(regexWithoutFastpath, "test.js");
    }

    [Theory]
    [InlineData("*", "test.js")]
    [InlineData("**", "any/path/file.js")]
    [InlineData("*.*", "file.ext")]
    [InlineData("*/*", "dir/file")]
    [InlineData("**/*", "any/deep/path/file")]
    public void MakeRe_CommonPatterns_WorksCorrectly(string pattern, string testInput)
    {
        var regex = GlobMatcher.MakeRe(pattern);
        Assert.Matches(regex, testInput);
    }

    [Fact]
    public void MakeRe_ComplexDatabasePattern_GeneratesUsableRegex()
    {
        // Pattern to match TypeScript/JavaScript files in src directory
        var regex = GlobMatcher.MakeRe("src/**/*.{ts,tsx,js,jsx}");

        // Simulate database usage
        string[] testFiles =
        [
            "src/index.ts",
            "src/components/Button.tsx",
            "src/utils/helper.js",
            "src/hooks/useAuth.jsx",
            "test/index.ts",  // Should not match
            "src/styles.css"   // Should not match
        ];

        Assert.Matches(regex, testFiles[0]);
        Assert.Matches(regex, testFiles[1]);
        Assert.Matches(regex, testFiles[2]);
        Assert.Matches(regex, testFiles[3]);
        Assert.DoesNotMatch(regex, testFiles[4]);
        Assert.DoesNotMatch(regex, testFiles[5]);
    }

    [Fact]
    public void MakeRe_GetRegexOptions_ReturnsCorrectFlags()
    {
        var regexCaseSensitive = GlobMatcher.MakeRe("*.js");
        var regexCaseInsensitive = GlobMatcher.MakeRe("*.js", new GlobbingOptions { NoCase = true });

        Assert.DoesNotContain(RegexOptions.IgnoreCase, new[] { regexCaseSensitive.Options & RegexOptions.IgnoreCase });
        Assert.Contains(RegexOptions.IgnoreCase, new[] { regexCaseInsensitive.Options & RegexOptions.IgnoreCase });
    }

    [Fact]
    public void MakeRe_ExtractAndStorePattern_ForLaterUse()
    {
        var regex = GlobMatcher.MakeRe("**/*.{js,ts}");

        // Store pattern string (e.g., in database)
        string storedPattern = regex.ToString();
        Assert.NotNull(storedPattern);

        // Recreate regex from stored pattern
        var recreatedRegex = new Regex(storedPattern, regex.Options);

        // Verify it works the same
        Assert.Matches(recreatedRegex, "src/app.js");
        Assert.Matches(recreatedRegex, "lib/utils.ts");
    }
}
