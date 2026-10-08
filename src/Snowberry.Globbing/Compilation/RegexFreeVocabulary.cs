using System;

namespace Snowberry.Globbing.Compilation;

/// <summary>
/// The regex text <see cref="RegexFreeMatcher"/> recognizes, for one separator style.
/// </summary>
/// <remarks>The text is taken from the <see cref="RegexFragments"/> and <see cref="GlobChars"/> the emitter uses with default options.</remarks>
internal sealed class RegexFreeVocabulary
{
    private readonly string _boundedStarStart;
    private readonly string _consumingStarStart;
    private readonly string[] _dotLeadingGlobstars;
    private readonly string _dotSegmentStarStart;
    private readonly string _dotSegmentSlashStarStart;
    private readonly string _emptyStarStart;
    private readonly string _guardedStar;
    private readonly string _middleStar;
    private readonly string _starStart;

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
        MiddleGlobstar = f.MiddleGlobstarOrEnd;
        TrailingGlobstar = f.TrailingGlobstar;
        WholeGlobstar = chars.NoDot + f.Globstar;
        var dot = RegexFragments.For(GlobOptions.Default with { PathStyle = windows ? GlobPathStyle.Windows : GlobPathStyle.Posix, MatchDotFiles = true });
        _dotLeadingGlobstars = [string.Concat("(?:", chars.NoDots, dot.Globstar, sep, ")?"), dot.LeadingGlobstar];
        OneChar = chars.OneChar;
        _starStart = chars.OneCharNoDot;
        _consumingStarStart = chars.SegmentFirstChar;
        _dotSegmentStarStart = chars.NoDots + chars.OneChar;
        _dotSegmentSlashStarStart = chars.NoDotsSlash + chars.OneChar;
        _emptyStarStart = chars.NoDot;
        _guardedStar = chars.OneChar + chars.SegmentRun;
        _middleStar = chars.SegmentRun;
        _boundedStarStart = chars.NotSeparatorOpen;
    }

    /// <summary>Gets the lookahead that requires a character other than a line terminator.</summary>
    public string OneChar { get; }

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
    /// Determines whether <paramref name="source"/> has <paramref name="text"/> at <paramref name="index"/>.
    /// </summary>
    /// <param name="source">The regex source.</param>
    /// <param name="index">The index to look at.</param>
    /// <param name="text">The text to look for.</param>
    /// <returns><see langword="true"/> if <paramref name="text"/> is at <paramref name="index"/>; otherwise, <see langword="false"/>.</returns>
    public static bool At(string source, int index, string text)
    {
        return (uint)index <= (uint)source.Length && source.AsSpan(index).StartsWith(text.AsSpan());
    }

    /// <summary>
    /// Gets the length of the leading globstar of <see cref="GlobOptions.MatchDotFiles"/> at <paramref name="index"/>.
    /// </summary>
    /// <param name="source">The regex source.</param>
    /// <param name="index">The position in <paramref name="source"/>.</param>
    /// <returns>The length of the globstar, or 0 if there is none; it matches segments, none of them <c>.</c> or <c>..</c>, each followed by a separator.</returns>
    public int DotLeadingGlobstarLengthAt(string source, int index)
    {
        foreach (string globstar in _dotLeadingGlobstars)
        {
            if (At(source, index, globstar))
                return globstar.Length;
        }

        return 0;
    }

    /// <summary>
    /// Gets the length of the star at <paramref name="index"/> of <paramref name="source"/>: a run of non-separators that does
    /// not start with a line terminator and is not empty, and that does not start with a dot or, with <paramref name="dotSegments"/>, is not <c>.</c> or <c>..</c>.
    /// </summary>
    /// <param name="source">The regex source.</param>
    /// <param name="index">The index to look at.</param>
    /// <param name="bound">The character the star stops before, as in <c>[^/-]*</c>, or <c>-1</c> if it has none.</param>
    /// <param name="dotSegments">Whether the star may start with a dot unless its segment is <c>.</c> or <c>..</c>, and must start the input or follow a leading globstar of <see cref="GlobOptions.MatchDotFiles"/>.</param>
    /// <param name="mayBeEmpty">Whether the star may be empty or start with a line terminator, and only must not start with a dot.</param>
    /// <param name="consumes">Whether the star takes the first character of its segment itself, even when a literal follows.</param>
    /// <returns>The length of the star text, or <c>0</c> if there is none at <paramref name="index"/>.</returns>
    public int StarLengthAt(string source, int index, out int bound, out bool dotSegments, out bool mayBeEmpty, out bool consumes)
    {
        dotSegments = false;
        mayBeEmpty = false;
        consumes = false;
        if (At(source, index, _starStart))
            return StarLength(source, index, _starStart, out bound);

        if (At(source, index, _consumingStarStart))
        {
            consumes = true;
            return StarLength(source, index, _consumingStarStart, out bound);
        }

        dotSegments = true;
        if (At(source, index, _dotSegmentStarStart))
            return StarLength(source, index, _dotSegmentStarStart, out bound);

        if (At(source, index, _dotSegmentSlashStarStart))
            return StarLength(source, index, _dotSegmentSlashStarStart, out bound);

        dotSegments = false;
        mayBeEmpty = At(source, index, _emptyStarStart);
        if (mayBeEmpty)
            return StarLength(source, index, _emptyStarStart, out bound);

        bound = -1;
        return 0;
    }

    /// <summary>
    /// Gets the length of the star inside a segment at <paramref name="index"/> of <paramref name="source"/>: a run of
    /// non-separators, or a run of characters other than a separator and one bound character, as in <c>[^/-]*</c>.
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

        if (At(source, index, _middleStar))
            return _middleStar.Length;

        int q = index + _boundedStarStart.Length;
        if (!At(source, index, _boundedStarStart) || q + 3 > source.Length)
            return 0;

        char c = source[q];
        if (c == '\\')
        {
            c = source[++q];
            if (RegexSyntax.IsAsciiLetterOrDigit(c))
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
    /// Gets the length of a star that starts with <paramref name="start"/> at <paramref name="index"/>.
    /// </summary>
    /// <param name="source">The regex source.</param>
    /// <param name="index">The index of <paramref name="start"/>.</param>
    /// <param name="start">The guard the star starts with.</param>
    /// <param name="bound">The character the star stops before, or <c>-1</c> if it has none.</param>
    /// <returns>The length of the guard and star text, or <c>0</c> if no unguarded star follows the guard.</returns>
    private int StarLength(string source, int index, string start, out int bound)
    {
        int length = MiddleStarLengthAt(source, index + start.Length, out bound, out bool guarded);
        if (guarded)
            return 0;

        return length > 0 ? start.Length + length : 0;
    }
}
