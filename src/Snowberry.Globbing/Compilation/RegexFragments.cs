namespace Snowberry.Globbing.Compilation;

/// <summary>
/// The regex building blocks for one combination of <see cref="GlobOptions.PathStyle"/>, <see cref="GlobOptions.MatchDotFiles"/>,
/// <see cref="GlobOptions.CaptureGroups"/> and <see cref="GlobOptions.BashCompatibility"/>.
/// </summary>
/// <remarks>Instances are cached and shared; every combination is built once.</remarks>
internal sealed class RegexFragments
{
    private static readonly RegexFragments?[] s_Cache = new RegexFragments?[16];

    /// <summary>
    /// Initializes a new instance of the <see cref="RegexFragments"/> class.
    /// </summary>
    /// <param name="windows"><see langword="true"/> if both <c>/</c> and <c>\</c> separate path segments, as with <see cref="GlobPathStyle.Windows"/>.</param>
    /// <param name="matchDotFiles"><see langword="true"/> if wildcards may match a leading dot, as with <see cref="GlobOptions.MatchDotFiles"/>.</param>
    /// <param name="captureGroups"><see langword="true"/> if wildcards, braces and extended globs are written as capture groups, as with <see cref="GlobOptions.CaptureGroups"/>.</param>
    /// <param name="bash"><see langword="true"/> for <see cref="GlobOptions.BashCompatibility"/>.</param>
    private RegexFragments(bool windows, bool matchDotFiles, bool captureGroups, bool bash)
    {
        Chars = GlobChars.For(windows);
        Capture = captureGroups ? "" : "?:";
        NoDot = matchDotFiles ? "" : Chars.NoDot;
        SegmentStartQmark = matchDotFiles ? Chars.Qmark : Chars.QmarkNoDot;
        // Any number of segments, none starting with an excluded dot: a dot at the start of input or after a
        // separator, or a "." or ".." segment when dot files match.
        string excluded = matchDotFiles ? Chars.DotsSlash : Chars.DotLiteral;
        // It also matches nothing at a leading dot, so a negation of it excludes dot files.
        Globstar = string.Concat(
            "(", Capture, "(?:(?!^", excluded, ")", Chars.Qmark, "*(?:", Chars.SlashLiteral, "(?!", excluded, ")", Chars.Qmark, "*)*|^(?=", excluded, ")))");
        // Greedy and lazy stars accept the same inputs; lazy ones only decide what a capture group holds.
        string lazy = captureGroups ? "?" : "";
        BashStar = string.Concat(RegexSyntax.c_AnyNonLineTerminator, "*", lazy);
        Star = bash ? Globstar : captureGroups ? string.Concat("(", Chars.Star, lazy, ")") : Chars.Star;
        string shapeStar = bash ? BashStar : Chars.Star + lazy;
        ShapeStar = captureGroups ? string.Concat("(", shapeStar, ")") : shapeStar;
        SegmentLoops = !captureGroups && !matchDotFiles;
        Segment = string.Concat(Chars.NoDot, Chars.Star);
        LeadingSegments = string.Concat("(?:", Segment, Chars.SlashLiteral, ")*");
        LeadingGlobstar = SegmentLoops ? LeadingSegments : string.Concat("(?:", Globstar, Chars.SlashLiteral, ")?");
        AlternativeLeadingGlobstar = string.Concat("(?:^|", Chars.SlashLiteral, "|", Globstar, Chars.SlashLiteral, ")");
        OptionalSlash = string.Concat(Chars.SlashLiteral, "?");
    }

    /// <summary>
    /// Gets the fragments for <paramref name="options"/>.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <returns>The cached fragments.</returns>
    public static RegexFragments For(GlobOptions options)
    {
        bool windows = options.PathStyle == GlobPathStyle.Windows;
        int key = (windows ? 1 : 0) | (options.MatchDotFiles ? 2 : 0) | (options.CaptureGroups ? 4 : 0) | (options.BashCompatibility ? 8 : 0);
        return s_Cache[key] ??= new RegexFragments(windows, options.MatchDotFiles, options.CaptureGroups, options.BashCompatibility);
    }

    /// <summary>
    /// Gets the regex for <c>**/</c> at the start of a brace alternative, or of the pattern with <see cref="GlobOptions.MatchSubstring"/>:
    /// the start of input, a separator, or any segments followed by a separator.
    /// </summary>
    public string AlternativeLeadingGlobstar { get; }

    /// <summary>
    /// Gets the regex for <c>*</c> with <see cref="GlobOptions.BashCompatibility"/>: any characters except line terminators, including
    /// separators. It is lazy with <see cref="GlobOptions.CaptureGroups"/> but not itself a group.
    /// </summary>
    public string BashStar { get; }

    /// <summary>Gets <c>?:</c>, or an empty string with <see cref="GlobOptions.CaptureGroups"/>, for the start of a group that captures only then.</summary>
    public string Capture { get; }

    /// <summary>Gets the platform-specific fragments.</summary>
    public GlobChars Chars { get; }

    /// <summary>
    /// Gets the regex for <c>**</c>: any number of path segments, none starting with a dot unless <see cref="GlobOptions.MatchDotFiles"/> is set,
    /// and never <c>.</c> or <c>..</c>. It is a group that captures with <see cref="GlobOptions.CaptureGroups"/>. The first segment is
    /// checked only at the start of input; elsewhere the caller writes its own dot guard before it.
    /// </summary>
    public string Globstar { get; }

    /// <summary>Gets the regex for <c>**/</c> at the start of an anchored pattern: nothing, or any segments followed by a separator.</summary>
    public string LeadingGlobstar { get; }

    /// <summary>
    /// Gets any number of <see cref="Segment"/>s, each followed by a separator: the regex for a dot-guarded <c>**/</c> when
    /// <see cref="SegmentLoops"/> is set.
    /// </summary>
    public string LeadingSegments { get; }

    /// <summary>Gets one path segment that does not start with a dot, possibly empty: the repeated part of <see cref="LeadingSegments"/>.</summary>
    public string Segment { get; }

    /// <summary>
    /// Gets a value indicating whether a dot-guarded globstar is written as a loop of <see cref="Segment"/>s: without
    /// <see cref="GlobOptions.CaptureGroups"/> and <see cref="GlobOptions.MatchDotFiles"/>.
    /// </summary>
    public bool SegmentLoops { get; }

    /// <summary>Gets the lookahead that keeps a segment from starting with a dot, or an empty string with <see cref="GlobOptions.MatchDotFiles"/>.</summary>
    public string NoDot { get; }

    /// <summary>Gets an optional separator.</summary>
    public string OptionalSlash { get; }

    /// <summary>Gets the regex for <c>?</c> at the start of a path segment: any non-separator other than a dot, or any non-separator with <see cref="GlobOptions.MatchDotFiles"/>.</summary>
    public string SegmentStartQmark { get; }

    /// <summary>
    /// Gets the regex for <c>*</c> in the compact forms of common patterns such as <c>*.js</c>: <see cref="BashStar"/> with
    /// <see cref="GlobOptions.BashCompatibility"/>, otherwise any characters within a segment; a lazy capture group with <see cref="GlobOptions.CaptureGroups"/>.
    /// </summary>
    public string ShapeStar { get; }

    /// <summary>
    /// Gets the regex for <c>*</c>: any characters within a segment, as a lazy capture group with <see cref="GlobOptions.CaptureGroups"/>.
    /// With <see cref="GlobOptions.BashCompatibility"/> it is <see cref="Globstar"/>, used only by plain patterns and negated extended globs;
    /// every other bash star is <see cref="BashStar"/>.
    /// </summary>
    public string Star { get; }
}