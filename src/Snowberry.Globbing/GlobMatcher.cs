using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Snowberry.Globbing.Models;
using Snowberry.Globbing.Parsing;
using Snowberry.Globbing.Utilities;

namespace Snowberry.Globbing;

/// <summary>
/// Accurate glob pattern matching library.
/// </summary>
public static partial class GlobMatcher
{
#if NET7_0_OR_GREATER
    /// <summary>
    /// A regex that never matches anything - used as a fallback for invalid patterns.
    /// </summary>
    [GeneratedRegex("$^")]
    private static partial Regex NonMatchingRegexGenerated();
    private static readonly Regex s_NonMatchingRegex = NonMatchingRegexGenerated();
#else
    /// <summary>
    /// A regex that never matches anything - used as a fallback for invalid patterns.
    /// </summary>
    private static readonly Regex s_NonMatchingRegex = new("$^", RegexOptions.Compiled);
#endif

    /// <summary>
    /// Creates a matcher function from multiple glob patterns (OR logic).
    /// </summary>
    /// <param name="globs">Array of glob patterns.</param>
    /// <param name="options">Optional matching options.</param>
    /// <returns>A function that tests whether a string matches any of the patterns.</returns>
    /// <exception cref="ArgumentException"><paramref name="globs"/> is <see langword="null"/> or empty.</exception>
    public static MatcherHandler Create(string[] globs, GlobbingOptions? options = null)
    {
        if (globs == null || globs.Length == 0)
            throw new ArgumentException("Expected pattern to be a non-empty string");

        options ??= new GlobbingOptions();

        var fns = globs.Select(input => Create(input, options)).ToList();
        return str => MatchAny(str, fns);
    }

    /// <summary>
    /// Creates a matcher function from a single glob pattern.
    /// </summary>
    /// <param name="glob">The glob pattern.</param>
    /// <param name="options">Optional matching options.</param>
    /// <returns>A function that tests whether a string matches the pattern.</returns>
    /// <exception cref="ArgumentException"><paramref name="glob"/> is <see langword="null"/> or empty.</exception>
    public static MatcherHandler Create(string glob, GlobbingOptions? options = null)
    {
        if (string.IsNullOrEmpty(glob))
            throw new ArgumentException("Expected pattern to be a non-empty string");

        options ??= new();

        var regex = MakeRe(glob, options);

        MatcherHandler? isIgnored = null;
        if (options.Ignore != null && options.Ignore.Length > 0)
        {
            var ignoreOpts = options.GetIgnoreOptions();
            isIgnored = Create(options.Ignore, ignoreOpts);
        }

        // Per-matcher invariants computed once instead of on every match.
        bool transformPosixSlashes = ShouldConvertToPosixSlashes(options);
        var format = options.Format ?? (transformPosixSlashes ? Utils.ToPosixSlashes : null);

        return input => MatchWithCallbacks(input, glob, regex, options, isIgnored, transformPosixSlashes, format);
    }

    /// <summary>
    /// Tests an input string against a compiled regex with optional formatting.
    /// </summary>
    /// <param name="input">The string to test.</param>
    /// <param name="regex">The compiled regex.</param>
    /// <param name="options">Optional options for formatting.</param>
    /// <returns>A tuple with match status, the Match object, and the formatted output.</returns>
    public static (bool IsMatch, Match? Match, string Output) Test(
        string input,
        Regex regex,
        GlobbingOptions? options = null)
    {
        if (string.IsNullOrEmpty(input))
            return (false, null, "");

        bool transformPosixSlashes = ShouldConvertToPosixSlashes(options);
        if (options == null || (options.Format == null && !transformPosixSlashes))
            return TestDirectly(input, regex);

        var format = options.Format ?? (transformPosixSlashes ? Utils.ToPosixSlashes : null);
        string output = format == null ? input : format(input);
        var formattedMatch = regex.Match(output);

        return TestDirectly(output, regex);
    }

    /// <summary>
    /// Matches the basename of a filepath against a regex (ignores directory path).
    /// </summary>
    /// <param name="input">The full file path.</param>
    /// <param name="regex">The compiled regex.</param>
    /// <param name="windows">Whether to use Windows path separators.</param>
    /// <returns><see langword="true"/> if the basename matches.</returns>
    public static bool MatchBase(string input, Regex regex, bool windows = false)
    {
        return regex.IsMatch(Utils.BaseName(input, windows));
    }

    /// <summary>
    /// Matches the basename of a filepath against a glob pattern (ignores directory path).
    /// </summary>
    /// <param name="input">The full file path.</param>
    /// <param name="glob">The glob pattern.</param>
    /// <param name="options">Optional matching options.</param>
    /// <returns><see langword="true"/> if the basename matches.</returns>
    public static bool MatchBase(string input, string glob, GlobbingOptions? options = null)
    {
        var regex = MakeRe(glob, options);
        return MatchBase(input, regex, options?.Windows ?? RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
    }

    /// <summary>
    /// Returns <see langword="true"/> if the string matches the glob pattern.
    /// </summary>
    /// <param name="str">The string to test.</param>
    /// <param name="pattern">The glob pattern.</param>
    /// <param name="options">Optional matching options.</param>
    /// <returns><see langword="true"/> if matched.</returns>
    public static bool IsMatch(string str, string pattern, GlobbingOptions? options = null)
    {
        return Create(pattern, options)(str);
    }

    /// <summary>
    /// Returns <see langword="true"/> if the string matches any of the glob patterns (OR logic).
    /// </summary>
    /// <param name="str">The string to test.</param>
    /// <param name="patterns">Array of glob patterns.</param>
    /// <param name="options">Optional matching options.</param>
    /// <returns><see langword="true"/> if any pattern matches.</returns>
    public static bool IsMatch(string str, string[] patterns, GlobbingOptions? options = null)
    {
        return Create(patterns, options)(str);
    }

    /// <summary>
    /// Parses a glob pattern and returns its internal structure.
    /// </summary>
    /// <param name="pattern">The glob pattern to parse.</param>
    /// <param name="options">Optional parsing options.</param>
    /// <returns>A <see cref="ParseState"/> with parsed pattern data.</returns>
    public static ParseState Parse(string pattern, GlobbingOptions? options = null)
    {
        options ??= new GlobbingOptions();
        options.FastPaths = false;
        var parser = new GlobParser(options);
        return parser.Parse(pattern);
    }

    /// <summary>
    /// Parses multiple glob patterns.
    /// </summary>
    /// <param name="patterns">Array of glob patterns.</param>
    /// <param name="options">Optional parsing options.</param>
    /// <returns>Array of <see cref="ParseState"/> objects.</returns>
    public static ParseState[] Parse(string[] patterns, GlobbingOptions? options = null)
    {
        return [.. patterns.Select(p => Parse(p, options))];
    }

    /// <summary>
    /// Scans a glob pattern and returns structural information without compiling to regex.
    /// </summary>
    /// <param name="input">The glob pattern to scan.</param>
    /// <param name="options">Optional scan options.</param>
    /// <returns>A <see cref="ScanResult"/> with pattern structure info.</returns>
    public static ScanResult Scan(string input, ScanOptions? options = null)
    {
        var scanner = new GlobScanner(input, options);
        return scanner.Scan();
    }

    /// <summary>
    /// Generates a regular expression pattern string based on the specified parse state and globbing options.
    /// </summary>
    /// <remarks>The returned regular expression pattern may include anchors or negation based on the provided
    /// options and parse state. The pattern is suitable for use with .NET regular expression APIs.</remarks>
    /// <param name="state">The parse state containing the output pattern and negation flag to use when constructing the regular expression.</param>
    /// <param name="options">The globbing options that influence how the regular expression is generated. If <see langword="null"/>, default options are used.</param>
    /// <returns>A string containing the generated regular expression pattern that reflects the provided parse state and options.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="state"/> is <see langword="null"/>.</exception>
    public static string GenerateRegex(ParseState state, GlobbingOptions? options = null)
    {
        _ = state ?? throw new ArgumentNullException(nameof(state));
        options ??= new GlobbingOptions();

        string prepend = options.Contains ? "" : "^";
        string append = options.Contains ? "" : "$";
        string source = $"{prepend}(?:{state.Output}){append}";

        if (state.Negated)
            source = $"^(?!{source}).*$";

        return source;
    }

    /// <summary>
    /// Generates a regular expression pattern that matches the specified glob pattern.
    /// </summary>
    /// <param name="input">The glob pattern to convert to a regular expression.</param>
    /// <param name="options">An optional set of options that control globbing behavior. If <see langword="null"/>, default options are used.</param>
    /// <returns>A string containing the regular expression pattern equivalent to the specified glob pattern.</returns>
    /// <exception cref="ArgumentException"><paramref name="input"/> is <see langword="null"/> or empty.</exception>
    public static string GenerateRegex(string input, GlobbingOptions? options = null)
    {
        if (string.IsNullOrEmpty(input))
            throw new ArgumentException("Expected a non-empty string");

        options ??= new GlobbingOptions();

        var parser = new GlobParser(options);
        var parsed = parser.Parse(input);

        string source = GenerateRegex(parsed, options);

        // GenerateRegex(state) reads only Output/Negated; the ParseState is discarded here.
        parser.RecycleTokens();

        return source;
    }

    /// <summary>
    /// Compiles a regex from a parsed <see cref="ParseState"/>.
    /// </summary>
    /// <param name="state">The parsed pattern state.</param>
    /// <param name="options">Optional compilation options.</param>
    /// <returns>A compiled <see cref="Regex"/> built from the regular expression generated for <paramref name="state"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="state"/> is <see langword="null"/>.</exception>
    public static Regex CompileRe(ParseState state, GlobbingOptions? options = null)
    {
        _ = state ?? throw new ArgumentNullException(nameof(state));

        options ??= new GlobbingOptions();

        string source = GenerateRegex(state, options);
        return ToRegex(source, options);
    }

    /// <summary>
    /// Creates a compiled regex from a glob pattern.
    /// </summary>
    /// <param name="input">The glob pattern (e.g., <c>"*.js"</c>, <c>"src/**/*.ts"</c>).</param>
    /// <param name="options">Optional regex generation options.</param>
    /// <returns>A <see cref="Regex"/> compiled from the glob pattern. <see cref="RegexOptions.Compiled"/> is applied only when <see cref="GlobbingOptions.CompiledRegex"/> is <see langword="true"/>.</returns>
    /// <exception cref="ArgumentException"><paramref name="input"/> is <see langword="null"/> or empty.</exception>
    public static Regex MakeRe(string input, GlobbingOptions? options = null)
    {
        if (string.IsNullOrEmpty(input))
            throw new ArgumentException("Expected a non-empty string");

        options ??= new GlobbingOptions();

        var parser = new GlobParser(options);
        var parsed = parser.Parse(input);
        var regex = CompileRe(parsed, options);

        // Tokens are only needed to build parsed.Output (consumed by CompileRe above); the
        // ParseState is discarded here, so recycle them for the next parse.
        parser.RecycleTokens();

        return regex;
    }

    /// <summary>
    /// Compiles a regex pattern string with specified options.
    /// </summary>
    /// <param name="source">The regex pattern string.</param>
    /// <param name="options">Optional options with flags.</param>
    /// <returns>A compiled <see cref="Regex"/>. On failure, returns a non-matching regex unless <see cref="GlobbingOptions.Debug"/> is <see langword="true"/>.</returns>
    public static Regex ToRegex(string source, GlobbingOptions? options = null)
    {
        try
        {
            options ??= new GlobbingOptions();

            var regexOptions = options.CompiledRegex ? RegexOptions.Compiled : RegexOptions.None;

            if (options.Flags != RegexFlags.None)
            {
                if (options.Flags.HasFlag(RegexFlags.IgnoreCase))
                    regexOptions |= RegexOptions.IgnoreCase;
                if (options.Flags.HasFlag(RegexFlags.MultiLine))
                    regexOptions |= RegexOptions.Multiline;
                if (options.Flags.HasFlag(RegexFlags.SingleLine))
                    regexOptions |= RegexOptions.Singleline;
            }
            else if (options.NoCase)
            {
                regexOptions |= RegexOptions.IgnoreCase;
            }

            return new Regex(source, regexOptions);
        }
        catch
        {
            if (options?.Debug == true)
                throw;

            return s_NonMatchingRegex;
        }
    }

    private static bool MatchAny(string str, List<MatcherHandler> matchers)
    {
        for (int i = 0; i < matchers.Count; i++)
        {
            var isMatch = matchers[i];
            if (isMatch(str))
                return true;
        }

        return false;
    }

    private static bool MatchWithCallbacks(
        string input,
        string glob,
        Regex regex,
        GlobbingOptions opts,
        MatcherHandler? isIgnored,
        bool transformPosixSlashes,
        FormatHandler? format)
    {
        bool hasCallbacks = opts.OnResult != null || opts.OnMatch != null || opts.OnIgnore != null;

        if (isIgnored != null && isIgnored(input))
        {
            if (opts.OnIgnore != null || opts.OnResult != null)
            {
                var ignoreMatchResult = new MatchResult
                {
                    Glob = glob,
                    Regex = regex,
                    TransformToPosixSlashes = transformPosixSlashes,
                    Input = input,
                    Output = null,
                    Match = null,
                    IsMatch = false,
                    Options = opts
                };

                opts.OnResult?.Invoke(ignoreMatchResult);
                opts.OnIgnore?.Invoke(ignoreMatchResult);
            }

            return false;
        }

        bool directMatch = input == glob;
        string output = format != null ? format(input) : input;

        // NOTE(VNC): Check after formatting.
        if (!directMatch)
            directMatch = output == glob;

        // Fast path: without callbacks only a boolean is needed, so avoid allocating a
        // Match object. regex.Match allocates a Match on success (returns the shared
        // Match.Empty singleton on failure); regex.IsMatch allocates nothing.
        if (!hasCallbacks)
        {
            if (directMatch)
                return true;

            if (opts.BaseName)
                return MatchBase(output, regex, !transformPosixSlashes);

            return !string.IsNullOrEmpty(output) && regex.IsMatch(output);
        }

        (bool IsMatch, Match? Match, string Output) result;
        if (directMatch)
        {
            result = (true, null, output);
        }
        else if (opts.BaseName)
        {
            bool baseMatch = MatchBase(output, regex, !transformPosixSlashes);
            result = (baseMatch, null, output);
        }
        else
        {
            result = TestDirectly(output, regex);
        }

        var matchResult = new MatchResult
        {
            Glob = glob,
            Regex = regex,
            TransformToPosixSlashes = transformPosixSlashes,
            Input = input,
            Output = result.Output,
            Match = result.Match,
            IsMatch = result.IsMatch,
            Options = opts
        };

        opts.OnResult?.Invoke(matchResult);

        if (!result.IsMatch)
            return false;

        if (isIgnored != null && isIgnored(input))
        {
            opts.OnIgnore?.Invoke(matchResult);
            return false;
        }

        opts.OnMatch?.Invoke(matchResult);
        return true;
    }

    /// <summary>
    /// Tests an input string directly against a compiled regex, without any path formatting.
    /// </summary>
    /// <param name="input">The string to test.</param>
    /// <param name="regex">The compiled regex to test against.</param>
    /// <returns>
    /// A tuple containing the match status, the <see cref="Match"/> object (or <see langword="null"/> when
    /// <paramref name="input"/> is <see langword="null"/> or empty), and the tested input string.
    /// </returns>
    public static (bool IsMatch, Match? Match, string Output) TestDirectly(
        string input,
        Regex regex)
    {
        if (string.IsNullOrEmpty(input))
            return (false, null, "");

        var match = regex.Match(input);
        return (match.Success, match, input);
    }

    /// <summary>
    /// Determines whether input paths should have backslashes converted to forward slashes for the given options.
    /// </summary>
    /// <param name="options">The matching options, or <see langword="null"/> to decide based on the current operating system.</param>
    /// <returns>
    /// <see langword="true"/> if backslashes should be converted to forward slashes; otherwise, <see langword="false"/>.
    /// When <paramref name="options"/> or <see cref="GlobbingOptions.Windows"/> is <see langword="null"/>, the current
    /// operating system determines the result.
    /// </returns>
    public static bool ShouldConvertToPosixSlashes(GlobbingOptions? options)
    {
        if (options == null || options.Windows == null)
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        return options.Windows.Value;
    }
}
