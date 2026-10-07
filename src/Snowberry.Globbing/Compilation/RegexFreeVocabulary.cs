namespace Snowberry.Globbing.Compilation;

/// <summary>
/// The regex text <see cref="RegexFreeMatcher"/> recognizes, for one separator style.
/// </summary>
/// <remarks>The text is taken from the <see cref="RegexFragments"/> and <see cref="GlobChars"/> the emitter uses with default options.</remarks>
internal sealed class RegexFreeVocabulary
{
    /// <summary>The Windows separator class.</summary>
    public const string c_WindowsSeparator = "[\\\\/]";

    private readonly string _boundedStarStart;
    private readonly string _guardedStar;
    private readonly string[] _starStarts;

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

        // The guards of a star that starts a segment and must match a character; the last two, of MatchDotFiles, only keep out . and .. segments.
        _starStarts = [chars.OneCharNoDot, chars.SegmentFirstChar, chars.NoDots + chars.OneChar, chars.NoDotsSlash + chars.OneChar, chars.NoDot];
        _guardedStar = chars.OneChar + chars.Star;
        ConsumingStarStart = chars.SegmentFirstChar;
        MiddleStar = chars.Star;
        _boundedStarStart = chars.Qmark[..^1];
    }

    /// <summary>Gets the guard of a star that starts a segment and consumes its first character itself.</summary>
    public string ConsumingStarStart { get; }

    /// <summary>Gets <c>[^/]*</c>: a star inside a segment, between two literals.</summary>
    public string MiddleStar { get; }

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
    /// not start with a line terminator and is not empty, and that does not start with a dot or, with <paramref name="dotSegments"/>, is not <c>.</c> or <c>..</c>.
    /// </summary>
    /// <param name="source">The regex source.</param>
    /// <param name="index">The index to look at.</param>
    /// <param name="bound">The character the star stops before, as in <c>[^/-]*</c>, or <c>-1</c> if it has none.</param>
    /// <param name="dotSegments">Whether the star may start with a dot unless its segment is <c>.</c> or <c>..</c>, and must start the input.</param>
    /// <param name="mayBeEmpty">Whether the star may be empty or start with a line terminator, and only must not start with a dot.</param>
    /// <returns>The length of the star text, or <c>0</c> if there is none at <paramref name="index"/>.</returns>
    public int StarLengthAt(string source, int index, out int bound, out bool dotSegments, out bool mayBeEmpty)
    {
        bound = -1;
        dotSegments = false;
        mayBeEmpty = false;
        for (int i = 0; i < _starStarts.Length; i++)
        {
            string start = _starStarts[i];
            if (At(source, index, start))
            {
                dotSegments = i is 2 or 3;
                mayBeEmpty = i == 4;
                int length = MiddleStarLengthAt(source, index + start.Length, out bound, out bool guarded);
                if (guarded)
                    return 0;

                return length > 0 ? start.Length + length : 0;
            }
        }

        return 0;
    }

    /// <summary>
    /// Gets the length of the star inside a segment at <paramref name="index"/> of <paramref name="source"/>: <see cref="MiddleStar"/>,
    /// or a run of characters other than a separator and one bound character, as in <c>[^/-]*</c>.
    /// </summary>
    /// <param name="source">The regex source.</param>
    /// <param name="index">The index to look at.</param>
    /// <param name="bound">The bound character, or <c>-1</c> if the star has none.</param>
    /// <param name="guarded">Whether the star is preceded by a guard that the next character exists and is not a line terminator.</param>
    /// <returns>The length of the star text, or <c>0</c> if there is none at <paramref name="index"/>.</returns>
    public int MiddleStarLengthAt(string source, int index, out int bound, out bool guarded)
    {
        bound = -1;
        guarded = At(source, index, _guardedStar);
        if (guarded)
            return _guardedStar.Length;

        if (At(source, index, MiddleStar))
            return MiddleStar.Length;

        int q = index + _boundedStarStart.Length;
        if (!At(source, index, _boundedStarStart) || q + 3 > source.Length)
            return 0;

        char c = source[q];
        if (c == '\\')
        {
            c = source[++q];
            if (c < 128 && char.IsLetterOrDigit(c))
                return 0;
        }
        else if (c is ']' or '[' or '^' or '-')
        {
            return 0;
        }

        if (!At(source, q + 1, "]*"))
            return 0;

        bound = c;
        return q + 3 - index;
    }

    /// <summary>
    /// Determines whether <paramref name="source"/> has <paramref name="text"/> at <paramref name="index"/>.
    /// </summary>
    /// <param name="source">The regex source.</param>
    /// <param name="index">The index to look at.</param>
    /// <param name="text">The text to look for.</param>
    /// <returns><see langword="true"/> if <paramref name="text"/> is at <paramref name="index"/>; otherwise, <see langword="false"/>.</returns>
    private static bool At(string source, int index, string text)
    {
        return index + text.Length <= source.Length && string.CompareOrdinal(source, index, text, 0, text.Length) == 0;
    }
}
