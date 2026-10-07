using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
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
    private const int c_StackNormalizeLength = 256;
    private const int c_StackCandidateWords = 32;

    private static readonly ConcurrentDictionary<(string Pattern, GlobOptions Options), Glob> s_Cache = new(GlobCacheKeyComparer.Instance);

    private static readonly (string Pattern, GlobOptions Options)[] s_CacheKeys = new (string, GlobOptions)[c_CacheCapacity];

    private static readonly object s_CacheLock = new();

    private static int s_CacheCount;

    private static uint s_CacheSeed = 2463534242;

    private readonly CompiledPattern[] _compiled;
    private readonly bool _convertSeparators;
    private readonly Glob? _ignore;
    private readonly PatternIndex? _index;
    private readonly bool _matchesSpans;
    private readonly CompiledPattern? _single;

    private readonly string[] _patterns;
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
        ulong[][]?[] keyWindows = new ulong[][]?[patterns.Length];
        for (int i = 0; i < patterns.Length; i++)
        {
            try
            {
                _compiled[i] = new CompiledPattern(patterns[i], Options, regexOptions, findKeys: patterns.Length > 1, out keyWindows[i]);
            }
            catch (GlobParseException e)
            {
                throw e.ForParameter(paramName);
            }
        }

        _index = PatternIndex.Create(_compiled, keyWindows);

        // With IgnorePatternWhitespace, a "#" in one pattern comments out the rest of the combined regex.
        if (patterns.Length > 1 && (regexOptions & RegexOptions.IgnorePatternWhitespace) != 0)
        {
            try
            {
                _combinedRegex = CreateCombinedRegex();
            }
            catch (ArgumentException e)
            {
                throw new GlobParseException(ToString(), GlobParseError.InvalidPattern, -1, $"The patterns do not combine into a valid regular expression: {e.Message}", e).ForParameter(paramName);
            }
        }

        if (Options.IgnorePatterns.Count > 0)
        {
            string ignoreName = nameof(GlobOptions.IgnorePatterns);
            _ignore = new Glob(ValidatePatterns(Options.IgnorePatterns, ignoreName), Options with { IgnorePatterns = [] }, ignoreName);
        }

        _matchesSpans = Options.InputNormalizer == null && Array.TrueForAll(_compiled, c => c.MatchesSpanWithoutString) && (_ignore == null || _ignore._matchesSpans);

        // One pattern whose input needs no normalization beyond separators is matched directly.
        if (_compiled.Length == 1 && _ignore == null && Options.InputNormalizer == null && !Options.MatchFileNameOnly)
            _single = _compiled[0];
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
    /// Determines whether <paramref name="input"/> matches <paramref name="pattern"/>, using <see cref="GlobOptions.Default"/>.
    /// </summary>
    /// <param name="input">The input to match, typically a path.</param>
    /// <param name="pattern">The glob pattern.</param>
    /// <returns><see langword="true"/> if <paramref name="input"/> matches; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="input"/> or <paramref name="pattern"/> is <see langword="null"/>.</exception>
    /// <exception cref="GlobParseException"><paramref name="pattern"/> is empty (<see cref="GlobParseError.EmptyPattern"/>) or cannot be compiled.</exception>
    /// <exception cref="RegexMatchTimeoutException">A regex evaluation exceeded the match timeout (see <see cref="GlobOptions.MatchTimeout"/>).</exception>
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
    /// <exception cref="RegexMatchTimeoutException">A regex evaluation exceeded the match timeout (see <see cref="GlobOptions.MatchTimeout"/>).</exception>
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
    /// <exception cref="RegexMatchTimeoutException">A regex evaluation exceeded the match timeout (see <see cref="GlobOptions.MatchTimeout"/>).</exception>
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
    /// <exception cref="RegexMatchTimeoutException">A regex evaluation exceeded the match timeout (see <see cref="GlobOptions.MatchTimeout"/>).</exception>
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
    /// <exception cref="RegexMatchTimeoutException">A regex evaluation exceeded the match timeout (see <see cref="GlobOptions.MatchTimeout"/>).</exception>
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
    /// <exception cref="RegexMatchTimeoutException">A regex evaluation exceeded the match timeout (see <see cref="GlobOptions.MatchTimeout"/>).</exception>
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
    /// Gets the cached glob for <paramref name="pattern"/> and <paramref name="options"/>, compiling and caching it on a miss.
    /// </summary>
    /// <remarks>Once the cache is full, a new glob replaces a randomly chosen one.</remarks>
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
        lock (s_CacheLock)
        {
            if (s_Cache.TryGetValue(key, out var cached))
                return cached;

            if (s_CacheCount < c_CacheCapacity)
            {
                s_CacheKeys[s_CacheCount++] = key;
            }
            else
            {
                // A xorshift step picks the entry to replace.
                uint seed = s_CacheSeed;
                seed ^= seed << 13;
                seed ^= seed >> 17;
                seed ^= seed << 5;
                s_CacheSeed = seed;

                int slot = (int)(seed % c_CacheCapacity);
                s_Cache.TryRemove(s_CacheKeys[slot], out _);
                s_CacheKeys[slot] = key;
            }

            s_Cache[key] = glob;
        }

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
    /// Returns the inputs that match this glob.
    /// </summary>
    /// <param name="inputs">The inputs to filter, typically paths.</param>
    /// <returns>The elements of <paramref name="inputs"/> that match, in their original order, evaluated lazily as the result is enumerated.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="inputs"/> is <see langword="null"/>, or, during enumeration, contains a <see langword="null"/> element.</exception>
    /// <exception cref="RegexMatchTimeoutException">During enumeration, a regex evaluation exceeded the match timeout (see <see cref="GlobOptions.MatchTimeout"/>).</exception>
    public IEnumerable<string> Filter(IEnumerable<string> inputs)
    {
        Guard.NotNull(inputs);

        return inputs.Where(IsMatch);
    }

    /// <summary>
    /// Determines whether <paramref name="input"/> matches this glob.
    /// </summary>
    /// <param name="input">The input to match, typically a path.</param>
    /// <returns><see langword="true"/> if <paramref name="input"/> matches a pattern and no ignore pattern; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="input"/> is <see langword="null"/>.</exception>
    /// <exception cref="RegexMatchTimeoutException">A regex evaluation exceeded the match timeout (see <see cref="GlobOptions.MatchTimeout"/>).</exception>
    public bool IsMatch(string input)
    {
        Guard.NotNull(input);

        if (_single != null)
        {
            if (!_convertSeparators || input.IndexOf('\\') < 0)
                return input.Length > 0 && (input == _single.Pattern || _single.IsMatch(input));

            if (_matchesSpans)
                return IsMatchConverted(input.AsSpan());
        }

        return IsMatchAny(input);
    }

    /// <summary>
    /// Determines whether <paramref name="input"/> matches a pattern of this glob and no ignore pattern.
    /// </summary>
    /// <param name="input">The input to match.</param>
    /// <returns><see langword="true"/> if <paramref name="input"/> matches a pattern and no ignore pattern; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool IsMatchAny(string input)
    {
        if (_matchesSpans && (Options.MatchFileNameOnly || (_convertSeparators && input.AsSpan().IndexOf('\\') >= 0)))
            return IsMatchCore(input.AsSpan());

        string normalized = Normalize(input);
        return FindPattern(input, normalized) != null && !IsIgnored(input, normalized);
    }

    /// <summary>
    /// Determines whether <paramref name="input"/> matches this glob.
    /// </summary>
    /// <param name="input">The input to match, typically a path.</param>
    /// <returns><see langword="true"/> if <paramref name="input"/> matches a pattern and no ignore pattern; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="RegexMatchTimeoutException">A regex evaluation exceeded the match timeout (see <see cref="GlobOptions.MatchTimeout"/>).</exception>
    public bool IsMatch(ReadOnlySpan<char> input)
    {
        if (_matchesSpans)
        {
            if (_single != null)
            {
                if (!_convertSeparators || input.IndexOf('\\') < 0)
                    return !input.IsEmpty && (input.SequenceEqual(_single.Pattern.AsSpan()) || _single.IsMatch(input));

                return IsMatchConverted(input);
            }

            return IsMatchCore(input);
        }

        return IsMatch(input.ToString());
    }

    /// <summary>
    /// Determines whether <paramref name="input"/> matches this glob, converting separators into a stack or pooled buffer.
    /// </summary>
    /// <remarks>
    /// Only used without <see cref="GlobOptions.InputNormalizer"/>, whose result must be a string, and, where the regex
    /// engine cannot match spans, only when every pattern has a regex-free matcher.
    /// </remarks>
    /// <param name="input">The input to match.</param>
    /// <returns><see langword="true"/> if <paramref name="input"/> matches a pattern and no ignore pattern; otherwise, <see langword="false"/>.</returns>
    private bool IsMatchCore(ReadOnlySpan<char> input)
    {
        if (input.IsEmpty)
            return false;

        if (!_convertSeparators || input.IndexOf('\\') < 0)
            return FindPatternIndex(input, input, changed: false) >= 0 && (_ignore == null || _ignore.FindPatternIndex(input, input, changed: false) < 0);

        return IsMatchConverted(input);
    }

    /// <summary>
    /// Determines whether <paramref name="input"/>, which contains backslashes to convert, matches this glob.
    /// </summary>
    /// <param name="input">The non-empty input to match.</param>
    /// <returns><see langword="true"/> if <paramref name="input"/> matches a pattern and no ignore pattern; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool IsMatchConverted(ReadOnlySpan<char> input)
    {
        char[]? rented = null;
        var buffer = input.Length <= c_StackNormalizeLength
            ? stackalloc char[c_StackNormalizeLength]
            : (rented = ArrayPool<char>.Shared.Rent(input.Length));
        try
        {
            var normalized = buffer[..input.Length];
            PathUtilities.ToPosixSlashes(input, normalized);
            return FindPatternIndex(input, normalized, changed: true) >= 0
                && (_ignore == null || _ignore.FindPatternIndex(input, normalized, changed: true) < 0);
        }
        finally
        {
            if (rented != null)
                ArrayPool<char>.Shared.Return(rented, clearArray: true);
        }
    }

    /// <summary>
    /// Finds the first pattern that matches the input, ignoring the ignore patterns.
    /// </summary>
    /// <param name="input">The original input.</param>
    /// <param name="normalized">The normalized input.</param>
    /// <param name="changed">Whether <paramref name="normalized"/> may differ from <paramref name="input"/>.</param>
    /// <returns>The index of the first matching pattern, or -1 if none matches.</returns>
    private int FindPatternIndex(ReadOnlySpan<char> input, ReadOnlySpan<char> normalized, bool changed)
    {
        var target = Options.MatchFileNameOnly ? PathUtilities.BaseName(normalized, _convertSeparators) : normalized;
        if (_index != null)
            return FindIndexed(input, normalized, changed, target);

        for (int i = 0; i < _compiled.Length; i++)
        {
            var compiled = _compiled[i];
            var pattern = compiled.Pattern.AsSpan();
            if (input.SequenceEqual(pattern) || (changed && normalized.SequenceEqual(pattern)))
                return i;

            if (!normalized.IsEmpty && compiled.IsMatch(target))
                return i;
        }

        return -1;
    }

    /// <summary>
    /// Finds the first pattern that matches the input, as <see cref="FindPatternIndex"/> does, testing only the candidates of <see cref="_index"/>.
    /// </summary>
    /// <param name="input">The original input.</param>
    /// <param name="normalized">The normalized input.</param>
    /// <param name="changed">Whether <paramref name="normalized"/> may differ from <paramref name="input"/>.</param>
    /// <param name="target">The text the patterns are matched against.</param>
    /// <returns>The index of the first matching pattern, or -1 if none matches.</returns>
    private int FindIndexed(ReadOnlySpan<char> input, ReadOnlySpan<char> normalized, bool changed, ReadOnlySpan<char> target)
    {
        int words = _index!.Words;
        ulong[]? rented = null;
        var candidates = words <= c_StackCandidateWords ? stackalloc ulong[c_StackCandidateWords] : (rented = ArrayPool<ulong>.Shared.Rent(words));
        candidates = candidates[..words];
        try
        {
            _index.FindCandidates(target, candidates);
            _index.MarkEqual(input, candidates);
            if (changed)
                _index.MarkEqual(normalized, candidates);

            for (int w = 0; w < words; w++)
            {
                for (ulong bits = candidates[w]; bits != 0; bits &= bits - 1)
                {
                    int i = (w << 6) + BitUtilities.TrailingZeroCount(bits);
                    var compiled = _compiled[i];
                    var pattern = compiled.Pattern.AsSpan();
                    if (input.SequenceEqual(pattern) || (changed && normalized.SequenceEqual(pattern)))
                        return i;

                    if (!normalized.IsEmpty && compiled.IsMatch(target))
                        return i;
                }
            }

            return -1;
        }
        finally
        {
            if (rented != null)
                ArrayPool<ulong>.Shared.Return(rented);
        }
    }

    /// <summary>
    /// Finds the first pattern that matches the input, as <see cref="FindPattern"/> does, testing only the candidates of <see cref="_index"/>.
    /// </summary>
    /// <param name="input">The original input.</param>
    /// <param name="normalized">The input as returned by <see cref="Normalize"/>.</param>
    /// <param name="target">The text the patterns are matched against.</param>
    /// <returns>The first matching pattern, or <see langword="null"/> if none matches.</returns>
    private string? FindIndexed(string input, string normalized, string target)
    {
        int words = _index!.Words;
        ulong[]? rented = null;
        var candidates = words <= c_StackCandidateWords ? stackalloc ulong[c_StackCandidateWords] : (rented = ArrayPool<ulong>.Shared.Rent(words));
        candidates = candidates[..words];
        try
        {
            bool changed = !ReferenceEquals(normalized, input);
            _index.FindCandidates(target.AsSpan(), candidates);
            _index.MarkEqual(input.AsSpan(), candidates);
            if (changed)
                _index.MarkEqual(normalized.AsSpan(), candidates);

            for (int w = 0; w < words; w++)
            {
                for (ulong bits = candidates[w]; bits != 0; bits &= bits - 1)
                {
                    var compiled = _compiled[(w << 6) + BitUtilities.TrailingZeroCount(bits)];
                    string pattern = compiled.Pattern;
                    if (input == pattern || (changed && normalized == pattern))
                        return pattern;

                    if (normalized.Length > 0 && compiled.IsMatch(target))
                        return pattern;
                }
            }

            return null;
        }
        finally
        {
            if (rented != null)
                ArrayPool<ulong>.Shared.Return(rented);
        }
    }

    /// <summary>
    /// Matches <paramref name="input"/> against this glob and describes the outcome.
    /// </summary>
    /// <param name="input">The input to match, typically a path.</param>
    /// <returns>The result of the match, including whether an ignore pattern excluded the input.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="input"/> is <see langword="null"/>.</exception>
    /// <exception cref="RegexMatchTimeoutException">A regex evaluation exceeded the match timeout (see <see cref="GlobOptions.MatchTimeout"/>).</exception>
    public GlobMatch Match(string input)
    {
        Guard.NotNull(input);

        string normalized = Normalize(input);
        if (_single != null && ReferenceEquals(normalized, input))
        {
            bool success = input.Length > 0 && (input == _single.Pattern || _single.IsMatch(input));
            return new GlobMatch(success, false, input, input, success ? _single.Pattern : null);
        }

        string? pattern = FindPattern(input, normalized);
        if (pattern == null)
            return new GlobMatch(false, false, input, normalized, null);

        bool ignored = IsIgnored(input, normalized);
        return new GlobMatch(!ignored, ignored, input, normalized, pattern);
    }

    /// <summary>
    /// Returns a regex equivalent to the patterns of this glob, with <see cref="GlobOptions.RegexOptions"/> and
    /// <see cref="GlobOptions.MatchTimeout"/> applied and, when <see cref="GlobOptions.IgnoreCase"/> is set,
    /// <see cref="RegexOptions.IgnoreCase"/> and <see cref="RegexOptions.CultureInvariant"/>.
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

        return _combinedRegex ?? Interlocked.CompareExchange(ref _combinedRegex, CreateCombinedRegex(), null) ?? _combinedRegex;
    }

    /// <summary>
    /// Builds the regex of all patterns, with the options and match timeout of the patterns.
    /// </summary>
    /// <returns>The regex.</returns>
    /// <exception cref="ArgumentException">The combined source is not a valid regex.</exception>
    private Regex CreateCombinedRegex()
    {
        return Options.MatchTimeout is { } timeout
            ? new Regex(ToRegexString(), _compiled[0].RegexOptions, timeout)
            : new Regex(ToRegexString(), _compiled[0].RegexOptions);
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
    /// The source does not encode <see cref="GlobOptions.RegexOptions"/>, <see cref="GlobOptions.MatchTimeout"/>,
    /// <see cref="GlobOptions.IgnoreCase"/>, <see cref="GlobOptions.IgnorePatterns"/>, <see cref="GlobOptions.MatchFileNameOnly"/>,
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
        if (Options.MatchFileNameOnly && _matchesSpans)
        {
            int index = FindPatternIndex(input.AsSpan(), normalized.AsSpan(), !ReferenceEquals(normalized, input));
            return index < 0 ? null : _compiled[index].Pattern;
        }

        string? fileName = Options.MatchFileNameOnly ? PathUtilities.BaseName(normalized, _convertSeparators) : null;
        if (_index != null)
            return FindIndexed(input, normalized, fileName ?? normalized);

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
    /// <remarks>
    /// The ignore glob has the same options apart from its ignore patterns, so without <see cref="GlobOptions.InputNormalizer"/>
    /// it would normalize <paramref name="input"/> to <paramref name="normalized"/> too; with one, the normalizer is called again.
    /// </remarks>
    /// <param name="input">The original input, before normalization.</param>
    /// <param name="normalized">The input as returned by <see cref="Normalize"/>.</param>
    /// <returns><see langword="true"/> if there are ignore patterns and one matches <paramref name="input"/>; otherwise, <see langword="false"/>.</returns>
    private bool IsIgnored(string input, string normalized)
    {
        if (_ignore == null)
            return false;

        return Options.InputNormalizer == null ? _ignore.FindPattern(input, normalized) != null : _ignore.IsMatch(input);
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
    /// Gets the options this glob was compiled with.
    /// </summary>
    public GlobOptions Options { get; }

    /// <summary>
    /// Gets the patterns of this glob.
    /// </summary>
    public IReadOnlyList<string> Patterns => _patterns;
}