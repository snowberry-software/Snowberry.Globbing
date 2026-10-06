using System;
using System.Text.RegularExpressions;
using Snowberry.Globbing.Compilation;

namespace Snowberry.Globbing;

/// <summary>
/// One compiled pattern of a <see cref="Glob"/>: its regex, plus a literal-text check that rejects many inputs without
/// running it and, for a negated pattern, a regex of its body that avoids the negative lookahead.
/// </summary>
internal sealed class CompiledPattern
{
    private readonly LiteralHint? _hint;
    private readonly string? _positiveSource;
    private Regex? _positive;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompiledPattern"/> class.
    /// </summary>
    /// <param name="pattern">The glob pattern.</param>
    /// <param name="options">The options.</param>
    /// <param name="regexOptions">The options of the regex.</param>
    /// <exception cref="GlobParseException"><paramref name="pattern"/> cannot be compiled, or its regex is invalid with <paramref name="regexOptions"/>.</exception>
    public CompiledPattern(string pattern, GlobOptions options, RegexOptions regexOptions)
    {
        var compilation = GlobCompiler.Compile(pattern, options);
        Pattern = pattern;
        Source = compilation.Source;
        _positiveSource = compilation.PositiveSource;
        _hint = compilation.Hint;

        try
        {
            Regex = new Regex(Source, regexOptions);
        }
        catch (ArgumentException e)
        {
            throw new GlobParseException(pattern, GlobParseError.InvalidPattern, -1, $"The pattern \"{pattern}\" does not translate to a valid regular expression: {e.Message}", e);
        }
    }

    /// <summary>Gets the glob pattern.</summary>
    public string Pattern { get; }

    /// <summary>Gets the portable regex source of the pattern.</summary>
    public string Source { get; }

    /// <summary>Gets the regex of the pattern.</summary>
    public Regex Regex { get; }

    /// <summary>
    /// Determines whether <paramref name="input"/> matches the regex of the pattern.
    /// </summary>
    /// <param name="input">The normalized input.</param>
    /// <returns><see langword="true"/> if <paramref name="input"/> matches; otherwise, <see langword="false"/>.</returns>
    public bool IsMatch(string input)
    {
        if (_positiveSource == null)
            return (_hint == null || _hint.IsSatisfiedBy(input)) && Regex.IsMatch(input);

        return !HasLineTerminator(input.AsSpan()) && !((_hint == null || _hint.IsSatisfiedBy(input)) && Positive.IsMatch(input));
    }

#if NET7_0_OR_GREATER
    /// <summary>
    /// Determines whether <paramref name="input"/> matches the regex of the pattern.
    /// </summary>
    /// <param name="input">The normalized input.</param>
    /// <returns><see langword="true"/> if <paramref name="input"/> matches; otherwise, <see langword="false"/>.</returns>
    public bool IsMatch(ReadOnlySpan<char> input)
    {
        if (_positiveSource == null)
            return (_hint == null || _hint.IsSatisfiedBy(input)) && Regex.IsMatch(input);

        return !HasLineTerminator(input) && !((_hint == null || _hint.IsSatisfiedBy(input)) && Positive.IsMatch(input));
    }
#endif

    /// <summary>
    /// Gets the regex of the body of a negated pattern, built on first use.
    /// </summary>
    /// <remarks>It is built from a part of the already validated regex, so it cannot fail; a race only builds it twice.</remarks>
    private Regex Positive => _positive ??= new Regex(_positiveSource!, Regex.Options);

    /// <summary>
    /// Determines whether <paramref name="input"/> contains a line terminator: line feed, carriage return, line separator or paragraph separator.
    /// </summary>
    /// <param name="input">The input to inspect.</param>
    /// <returns><see langword="true"/> if <paramref name="input"/> contains a line terminator; otherwise, <see langword="false"/>.</returns>
    private static bool HasLineTerminator(ReadOnlySpan<char> input)
    {
        return input.IndexOfAny("\n\r\u2028\u2029".AsSpan()) >= 0;
    }
}
