namespace Snowberry.Globbing;

/// <summary>
/// Specifies why a glob pattern could not be compiled.
/// </summary>
public enum GlobParseError
{
    /// <summary>
    /// The pattern is longer than <see cref="GlobOptions.MaxPatternLength"/>.
    /// </summary>
    PatternTooLong,

    /// <summary>
    /// A <c>[</c> has no matching <c>]</c>, with <see cref="GlobOptions.StrictBrackets"/> enabled.
    /// </summary>
    MissingClosingBracket,

    /// <summary>
    /// A <c>]</c> has no matching <c>[</c>, with <see cref="GlobOptions.StrictBrackets"/> enabled.
    /// </summary>
    MissingOpeningBracket,

    /// <summary>
    /// A <c>(</c>, including one that opens an extended glob, has no matching <c>)</c>, with
    /// <see cref="GlobOptions.StrictBrackets"/> enabled.
    /// </summary>
    MissingClosingParenthesis,

    /// <summary>
    /// A <c>)</c> has no matching <c>(</c>, with <see cref="GlobOptions.StrictBrackets"/> enabled.
    /// </summary>
    MissingOpeningParenthesis,

    /// <summary>
    /// A <c>{</c> has no matching <c>}</c>, with <see cref="GlobOptions.StrictBrackets"/> enabled.
    /// </summary>
    MissingClosingBrace,

    /// <summary>
    /// The pattern translates to an invalid regular expression, for example a reversed range such as <c>[z-a]</c>, or
    /// <see cref="GlobOptions.RegexOptions"/> is not a valid combination or is not supported for the generated regex,
    /// as with <c>RegexOptions.NonBacktracking</c>.
    /// </summary>
    InvalidPattern,

    /// <summary>
    /// Groups, braces and extended globs are nested more deeply than supported.
    /// </summary>
    NestingTooDeep,

    /// <summary>
    /// The pattern is <see langword="null"/> or empty; <see cref="GlobParseException.Pattern"/> is then an empty string.
    /// </summary>
    EmptyPattern,
}