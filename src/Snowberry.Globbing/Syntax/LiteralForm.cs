namespace Snowberry.Globbing.Syntax;

/// <summary>
/// How a literal character is written to the regex.
/// </summary>
internal enum LiteralForm : byte
{
    /// <summary>The character is matched literally; regex metacharacters and <c>/</c> are escaped.</summary>
    Plain,

    /// <summary>The character was escaped with a backslash; the escape is passed to the regex as written, so <c>\d</c> stays a regex class.</summary>
    Escaped,

    /// <summary>
    /// The character was escaped with a backslash and <see cref="GlobOptions.Unescape"/> is set; it is written without the
    /// backslash and without escaping, so a regex metacharacter keeps its regex meaning.
    /// </summary>
    Raw,
}