using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Snowberry.Globbing;

/// <summary>
/// Immutable options that control how a <see cref="Glob"/> pattern is compiled and matched.
/// </summary>
/// <remarks>
/// Instances are immutable and safe to share across threads. Derive variants with a <see langword="with"/> expression,
/// for example <c>GlobOptions.Default with { IgnoreCase = true }</c>.
/// </remarks>
public sealed record GlobOptions
{
    private static readonly TimeSpan s_MaxMatchTimeout = TimeSpan.FromMilliseconds(int.MaxValue - 1);

    private readonly IReadOnlyList<string> _ignorePatterns = [];
    private readonly TimeSpan? _matchTimeout;
    private readonly int _maxPatternLength = 65536;

    /// <summary>
    /// Gets a value indicating whether bash matching rules are followed: <c>*</c> also matches directory separators
    /// and <c>\/</c> is a literal slash instead of a path separator. Default is <see langword="false"/>.
    /// </summary>
    /// <remarks>A <c>**</c> that forms a whole path segment is still a globstar.</remarks>
    public bool BashCompatibility { get; init; }

    /// <summary>
    /// Gets a value indicating whether brace expressions are supported, so <c>{a,b}</c> matches <c>a</c> or <c>b</c> and
    /// <c>{1..3}</c> matches <c>1</c> to <c>3</c>. Default is <see langword="true"/>.
    /// </summary>
    /// <remarks>When <see langword="false"/>, braces match literally.</remarks>
    public bool BraceExpansion { get; init; } = true;

    /// <summary>
    /// Gets a function that converts the parts of a brace range into a regex fragment, or <see langword="null"/> to use
    /// the built-in conversion. Default is <see langword="null"/>.
    /// </summary>
    /// <remarks>
    /// The function receives the text between the <c>..</c> separators, such as <c>1</c> and <c>5</c> for <c>{1..5}</c>
    /// or <c>1</c>, <c>10</c> and <c>2</c> for <c>{1..10..2}</c>. Its result is inserted into the regex as written.
    /// </remarks>
    public Func<IReadOnlyList<string>, string>? BraceRangeExpander { get; init; }

    /// <summary>
    /// Gets a value indicating whether bracket expressions such as <c>[abc]</c> and <c>[a-z]</c> are supported.
    /// Default is <see langword="true"/>.
    /// </summary>
    /// <remarks>When <see langword="false"/>, brackets match literally.</remarks>
    public bool BracketExpressions { get; init; } = true;

    /// <summary>
    /// Gets how bracket expressions whose content could also be literal text are compiled.
    /// Default is <see cref="GlobBracketMode.Auto"/>.
    /// </summary>
    /// <remarks>Bracket expressions with ranges, POSIX classes or other regex metacharacters always match one character.</remarks>
    public GlobBracketMode BracketMode { get; init; }

    /// <summary>
    /// Gets a value indicating whether wildcards, brace expressions and extended globs are emitted as capturing groups
    /// in the generated regex. Default is <see langword="false"/>.
    /// </summary>
    public bool CaptureGroups { get; init; }

    /// <summary>
    /// Gets the default options.
    /// </summary>
    public static GlobOptions Default { get; } = new();

    /// <summary>
    /// Gets a value indicating whether extended globs such as <c>!(a|b)</c>, <c>?(a)</c>, <c>+(a)</c>, <c>*(a)</c>
    /// and <c>@(a)</c> are supported. Default is <see langword="true"/>.
    /// </summary>
    public bool Extglobs { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether <c>**</c> matches across directory separators. Default is <see langword="true"/>.
    /// </summary>
    /// <remarks>When <see langword="false"/>, <c>**</c> behaves like <c>*</c>.</remarks>
    public bool Globstar { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether matching ignores case. Default is <see langword="false"/>.
    /// </summary>
    /// <remarks>
    /// Applied as <see cref="RegexOptions.IgnoreCase"/> and <see cref="RegexOptions.CultureInvariant"/> to the regex used
    /// for matching and returned by <see cref="Glob.ToRegex"/>. It is not encoded in <see cref="Glob.ToRegexString"/>.
    /// </remarks>
    public bool IgnoreCase { get; init; }

    /// <summary>
    /// Gets glob patterns that exclude inputs which would otherwise match. Default is empty.
    /// </summary>
    /// <remarks>
    /// An input is excluded when it matches any ignore pattern. Ignore patterns are compiled with the same options,
    /// without ignore patterns of their own. Creating a <see cref="Glob"/> throws <see cref="ArgumentException"/> if the
    /// list contains a <see langword="null"/> or empty pattern. The patterns are copied when set, so later changes to the
    /// source collection do not affect these options.
    /// </remarks>
    /// <exception cref="ArgumentNullException">The value is <see langword="null"/>.</exception>
    public IReadOnlyList<string> IgnorePatterns
    {
        get => _ignorePatterns;
        init => _ignorePatterns = new List<string>(value ?? throw new ArgumentNullException(nameof(IgnorePatterns))).AsReadOnly();
    }

    /// <summary>
    /// Gets a function that transforms each input before it is matched, or <see langword="null"/> to use the default
    /// separator normalization of <see cref="PathStyle"/>. Default is <see langword="null"/>.
    /// </summary>
    /// <remarks>When set, it replaces the separator normalization.</remarks>
    public Func<string, string>? InputNormalizer { get; init; }

    /// <summary>
    /// Gets a value indicating whether the double quotes around quoted text are kept as literal characters the input must
    /// contain. Default is <see langword="false"/>.
    /// </summary>
    /// <remarks>Text between double quotes is matched literally either way; by default the quotes themselves are removed.</remarks>
    public bool KeepQuotes { get; init; }

    /// <summary>
    /// Gets a value indicating whether wildcards also match path segments that start with a dot,
    /// such as <c>.gitignore</c>. Default is <see langword="false"/>.
    /// </summary>
    public bool MatchDotFiles { get; init; }

    /// <summary>
    /// Gets a value indicating whether the pattern is matched against the file name only, ignoring the directory part
    /// of the input. Default is <see langword="false"/>.
    /// </summary>
    /// <remarks>
    /// For example, <c>*.js</c> then matches <c>src/lib/app.js</c>. The file name is the last path segment of the
    /// normalized input, ignoring one trailing separator.
    /// </remarks>
    public bool MatchFileNameOnly { get; init; }

    /// <summary>
    /// Gets a value indicating whether the pattern may match any part of the input instead of the whole input.
    /// Default is <see langword="false"/>.
    /// </summary>
    public bool MatchSubstring { get; init; }

    /// <summary>
    /// Gets the time limit for each regex evaluation during matching, or <see langword="null"/> to use the process-wide
    /// <c>REGEX_DEFAULT_MATCH_TIMEOUT</c>. Default is <see langword="null"/>.
    /// </summary>
    /// <remarks>
    /// Exceeding the limit makes matching throw <see cref="RegexMatchTimeoutException"/>. A call evaluates up to one regex per
    /// pattern and per <see cref="IgnorePatterns"/> entry. <see cref="Regex.InfiniteMatchTimeout"/> disables the timeout.
    /// The limit also applies to <see cref="Glob.ToRegex"/>.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is zero, negative or greater than <see cref="int.MaxValue"/> minus one milliseconds, and is not <see cref="Regex.InfiniteMatchTimeout"/>.</exception>
    public TimeSpan? MatchTimeout
    {
        get => _matchTimeout;
        init => _matchTimeout = value is not { } timeout || timeout == Regex.InfiniteMatchTimeout || (timeout > TimeSpan.Zero && timeout <= s_MaxMatchTimeout)
            ? value
            : throw new ArgumentOutOfRangeException(nameof(MatchTimeout), value, $"The match timeout must be positive and at most {nameof(Int32)}.{nameof(int.MaxValue)} - 1 milliseconds, or {nameof(Regex)}.{nameof(Regex.InfiniteMatchTimeout)}.");
    }

    /// <summary>
    /// Gets the maximum pattern length in characters. Default is <c>65536</c>.
    /// </summary>
    /// <remarks>Compiling a longer pattern throws <see cref="GlobParseException"/> with <see cref="GlobParseError.PatternTooLong"/>.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is less than 1.</exception>
    public int MaxPatternLength
    {
        get => _maxPatternLength;
        init => _maxPatternLength = value >= 1 ? value : throw new ArgumentOutOfRangeException(nameof(MaxPatternLength), value, "The maximum pattern length must be at least 1.");
    }

    /// <summary>
    /// Gets a value indicating whether an odd number of leading <c>!</c> negates the pattern. Default is <see langword="true"/>.
    /// </summary>
    /// <remarks>
    /// A negated pattern matches every input without a line terminator that the rest of the pattern does not match.
    /// With <see cref="Extglobs"/>, a <c>!</c> that opens <c>!(...)</c> starts an extended glob instead.
    /// </remarks>
    public bool Negation { get; init; } = true;

    /// <summary>
    /// Gets which characters separate path segments in inputs and in the generated regex. Default is
    /// <see cref="GlobPathStyle.Auto"/>.
    /// </summary>
    /// <remarks>Patterns always use <c>/</c> as the separator; a backslash in a pattern is an escape.</remarks>
    public GlobPathStyle PathStyle { get; init; }

    /// <summary>
    /// Gets a value indicating whether POSIX character classes such as <c>[[:alpha:]]</c> and <c>[[:digit:]]</c> are
    /// expanded inside bracket expressions. Default is <see langword="true"/>.
    /// </summary>
    public bool PosixClasses { get; init; } = true;

    /// <summary>
    /// Gets additional options for the regex used for matching and returned by <see cref="Glob.ToRegex"/>.
    /// Default is <see cref="RegexOptions.None"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="IgnoreCase"/> adds <see cref="RegexOptions.IgnoreCase"/> and <see cref="RegexOptions.CultureInvariant"/>
    /// automatically. The options are not encoded in <see cref="Glob.ToRegexString"/>. An invalid or unsupported
    /// combination, such as <c>RegexOptions.NonBacktracking</c>, makes compiling throw <see cref="GlobParseException"/>
    /// with <see cref="GlobParseError.InvalidPattern"/>. Use <see cref="RegexOptions.Compiled"/> for a glob reused across a
    /// very large number of inputs.
    /// </remarks>
    public RegexOptions RegexOptions { get; init; }

    /// <summary>
    /// Gets a value indicating whether a <c>*</c> directly after a bracket expression, group or extended glob repeats it,
    /// as a regex quantifier, instead of matching any characters. Default is <see langword="false"/>.
    /// </summary>
    public bool RegexQuantifiers { get; init; }

    /// <summary>
    /// Gets a value indicating whether unbalanced brackets and parentheses and unclosed braces are rejected instead of
    /// being matched literally. Default is <see langword="false"/>.
    /// </summary>
    /// <remarks>
    /// When <see langword="true"/>, compiling such a pattern throws <see cref="GlobParseException"/>. A <c>}</c> without
    /// a matching <c>{</c> is still matched literally. <see cref="Glob.Analyze"/> ignores this option.
    /// </remarks>
    public bool StrictBrackets { get; init; }

    /// <summary>
    /// Gets a value indicating whether patterns ending in a wildcard or bracket expression no longer also match a single
    /// trailing separator.
    /// Default is <see langword="false"/>.
    /// </summary>
    public bool StrictSlashes { get; init; }

    /// <summary>
    /// Gets a value indicating whether backslash-escaped characters are written to the generated regex without their
    /// backslash, so they keep their regex meaning instead of matching literally. Default is <see langword="false"/>.
    /// </summary>
    /// <remarks>
    /// For example, <c>\*</c> then becomes the regex quantifier <c>*</c>. <see cref="Glob.Analyze"/> removes the
    /// escapes from <see cref="GlobInfo.BasePath"/> and <see cref="GlobInfo.GlobPart"/>.
    /// </remarks>
    public bool Unescape { get; init; }
}