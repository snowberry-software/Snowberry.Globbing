namespace Snowberry.Globbing.Syntax;

/// <summary>
/// A node of the glob syntax tree, stored in a flat list and linked by index.
/// </summary>
internal struct SyntaxNode
{
    /// <summary>
    /// Depends on <see cref="Kind"/>: the number of stars of a <see cref="SyntaxKind.Star"/>, the zero-based alternative index
    /// of a <see cref="SyntaxKind.Sequence"/>, the prefix length in characters of a <see cref="SyntaxKind.Group"/>, or the
    /// <see cref="LiteralForm"/> of a <see cref="SyntaxKind.Literal"/>; 0 otherwise.
    /// </summary>
    public int Count;

    /// <summary>The index of the first child, or -1.</summary>
    public int FirstChild;

    /// <summary>Whether a <see cref="SyntaxKind.Sequence"/> is inside a group or extended glob, at any depth.</summary>
    public bool InParens;

    /// <summary>
    /// Whether a <see cref="SyntaxKind.Dot"/> can start a path segment: it starts the pattern or follows a separator, or it
    /// is anywhere inside a group, extended glob or brace alternative. Never set for a plain pattern or another kind.
    /// </summary>
    public bool IsLeadingDot;

    /// <summary>The kind of node.</summary>
    public SyntaxKind Kind;

    /// <summary>The number of pattern characters the node covers; always 0 for a <see cref="SyntaxKind.Sequence"/>.</summary>
    public int Length;

    /// <summary>The index of the next sibling, or -1.</summary>
    public int Next;

    /// <summary>The role of a <see cref="SyntaxKind.Sequence"/>; <see cref="SequenceRole.Root"/> for other kinds.</summary>
    public SequenceRole Role;

    /// <summary>The position of the node in the lexed pattern; for a <see cref="SyntaxKind.Sequence"/>, the position of its first token.</summary>
    public int Start;

    /// <summary>
    /// The pattern character of the token the node was built from: the character of a <see cref="SyntaxKind.Literal"/>, the
    /// operator of an <see cref="SyntaxKind.Extglob"/> or <c>(</c> for a <see cref="SyntaxKind.Group"/>; <c>\0</c> for a
    /// <see cref="SyntaxKind.Sequence"/>, <see cref="SyntaxKind.Brace"/> or <see cref="SyntaxKind.BraceRange"/>.
    /// </summary>
    public char Value;

    /// <summary>
    /// Creates a node without children or siblings.
    /// </summary>
    /// <param name="kind">The kind of node.</param>
    /// <param name="start">The position of the node in the lexed pattern.</param>
    /// <param name="length">The number of pattern characters the node covers.</param>
    /// <returns>The node.</returns>
    public static SyntaxNode Create(SyntaxKind kind, int start, int length)
    {
        return new SyntaxNode { Kind = kind, Start = start, Length = length, FirstChild = -1, Next = -1 };
    }

    /// <summary>Gets the position after the node in the lexed pattern.</summary>
    public readonly int End => Start + Length;
}