namespace Snowberry.Globbing;

/// <summary>
/// Configuration options for glob pattern matching behavior.
/// </summary>
/// <remarks>
/// Controls features like case sensitivity, dotfile matching, brace expansion, extglobs, and more.
/// </remarks>
public class GlobbingOptions
{
    /// <summary>
    /// Match patterns without slashes against the basename only.
    /// </summary>
    /// <remarks>
    /// <para>Example with pattern <c>"*.js"</c>:</para>
    /// <list type="bullet">
    /// <item><description><c>"path/to/file.js"</c> → <see langword="true"/> (matches basename <c>"file.js"</c>)</description></item>
    /// <item><description><c>"file.js"</c> → <see langword="true"/></description></item>
    /// <item><description><c>"path/to/file.md"</c> → <see langword="false"/></description></item>
    /// </list>
    /// </remarks>
    public bool BaseName { get; set; }

    /// <summary>
    /// Follow bash matching rules more strictly for edge cases and special characters.
    /// </summary>
    public bool Bash { get; set; }

    /// <summary>
    /// Use capturing groups instead of non-capturing groups in the generated regex.
    /// Allows access to captured subpatterns in match results.
    /// </summary>
    public bool Capture { get; set; }

    /// <summary>
    /// Allow the pattern to match any part of the string (substring match).
    /// </summary>
    /// <remarks>
    /// <para>Example with pattern <c>"bar"</c>:</para>
    /// <list type="bullet">
    /// <item><description><see cref="Contains"/> = <see langword="true"/>: <c>"foobar"</c> → <see langword="true"/> (substring match)</description></item>
    /// <item><description><see cref="Contains"/> = <see langword="true"/>: <c>"barbaz"</c> → <see langword="true"/></description></item>
    /// <item><description><see cref="Contains"/> = <see langword="false"/>: <c>"foobar"</c> → <see langword="false"/> (must match entire string)</description></item>
    /// <item><description><see cref="Contains"/> = <see langword="false"/>: <c>"bar"</c> → <see langword="true"/></description></item>
    /// </list>
    /// </remarks>
    public bool Contains { get; set; }

    /// <summary>
    /// Current working directory for resolving relative paths.
    /// </summary>
    public string? Cwd { get; set; }

    /// <summary>
    /// Throw exceptions when regex compilation fails instead of returning a non-matching regex.
    /// Useful for debugging invalid patterns.
    /// </summary>
    public bool Debug { get; set; }

    /// <summary>
    /// Allow patterns to match dotfiles (files/directories starting with a dot).
    /// </summary>
    /// <remarks>
    /// <para>By default, <c>"*"</c> does not match dotfiles.</para>
    /// <para>Example with pattern <c>"*"</c>:</para>
    /// <list type="bullet">
    /// <item><description><see cref="Dot"/> = <see langword="true"/>: <c>".gitignore"</c> → <see langword="true"/></description></item>
    /// <item><description><see cref="Dot"/> = <see langword="true"/>: <c>"readme.md"</c> → <see langword="true"/></description></item>
    /// <item><description><see cref="Dot"/> = <see langword="false"/>: <c>".gitignore"</c> → <see langword="false"/></description></item>
    /// <item><description><see cref="Dot"/> = <see langword="false"/>: <c>"readme.md"</c> → <see langword="true"/></description></item>
    /// </list>
    /// </remarks>
    public bool Dot { get; set; }

    /// <summary>
    /// Custom handler for expanding range expressions like <c>"{1..5}"</c> or <c>"{a..z}"</c>.
    /// </summary>
    public ExpandRangeHandler? ExpandRange { get; set; }

    /// <summary>
    /// Throw an exception if no matches are found (bash <c>failglob</c> behavior).
    /// </summary>
    public bool FailGlob { get; set; }

    /// <summary>
    /// Enable fast-path optimizations for common glob patterns. Default is <see langword="true"/>.
    /// Disable only for debugging or when full parsing is required.
    /// </summary>
    public bool FastPaths { get; set; } = true;

    /// <summary>
    /// Regex flags to apply to the generated regular expression.
    /// Can be combined using bitwise OR.
    /// </summary>
    public RegexFlags Flags { get; set; } = RegexFlags.None;

    /// <summary>
    /// Custom handler for formatting input strings before matching (e.g., path normalization).
    /// </summary>
    public FormatHandler? Format { get; set; }

    /// <summary>
    /// Glob patterns to exclude from matches.
    /// </summary>
    /// <remarks>
    /// <para>Example with pattern <c>"**/*.js"</c> and <c>Ignore = ["*.test.js", "*.spec.js"]</c>:</para>
    /// <list type="bullet">
    /// <item><description><c>"app.js"</c> → <see langword="true"/></description></item>
    /// <item><description><c>"utils/helper.js"</c> → <see langword="true"/></description></item>
    /// <item><description><c>"app.test.js"</c> → <see langword="false"/> (ignored)</description></item>
    /// <item><description><c>"app.spec.js"</c> → <see langword="false"/> (ignored)</description></item>
    /// </list>
    /// </remarks>
    public string[]? Ignore { get; set; }

    /// <summary>
    /// Retain quotes in the generated regex pattern instead of stripping them.
    /// </summary>
    public bool KeepQuotes { get; set; }

    /// <summary>
    /// Treat brackets as literal characters instead of character classes.
    /// </summary>
    /// <remarks>
    /// <para>Example with pattern <c>"[abc].js"</c>:</para>
    /// <list type="bullet">
    /// <item><description><see cref="LiteralBrackets"/> = <see langword="true"/>: <c>"[abc].js"</c> → <see langword="true"/> (literal match)</description></item>
    /// <item><description><see cref="LiteralBrackets"/> = <see langword="true"/>: <c>"a.js"</c> → <see langword="false"/></description></item>
    /// <item><description><see cref="LiteralBrackets"/> = <see langword="false"/>: <c>"a.js"</c> → <see langword="true"/> (character class)</description></item>
    /// <item><description><see cref="LiteralBrackets"/> = <see langword="false"/>: <c>"b.js"</c> → <see langword="true"/></description></item>
    /// <item><description><see cref="LiteralBrackets"/> = <see langword="false"/>: <c>"[abc].js"</c> → <see langword="false"/></description></item>
    /// </list>
    /// </remarks>
    public bool? LiteralBrackets { get; set; }

    /// <summary>
    /// Maximum allowed length for patterns. Default is 65,536 characters.
    /// Provides protection against ReDoS attacks.
    /// </summary>
    public int MaxLength { get; set; } = Constants.c_MaxLength;

    /// <summary>
    /// Disable brace expansion (<c>"{a,b}"</c> and <c>"{1..5}"</c>).
    /// </summary>
    /// <remarks>
    /// <para>Example with pattern <c>"*.{js,ts}"</c>:</para>
    /// <list type="bullet">
    /// <item><description><see cref="NoBrace"/> = <see langword="false"/>: <c>"app.js"</c> → <see langword="true"/> (brace expanded)</description></item>
    /// <item><description><see cref="NoBrace"/> = <see langword="false"/>: <c>"app.ts"</c> → <see langword="true"/></description></item>
    /// <item><description><see cref="NoBrace"/> = <see langword="false"/>: <c>"app.{js,ts}"</c> → <see langword="false"/></description></item>
    /// <item><description><see cref="NoBrace"/> = <see langword="true"/>: <c>"app.{js,ts}"</c> → <see langword="true"/> (literal match)</description></item>
    /// <item><description><see cref="NoBrace"/> = <see langword="true"/>: <c>"app.js"</c> → <see langword="false"/></description></item>
    /// </list>
    /// </remarks>
    public bool NoBrace { get; set; }

    /// <summary>
    /// Disable bracket character classes. Similar to <see cref="LiteralBrackets"/>.
    /// </summary>
    public bool? NoBracket { get; set; }

    /// <summary>
    /// Enable case-insensitive matching.
    /// </summary>
    /// <remarks>
    /// <para>Example with pattern <c>"*.js"</c>:</para>
    /// <list type="bullet">
    /// <item><description><see cref="NoCase"/> = <see langword="true"/>: <c>"App.JS"</c> → <see langword="true"/></description></item>
    /// <item><description><see cref="NoCase"/> = <see langword="true"/>: <c>"app.js"</c> → <see langword="true"/></description></item>
    /// <item><description><see cref="NoCase"/> = <see langword="false"/>: <c>"App.JS"</c> → <see langword="false"/></description></item>
    /// <item><description><see cref="NoCase"/> = <see langword="false"/>: <c>"app.js"</c> → <see langword="true"/></description></item>
    /// </list>
    /// </remarks>
    public bool NoCase { get; set; }

    /// <summary>
    /// Alias for <see cref="NoExtglob"/>. Disable extglob support.
    /// </summary>
    public bool? NoExt { get; set; }

    /// <summary>
    /// Disable extglob patterns: <c>!()</c>, <c>?()</c>, <c>+()</c>, <c>*()</c>, <c>@()</c>.
    /// </summary>
    /// <remarks>
    /// <para>Extglob syntax:</para>
    /// <list type="bullet">
    /// <item><description><c>!(pattern)</c> – match anything except</description></item>
    /// <item><description><c>?(pattern)</c> – match zero or one</description></item>
    /// <item><description><c>+(pattern)</c> – match one or more</description></item>
    /// <item><description><c>*(pattern)</c> – match zero or more</description></item>
    /// <item><description><c>@(pattern)</c> – match exactly one</description></item>
    /// </list>
    /// <para>Example with pattern <c>"+(a|b)"</c>:</para>
    /// <list type="bullet">
    /// <item><description><see cref="NoExtglob"/> = <see langword="false"/>: <c>"a"</c> → <see langword="true"/></description></item>
    /// <item><description><see cref="NoExtglob"/> = <see langword="false"/>: <c>"aa"</c> → <see langword="true"/></description></item>
    /// <item><description><see cref="NoExtglob"/> = <see langword="false"/>: <c>"ab"</c> → <see langword="true"/></description></item>
    /// <item><description><see cref="NoExtglob"/> = <see langword="false"/>: <c>"c"</c> → <see langword="false"/></description></item>
    /// </list>
    /// </remarks>
    public bool NoExtglob { get; set; }

    /// <summary>
    /// Disable globstar (<c>**</c>) matching across directories.
    /// </summary>
    /// <remarks>
    /// <para>Example with pattern <c>"**/*.js"</c>:</para>
    /// <list type="bullet">
    /// <item><description><see cref="NoGlobstar"/> = <see langword="false"/>: <c>"app.js"</c> → <see langword="true"/></description></item>
    /// <item><description><see cref="NoGlobstar"/> = <see langword="false"/>: <c>"src/app.js"</c> → <see langword="true"/></description></item>
    /// <item><description><see cref="NoGlobstar"/> = <see langword="false"/>: <c>"src/lib/app.js"</c> → <see langword="true"/></description></item>
    /// <item><description><see cref="NoGlobstar"/> = <see langword="true"/>: <c>"src/lib/app.js"</c> → <see langword="false"/> (single level only)</description></item>
    /// </list>
    /// </remarks>
    public bool NoGlobstar { get; set; }

    /// <summary>
    /// Disable negation patterns (patterns starting with <c>!</c>).
    /// </summary>
    /// <remarks>
    /// <para>Example with pattern <c>"!*.md"</c>:</para>
    /// <list type="bullet">
    /// <item><description><see cref="NoNegate"/> = <see langword="false"/>: <c>"app.js"</c> → <see langword="true"/> (not .md)</description></item>
    /// <item><description><see cref="NoNegate"/> = <see langword="false"/>: <c>"readme.md"</c> → <see langword="false"/> (negated)</description></item>
    /// <item><description><see cref="NoNegate"/> = <see langword="true"/>: <c>"!readme.md"</c> → <see langword="true"/> (literal !)</description></item>
    /// </list>
    /// </remarks>
    public bool NoNegate { get; set; }

    /// <summary>
    /// Disable regex quantifiers (<c>+</c>, <c>*</c>, <c>?</c>) in certain contexts.
    /// </summary>
    public bool NoQuantifiers { get; set; }

    /// <summary>
    /// Callback invoked for items matching ignore patterns.
    /// </summary>
    public IgnoreHandler? OnIgnore { get; set; }

    /// <summary>
    /// Callback invoked for items that match the pattern (and are not ignored).
    /// </summary>
    public MatchHandler? OnMatch { get; set; }

    /// <summary>
    /// Callback invoked for all processed items, regardless of match status.
    /// </summary>
    public ResultHandler? OnResult { get; set; }

    /// <summary>
    /// Enable POSIX character class support.
    /// </summary>
    /// <remarks>
    /// <para>Supported classes:</para>
    /// <list type="bullet">
    /// <item><description><c>[:alnum:]</c> – alphanumeric characters</description></item>
    /// <item><description><c>[:alpha:]</c> – alphabetic characters</description></item>
    /// <item><description><c>[:digit:]</c> – digits</description></item>
    /// <item><description><c>[:lower:]</c> – lowercase letters</description></item>
    /// <item><description><c>[:upper:]</c> – uppercase letters</description></item>
    /// <item><description><c>[:space:]</c> – whitespace</description></item>
    /// </list>
    /// <para>Example with pattern <c>"[[:digit:]]*"</c>:</para>
    /// <list type="bullet">
    /// <item><description><c>"123"</c> → <see langword="true"/></description></item>
    /// <item><description><c>"abc"</c> → <see langword="false"/></description></item>
    /// </list>
    /// </remarks>
    public bool Posix { get; set; }

    /// <summary>
    /// String to prepend to the generated regex pattern.
    /// </summary>
    public string? Prepend { get; set; }

    /// <summary>
    /// Treat <c>+</c> as a regex quantifier (one or more) instead of a literal character.
    /// </summary>
    public bool Regex { get; set; }

    /// <summary>
    /// Throw an error for imbalanced brackets, braces, or parentheses.
    /// Helps catch pattern errors early.
    /// </summary>
    public bool? StrictBrackets { get; set; }

    /// <summary>
    /// Prevent single stars from matching trailing slashes.
    /// </summary>
    /// <remarks>
    /// <para>Example with pattern <c>"foo/*"</c>:</para>
    /// <list type="bullet">
    /// <item><description><see cref="StrictSlashes"/> = <see langword="true"/>: <c>"foo/bar"</c> → <see langword="true"/></description></item>
    /// <item><description><see cref="StrictSlashes"/> = <see langword="true"/>: <c>"foo/bar/"</c> → <see langword="false"/></description></item>
    /// </list>
    /// </remarks>
    public bool? StrictSlashes { get; set; }

    /// <summary>
    /// Remove backslashes from escaped characters in the output.
    /// </summary>
    public bool? Unescape { get; set; }

    /// <summary>
    /// Treat backslashes as path separators (Windows-style paths).
    /// </summary>
    /// <remarks>
    /// <para>When <see langword="null"/> (default), auto-detects based on the current OS.</para>
    /// <para>Example with pattern <c>"src/**/*.js"</c>:</para>
    /// <list type="bullet">
    /// <item><description><see cref="Windows"/> = <see langword="true"/>: <c>"src/lib/app.js"</c> → <see langword="true"/></description></item>
    /// <item><description><see cref="Windows"/> = <see langword="true"/>: <c>"src\lib\app.js"</c> → <see langword="true"/></description></item>
    /// <item><description><see cref="Windows"/> = <see langword="false"/>: <c>"src/lib/app.js"</c> → <see langword="true"/></description></item>
    /// <item><description><see cref="Windows"/> = <see langword="false"/>: <c>"src\lib\app.js"</c> → <see langword="false"/></description></item>
    /// </list>
    /// </remarks>
    public bool? Windows { get; set; }

    /// <summary>
    /// Creates a copy of the options without callbacks and ignore patterns.
    /// </summary>
    /// <returns>A new <see cref="GlobbingOptions"/> instance without <see cref="Ignore"/>, <see cref="OnIgnore"/>, <see cref="OnMatch"/>, and <see cref="OnResult"/>.</returns>
    public GlobbingOptions GetIgnoreOptions()
    {
        return new GlobbingOptions
        {
            BaseName = BaseName,
            Bash = Bash,
            Capture = Capture,
            Contains = Contains,
            Cwd = Cwd,
            Debug = Debug,
            Dot = Dot,
            ExpandRange = ExpandRange,
            FailGlob = FailGlob,
            FastPaths = FastPaths,
            Flags = Flags,
            Format = Format,
            KeepQuotes = KeepQuotes,
            LiteralBrackets = LiteralBrackets,
            MaxLength = MaxLength,
            NoBrace = NoBrace,
            NoBracket = NoBracket,
            NoCase = NoCase,
            NoExt = NoExt,
            NoExtglob = NoExtglob,
            NoGlobstar = NoGlobstar,
            NoNegate = NoNegate,
            NoQuantifiers = NoQuantifiers,
            Posix = Posix,
            Prepend = Prepend,
            Regex = Regex,
            StrictBrackets = StrictBrackets,
            StrictSlashes = StrictSlashes,
            Unescape = Unescape,
            Windows = Windows
        };
    }
}
