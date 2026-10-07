using System;
using System.Collections.Generic;

namespace Snowberry.Globbing;

/// <summary>
/// Compares the keys of the static <see cref="Glob"/> cache with a hash code over the pattern and the scalar options.
/// </summary>
/// <remarks>Equality is still decided by <see cref="GlobOptions.Equals(GlobOptions?)"/>.</remarks>
internal sealed class GlobCacheKeyComparer : IEqualityComparer<(string Pattern, GlobOptions Options)>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GlobCacheKeyComparer"/> class.
    /// </summary>
    private GlobCacheKeyComparer()
    {
    }

    /// <inheritdoc/>
    public bool Equals((string Pattern, GlobOptions Options) x, (string Pattern, GlobOptions Options) y)
    {
        return string.Equals(x.Pattern, y.Pattern, StringComparison.Ordinal) && x.Options.Equals(y.Options);
    }

    /// <inheritdoc/>
    public int GetHashCode((string Pattern, GlobOptions Options) obj)
    {
        var o = obj.Options;
        int flags = (o.BashCompatibility ? 1 : 0)
            | (o.BraceExpansion ? 1 << 1 : 0)
            | (o.BracketExpressions ? 1 << 2 : 0)
            | (o.CaptureGroups ? 1 << 3 : 0)
            | (o.Extglobs ? 1 << 4 : 0)
            | (o.Globstar ? 1 << 5 : 0)
            | (o.IgnoreCase ? 1 << 6 : 0)
            | (o.KeepQuotes ? 1 << 7 : 0)
            | (o.MatchDotFiles ? 1 << 8 : 0)
            | (o.MatchFileNameOnly ? 1 << 9 : 0)
            | (o.MatchSubstring ? 1 << 10 : 0)
            | (o.Negation ? 1 << 11 : 0)
            | (o.PosixClasses ? 1 << 12 : 0)
            | (o.RegexQuantifiers ? 1 << 13 : 0)
            | (o.StrictBrackets ? 1 << 14 : 0)
            | (o.StrictSlashes ? 1 << 15 : 0)
            | (o.Unescape ? 1 << 16 : 0)
            | ((int)o.PathStyle << 17)
            | ((int)o.BracketMode << 20);
        int scalars = unchecked((flags * 31) + (int)o.RegexOptions);
        scalars = unchecked((scalars * 31) + o.MaxPatternLength);
        scalars = unchecked((scalars * 31) + o.IgnorePatterns.Count);
        return unchecked((StringComparer.Ordinal.GetHashCode(obj.Pattern) * 397) ^ scalars);
    }

    /// <summary>Gets the comparer.</summary>
    public static GlobCacheKeyComparer Instance { get; } = new();
}
