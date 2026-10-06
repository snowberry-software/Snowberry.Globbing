using System;

namespace Snowberry.Globbing.Syntax;

/// <summary>
/// The syntax features found in a glob pattern.
/// </summary>
[Flags]
internal enum GlobFeatures : byte
{
    /// <summary>No glob syntax.</summary>
    None = 0,

    /// <summary>
    /// Syntax that makes the pattern a glob rather than a literal path: a star, a <c>?</c>, a bracket expression, a brace
    /// expansion or range, an extended glob or a group.
    /// </summary>
    Glob = 1,

    /// <summary>A brace expansion or brace range; <see cref="GlobAnalyzer"/> also reports a matched brace pair that is parsed as literal text.</summary>
    Braces = 2,

    /// <summary>A bracket expression.</summary>
    Brackets = 4,

    /// <summary>An extended glob.</summary>
    Extglob = 8,

    /// <summary>A run of two or more <c>*</c>, wherever it appears.</summary>
    Globstar = 16,
}
