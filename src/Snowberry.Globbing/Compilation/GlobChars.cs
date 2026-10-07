namespace Snowberry.Globbing.Compilation;

/// <summary>
/// Regex fragments for one path style.
/// </summary>
internal sealed class GlobChars
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GlobChars"/> class.
    /// </summary>
    /// <param name="slash">The regex that matches one path separator.</param>
    /// <param name="separators">The separator characters, escaped for use inside a character class.</param>
    private GlobChars(string slash, string separators)
    {
        SlashLiteral = slash;
        NotSeparatorOpen = "[^" + separators;
        Qmark = NotSeparatorOpen + "]";
        QmarkNoDot = string.Concat("[^.", separators, "]");
        SegmentFirstChar = string.Concat("[^.", separators, RegexSyntax.c_LineTerminators, "]");
        SegmentRun = Qmark + "*";
        DotsSlash = string.Concat("\\.{1,2}(?:", slash, "|", RegexSyntax.c_EndOfInput, ")");
        NoDots = string.Concat("(?!(?:^|", slash, ")", DotsSlash, ")");
        NoDotSlash = string.Concat("(?!\\.{0,1}(?:", slash, "|", RegexSyntax.c_EndOfInput, "))");
        NoDotsSlash = string.Concat("(?!", DotsSlash, ")");
    }

    /// <summary>
    /// Gets the fragments for the specified path style.
    /// </summary>
    /// <param name="windows"><see langword="true"/> for fragments that treat both <c>/</c> and <c>\</c> as separators; otherwise, only <c>/</c>.</param>
    /// <returns>The shared fragments.</returns>
    public static GlobChars For(bool windows)
    {
        return windows ? Windows : Posix;
    }

    /// <summary>Gets a literal dot.</summary>
    public string DotLiteral { get; } = "\\.";

    /// <summary>Gets one or two dots followed by a separator or the end of input.</summary>
    public string DotsSlash { get; }

    /// <summary>Gets a lookahead rejecting a dot.</summary>
    public string NoDot { get; } = "(?!\\.)";

    /// <summary>Gets a lookahead rejecting an empty or <c>.</c> remainder of a segment, that is a separator or the end of input, optionally after one dot.</summary>
    public string NoDotSlash { get; }

    /// <summary>Gets a lookahead rejecting a <c>.</c> or <c>..</c> segment at the start of input or after a separator at this position.</summary>
    public string NoDots { get; }

    /// <summary>Gets a lookahead rejecting a <c>.</c> or <c>..</c> segment that starts at this position.</summary>
    public string NoDotsSlash { get; }

    /// <summary>Gets the start of a class of non-separators: <see cref="Qmark"/> without its closing bracket.</summary>
    public string NotSeparatorOpen { get; }

    /// <summary>Gets a lookahead requiring one more character that is not a line terminator.</summary>
    public string OneChar { get; } = "(?=" + RegexSyntax.c_AnyNonLineTerminator + ")";

    /// <summary>Gets a lookahead requiring one more character that is neither a dot nor a line terminator.</summary>
    public string OneCharNoDot { get; } = "(?=[^." + RegexSyntax.c_LineTerminators + "])";

    /// <summary>Gets a literal plus.</summary>
    public string PlusLiteral { get; } = "\\+";

    /// <summary>Gets the fragments for paths separated by <c>/</c>.</summary>
    public static GlobChars Posix { get; } = new("\\/", "/");

    /// <summary>Gets a single character other than a separator.</summary>
    public string Qmark { get; }

    /// <summary>Gets a single character other than a dot or separator.</summary>
    public string QmarkNoDot { get; }

    /// <summary>Gets a single character other than a dot, separator or line terminator.</summary>
    public string SegmentFirstChar { get; }

    /// <summary>Gets any characters within a segment, as a greedy run of <see cref="Qmark"/>.</summary>
    public string SegmentRun { get; }

    /// <summary>Gets a path separator.</summary>
    public string SlashLiteral { get; }

    /// <summary>Gets the fragments for paths separated by <c>/</c> or <c>\</c>.</summary>
    public static GlobChars Windows { get; } = new("[\\\\/]", "\\\\/");
}
