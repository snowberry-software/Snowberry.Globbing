namespace Snowberry.Globbing;

/// <summary>
/// Specifies which characters separate path segments in inputs and in the generated regex.
/// </summary>
public enum GlobPathStyle
{
    /// <summary>
    /// Only <c>/</c> separates path segments in the generated regex. When running on Windows, backslashes in inputs are
    /// converted to <c>/</c> before matching; on other operating systems inputs are matched as-is.
    /// </summary>
    Auto,

    /// <summary>
    /// Only <c>/</c> separates path segments; a backslash in an input is an ordinary character.
    /// </summary>
    Posix,

    /// <summary>
    /// Both <c>/</c> and <c>\</c> separate path segments in inputs: backslashes in inputs are converted to <c>/</c>
    /// before matching, and the generated regex accepts either separator.
    /// </summary>
    /// <remarks>Patterns still use <c>/</c>; a backslash in a pattern is an escape.</remarks>
    Windows,
}