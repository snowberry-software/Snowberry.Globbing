using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Snowberry.Globbing.Syntax;
using Snowberry.Globbing.Utilities;

namespace Snowberry.Globbing;

/// <summary>
/// A compiled glob pattern, or a set of patterns of which any may match.
/// </summary>
/// <remarks>
/// <para>
/// Instances are immutable and thread-safe. Create one and reuse it to match many inputs; the static
/// <see cref="IsMatch(string, string, GlobOptions?)"/> overloads that take a single pattern keep a small cache of
/// compiled patterns for one-off checks.
/// </para>
/// <para>
/// Matching follows picomatch semantics. Before matching, an input is transformed by
/// <see cref="GlobOptions.InputNormalizer"/> or, without one, has its backslashes converted to <c>/</c> when
/// <see cref="GlobOptions.PathStyle"/> calls for it. An input that equals a pattern, before or after this
/// normalization, always matches that pattern. <see cref="ToRegexString"/> returns a regex that behaves the same in
/// .NET, JavaScript and PostgreSQL 17 or later.
/// </para>
/// </remarks>
public sealed class Glob
{
    private const int c_CacheCapacity = 256;

    private static readonly ConcurrentDictionary<(string Pattern, GlobOptions Options), Glob> s_Cache = new();

    private readonly string[] _patterns;
    private readonly CompiledPattern[] _compiled;
    private readonly Glob? _ignore;
    private readonly bool _convertSeparators;
    private Regex? _combinedRegex;

    /// <summary>
    /// Initializes a new instance of the <see cref="Glob"/> class for a single pattern with <see cref="GlobOptions.Default"/>.
    /// </summary>
    /// <param name="pattern">The glob pattern, for example <c>src/**/*.cs</c>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="pattern"/> is <see langword="null"/>.</exception>
    /// <exception cref="GlobParseException"><paramref name="pattern"/> is empty (<see cref="GlobParseError.EmptyPattern"/>) or cannot be compiled.</exception>
    public Glob(string pattern)
        : this(pattern, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Glob"/> class for a single pattern.
    /// </summary>
    /// <param name="pattern">The glob pattern, for example <c>src/**/*.cs</c>.</param>
    /// <param name="options">The options, or <see langword="null"/> for <see cref="GlobOptions.Default"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="pattern"/> is <see langword="null"/>.</exception>
    /// <exception cref="GlobParseException"><paramref name="pattern"/> or a <see cref="GlobOptions.IgnorePatterns"/> entry is <see langword="null"/> or empty (<see cref="GlobParseError.EmptyPattern"/>), or cannot be compiled.</exception>
    public Glob(string pattern, GlobOptions? options)
        : this([ValidatePattern(pattern, nameof(pattern))], options, nameof(pattern))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Glob"/> class with <see cref="GlobOptions.Default"/> that matches an
    /// input when any of <paramref name="patterns"/> matches.
    /// </summary>
    /// <param name="patterns">The glob patterns.</param>
    /// <exception cref="ArgumentNullException"><paramref name="patterns"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="patterns"/> contains no patterns.</exception>
    /// <exception cref="GlobParseException">A pattern is <see langword="null"/> or empty (<see cref="GlobParseError.EmptyPattern"/>), or cannot be compiled.</exception>
    public Glob(IEnumerable<string> patterns)
        : this(patterns, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Glob"/> class that matches an input when any of
    /// <paramref name="patterns"/> matches.
    /// </summary>
    /// <param name="patterns">The glob patterns.</param>
    /// <param name="options">The options, or <see langword="null"/> for <see cref="GlobOptions.Default"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="patterns"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="patterns"/> contains no patterns.</exception>
    /// <exception cref="GlobParseException">A pattern or a <see cref="GlobOptions.IgnorePatterns"/> entry is <see langword="null"/> or empty (<see cref="GlobParseError.EmptyPattern"/>), or cannot be compiled.</exception>
    public Glob(IEnumerable<string> patterns, GlobOptions? options)
        : this(ValidatePatterns(patterns, nameof(patterns)), options, nameof(patterns))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Glob"/> class that compiles already validated <paramref name="patterns"/> and any ignore patterns of the options.
    /// </summary>
    /// <param name="patterns">The non-empty array of non-empty patterns.</param>
    /// <param name="options">The options, or <see langword="null"/> for <see cref="GlobOptions.Default"/>.</param>
    /// <param name="paramName">The name of the argument that supplied <paramref name="patterns"/>, reported by <see cref="ArgumentException.ParamName"/>.</param>
    /// <exception cref="GlobParseException">A pattern or an ignore pattern is <see langword="null"/> or empty (<see cref="GlobParseError.EmptyPattern"/>), or cannot be compiled.</exception>
    private Glob(string[] patterns, GlobOptions? options, string paramName)
    {
        Options = options ?? GlobOptions.Default;
        _patterns = patterns;
        _convertSeparators = Options.PathStyle switch
        {
            GlobPathStyle.Windows => true,
            GlobPathStyle.Posix => false,
            _ => RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
        };

        var regexOptions = Options.RegexOptions | (Options.IgnoreCase ? RegexOptions.IgnoreCase | RegexOptions.CultureInvariant : RegexOptions.None);
        _compiled = new CompiledPattern[patterns.Length];
        for (int i = 0; i < patterns.Length; i++)
        {
            try
            {
                _compiled[i] = new CompiledPattern(patterns[i], Options, regexOptions);
            }
            catch (GlobParseException e)
            {
                throw e.ForParameter(paramName);
            }
        }

        if (Options.IgnorePatterns.Count > 0)
        {
            string ignoreName = nameof(GlobOptions.IgnorePatterns);
            _ignore = new Glob(ValidatePatterns(Options.IgnorePatterns, ignoreName), Options with { IgnorePatterns = [] }, ignoreName);
        }
    }

    /// <summary>
    /// Gets the patterns of this glob.
    /// </summary>
    public IReadOnlyList<string> Patterns => _patterns;

    /// <summary>
    /// Gets the options this glob was compiled with.
    /// </summary>
    public GlobOptions Options { get; }

    /// <summary>
    /// Determines whether <paramref name="input"/> matches <paramref name="pattern"/>, using <see cref="GlobOptions.Default"/>.
    /// </summary>
    /// <param name="input">The input to match, typically a path.</param>
    /// <param name="pattern">The glob pattern.</param>
    /// <returns><see langword="true"/> if <paramref name="input"/> matches; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="input"/> or <paramref name="pattern"/> is <see langword="null"/>.</exception>
    /// <exception cref="GlobParseException"><paramref name="pattern"/> is empty (<see cref="GlobParseError.EmptyPattern"/>) or cannot be compiled.</exception>
    public static bool IsMatch(string input, string pattern)
    {
        return IsMatch(input, pattern, null);
    }

    /// <summary>
    /// Determines whether <paramref name="input"/> matches <paramref name="pattern"/>.
    /// </summary>
    /// <remarks>Compiled patterns are cached; for repeated matching, create a <see cref="Glob"/> and reuse it.</remarks>
    /// <param name="input">The input to match, typically a path.</param>
    /// <param name="pattern">The glob pattern.</param>
    /// <param name="options">The options, or <see langword="null"/> for <see cref="GlobOptions.Default"/>.</param>
    /// <returns><see langword="true"/> if <paramref name="input"/> matches <paramref name="pattern"/> and no ignore pattern; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="input"/> or <paramref name="pattern"/> is <see langword="null"/>.</exception>
    /// <exception cref="GlobParseException"><paramref name="pattern"/> or a <see cref="GlobOptions.IgnorePatterns"/> entry is <see langword="null"/> or empty (<see cref="GlobParseError.EmptyPattern"/>), or cannot be compiled.</exception>
    public static bool IsMatch(string input, string pattern, GlobOptions? options)
    {
        Guard.NotNull(input);

        return GetOrCreate(pattern, options).IsMatch(input);
    }

    /// <summary>
    /// Determines whether <paramref name="input"/> matches <paramref name="pattern"/>, using <see cref="GlobOptions.Default"/>.
    /// </summary>
    /// <param name="input">The input to match, typically a path.</param>
    /// <param name="pattern">The glob pattern.</param>
    /// <returns><see langword="true"/> if <paramref name="input"/> matches; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="pattern"/> is <see langword="null"/>.</exception>
    /// <exception cref="GlobParseException"><paramref name="pattern"/> is empty (<see cref="GlobParseError.EmptyPattern"/>) or cannot be compiled.</exception>
    public static bool IsMatch(ReadOnlySpan<char> input, string pattern)
    {
        return IsMatch(input, pattern, null);
    }

    /// <summary>
    /// Determines whether <paramref name="input"/> matches <paramref name="pattern"/>.
    /// </summary>
    /// <remarks>Compiled patterns are cached; for repeated matching, create a <see cref="Glob"/> and reuse it.</remarks>
    /// <param name="input">The input to match, typically a path.</param>
    /// <param name="pattern">The glob pattern.</param>
    /// <param name="options">The options, or <see langword="null"/> for <see cref="GlobOptions.Default"/>.</param>
    /// <returns><see langword="true"/> if <paramref name="input"/> matches <paramref name="pattern"/> and no ignore pattern; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="pattern"/> is <see langword="null"/>.</exception>
    /// <exception cref="GlobParseException"><paramref name="pattern"/> or a <see cref="GlobOptions.IgnorePatterns"/> entry is <see langword="null"/> or empty (<see cref="GlobParseError.EmptyPattern"/>), or cannot be compiled.</exception>
    public static bool IsMatch(ReadOnlySpan<char> input, string pattern, GlobOptions? options)
    {
        return GetOrCreate(pattern, options).IsMatch(input);
    }

    /// <summary>
    /// Determines whether <paramref name="input"/> matches any of <paramref name="patterns"/>, using <see cref="GlobOptions.Default"/>.
    /// </summary>
    /// <param name="input">The input to match, typically a path.</param>
    /// <param name="patterns">The glob patterns.</param>
    /// <returns><see langword="true"/> if <paramref name="input"/> matches any pattern; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="input"/> or <paramref name="patterns"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="patterns"/> contains no patterns.</exception>
    /// <exception cref="GlobParseException">A pattern is <see langword="null"/> or empty (<see cref="GlobParseError.EmptyPattern"/>), or cannot be compiled.</exception>
    public static bool IsMatch(string input, IEnumerable<string> patterns)
    {
        return IsMatch(input, patterns, null);
    }

    /// <summary>
    /// Determines whether <paramref name="input"/> matches any of <paramref name="patterns"/>.
    /// </summary>
    /// <remarks>The patterns are compiled on every call; for repeated matching, create a <see cref="Glob"/> and reuse it.</remarks>
    /// <param name="input">The input to match, typically a path.</param>
    /// <param name="patterns">The glob patterns.</param>
    /// <param name="options">The options, or <see langword="null"/> for <see cref="GlobOptions.Default"/>.</param>
    /// <returns><see langword="true"/> if <paramref name="input"/> matches any pattern and no ignore pattern; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="input"/> or <paramref name="patterns"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="patterns"/> contains no patterns.</exception>
    /// <exception cref="GlobParseException">A pattern or a <see cref="GlobOptions.IgnorePatterns"/> entry is <see langword="null"/> or empty (<see cref="GlobParseError.EmptyPattern"/>), or cannot be compiled.</exception>
    public static bool IsMatch(string input, IEnumerable<string> patterns, GlobOptions? options)
    {
        Guard.NotNull(input);

        return new Glob(patterns, options).IsMatch(input);
    }

    /// <summary>
    /// Attempts to compile <paramref name="pattern"/>.
    /// </summary>
    /// <param name="pattern">The glob pattern.</param>
    /// <param name="options">The options, or <see langword="null"/> for <see cref="GlobOptions.Default"/>.</param>
    /// <param name="glob">When this method returns <see langword="true"/>, the compiled glob; otherwise, <see langword="null"/>.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="pattern"/> is neither <see langword="null"/> nor empty and it and every
    /// <see cref="GlobOptions.IgnorePatterns"/> entry is a non-empty pattern that compiles; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool TryCreate([NotNullWhen(true)] string? pattern, GlobOptions? options, [NotNullWhen(true)] out Glob? glob)
    {
        return TryCreate(pattern, options, out glob, out _);
    }

    /// <summary>
    /// Attempts to compile <paramref name="pattern"/> and reports why it failed.
    /// </summary>
    /// <remarks>
    /// Every failure is described by <see cref="GlobParseException.Error"/>, so callers can switch on it: a
    /// <see langword="null"/> or empty pattern or ignore pattern is <see cref="GlobParseError.EmptyPattern"/>, and a pattern
    /// that cannot be compiled reports its own code, <see cref="GlobParseException.Offset"/> and
    /// <see cref="GlobParseException.Pattern"/>. <see cref="ArgumentException.ParamName"/> names the source of the pattern:
    /// <c>pattern</c> or <see cref="GlobOptions.IgnorePatterns"/>.
    /// </remarks>
    /// <param name="pattern">The glob pattern.</param>
    /// <param name="options">The options, or <see langword="null"/> for <see cref="GlobOptions.Default"/>.</param>
    /// <param name="glob">When this method returns <see langword="true"/>, the compiled glob; otherwise, <see langword="null"/>.</param>
    /// <param name="error">When this method returns <see langword="false"/>, why the pattern was rejected; otherwise, <see langword="null"/>.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="pattern"/> is neither <see langword="null"/> nor empty and it and every
    /// <see cref="GlobOptions.IgnorePatterns"/> entry is a non-empty pattern that compiles; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool TryCreate(
        [NotNullWhen(true)] string? pattern,
        GlobOptions? options,
        [NotNullWhen(true)] out Glob? glob,
        [NotNullWhen(false)] out GlobParseException? error)
    {
        glob = null;
        if (string.IsNullOrEmpty(pattern))
        {
            error = GlobParseException.EmptyPattern(nameof(pattern));
            return false;
        }

        try
        {
            glob = new Glob(pattern!, options);
            error = null;
            return true;
        }
        catch (GlobParseException e)
        {
            error = e;
            return false;
        }
    }

    /// <summary>
    /// Describes the structure of <paramref name="pattern"/> without compiling it to a regex.
    /// </summary>
    /// <remarks>
    /// The pattern is read with the same rules as matching, except that unbalanced delimiters are always literal text,
    /// even with <see cref="GlobOptions.StrictBrackets"/>, and <see cref="GlobOptions.MaxPatternLength"/> is not
    /// checked. Syntax disabled in <paramref name="options"/> is not reported.
    /// </remarks>
    /// <param name="pattern">The glob pattern.</param>
    /// <param name="options">The options, or <see langword="null"/> for <see cref="GlobOptions.Default"/>.</param>
    /// <returns>The structure of <paramref name="pattern"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="pattern"/> is <see langword="null"/>.</exception>
    /// <exception cref="GlobParseException"><paramref name="pattern"/> nests groups, braces or extended globs too deeply.</exception>
    public static GlobInfo Analyze(string pattern, GlobOptions? options = null)
    {
        Guard.NotNull(pattern);

        return GlobAnalyzer.Analyze(pattern, options ?? GlobOptions.Default);
    }

    /// <summary>
    /// Determines whether <paramref name="input"/> matches this glob.
    /// </summary>
    /// <param name="input">The input to match, typically a path.</param>
    /// <returns><see langword="true"/> if <paramref name="input"/> matches a pattern and no ignore pattern; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="input"/> is <see langword="null"/>.</exception>
    public bool IsMatch(string input)
    {
        Guard.NotNull(input);

        return FindPattern(input, Normalize(input)) != null && !IsIgnored(input);
    }

    /// <summary>
    /// Determines whether <paramref name="input"/> matches this glob.
    /// </summary>
    /// <param name="input">The input to match, typically a path.</param>
    /// <returns><see langword="true"/> if <paramref name="input"/> matches a pattern and no ignore pattern; otherwise, <see langword="false"/>.</returns>
    public bool IsMatch(ReadOnlySpan<char> input)
    {
#if NET7_0_OR_GREATER
        if (Options.InputNormalizer == null && !Options.MatchFileNameOnly && _ignore == null
            && (!_convertSeparators || input.IndexOf('\\') < 0))
        {
            if (input.IsEmpty)
                return false;

            foreach (var compiled in _compiled)
            {
                if (input.SequenceEqual(compiled.Pattern.AsSpan()) || compiled.IsMatch(input))
                    return true;
            }

            return false;
        }
#endif

        return IsMatch(input.ToString());
    }

    /// <summary>
    /// Matches <paramref name="input"/> against this glob and describes the outcome.
    /// </summary>
    /// <param name="input">The input to match, typically a path.</param>
    /// <returns>The result of the match, including whether an ignore pattern excluded the input.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="input"/> is <see langword="null"/>.</exception>
    public GlobMatch Match(string input)
    {
        Guard.NotNull(input);

        string normalized = Normalize(input);
        string? pattern = FindPattern(input, normalized);
        if (pattern == null)
            return new GlobMatch(false, false, input, normalized, null);

        bool ignored = IsIgnored(input);
        return new GlobMatch(!ignored, ignored, input, normalized, pattern);
    }

    /// <summary>
    /// Returns the inputs that match this glob.
    /// </summary>
    /// <param name="inputs">The inputs to filter, typically paths.</param>
    /// <returns>The elements of <paramref name="inputs"/> that match, in their original order, evaluated lazily as the result is enumerated.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="inputs"/> is <see langword="null"/>, or, during enumeration, contains a <see langword="null"/> element.</exception>
    public IEnumerable<string> Filter(IEnumerable<string> inputs)
    {
        Guard.NotNull(inputs);

        return inputs.Where(IsMatch);
    }

    /// <summary>
    /// Returns a regex equivalent to the patterns of this glob, with <see cref="GlobOptions.RegexOptions"/> applied and,
    /// when <see cref="GlobOptions.IgnoreCase"/> is set, <see cref="RegexOptions.IgnoreCase"/> and
    /// <see cref="RegexOptions.CultureInvariant"/>.
    /// </summary>
    /// <remarks>
    /// The regex does not apply <see cref="GlobOptions.IgnorePatterns"/>, <see cref="GlobOptions.MatchFileNameOnly"/>,
    /// <see cref="GlobOptions.InputNormalizer"/>, separator normalization or the rule that an input equal to a pattern
    /// matches; normalize inputs yourself when using it directly.
    /// </remarks>
    /// <returns>The regex, created on first use and cached.</returns>
    public Regex ToRegex()
    {
        if (_compiled.Length == 1)
            return _compiled[0].Regex;

        return _combinedRegex ??= new Regex(ToRegexString(), _compiled[0].Regex.Options);
    }

    /// <summary>
    /// Returns the source of a regex equivalent to the patterns of this glob.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The source behaves the same in .NET, JavaScript (<c>new RegExp(source)</c>, without flags) and PostgreSQL 17 or
    /// later (the <c>~</c> operator). When Npgsql translates
    /// <see cref="Regex.IsMatch(string, string, RegexOptions)"/> to SQL, pass <see cref="RegexOptions.Singleline"/> so
    /// that no newline-sensitive flag is added. Fragments returned by <see cref="GlobOptions.BraceRangeExpander"/> and
    /// regex syntax written into the pattern, such as <c>\d</c>, are emitted as written and are portable only if they
    /// are portable themselves.
    /// </para>
    /// <para>
    /// The source does not encode <see cref="GlobOptions.RegexOptions"/>, <see cref="GlobOptions.IgnoreCase"/>,
    /// <see cref="GlobOptions.IgnorePatterns"/>, <see cref="GlobOptions.MatchFileNameOnly"/>,
    /// <see cref="GlobOptions.InputNormalizer"/>, separator normalization or the rule that an input equal to a pattern
    /// matches.
    /// </para>
    /// </remarks>
    /// <returns>The regex source.</returns>
    public string ToRegexString()
    {
        return _compiled.Length == 1 ? _compiled[0].Source : string.Join("|", _compiled.Select(c => string.Concat("(?:", c.Source, ")")));
    }

    /// <inheritdoc/>
    /// <returns>The pattern of this glob, or its patterns separated by <c>", "</c>.</returns>
    public override string ToString()
    {
        return string.Join(", ", _patterns);
    }

    /// <summary>
    /// Gets the cached glob for <paramref name="pattern"/> and <paramref name="options"/>, compiling and caching it on a miss.
    /// </summary>
    /// <remarks>The static cache is cleared when it has reached its capacity before a new glob is added.</remarks>
    /// <param name="pattern">The glob pattern.</param>
    /// <param name="options">The options, or <see langword="null"/> for <see cref="GlobOptions.Default"/>.</param>
    /// <returns>The glob for the pattern and options.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="pattern"/> is <see langword="null"/>.</exception>
    /// <exception cref="GlobParseException"><paramref name="pattern"/> or a <see cref="GlobOptions.IgnorePatterns"/> entry is <see langword="null"/> or empty (<see cref="GlobParseError.EmptyPattern"/>), or cannot be compiled.</exception>
    private static Glob GetOrCreate(string pattern, GlobOptions? options)
    {
        var key = (ValidatePattern(pattern, nameof(pattern)), options ?? GlobOptions.Default);
        if (s_Cache.TryGetValue(key, out var glob))
            return glob;

        glob = new Glob(key.Item1, key.Item2);
        if (s_Cache.Count >= c_CacheCapacity)
            s_Cache.Clear();

        s_Cache[key] = glob;
        return glob;
    }

    /// <summary>
    /// Checks that <paramref name="pattern"/> is neither <see langword="null"/> nor empty.
    /// </summary>
    /// <param name="pattern">The pattern to validate.</param>
    /// <param name="paramName">The parameter name reported in exceptions.</param>
    /// <returns><paramref name="pattern"/>, unchanged.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="pattern"/> is <see langword="null"/>.</exception>
    /// <exception cref="GlobParseException"><paramref name="pattern"/> is empty (<see cref="GlobParseError.EmptyPattern"/>).</exception>
    private static string ValidatePattern(string pattern, string paramName)
    {
        Guard.NotNull(pattern, paramName);

        if (pattern.Length == 0)
            throw GlobParseException.EmptyPattern(paramName);

        return pattern;
    }

    /// <summary>
    /// Copies <paramref name="patterns"/> to an array after checking that there is at least one and that none is <see langword="null"/> or empty.
    /// </summary>
    /// <param name="patterns">The patterns to validate.</param>
    /// <param name="paramName">The parameter name reported in exceptions.</param>
    /// <returns>An array of the patterns.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="patterns"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="patterns"/> contains no patterns.</exception>
    /// <exception cref="GlobParseException">A pattern is <see langword="null"/> or empty (<see cref="GlobParseError.EmptyPattern"/>).</exception>
    private static string[] ValidatePatterns(IEnumerable<string> patterns, string paramName)
    {
        Guard.NotNull(patterns, paramName);

        string[] result = [.. patterns];
        if (result.Length == 0)
            throw new ArgumentException("At least one pattern is required.", paramName);

        foreach (string pattern in result)
        {
            if (string.IsNullOrEmpty(pattern))
                throw GlobParseException.EmptyPattern(paramName);
        }

        return result;
    }

    /// <summary>
    /// Converts <paramref name="input"/> to the form the patterns are matched against.
    /// </summary>
    /// <param name="input">The input to normalize.</param>
    /// <returns>The result of <see cref="GlobOptions.InputNormalizer"/> if set; otherwise, <paramref name="input"/> with backslashes converted to <c>/</c> when separators are converted for this glob.</returns>
    private string Normalize(string input)
    {
        if (Options.InputNormalizer != null)
            return Options.InputNormalizer(input);

        return _convertSeparators ? PathUtilities.ToPosixSlashes(input) : input;
    }

    /// <summary>
    /// Finds the first pattern that matches the input, ignoring the ignore patterns.
    /// </summary>
    /// <remarks>
    /// A pattern that equals <paramref name="input"/> or <paramref name="normalized"/> matches without running its regex.
    /// With <see cref="GlobOptions.MatchFileNameOnly"/>, only the base name of <paramref name="normalized"/> is matched.
    /// </remarks>
    /// <param name="input">The original input.</param>
    /// <param name="normalized">The input as returned by <see cref="Normalize"/>.</param>
    /// <returns>The first matching pattern, or <see langword="null"/> if none matches.</returns>
    private string? FindPattern(string input, string normalized)
    {
        string? fileName = Options.MatchFileNameOnly ? PathUtilities.BaseName(normalized, _convertSeparators) : null;
        foreach (var compiled in _compiled)
        {
            string pattern = compiled.Pattern;
            if (input == pattern || (!ReferenceEquals(normalized, input) && normalized == pattern))
                return pattern;

            if (normalized.Length > 0 && compiled.IsMatch(fileName ?? normalized))
                return pattern;
        }

        return null;
    }

    /// <summary>
    /// Determines whether the ignore patterns of this glob match <paramref name="input"/>.
    /// </summary>
    /// <param name="input">The original input, before normalization.</param>
    /// <returns><see langword="true"/> if there are ignore patterns and one matches <paramref name="input"/>; otherwise, <see langword="false"/>.</returns>
    private bool IsIgnored(string input)
    {
        return _ignore != null && _ignore.IsMatch(input);
    }
}