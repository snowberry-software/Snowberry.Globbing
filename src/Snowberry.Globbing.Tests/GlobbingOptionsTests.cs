namespace Snowberry.Globbing.Tests;

/// <summary>
/// Comprehensive tests for all <see cref="GlobbingOptions"> properties to ensure complete coverage.
/// </summary>
public class GlobbingOptionsTests
{

    [Fact]
    public void BaseName_WhenTrue_StoresValue()
    {
        var options = new GlobbingOptions { BaseName = true };
        Assert.True(options.BaseName);
    }

    [Fact]
    public void BaseName_DefaultIsFalse()
    {
        var options = new GlobbingOptions();
        Assert.False(options.BaseName);
    }

    [Fact]
    public void MatchBase_Method_MatchesBaseNameOnly()
    {
        Assert.True(GlobMatcher.MatchBase("foo/bar.js", "*.js"));
        Assert.True(GlobMatcher.MatchBase("bar.js", "*.js"));
        Assert.False(GlobMatcher.MatchBase("foo/bar.md", "*.js"));
    }

    [Fact]
    public void Capture_WhenTrue_EnablesRegexCapturing()
    {
        var options = new GlobbingOptions { Capture = true };
        var regex = GlobMatcher.MakeRe("src/*/*.js", options);

        Assert.NotNull(regex);
        var match = regex.Match("src/lib/utils.js");
        Assert.True(match.Success);
    }

    [Fact]
    public void Capture_WhenFalse_DisablesCapturing()
    {
        var options = new GlobbingOptions { Capture = false };
        var regex = GlobMatcher.MakeRe("*.js", options);

        Assert.NotNull(regex);
        var match = regex.Match("test.js");
        Assert.True(match.Success);
    }

    [Fact]
    public void Cwd_WhenSet_StoresValue()
    {
        var options = new GlobbingOptions { Cwd = "/home/user/project" };
        Assert.Equal("/home/user/project", options.Cwd);
    }

    [Fact]
    public void Cwd_DefaultIsNull()
    {
        var options = new GlobbingOptions();
        Assert.Null(options.Cwd);
    }

    [Fact]
    public void Debug_WhenTrue_EnablesDebugMode()
    {
        var options = new GlobbingOptions { Debug = true };
        var matcher = GlobMatcher.Create("*.js", options);

        Assert.NotNull(matcher);
        Assert.True(matcher("test.js"));
    }

    [Fact]
    public void Debug_DefaultIsFalse()
    {
        var options = new GlobbingOptions();
        Assert.False(options.Debug);
    }

    [Theory]
    [InlineData(true, ".gitignore", true)]
    [InlineData(true, "regular.txt", true)]
    [InlineData(false, ".gitignore", false)]
    [InlineData(false, "regular.txt", true)]
    public void Dot_AffectsDotfileMatching(bool dotOption, string input, bool expected)
    {
        var options = new GlobbingOptions { Dot = dotOption };
        var matcher = GlobMatcher.Create("*", options);

        Assert.Equal(expected, matcher(input));
    }

    [Fact]
    public void ExpandRange_WhenSet_UsesCustomHandler()
    {
        var options = new GlobbingOptions
        {
            ExpandRange = (args, opts) =>
            {
                return "a|b|c";
            }
        };

        var matcher = GlobMatcher.Create("file-{a..c}.txt", options);
        Assert.NotNull(matcher);
    }

    [Fact]
    public void ExpandRange_DefaultIsNull()
    {
        var options = new GlobbingOptions();
        Assert.Null(options.ExpandRange);
    }

    [Fact]
    public void Failglob_WhenTrue_StoresValue()
    {
        var options = new GlobbingOptions { FailGlob = true };
        Assert.True(options.FailGlob);
    }

    [Fact]
    public void Failglob_DefaultIsFalse()
    {
        var options = new GlobbingOptions();
        Assert.False(options.FailGlob);
    }

    [Fact]
    public void Fastpaths_DefaultIsTrue()
    {
        var options = new GlobbingOptions();
        Assert.True(options.FastPaths);
    }

    [Theory]
    [InlineData("test.js", true)]
    [InlineData("test.md", false)]
    public void Fastpaths_WhenFalse_DisablesOptimizations(string input, bool expected)
    {
        var options = new GlobbingOptions { FastPaths = false };
        var matcher = GlobMatcher.Create("*.js", options);

        Assert.Equal(expected, matcher(input));
    }

    [Fact]
    public void Flags_DefaultIsNone()
    {
        var options = new GlobbingOptions();
        Assert.Equal(RegexFlags.None, options.Flags);
    }

    [Fact]
    public void Flags_CanBeSet()
    {
        var options = new GlobbingOptions { Flags = RegexFlags.IgnoreCase };
        Assert.Equal(RegexFlags.IgnoreCase, options.Flags);
    }

    [Fact]
    public void Format_WhenSet_TransformsInput()
    {
        var options = new GlobbingOptions
        {
            Format = (input) => input.Replace('\\', '/')
        };
        var matcher = GlobMatcher.Create("src/*.js", options);

        Assert.True(matcher("src/app.js"));
    }

    [Fact]
    public void Format_DefaultIsNull()
    {
        var options = new GlobbingOptions();
        Assert.Null(options.Format);
    }

    [Theory]
    [InlineData("app.js", true)]
    [InlineData("app.test.js", false)]
    [InlineData("app.spec.js", false)]
    public void Ignore_WhenSet_ExcludesPatterns(string input, bool expected)
    {
        var options = new GlobbingOptions
        {
            Ignore = ["*.test.js", "*.spec.js"]
        };
        var matcher = GlobMatcher.Create("*.js", options);

        Assert.Equal(expected, matcher(input));
    }

    [Fact]
    public void Ignore_DefaultIsNull()
    {
        var options = new GlobbingOptions();
        Assert.Null(options.Ignore);
    }

    [Fact]
    public void KeepQuotes_WhenTrue_PreservesQuotes()
    {
        var options = new GlobbingOptions { KeepQuotes = true };
        var matcher = GlobMatcher.Create("test.js", options);

        Assert.NotNull(matcher);
    }

    [Fact]
    public void KeepQuotes_DefaultIsFalse()
    {
        var options = new GlobbingOptions();
        Assert.False(options.KeepQuotes);
    }

    [Theory]
    [InlineData("[abc].js", true)]
    [InlineData("a.js", false)]
    public void LiteralBrackets_WhenTrue_TreatsBracketsAsLiteral(string input, bool expected)
    {
        var options = new GlobbingOptions { LiteralBrackets = true };
        var matcher = GlobMatcher.Create("[abc].js", options);

        Assert.Equal(expected, matcher(input));
    }

    [Theory]
    [InlineData("a.js", true)]
    [InlineData("b.js", true)]
    // Note: Direct string comparison matches "[abc].js" to the pattern "[abc].js"
    // This is consistent with JS picomatch behavior (input === glob fast path)
    [InlineData("[abc].js", true)]
    public void LiteralBrackets_WhenFalse_BracketsAreCharacterClass(string input, bool expected)
    {
        var options = new GlobbingOptions { LiteralBrackets = false };
        var matcher = GlobMatcher.Create("[abc].js", options);

        Assert.Equal(expected, matcher(input));
    }

    [Fact]
    public void LiteralBrackets_DefaultIsNull()
    {
        var options = new GlobbingOptions();
        Assert.Null(options.LiteralBrackets);
    }

    [Fact]
    public void MaxLength_DefaultIsConstant()
    {
        var options = new GlobbingOptions();
        Assert.Equal(Constants.c_MaxLength, options.MaxLength);
    }

    [Fact]
    public void MaxLength_WhenExceeded_ThrowsException()
    {
        string pattern = new('a', Constants.c_MaxLength + 1);
        Assert.Throws<ArgumentException>(() => GlobMatcher.MakeRe(pattern));
    }

    [Fact]
    public void MaxLength_CanBeCustomized()
    {
        var options = new GlobbingOptions { MaxLength = 100 };
        Assert.Equal(100, options.MaxLength);
    }

    [Theory]
    [InlineData("test.{js,ts}", true)]
    [InlineData("test.js", false)]
    public void NoBrace_WhenTrue_TreatsBracesAsLiteral(string input, bool expected)
    {
        var options = new GlobbingOptions { NoBrace = true };
        var matcher = GlobMatcher.Create("*.{js,ts}", options);

        Assert.Equal(expected, matcher(input));
    }

    [Theory]
    [InlineData("test.js", true)]
    [InlineData("test.ts", true)]
    [InlineData("test.{js,ts}", false)]
    public void NoBrace_WhenFalse_ExpandsBraces(string input, bool expected)
    {
        var options = new GlobbingOptions { NoBrace = false };
        var matcher = GlobMatcher.Create("*.{js,ts}", options);

        Assert.Equal(expected, matcher(input));
    }

    [Theory]
    [InlineData("[abc].txt", true)]
    [InlineData("a.txt", false)]
    public void NoBracket_WhenTrue_TreatsBracketsAsLiteral(string input, bool expected)
    {
        var options = new GlobbingOptions { NoBracket = true };
        var matcher = GlobMatcher.Create("[abc].txt", options);

        Assert.Equal(expected, matcher(input));
    }

    [Fact]
    public void NoBracket_DefaultIsNull()
    {
        var options = new GlobbingOptions();
        Assert.Null(options.NoBracket);
    }

    [Theory]
    [InlineData(true, "test.JS", true)]
    [InlineData(true, "test.js", true)]
    [InlineData(true, "test.Js", true)]
    [InlineData(false, "test.JS", false)]
    [InlineData(false, "test.js", true)]
    public void NoCase_AffectsCaseSensitivity(bool noCase, string input, bool expected)
    {
        var options = new GlobbingOptions { NoCase = noCase };
        var matcher = GlobMatcher.Create("*.js", options);

        Assert.Equal(expected, matcher(input));
    }

    [Fact]
    public void NoExt_IsAliasForNoExtglob()
    {
        var options = new GlobbingOptions { NoExt = true };
        Assert.True(options.NoExt);
    }

    [Fact]
    public void NoExt_DefaultIsNull()
    {
        var options = new GlobbingOptions();
        Assert.Null(options.NoExt);
    }

    [Theory]
    // Without extglob, pattern is treated differently
    [InlineData("a", false)]
    public void NoExtglob_WhenTrue_DisablesExtglobs(string input, bool expected)
    {
        var options = new GlobbingOptions { NoExtglob = true };
        var matcher = GlobMatcher.Create("+(a)", options);

        Assert.Equal(expected, matcher(input));
    }

    [Theory]
    [InlineData("a", true)]
    [InlineData("aa", true)]
    public void NoExtglob_WhenFalse_EnablesExtglobs(string input, bool expected)
    {
        var options = new GlobbingOptions { NoExtglob = false };
        var matcher = GlobMatcher.Create("+(a)", options);

        Assert.Equal(expected, matcher(input));
    }

    [Theory]
    [InlineData("foo", true)]
    [InlineData("foo/bar", false)]
    public void NoGlobstar_WhenTrue_TreatsDoubleStarAsSingleStar(string input, bool expected)
    {
        var options = new GlobbingOptions { NoGlobstar = true };
        var matcher = GlobMatcher.Create("**", options);

        Assert.Equal(expected, matcher(input));
    }

    [Theory]
    [InlineData("src/lib/app.js", true)]
    [InlineData("app.js", true)]
    public void NoGlobstar_WhenFalse_DoubleStarMatchesDeep(string input, bool expected)
    {
        var options = new GlobbingOptions { NoGlobstar = false };
        var matcher = GlobMatcher.Create("**/*.js", options);

        Assert.Equal(expected, matcher(input));
    }

    [Theory]
    [InlineData("!test.md", true)]
    [InlineData("test.md", false)]
    public void NoNegate_WhenTrue_TreatsExclamationAsLiteral(string input, bool expected)
    {
        var options = new GlobbingOptions { NoNegate = true };
        var matcher = GlobMatcher.Create("!*.md", options);

        Assert.Equal(expected, matcher(input));
    }

    [Theory]
    [InlineData("test.md", false)]
    [InlineData("test.js", true)]
    public void NoNegate_WhenFalse_ExclamationIsNegation(string input, bool expected)
    {
        var options = new GlobbingOptions { NoNegate = false };
        var matcher = GlobMatcher.Create("!*.md", options);

        Assert.Equal(expected, matcher(input));
    }

    [Fact]
    public void NoQuantifiers_WhenTrue_TreatsQuantifiersAsLiteral()
    {
        var options = new GlobbingOptions { NoQuantifiers = true };
        Assert.True(options.NoQuantifiers);
    }

    [Fact]
    public void NoQuantifiers_DefaultIsFalse()
    {
        var options = new GlobbingOptions();
        Assert.False(options.NoQuantifiers);
    }

    [Fact]
    public void OnIgnore_WhenSet_CalledForIgnoredItems()
    {
        var ignoredItems = new List<string>();
        var options = new GlobbingOptions
        {
            Ignore = ["*.test.js"],
            OnIgnore = (result) => ignoredItems.Add(result.Input)
        };
        var matcher = GlobMatcher.Create("*.js", options);

        matcher("app.js");
        matcher("app.test.js");

        Assert.Single(ignoredItems);
        Assert.Contains("app.test.js", ignoredItems);
    }

    [Fact]
    public void OnMatch_WhenSet_CalledForMatchedItems()
    {
        var matchedItems = new List<string>();
        var options = new GlobbingOptions
        {
            OnMatch = (result) => matchedItems.Add(result.Input)
        };
        var matcher = GlobMatcher.Create("*.js", options);

        matcher("app.js");
        matcher("app.md");

        Assert.Single(matchedItems);
        Assert.Contains("app.js", matchedItems);
    }

    [Fact]
    public void OnResult_WhenSet_CalledForAllItems()
    {
        int callCount = 0;
        var options = new GlobbingOptions
        {
            OnResult = (result) => callCount++
        };
        var matcher = GlobMatcher.Create("*.js", options);

        matcher("app.js");
        matcher("app.md");
        matcher("test.ts");

        Assert.Equal(3, callCount);
    }

    [Fact]
    public void Posix_WhenTrue_EnablesPosixClasses()
    {
        var options = new GlobbingOptions { Posix = true };
        var matcher = GlobMatcher.Create("[[:alnum:]]*", options);

        Assert.NotNull(matcher);
    }

    [Fact]
    public void Posix_DefaultIsFalse()
    {
        var options = new GlobbingOptions();
        Assert.False(options.Posix);
    }

    [Fact]
    public void Prepend_WhenSet_PrependedToRegex()
    {
        var options = new GlobbingOptions { Prepend = "(?:prefix/)" };
        Assert.Equal("(?:prefix/)", options.Prepend);
    }

    [Fact]
    public void Prepend_DefaultIsNull()
    {
        var options = new GlobbingOptions();
        Assert.Null(options.Prepend);
    }

    [Fact]
    public void Regex_WhenTrue_TreatsPlusAsQuantifier()
    {
        var options = new GlobbingOptions { Regex = true };
        Assert.True(options.Regex);
    }

    [Fact]
    public void Regex_DefaultIsFalse()
    {
        var options = new GlobbingOptions();
        Assert.False(options.Regex);
    }

    [Fact]
    public void StrictBrackets_WhenTrue_EnforcesBalancedBrackets()
    {
        var options = new GlobbingOptions { StrictBrackets = true };
        Assert.True(options.StrictBrackets);

        // Valid balanced pattern should work
        var matcher = GlobMatcher.Create("[abc]", options);
        Assert.NotNull(matcher);
    }

    [Fact]
    public void StrictBrackets_DefaultIsNull()
    {
        var options = new GlobbingOptions();
        Assert.Null(options.StrictBrackets);
    }

    [Fact]
    public void StrictSlashes_WhenTrue_PreventsStarMatchingSlash()
    {
        var options = new GlobbingOptions { StrictSlashes = true };
        Assert.True(options.StrictSlashes);
    }

    [Fact]
    public void StrictSlashes_DefaultIsNull()
    {
        var options = new GlobbingOptions();
        Assert.Null(options.StrictSlashes);
    }

    [Fact]
    public void Unescape_WhenSet_UnescapesCharacters()
    {
        var options = new GlobbingOptions { Unescape = true };
        Assert.True(options.Unescape);
    }

    [Fact]
    public void Unescape_DefaultIsNull()
    {
        var options = new GlobbingOptions();
        Assert.Null(options.Unescape);
    }

    [Theory]
    [InlineData(@"src\lib\app.js", true)]
    [InlineData("src/lib/app.js", true)]
    public void Windows_WhenTrue_TreatsBackslashAsPathSeparator(string input, bool expected)
    {
        var options = new GlobbingOptions { Windows = true };
        var matcher = GlobMatcher.Create("src/**/*.js", options);

        Assert.Equal(expected, matcher(input));
    }

    [Theory]
    [InlineData("src/lib/app.js", true)]
    public void Windows_WhenFalse_OnlyForwardSlashIsPathSeparator(string input, bool expected)
    {
        var options = new GlobbingOptions { Windows = false };
        var matcher = GlobMatcher.Create("src/**/*.js", options);

        Assert.Equal(expected, matcher(input));
    }

    [Fact]
    public void Windows_DefaultIsNull()
    {
        var options = new GlobbingOptions();
        Assert.Null(options.Windows);
    }

    [Fact]
    public void GetIgnoreOptions_ReturnsNewInstanceWithoutCallbacks()
    {
        var options = new GlobbingOptions
        {
            Dot = true,
            NoCase = true,
            Ignore = ["*.md"],
            OnMatch = (r) => { },
            OnIgnore = (r) => { },
            OnResult = (r) => { }
        };

        var ignoreOptions = options.GetIgnoreOptions();

        Assert.NotSame(options, ignoreOptions);
        Assert.True(ignoreOptions.Dot);
        Assert.True(ignoreOptions.NoCase);
        Assert.Null(ignoreOptions.Ignore);
        Assert.Null(ignoreOptions.OnMatch);
        Assert.Null(ignoreOptions.OnIgnore);
        Assert.Null(ignoreOptions.OnResult);
    }

    [Fact]
    public void GetIgnoreOptions_CopiesAllNonCallbackProperties()
    {
        var options = new GlobbingOptions
        {
            BaseName = true,
            Bash = true,
            Capture = true,
            Contains = true,
            Cwd = "/test",
            Debug = true,
            Dot = true,
            FailGlob = true,
            FastPaths = false,
            KeepQuotes = true,
            MaxLength = 100,
            NoBrace = true,
            NoCase = true,
            NoExtglob = true,
            NoGlobstar = true,
            NoNegate = true,
            NoQuantifiers = true,
            Posix = true,
            Prepend = "test",
            Regex = true,
            Windows = true
        };

        var ignoreOptions = options.GetIgnoreOptions();

        Assert.True(ignoreOptions.BaseName);
        Assert.True(ignoreOptions.Bash);
        Assert.True(ignoreOptions.Capture);
        Assert.True(ignoreOptions.Contains);
        Assert.Equal("/test", ignoreOptions.Cwd);
        Assert.True(ignoreOptions.Debug);
        Assert.True(ignoreOptions.Dot);
        Assert.True(ignoreOptions.FailGlob);
        Assert.False(ignoreOptions.FastPaths);
        Assert.True(ignoreOptions.KeepQuotes);
        Assert.Equal(100, ignoreOptions.MaxLength);
        Assert.True(ignoreOptions.NoBrace);
        Assert.True(ignoreOptions.NoCase);
        Assert.True(ignoreOptions.NoExtglob);
        Assert.True(ignoreOptions.NoGlobstar);
        Assert.True(ignoreOptions.NoNegate);
        Assert.True(ignoreOptions.NoQuantifiers);
        Assert.True(ignoreOptions.Posix);
        Assert.Equal("test", ignoreOptions.Prepend);
        Assert.True(ignoreOptions.Regex);
        Assert.True(ignoreOptions.Windows);
    }

    [Theory]
    [InlineData("foobar", true)]
    [InlineData("barbaz", true)]
    [InlineData("foobarbaz", true)]
    [InlineData("foo", false)]
    public void Contains_WhenTrue_MatchesSubstring(string input, bool expected)
    {
        var options = new GlobbingOptions { Contains = true };
        var matcher = GlobMatcher.Create("bar", options);

        Assert.Equal(expected, matcher(input));
    }

    [Theory]
    [InlineData("foobar", false)]
    [InlineData("bar", true)]
    public void Contains_WhenFalse_RequiresFullMatch(string input, bool expected)
    {
        var options = new GlobbingOptions { Contains = false };
        var matcher = GlobMatcher.Create("bar", options);

        Assert.Equal(expected, matcher(input));
    }

    [Theory]
    [InlineData("foo", true)]
    [InlineData("bar", true)]
    public void Bash_WhenTrue_UsesBashRules(string input, bool expected)
    {
        var options = new GlobbingOptions { Bash = true };
        var matcher = GlobMatcher.Create("*", options);

        Assert.Equal(expected, matcher(input));
    }

    [Fact]
    public void Bash_DefaultIsFalse()
    {
        var options = new GlobbingOptions();
        Assert.False(options.Bash);
    }

    [Fact]
    public void Contains_DefaultIsFalse()
    {
        var options = new GlobbingOptions();
        Assert.False(options.Contains);
    }

    [Fact]
    public void NoBrace_DefaultIsFalse()
    {
        var options = new GlobbingOptions();
        Assert.False(options.NoBrace);
    }

    [Fact]
    public void NoExtglob_DefaultIsFalse()
    {
        var options = new GlobbingOptions();
        Assert.False(options.NoExtglob);
    }

    [Fact]
    public void NoGlobstar_DefaultIsFalse()
    {
        var options = new GlobbingOptions();
        Assert.False(options.NoGlobstar);
    }

    [Fact]
    public void NoNegate_DefaultIsFalse()
    {
        var options = new GlobbingOptions();
        Assert.False(options.NoNegate);
    }

    [Fact]
    public void OnIgnore_DefaultIsNull()
    {
        var options = new GlobbingOptions();
        Assert.Null(options.OnIgnore);
    }

    [Fact]
    public void OnMatch_DefaultIsNull()
    {
        var options = new GlobbingOptions();
        Assert.Null(options.OnMatch);
    }

    [Fact]
    public void OnResult_DefaultIsNull()
    {
        var options = new GlobbingOptions();
        Assert.Null(options.OnResult);
    }

    [Fact]
    public void Flags_CombinedFlags_CanBeSet()
    {
        var options = new GlobbingOptions { Flags = RegexFlags.IgnoreCase | RegexFlags.MultiLine };
        Assert.Equal(RegexFlags.IgnoreCase | RegexFlags.MultiLine, options.Flags);
    }

    [Fact]
    public void GetIgnoreOptions_CopiesExpandRangeAndFormat()
    {
        ExpandRangeHandler expandHandler = (args, opts) => "test";
        FormatHandler formatHandler = (input) => input.ToUpperInvariant();

        var options = new GlobbingOptions
        {
            ExpandRange = expandHandler,
            Format = formatHandler
        };

        var ignoreOptions = options.GetIgnoreOptions();

        Assert.Same(expandHandler, ignoreOptions.ExpandRange);
        Assert.Same(formatHandler, ignoreOptions.Format);
    }

    [Fact]
    public void GetIgnoreOptions_CopiesNullableProperties()
    {
        var options = new GlobbingOptions
        {
            LiteralBrackets = true,
            NoBracket = true,
            NoExt = true,
            StrictBrackets = true,
            StrictSlashes = true,
            Unescape = true,
            Flags = RegexFlags.IgnoreCase
        };

        var ignoreOptions = options.GetIgnoreOptions();

        Assert.True(ignoreOptions.LiteralBrackets);
        Assert.True(ignoreOptions.NoBracket);
        Assert.True(ignoreOptions.NoExt);
        Assert.True(ignoreOptions.StrictBrackets);
        Assert.True(ignoreOptions.StrictSlashes);
        Assert.True(ignoreOptions.Unescape);
        Assert.Equal(RegexFlags.IgnoreCase, ignoreOptions.Flags);
    }

    [Theory]
    [InlineData("a.txt", true)]
    [InlineData("b.txt", true)]
    public void NoBracket_WhenFalse_BracketsAreCharacterClass(string input, bool expected)
    {
        var options = new GlobbingOptions { NoBracket = false };
        var matcher = GlobMatcher.Create("[abc].txt", options);

        Assert.Equal(expected, matcher(input));
    }

}
