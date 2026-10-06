namespace Snowberry.Globbing.Syntax;

/// <summary>
/// The role of a <see cref="SyntaxKind.Sequence"/> within its parent.
/// </summary>
internal enum SequenceRole : byte
{
    /// <summary>The whole pattern.</summary>
    Root,

    /// <summary>An alternative of a brace expansion.</summary>
    BraceAlternative,

    /// <summary>An alternative of a regex group.</summary>
    GroupAlternative,

    /// <summary>An alternative of an extended glob.</summary>
    ExtglobAlternative,
}
