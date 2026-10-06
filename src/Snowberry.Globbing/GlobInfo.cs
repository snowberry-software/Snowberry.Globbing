using System.Collections.Generic;

namespace Snowberry.Globbing;

/// <summary>
/// Describes the structure of a glob pattern, as returned by <see cref="Glob.Analyze"/>.
/// </summary>
/// <remarks>
/// Useful for directory walkers: <see cref="BasePath"/> is the literal directory to start from and
/// <see cref="GlobPart"/> is the part that has to be matched against entries below it.
/// </remarks>
public sealed class GlobInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GlobInfo"/> class.
    /// </summary>
    /// <param name="pattern">The analyzed pattern.</param>
    /// <param name="prefix">The leading negation and <c>./</c> that precede the pattern body, or an empty string.</param>
    /// <param name="basePath">The literal directory part of the body before the first segment that contains a glob, the whole body if it contains no glob, or an empty string.</param>
    /// <param name="globPart">The part of the body after <paramref name="basePath"/> and its separator.</param>
    /// <param name="isGlob">Whether the pattern contains glob syntax.</param>
    /// <param name="isNegated">Whether leading <c>!</c> negate the pattern.</param>
    /// <param name="isNegatedExtglob">Whether the body starts with a negated extended glob <c>!(...)</c>.</param>
    /// <param name="hasBraces">Whether the pattern contains a balanced pair of braces, including one matched as literal text such as <c>{a}</c>.</param>
    /// <param name="hasBrackets">Whether the pattern contains a bracket expression.</param>
    /// <param name="hasExtglob">Whether the pattern contains an extended glob.</param>
    /// <param name="hasGlobstar">Whether the pattern contains two or more consecutive <c>*</c> anywhere, such as in <c>a**b</c>, not only as a <c>**</c> segment.</param>
    /// <param name="segments">The path segments of the pattern after <paramref name="prefix"/>.</param>
    internal GlobInfo(
        string pattern,
        string prefix,
        string basePath,
        string globPart,
        bool isGlob,
        bool isNegated,
        bool isNegatedExtglob,
        bool hasBraces,
        bool hasBrackets,
        bool hasExtglob,
        bool hasGlobstar,
        IReadOnlyList<string> segments)
    {
        Pattern = pattern;
        Prefix = prefix;
        BasePath = basePath;
        GlobPart = globPart;
        IsGlob = isGlob;
        IsNegated = isNegated;
        IsNegatedExtglob = isNegatedExtglob;
        HasBraces = hasBraces;
        HasBrackets = hasBrackets;
        HasExtglob = hasExtglob;
        HasGlobstar = hasGlobstar;
        Segments = segments;
    }

    /// <summary>
    /// Gets the literal directory part of the pattern, after <see cref="Prefix"/>, before the first segment that
    /// contains a glob, or an empty string if the first segment contains one.
    /// </summary>
    /// <remarks>
    /// For <c>src/lib/**/*.cs</c> this is <c>src/lib</c>, and for <c>/*.cs</c> it is <c>/</c>. For a pattern without
    /// glob syntax it is the whole pattern after <see cref="Prefix"/>. With <see cref="GlobOptions.Unescape"/>, backslash
    /// escapes are removed.
    /// </remarks>
    public string BasePath { get; }

    /// <summary>
    /// Gets the part of the pattern after <see cref="BasePath"/> and the separator that follows it.
    /// </summary>
    /// <remarks>
    /// For <c>src/lib/**/*.cs</c> this is <c>**/*.cs</c>; for a pattern without glob syntax it is an empty string.
    /// With <see cref="GlobOptions.Unescape"/>, backslash escapes are removed.
    /// </remarks>
    public string GlobPart { get; }

    /// <summary>
    /// Gets a value indicating whether the pattern contains a balanced pair of braces, such as <c>{a,b}</c>.
    /// </summary>
    /// <remarks>
    /// This includes a pair that is matched as literal text, such as <c>{a}</c>. Always <see langword="false"/> when
    /// <see cref="GlobOptions.BraceExpansion"/> is disabled.
    /// </remarks>
    public bool HasBraces { get; }

    /// <summary>
    /// Gets a value indicating whether the pattern contains a bracket expression such as <c>[a-z]</c>.
    /// </summary>
    public bool HasBrackets { get; }

    /// <summary>
    /// Gets a value indicating whether the pattern contains an extended glob such as <c>+(a)</c>.
    /// </summary>
    public bool HasExtglob { get; }

    /// <summary>
    /// Gets a value indicating whether the pattern contains <c>**</c>, that is, two or more consecutive <c>*</c>.
    /// </summary>
    /// <remarks>
    /// This is reported wherever the stars appear, such as in <c>a**b</c>, whether or not they form a <c>**</c> path
    /// segment and even when <see cref="GlobOptions.Globstar"/> is disabled.
    /// </remarks>
    public bool HasGlobstar { get; }

    /// <summary>
    /// Gets a value indicating whether the pattern contains any glob syntax; <see langword="false"/> for a literal path.
    /// </summary>
    public bool IsGlob { get; }

    /// <summary>
    /// Gets a value indicating whether the pattern is negated: it starts, after an optional <c>./</c>, with an odd
    /// number of <c>!</c>.
    /// </summary>
    /// <remarks>
    /// Always <see langword="false"/> when <see cref="GlobOptions.Negation"/> is disabled. With
    /// <see cref="GlobOptions.Extglobs"/>, a <c>!</c> that opens <c>!(...)</c> is not a negation.
    /// </remarks>
    public bool IsNegated { get; }

    /// <summary>
    /// Gets a value indicating whether the pattern, after <see cref="Prefix"/>, starts with a negated extended glob such
    /// as <c>!(a|b)</c>.
    /// </summary>
    public bool IsNegatedExtglob { get; }

    /// <summary>
    /// Gets the analyzed pattern.
    /// </summary>
    public string Pattern { get; }

    /// <summary>
    /// Gets the leading negation and <c>./</c> that precede the pattern body, such as <c>!</c>, <c>./</c> or <c>!./</c>, or an empty string.
    /// </summary>
    public string Prefix { get; }

    /// <summary>
    /// Gets the path segments of the pattern after <see cref="Prefix"/>, split on separators outside braces, brackets and groups.
    /// </summary>
    public IReadOnlyList<string> Segments { get; }
}