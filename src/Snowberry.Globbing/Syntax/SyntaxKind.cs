namespace Snowberry.Globbing.Syntax;

/// <summary>
/// The kind of a <see cref="SyntaxNode"/>.
/// </summary>
internal enum SyntaxKind : byte
{
    /// <summary>
    /// An ordered list of items, linked through <see cref="SyntaxNode.FirstChild"/>: the root, or one alternative of a brace,
    /// group or extended glob, as told by <see cref="SyntaxNode.Role"/>.
    /// </summary>
    Sequence,

    /// <summary>A literal character, stored in <see cref="SyntaxNode.Value"/>; <see cref="SyntaxNode.Count"/> holds its <see cref="LiteralForm"/>.</summary>
    Literal,

    /// <summary>A path separator <c>/</c>, also written as <c>\/</c> unless <see cref="GlobOptions.BashCompatibility"/> is set.</summary>
    Separator,

    /// <summary>A literal <c>.</c>, also written as <c>\.</c>; <see cref="SyntaxNode.IsLeadingDot"/> marks a dot that can start a path segment.</summary>
    Dot,

    /// <summary>A run of <c>*</c>; <see cref="SyntaxNode.Count"/> is the number of stars.</summary>
    Star,

    /// <summary><c>?</c>: one character, or a quantifier after a group or extended glob.</summary>
    Question,

    /// <summary><c>+</c>: a quantifier after a bracket expression, brace, group or some extended globs, or after most items inside parentheses; otherwise a literal.</summary>
    Plus,

    /// <summary>A bracket expression; the node spans it in the pattern, including the brackets.</summary>
    CharClass,

    /// <summary>A brace expansion <c>{a,b}</c>; each child is the <see cref="Sequence"/> of one alternative.</summary>
    Brace,

    /// <summary>A brace range such as <c>{1..5}</c> or <c>{a..e..2}</c>; the node spans the text between the braces and has no children.</summary>
    BraceRange,

    /// <summary>An extended glob such as <c>!(a|b)</c>; <see cref="SyntaxNode.Value"/> is the operator and each child is the <see cref="Sequence"/> of one alternative.</summary>
    Extglob,

    /// <summary>
    /// A parenthesized regex group; each child is the <see cref="Sequence"/> of one alternative and
    /// <see cref="SyntaxNode.Count"/> is the length in characters of a regex prefix such as <c>?:</c>, or 0.
    /// </summary>
    Group,

    /// <summary>
    /// A <c>|</c> that does not separate the alternatives of a group or extended glob, such as one at the top level or in a
    /// brace alternative, written as regex alternation. In a plain pattern a <c>|</c> is a <see cref="Literal"/> instead.
    /// </summary>
    Pipe,
}
