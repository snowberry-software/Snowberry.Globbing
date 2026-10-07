namespace Snowberry.Globbing.Compilation;

/// <summary>
/// The regex text <see cref="RegexFreeMatcher"/> recognizes, for one separator style.
/// </summary>
/// <remarks>The text is taken from the <see cref="RegexFragments"/> and <see cref="GlobChars"/> the emitter uses with default options.</remarks>
internal sealed class RegexFreeVocabulary
{
    /// <summary>The Windows separator class.</summary>
    public const string c_WindowsSeparator = "[\\\\/]";

    private readonly string[] _stars;

    /// <summary>
    /// Initializes a new instance of the <see cref="RegexFreeVocabulary"/> class.
    /// </summary>
    /// <param name="windows">Whether separators are <c>/</c> and <c>\</c> rather than only <c>/</c>.</param>
    public RegexFreeVocabulary(bool windows)
    {
        var f = RegexFragments.For(GlobOptions.Default with { PathStyle = windows ? GlobPathStyle.Windows : GlobPathStyle.Posix });
        var chars = f.Chars;
        string sep = chars.SlashLiteral;
        Windows = windows;
        Separator = sep;
        LeadingGlobstar = f.LeadingSegments;
        MiddleGlobstar = string.Concat("(?:", sep, f.LeadingSegments, "|", RegexSyntax.c_EndOfInput, ")");
        TrailingGlobstar = string.Concat("(?:(?:", sep, f.Segment, ")+|", RegexSyntax.c_EndOfInput, ")");
        WholeGlobstar = chars.NoDot + f.Globstar;

        // The two forms of a star that starts a segment and must match a character.
        _stars = [chars.OneCharNoDot + chars.Star, chars.SegmentFirstChar + chars.Star];
    }

    /// <summary>Gets <c>(?:(?!\.)[^/]*/)*</c>: any number of segments that do not start with a dot, each followed by a separator.</summary>
    public string LeadingGlobstar { get; }

    /// <summary>Gets the <c>/**/</c> between a literal and the rest: a separator, then any number of segments that do not start with a dot, each followed by a separator.</summary>
    public string MiddleGlobstar { get; }

    /// <summary>Gets the separator.</summary>
    public string Separator { get; }

    /// <summary>Gets the <c>/**</c> that ends the pattern: the end of input, or a separator and segments that do not start with a dot.</summary>
    public string TrailingGlobstar { get; }

    /// <summary>Gets a <c>**</c> that is the whole pattern: segments that do not start with a dot.</summary>
    public string WholeGlobstar { get; }

    /// <summary>Gets a value indicating whether separators are <c>/</c> and <c>\</c>.</summary>
    public bool Windows { get; }

    /// <summary>
    /// Gets the length of the star at <paramref name="index"/> of <paramref name="source"/>: a run of non-separators that does
    /// not start with a dot or line terminator and is not empty.
    /// </summary>
    /// <param name="source">The regex source.</param>
    /// <param name="index">The index to look at.</param>
    /// <returns>The length of the star text, or <c>0</c> if there is none at <paramref name="index"/>.</returns>
    public int StarLengthAt(string source, int index)
    {
        foreach (string star in _stars)
        {
            if (index + star.Length <= source.Length && string.CompareOrdinal(source, index, star, 0, star.Length) == 0)
                return star.Length;
        }

        return 0;
    }
}
