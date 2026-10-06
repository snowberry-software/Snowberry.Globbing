namespace Snowberry.Globbing.Compilation;

/// <summary>
/// What a sequence ended with; an optional trailing separator follows the root only after <see cref="Star"/> or <see cref="CharClass"/>.
/// </summary>
internal enum EmittedKind : byte
{
    /// <summary>Nothing was written.</summary>
    Nothing,

    /// <summary>A star that is not a globstar form: a single-segment wildcard, a bash star or a quantifier.</summary>
    Star,

    /// <summary>A globstar form.</summary>
    Globstar,

    /// <summary>A bracket expression.</summary>
    CharClass,

    /// <summary>Anything else.</summary>
    Other,
}