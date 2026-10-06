namespace Snowberry.Globbing.Syntax;

/// <summary>
/// A lexical unit of a glob pattern.
/// </summary>
/// <param name="Kind">The kind of token.</param>
/// <param name="Value">The first pattern character of the token, or for an escaped <see cref="GlobTokenKind.Literal"/> the escaped character; for an <see cref="GlobTokenKind.ExtglobOpen"/>, the operator character.</param>
/// <param name="Form">How a <see cref="GlobTokenKind.Literal"/> is written to the regex; <see cref="LiteralForm.Plain"/> for every other kind.</param>
/// <param name="Start">The position of the token in the lexed pattern.</param>
/// <param name="Length">The number of pattern characters the token covers.</param>
internal readonly record struct GlobToken(GlobTokenKind Kind, char Value, LiteralForm Form, int Start, int Length)
{
    /// <summary>
    /// Gets the position after the token in the lexed pattern.
    /// </summary>
    public int End => Start + Length;
}