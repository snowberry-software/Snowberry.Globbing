using System;
using System.Text.RegularExpressions;
using System.Threading;
using Snowberry.Globbing.Compilation;

namespace Snowberry.Globbing;

/// <summary>
/// One compiled pattern of a <see cref="Glob"/>: a regex-free matcher for common shapes, or its regex with a literal-text
/// pre-check and, for a negated pattern, the regex of its body.
/// </summary>
internal sealed class CompiledPattern
{
    // RegexOptions.NonBacktracking, which netstandard2.0 does not define.
    private const RegexOptions c_NonBacktracking = (RegexOptions)0x400;
    private const RegexOptions c_NeutralOptions = RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase;

    private const string c_Escape = @"\";

    private static readonly char[] s_LineTerminators = [(char)0x0A, (char)0x0D, (char)0x2028, (char)0x2029];

    private readonly RegexFreeMatcher? _fast;
    private readonly RegexFreeMatcher? _fastPositive;
    private readonly LiteralHint? _hint;
    private readonly LiteralHint? _fastPrefix;
    private readonly TimeSpan? _matchTimeout;
    private readonly Regex? _positive;
    private Regex? _regex;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompiledPattern"/> class.
    /// </summary>
    /// <param name="pattern">The glob pattern.</param>
    /// <param name="options">The options.</param>
    /// <param name="regexOptions">The options of the regex.</param>
    /// <param name="findKeys"><see langword="true"/> to find the key sets of the pattern.</param>
    /// <param name="keyWindows">When this constructor returns, the key sets of the pattern for <see cref="PatternIndex"/>, or <see langword="null"/>.</param>
    /// <exception cref="GlobParseException"><paramref name="pattern"/> cannot be compiled, or its regex is invalid or unsupported with <paramref name="regexOptions"/>.</exception>
    public CompiledPattern(string pattern, GlobOptions options, RegexOptions regexOptions, bool findKeys, out ulong[][]? keyWindows)
    {
        var compilation = GlobCompiler.Compile(pattern, options, findKeys);
        keyWindows = compilation.KeyWindows;
        Pattern = pattern;
        Source = compilation.Source;
        RegexOptions = regexOptions;
        _matchTimeout = options.MatchTimeout;
        _hint = compilation.Hint;
        string? positiveSource = compilation.PositiveSource;

        try
        {
            _fast = RegexFreeMatcher.TryCreate(Source, regexOptions);

            // A literal prefix rejects most inputs more cheaply than the regex-free matcher.
            if (_fast != null && _hint is { HasPrefix: true })
                _fastPrefix = _hint;
            if (positiveSource != null)
                _fastPositive = RegexFreeMatcher.TryCreate(positiveSource, regexOptions);

            // Built on first use unless the pattern has escapes or verbatim regex text, or the options change how the regex is read.
            bool verbatim = options.BraceRangeExpander != null || options.Unescape || pattern.Contains(c_Escape) || (regexOptions & ~c_NeutralOptions) != 0;
            if (verbatim || (_fast == null && positiveSource == null))
                _regex = Create(Source, regexOptions, _matchTimeout);

            if (positiveSource != null && (verbatim || _fastPositive == null))
                _positive = Create(positiveSource, regexOptions, _matchTimeout);
        }
        catch (ArgumentException e)
        {
            throw new GlobParseException(pattern, GlobParseError.InvalidPattern, -1, $"The pattern \"{pattern}\" does not translate to a valid regular expression: {e.Message}", e);
        }
        catch (NotSupportedException e)
        {
            throw new GlobParseException(pattern, GlobParseError.InvalidPattern, -1, $"The pattern \"{pattern}\" cannot be compiled with the regex options {regexOptions}: {e.Message}{((regexOptions & c_NonBacktracking) != 0 ? $" RegexOptions.NonBacktracking is not supported; use {nameof(GlobOptions)}.{nameof(GlobOptions.MatchTimeout)} to bound matching time." : "")}", e);
        }

#if NET7_0_OR_GREATER
        MatchesSpanWithoutString = true;
#else
        MatchesSpanWithoutString = IsRegexFree;
#endif
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CompiledPattern"/> class without its key sets.
    /// </summary>
    /// <param name="pattern">The glob pattern.</param>
    /// <param name="options">The options.</param>
    /// <param name="regexOptions">The options of the regex.</param>
    /// <exception cref="GlobParseException"><paramref name="pattern"/> cannot be compiled, or its regex is invalid or unsupported with <paramref name="regexOptions"/>.</exception>
    public CompiledPattern(string pattern, GlobOptions options, RegexOptions regexOptions)
        : this(pattern, options, regexOptions, findKeys: false, out _)
    {
    }

    /// <summary>
    /// Determines whether <paramref name="input"/> contains a line terminator: line feed, carriage return, line separator or paragraph separator.
    /// </summary>
    /// <param name="input">The input to inspect.</param>
    /// <returns><see langword="true"/> if <paramref name="input"/> contains a line terminator; otherwise, <see langword="false"/>.</returns>
    private static bool HasLineTerminator(ReadOnlySpan<char> input)
    {
        return input.IndexOfAny(s_LineTerminators) >= 0;
    }

    /// <summary>
    /// Builds a regex, with the default match timeout unless <paramref name="matchTimeout"/> is set.
    /// </summary>
    /// <param name="source">The regex source.</param>
    /// <param name="regexOptions">The options of the regex.</param>
    /// <param name="matchTimeout">The match timeout, or <see langword="null"/> for the default.</param>
    /// <returns>The regex.</returns>
    /// <exception cref="ArgumentException"><paramref name="source"/> is not a valid regex.</exception>
    /// <exception cref="NotSupportedException">The regex is not supported with <paramref name="regexOptions"/>.</exception>
    private static Regex Create(string source, RegexOptions regexOptions, TimeSpan? matchTimeout)
    {
        return matchTimeout is { } timeout ? new Regex(source, regexOptions, timeout) : new Regex(source, regexOptions);
    }

    /// <summary>
    /// Determines whether <paramref name="input"/> matches the pattern.
    /// </summary>
    /// <param name="input">The normalized input.</param>
    /// <returns><see langword="true"/> if <paramref name="input"/> matches; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="RegexMatchTimeoutException">The regex evaluation exceeded its match timeout.</exception>
    public bool IsMatch(string input)
    {
        if (_fast != null)
            return (_fastPrefix == null || _fastPrefix.IsSatisfiedBy(input)) && _fast.IsMatch(input.AsSpan());

        if (_fastPositive != null)
            return !HasLineTerminator(input.AsSpan()) && !_fastPositive.IsMatch(input.AsSpan());

        if (_positive == null)
            return (_hint == null || _hint.IsSatisfiedBy(input)) && _regex!.IsMatch(input);

        return !HasLineTerminator(input.AsSpan()) && !((_hint == null || _hint.IsSatisfiedBy(input)) && _positive.IsMatch(input));
    }

    /// <summary>
    /// Determines whether <paramref name="input"/> matches the pattern.
    /// </summary>
    /// <remarks>Without span support in the regex engine, only the regex-free matcher avoids converting <paramref name="input"/> to a string.</remarks>
    /// <param name="input">The normalized input.</param>
    /// <returns><see langword="true"/> if <paramref name="input"/> matches; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="RegexMatchTimeoutException">The regex evaluation exceeded its match timeout.</exception>
    public bool IsMatch(ReadOnlySpan<char> input)
    {
        if (_fast != null)
            return (_fastPrefix == null || _fastPrefix.IsSatisfiedBy(input)) && _fast.IsMatch(input);

        if (_fastPositive != null)
            return !HasLineTerminator(input) && !_fastPositive.IsMatch(input);

#if NET7_0_OR_GREATER
        if (_positive == null)
            return (_hint == null || _hint.IsSatisfiedBy(input)) && _regex!.IsMatch(input);

        return !HasLineTerminator(input) && !((_hint == null || _hint.IsSatisfiedBy(input)) && _positive.IsMatch(input));
#else
        return IsMatch(input.ToString());
#endif
    }

    /// <summary>Gets a value indicating whether matching decides without running a regex.</summary>
    public bool IsRegexFree => _fast != null || _fastPositive != null;

    /// <summary>Gets a rough cost, in relative units, of testing a typical input this pattern does not match.</summary>
    public int EstimatedRejectCost => (_fast ?? _fastPositive)?.EstimatedRejectCost ?? _hint?.EstimatedRejectCost ?? LiteralHint.RegexRunCost;

    /// <summary>Gets a value indicating whether <see cref="IsMatch(ReadOnlySpan{char})"/> matches without converting its input to a string.</summary>
    public bool MatchesSpanWithoutString { get; }

    /// <summary>Gets the glob pattern.</summary>
    public string Pattern { get; }

    /// <summary>Gets the regex of the pattern, built on first use when matching does not need it; every caller gets the same instance.</summary>
    public Regex Regex => _regex ?? Interlocked.CompareExchange(ref _regex, Create(Source, RegexOptions, _matchTimeout), null) ?? _regex;

    /// <summary>Gets the options of the regex.</summary>
    public RegexOptions RegexOptions { get; }

    /// <summary>Gets the portable regex source of the pattern.</summary>
    public string Source { get; }
}
