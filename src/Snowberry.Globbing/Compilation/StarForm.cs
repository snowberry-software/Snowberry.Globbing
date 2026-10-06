namespace Snowberry.Globbing.Compilation;

/// <summary>
/// How a run of <c>*</c> is written to the regex.
/// </summary>
internal enum StarForm : byte
{
    /// <summary>Any characters within a segment; also a <c>**</c> that cannot span segments, such as one in <c>a**</c> or before a literal.</summary>
    Star,

    /// <summary>A regex <c>*</c> quantifier after a bracket expression, group or extended glob, with <see cref="GlobOptions.RegexQuantifiers"/>.</summary>
    Quantifier,

    /// <summary>Any characters except line terminators, as in bash; used for every star run of a non-plain pattern with <see cref="GlobOptions.BashCompatibility"/> except a <c>**</c> that spans whole segments.</summary>
    BashStar,

    /// <summary>A <c>**</c> that spans segments in a position none of the other globstar forms covers, such as before a brace or group, or at the end of a nested alternative.</summary>
    Globstar,

    /// <summary>A <c>**</c> that is the whole pattern.</summary>
    WholeGlobstar,

    /// <summary>A <c>/**</c> that ends the pattern, after a separator that is not the first node and does not follow a star.</summary>
    TrailingGlobstar,

    /// <summary>A <c>/**/</c> between segments, after a separator that is not the first node; repeated <c>/**/**/</c> are merged.</summary>
    MiddleGlobstar,

    /// <summary>A <c>**/</c> that starts the pattern or a brace alternative.</summary>
    LeadingGlobstar,
}