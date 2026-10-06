using Snowberry.Globbing.Compilation;

namespace Snowberry.Globbing.Tests;

public class GlobTests
{
    private static readonly GlobOptions s_Posix = new() { PathStyle = GlobPathStyle.Posix };

    [Fact]
    public void Constructor_ExposesPatternsAndOptions()
    {
        var glob = new Glob("*.js", s_Posix);

        glob.Patterns.Should().Equal(["*.js"]);
        glob.Options.Should().BeSameAs(s_Posix);
        glob.ToString().Should().Be("*.js");
    }

    [Fact]
    public void Constructor_WithEmptyIgnorePattern_ThrowsEmptyPattern()
    {
        var e = FluentActions.Invoking(() => new Glob("*.js", new GlobOptions { IgnorePatterns = [""] })).Should().ThrowExactly<GlobParseException>().Which;
        e.Error.Should().Be(GlobParseError.EmptyPattern);
        e.ParamName.Should().Be(nameof(GlobOptions.IgnorePatterns));
    }

    [Fact]
    public void Constructor_WithEmptyPatternInList_ThrowsEmptyPattern()
    {
        var e = FluentActions.Invoking(() => new Glob(new[] { "*.js", "" })).Should().ThrowExactly<GlobParseException>().Which;
        e.Error.Should().Be(GlobParseError.EmptyPattern);
        e.ParamName.Should().Be("patterns");
    }

    [Fact]
    public void Constructor_WithEmptyPatternList_ThrowsArgumentException()
    {
        var e = FluentActions.Invoking(() => new Glob(Array.Empty<string>())).Should().ThrowExactly<ArgumentException>().Which;
        e.ParamName.Should().Be("patterns");
    }

    [Fact]
    public void Constructor_WithEmptyPattern_ThrowsEmptyPattern()
    {
        var e = FluentActions.Invoking(() => new Glob("")).Should().ThrowExactly<GlobParseException>().Which;
        e.Error.Should().Be(GlobParseError.EmptyPattern);
        e.ParamName.Should().Be("pattern");
    }

    [Fact]
    public void Constructor_WithNullPatternList_ThrowsArgumentNullException()
    {
        FluentActions.Invoking(() => new Glob((string[])null!)).Should().ThrowExactly<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullPattern_ThrowsArgumentNullException()
    {
        var e = FluentActions.Invoking(() => new Glob((string)null!)).Should().ThrowExactly<ArgumentNullException>().Which;
        e.ParamName.Should().Be("pattern");
    }

    [Theory]
    [InlineData("app.js", true)]
    [InlineData("app.ts", true)]
    [InlineData("readme.md", true)]
    [InlineData("app.css", false)]
    public void Constructor_WithPatternList_MatchesAnyPattern(string input, bool expected)
    {
        var glob = new Glob(["*.js", "*.ts", "*.md"]);

        glob.IsMatch(input).Should().Be(expected);
    }

    [Fact]
    public void Constructor_WithoutOptions_UsesDefault()
    {
        new Glob("*.js").Options.Should().BeSameAs(GlobOptions.Default);
    }

    [Fact]
    public void Filter_ReturnsMatchingInputsInOrder()
    {
        var glob = new Glob("**/*.cs", s_Posix with { IgnorePatterns = ["**/obj/**"] });

        var result = glob.Filter(["b.cs", "README.md", "obj/x.cs", "a/c.cs"]);

        result.Should().Equal(["b.cs", "a/c.cs"]);
    }

    [Fact]
    public void Filter_WithNullInputs_ThrowsArgumentNullException()
    {
        FluentActions.Invoking(() => new Glob("*").Filter(null!)).Should().ThrowExactly<ArgumentNullException>();
    }

    [Fact]
    public void IsMatch_Span_AppliesIgnorePatterns()
    {
        var glob = new Glob("*.js", s_Posix with { IgnorePatterns = ["a.*"] });

        glob.IsMatch("a.js".AsSpan()).Should().BeFalse();
        glob.IsMatch("b.js".AsSpan()).Should().BeTrue();
    }

    [Theory]
    [InlineData("a.js", true)]
    [InlineData("src/a.js", false)]
    [InlineData("a.ts", false)]
    public void IsMatch_Span_MatchesLikeString(string input, bool expected)
    {
        var glob = new Glob("*.js", s_Posix);

        glob.IsMatch(input.AsSpan()).Should().Be(expected);
        Glob.IsMatch(input.AsSpan(), "*.js", s_Posix).Should().Be(expected);
    }

    [Theory]
    [InlineData("foo", "foo/", false)]
    [InlineData("abc", "a*(b|c)", true)]
    [InlineData("abbb", "a*(b|c)", true)]
    [InlineData("accc", "a*(b|c)", true)]
    [InlineData("abbb", "a+(b|c)", true)]
    [InlineData("a", "a+(b|c)", false)]
    [InlineData("ad", "a?(b|c)", false)]
    [InlineData("abc", "a@(b|c)", false)]
    [InlineData("test-a.js", "test-[^0-9].js", true)]
    [InlineData("test-0.js", "test-[^0-9].js", false)]
    [InlineData("app.ts", "**/!(*.d).ts", true)]
    [InlineData("src/utils/helper.ts", "**/!(*.d).ts", true)]
    [InlineData("app.d.ts", "**/!(*.d).ts", false)]
    [InlineData("src/types.d.ts", "**/!(*.d).ts", false)]
    [InlineData("deep/nested/path/types.d.ts", "**/!(*.d).ts", false)]
    [InlineData("src/app.js", "**/!(*.d).ts", false)]
    public void IsMatch_Static_ReturnsWhetherInputMatches(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Fact]
    public void IsMatch_Static_WithEmptyPattern_ThrowsEmptyPattern()
    {
        FluentActions.Invoking(() => Glob.IsMatch("a", "")).Should().ThrowExactly<GlobParseException>()
            .Which.Error.Should().Be(GlobParseError.EmptyPattern);
    }

    [Fact]
    public void IsMatch_Static_WithEqualOptions_ReturnsSameResult()
    {
        Glob.IsMatch("A.JS", "*.js", new GlobOptions { IgnoreCase = true }).Should().BeTrue();
        Glob.IsMatch("A.JS", "*.js", new GlobOptions { IgnoreCase = true }).Should().BeTrue();
        Glob.IsMatch("A.JS", "*.js", new GlobOptions { IgnoreCase = false }).Should().BeFalse();
    }

    [Theory]
    [InlineData("test.js", new[] { "*.js", "*.ts" }, true)]
    [InlineData("test.ts", new[] { "*.js", "*.ts" }, true)]
    [InlineData("test.md", new[] { "*.js", "*.ts" }, false)]
    [InlineData("app.css", new[] { "*.js", "*.ts", "*.css" }, true)]
    public void IsMatch_Static_WithPatternList_MatchesAny(string input, string[] patterns, bool expected)
    {
        Glob.IsMatch(input, patterns).Should().Be(expected);
    }

    [Fact]
    public void IsMatch_WithEmptyInput_ReturnsFalse()
    {
        Glob.IsMatch("", "*").Should().BeFalse();
    }

    [Fact]
    public void IsMatch_WithNullInput_ThrowsArgumentNullException()
    {
        var glob = new Glob("*.js");

        FluentActions.Invoking(() => glob.IsMatch(null!)).Should().ThrowExactly<ArgumentNullException>().WithParameterName("input");
        FluentActions.Invoking(() => Glob.IsMatch(null!, "*.js")).Should().ThrowExactly<ArgumentNullException>().WithParameterName("input");
    }

    [Fact]
    public void Match_ReportsIgnoredInput()
    {
        var glob = new Glob("**/*.cs", s_Posix with { IgnorePatterns = ["**/obj/**"] });

        var ignored = glob.Match("src/obj/a.cs");
        var unmatched = glob.Match("src/a.md");

        ignored.Success.Should().BeFalse();
        ignored.IsIgnored.Should().BeTrue();
        ignored.Pattern.Should().Be("**/*.cs");
        ignored.Input.Should().Be("src/obj/a.cs");
        unmatched.Success.Should().BeFalse();
        unmatched.IsIgnored.Should().BeFalse();
        unmatched.Pattern.Should().BeNull();
        unmatched.Input.Should().Be("src/a.md");
    }

    [Fact]
    public void Match_ReportsMatchedPattern()
    {
        var glob = new Glob(new[] { "*.js", "*.ts" }, s_Posix);

        var result = glob.Match("a.ts");

        result.Success.Should().BeTrue();
        result.IsIgnored.Should().BeFalse();
        result.Pattern.Should().Be("*.ts");
        result.Input.Should().Be("a.ts");
        result.NormalizedInput.Should().Be("a.ts");
    }

    [Fact]
    public void Match_ReportsNormalizedInput()
    {
        var glob = new Glob("src/*.cs", new GlobOptions { PathStyle = GlobPathStyle.Windows });

        var result = glob.Match(@"src\a.cs");

        result.Success.Should().BeTrue();
        result.NormalizedInput.Should().Be("src/a.cs");
    }

    [Fact]
    public void Match_WithEmptyInput_Fails()
    {
        var result = new Glob("*.js").Match("");

        result.Success.Should().BeFalse();
        result.Pattern.Should().BeNull();
        result.NormalizedInput.Should().Be("");
    }

    [Fact]
    public void Match_WithInputNormalizer_ReportsOriginalAndNormalizedInput()
    {
        var result = new Glob("*.js", new GlobOptions { InputNormalizer = s => s.ToUpper() }).Match("test.js");

        result.Input.Should().Be("test.js");
        result.NormalizedInput.Should().Be("TEST.JS");
        // The normalized input is matched, and "*.js" is case-sensitive.
        result.Success.Should().BeFalse();
    }

    [Fact]
    public void ToRegexString_ForSinglePattern_EqualsGeneratedSource()
    {
        new Glob("*.js", s_Posix).ToRegexString().Should().Be(GlobCompiler.CompileRegexSource("*.js", s_Posix));
    }

    [Fact]
    public void ToRegex_AppliesIgnoreCaseAndRegexOptions()
    {
        var regex = new Glob("*.js", new GlobOptions { IgnoreCase = true, RegexOptions = RegexOptions.Compiled }).ToRegex();

        regex.Options.HasFlag(RegexOptions.IgnoreCase).Should().BeTrue();
        regex.Options.HasFlag(RegexOptions.Compiled).Should().BeTrue();
        "A.JS".Should().MatchRegex(regex);
    }

    [Fact]
    public void ToRegex_ByDefault_IsInterpretedAndCaseSensitive()
    {
        var regex = new Glob("*.js").ToRegex();

        regex.Options.HasFlag(RegexOptions.Compiled).Should().BeFalse();
        regex.Options.HasFlag(RegexOptions.IgnoreCase).Should().BeFalse();
    }

    [Theory]
    [InlineData("*.js", "test.js", true)]
    [InlineData("*.js", "test.md", false)]
    [InlineData("**/*.js", "src/lib/utils.js", true)]
    [InlineData("**/*.js", "test.md", false)]
    [InlineData("*.{js,ts,md}", "readme.md", true)]
    [InlineData("*.{js,ts,md}", "app.css", false)]
    [InlineData("[abc].js", "c.js", true)]
    [InlineData("[abc].js", "d.js", false)]
    [InlineData("test-?.js", "test-a.js", true)]
    [InlineData("test-?.js", "test-12.js", false)]
    [InlineData("!(*.md)", "test.js", true)]
    [InlineData("!*.md", "readme.md", false)]
    [InlineData("src/**/*.{ts,tsx,js,jsx}", "src/components/Button.tsx", true)]
    [InlineData("src/**/*.{ts,tsx,js,jsx}", "test/index.ts", false)]
    [InlineData("src/**/*.{ts,tsx,js,jsx}", "src/styles.css", false)]
    [InlineData("*", "test.js", true)]
    [InlineData("**", "any/path/file.js", true)]
    [InlineData("*.*", "file.ext", true)]
    [InlineData("*/*", "dir/file", true)]
    [InlineData("**/*", "any/deep/path/file", true)]
    public void ToRegex_MatchesSameInputsAsIsMatch(string pattern, string input, bool expected)
    {
        var glob = new Glob(pattern);

        glob.IsMatch(input).Should().Be(expected);
        glob.ToRegex().IsMatch(input).Should().Be(expected);
        new Regex(glob.ToRegexString()).IsMatch(input).Should().Be(expected);
    }

    [Fact]
    public void ToRegex_ReturnsSameInstance()
    {
        var glob = new Glob("*.js");

        glob.ToRegex().Should().BeSameAs(glob.ToRegex());
    }

    [Theory]
    [InlineData("a.js", true)]
    [InlineData("a.ts", true)]
    [InlineData("a.md", false)]
    [InlineData("x.md", true)]
    public void ToRegex_WithPatternList_MatchesAny(string input, bool expected)
    {
        var glob = new Glob(new[] { "*.js", "*.ts", "!a.*" }, s_Posix);

        glob.ToRegex().IsMatch(input).Should().Be(expected);
        new Regex(glob.ToRegexString()).IsMatch(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("*.js", true)]
    [InlineData("a[b", true)]
    [InlineData("[z-a]", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void TryCreate_ReturnsWhetherPatternCompiles(string? pattern, bool expected)
    {
        bool created = Glob.TryCreate(pattern, null, out var glob);

        created.Should().Be(expected);
        (glob != null).Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("[z-a]")]
    public void TryCreate_WithError_InvalidIgnorePattern_ReportsIgnorePatterns(string ignorePattern)
    {
        var options = new GlobOptions { IgnorePatterns = [ignorePattern] };

        Glob.TryCreate("*.js", options, out _, out var error).Should().BeFalse();

        error!.ParamName.Should().Be(nameof(GlobOptions.IgnorePatterns));
        error.Error.Should().Be(ignorePattern.Length == 0 ? GlobParseError.EmptyPattern : GlobParseError.InvalidPattern);
    }

    [Theory]
    [InlineData("[z-a]", false, GlobParseError.InvalidPattern, -1)]
    [InlineData("a[b", true, GlobParseError.MissingClosingBracket, 1)]
    [InlineData("a(b", true, GlobParseError.MissingClosingParenthesis, 1)]
    public void TryCreate_WithError_InvalidPattern_ReportsTheParseError(string pattern, bool strictBrackets, GlobParseError expectedError, int expectedOffset)
    {
        Glob.TryCreate(pattern, new GlobOptions { StrictBrackets = strictBrackets }, out _, out var error).Should().BeFalse();

        var parseError = error.Should().BeOfType<GlobParseException>().Which;
        parseError.Error.Should().Be(expectedError);
        parseError.Offset.Should().Be(expectedOffset);
        parseError.Pattern.Should().Be(pattern);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void TryCreate_WithError_NullOrEmptyPattern_ReportsEmptyPattern(string? pattern)
    {
        Glob.TryCreate(pattern, null, out var glob, out var error).Should().BeFalse();

        glob.Should().BeNull();
        error!.Error.Should().Be(GlobParseError.EmptyPattern);
        error.Pattern.Should().BeEmpty();
        error.ParamName.Should().Be("pattern");
    }

    [Fact]
    public void TryCreate_WithError_OnSuccess_ReturnsGlobAndNoError()
    {
        Glob.TryCreate("*.js", null, out var glob, out var error).Should().BeTrue();

        glob.Should().NotBeNull();
        error.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("[z-a]")]
    public void TryCreate_WithInvalidIgnorePattern_ReturnsFalse(string? ignorePattern)
    {
        var options = new GlobOptions { IgnorePatterns = [ignorePattern!] };

        Glob.TryCreate("*.js", options, out var glob).Should().BeFalse();
        glob.Should().BeNull();
    }

    [Fact]
    public void TryCreate_WithStrictBracketsViolation_ReturnsFalse()
    {
        Glob.TryCreate("a[b", new GlobOptions { StrictBrackets = true }, out _).Should().BeFalse();
    }
}