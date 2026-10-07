using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Snowberry.Globbing.Compilation;

/// <summary>
/// Decides without running a regex whether an input matches one of the regex shapes the emitter writes for the most
/// common globs, such as <c>*.js</c>, <c>**/*.js</c>, <c>*.{js,ts}</c>, <c>src/**</c>, <c>src/**/*.cs</c>,
/// <c>**/bin/**</c>, <c>**</c> and plain literals.
/// </summary>
/// <remarks>
/// The shape is recognized from the regex source; any other source, or a regex option that changes its meaning, runs the regex.
/// Literals are at most 256 characters, so matching is linear in the input; it ignores <see cref="GlobOptions.MatchTimeout"/>.
/// </remarks>
internal sealed class RegexFreeMatcher
{
    private const string c_End = ")$(?!\\n)";
    private const int c_MaxLiteralLength = 256;
    private const string c_Start = "^(?:";

    private static readonly RegexFreeVocabulary s_Posix = new(windows: false);
    private static readonly RegexFreeVocabulary s_Windows = new(windows: true);

    private readonly string[]? _alternatives;
    private readonly string? _headLiteral;
    private readonly bool _leadingSegments;
    private readonly string _literal;
    private readonly bool _optionalSeparator;
    private readonly bool _star;
    private readonly bool _trailingGlobstar;
    private readonly bool _wholeGlobstar;
    private readonly bool _windows;

    /// <summary>
    /// Initializes a new instance of the <see cref="RegexFreeMatcher"/> class.
    /// </summary>
    /// <param name="windows">Whether <c>\</c> is a separator too, and a <c>/</c> in the literals stands for either separator.</param>
    /// <param name="headLiteral">The literal before a middle globstar, or <see langword="null"/> if there is none.</param>
    /// <param name="leadingSegments">Whether segments that do not start with a dot may come before the star or literal.</param>
    /// <param name="wholeGlobstar">Whether the regex is a whole-input globstar.</param>
    /// <param name="star">Whether a star run comes before the literal.</param>
    /// <param name="literal">The literal text.</param>
    /// <param name="alternatives">The literal alternatives after <paramref name="literal"/>, or <see langword="null"/> if there are none.</param>
    /// <param name="trailingGlobstar">Whether a trailing globstar follows the literal.</param>
    /// <param name="optionalSeparator">Whether one separator may end the input.</param>
    private RegexFreeMatcher(
        bool windows,
        string? headLiteral,
        bool leadingSegments,
        bool wholeGlobstar,
        bool star,
        string literal,
        string[]? alternatives,
        bool trailingGlobstar,
        bool optionalSeparator)
    {
        _windows = windows;
        _headLiteral = headLiteral;
        _leadingSegments = leadingSegments;
        _wholeGlobstar = wholeGlobstar;
        _star = star;
        _literal = literal;
        _alternatives = alternatives;
        _trailingGlobstar = trailingGlobstar;
        _optionalSeparator = optionalSeparator;
    }

    /// <summary>
    /// Creates a matcher equivalent to a regex with the given source and options, if the source has one of the supported shapes.
    /// </summary>
    /// <param name="source">The regex source.</param>
    /// <param name="options">The regex options; any option that changes what the source means disables the matcher.</param>
    /// <returns>The matcher, or <see langword="null"/> if the regex must be run.</returns>
    public static RegexFreeMatcher? TryCreate(string source, RegexOptions options)
    {
        const RegexOptions neutral = RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture;
        if ((options & ~neutral) != 0)
            return null;

        return TryParse(source, s_Posix) ?? TryParse(source, s_Windows);
    }

    /// <summary>
    /// Gets a rough cost, in relative units, of rejecting a typical input: low when the input must start with a literal,
    /// higher when a star or leading globstar has to be scanned.
    /// </summary>
    public int EstimatedRejectCost => _headLiteral != null || !(_wholeGlobstar || _leadingSegments || _star) ? 3 : 15;

    /// <summary>
    /// Recognizes <paramref name="source"/> as one of the supported shapes, written with the separator forms of <paramref name="v"/>.
    /// </summary>
    /// <param name="source">The regex source.</param>
    /// <param name="v">The regex text of the shape parts.</param>
    /// <returns>The matcher, or <see langword="null"/> if <paramref name="source"/> has no supported shape.</returns>
    private static RegexFreeMatcher? TryParse(string source, RegexFreeVocabulary v)
    {
        if (!source.StartsWith(c_Start, StringComparison.Ordinal) || !source.EndsWith(c_End, StringComparison.Ordinal))
            return null;

        bool windows = v.Windows;
        int end = source.Length - c_End.Length;
        int p = c_Start.Length;

        string? headLiteral = null;
        bool leading = false;
        bool whole = false;
        if (At(source, p, v.LeadingGlobstar))
        {
            leading = true;
            p += v.LeadingGlobstar.Length;
        }
        else if (At(source, p, v.WholeGlobstar))
        {
            whole = true;
            p += v.WholeGlobstar.Length;
        }
        else
        {
            int q = p;
            if (!TryParseLiteral(source, ref q, end, windows, out string head))
                return null;

            if (At(source, q, v.MiddleGlobstar))
            {
                headLiteral = head;
                leading = true;
                p = q + v.MiddleGlobstar.Length;
            }
        }

        int starLength = whole ? 0 : v.StarLengthAt(source, p);
        bool star = starLength > 0;
        p += starLength;

        string literal = "";
        if (!whole && !TryParseLiteral(source, ref p, end, windows, out literal))
            return null;

        string[]? alternatives = null;
        if (!whole && p < end && source[p] == '(' && !At(source, p, v.TrailingGlobstar) && !TryParseAlternatives(source, ref p, end, windows, out alternatives))
            return null;

        bool trailing = !whole && !star && alternatives == null && literal.Length > 0 && At(source, p, v.TrailingGlobstar);
        if (trailing)
            p += v.TrailingGlobstar.Length;

        bool optionalSeparator = !trailing && end - p == v.Separator.Length + 1 && At(source, p, v.Separator) && source[end - 1] == '?';
        if (optionalSeparator)
            p = end;

        if (p != end)
            return null;

        // Matching costs up to the input length times the literal length.
        if (literal.Length > c_MaxLiteralLength || headLiteral?.Length > c_MaxLiteralLength
            || (alternatives != null && Array.Exists(alternatives, a => a.Length > c_MaxLiteralLength)))
            return null;

        // A star run does not take a separator, so the text after it must not have one.
        if (star && (HasSlash(literal) || (alternatives != null && Array.Exists(alternatives, HasSlash))))
            return null;

        // The end-of-input alternative of a middle globstar must stay unable to match, so the rest needs a character.
        if (headLiteral != null && !star && literal.Length == 0 && (alternatives == null || Array.Exists(alternatives, a => a.Length == 0)))
            return null;

        return new RegexFreeMatcher(windows, headLiteral, leading, whole, star, literal, alternatives, trailing, optionalSeparator);
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

    /// <summary>
    /// Determines whether <paramref name="literal"/> has a separator.
    /// </summary>
    /// <param name="literal">A literal as read by <see cref="TryParseLiteral"/>.</param>
    /// <returns><see langword="true"/> if <paramref name="literal"/> has a <c>/</c>; otherwise, <see langword="false"/>.</returns>
    private static bool HasSlash(string literal)
    {
        return literal.AsSpan().IndexOf('/') >= 0;
    }

    /// <summary>
    /// Reads a group of literal alternatives, <c>(?:a|b)</c>.
    /// </summary>
    /// <param name="source">The regex source.</param>
    /// <param name="p">The index of the group; on success, the index after it.</param>
    /// <param name="end">The index the shape ends at.</param>
    /// <param name="windows">Whether separators are written as Windows separator classes.</param>
    /// <param name="alternatives">The alternatives, if the group has only literal alternatives.</param>
    /// <returns><see langword="true"/> if the group has only literal alternatives; otherwise, <see langword="false"/>.</returns>
    private static bool TryParseAlternatives(string source, ref int p, int end, bool windows, out string[]? alternatives)
    {
        alternatives = null;
        if (!At(source, p, "(?:"))
            return false;

        int q = p + 3;
        var list = new List<string>();
        while (true)
        {
            if (!TryParseLiteral(source, ref q, end, windows, out string alternative))
                return false;

            list.Add(alternative);
            if (q >= end)
                return false;

            if (source[q] == ')')
                break;

            if (source[q] != '|')
                return false;

            q++;
        }

        alternatives = [.. list];
        p = q + 1;
        return true;
    }

    /// <summary>
    /// Reads a run of literal characters, written as the emitter writes them; a separator is read as <c>/</c>.
    /// </summary>
    /// <remarks>
    /// The run stops at a regex construct or at an optional separator. A quantifier after it, or an escape that is not
    /// a literal character, fails the read, as does an escaped separator character in a Windows-style source, where a
    /// separator is a class and an escaped one would match only itself.
    /// </remarks>
    /// <param name="source">The regex source.</param>
    /// <param name="p">The index to read from; on success, the index after the run.</param>
    /// <param name="end">The index the shape ends at.</param>
    /// <param name="windows">Whether separators are written as Windows separator classes.</param>
    /// <param name="literal">The literal text read, possibly empty.</param>
    /// <returns><see langword="true"/> if the text read is literal; otherwise, <see langword="false"/>.</returns>
    private static bool TryParseLiteral(string source, ref int p, int end, bool windows, out string literal)
    {
        literal = "";
        var sb = new StringBuilder();
        int q = p;
        while (q < end)
        {
            char c = source[q];
            if (c == '\\')
            {
                if (q + 1 >= end)
                    return false;

                char escaped = source[q + 1];
                if ((escaped < 128 && char.IsLetterOrDigit(escaped)) || (windows && escaped is '/' or '\\'))
                    return false;

                if (escaped == '/' && q + 2 < source.Length && source[q + 2] == '?')
                    break;

                sb.Append(escaped);
                q += 2;
                continue;
            }

            if (windows && At(source, q, RegexFreeVocabulary.c_WindowsSeparator))
            {
                if (q + RegexFreeVocabulary.c_WindowsSeparator.Length < source.Length && source[q + RegexFreeVocabulary.c_WindowsSeparator.Length] == '?')
                    break;

                sb.Append('/');
                q += RegexFreeVocabulary.c_WindowsSeparator.Length;
                continue;
            }

            if (c is '^' or '$' or '.' or '|' or '?' or '*' or '+' or '(' or ')' or '[' or ']' or '{' or '}')
                break;

            sb.Append(c);
            q++;
        }

        // A quantifier would apply to the last character read.
        if (q < end && source[q] is '?' or '*' or '+' or '{')
            return false;

        literal = sb.ToString();
        p = q;
        return true;
    }

    /// <summary>
    /// Determines whether <paramref name="c"/> is a line terminator: line feed, carriage return, line separator or paragraph separator.
    /// </summary>
    /// <param name="c">The character.</param>
    /// <returns><see langword="true"/> if <paramref name="c"/> is a line terminator; otherwise, <see langword="false"/>.</returns>
    private static bool IsLineTerminator(char c)
    {
        return c is (char)10 or (char)13 or (char)0x2028 or (char)0x2029;
    }

    /// <summary>
    /// Determines whether <paramref name="input"/> matches the regex this matcher stands for.
    /// </summary>
    /// <param name="input">The input.</param>
    /// <returns><see langword="true"/> if the regex matches <paramref name="input"/>; otherwise, <see langword="false"/>.</returns>
    public bool IsMatch(ReadOnlySpan<char> input)
    {
        int n = input.Length;
        int start = 0;
        if (_headLiteral != null)
        {
            int h = _headLiteral.Length;
            if (n <= h || !LiteralAt(input, 0, _headLiteral) || !IsSeparator(input[h]))
                return false;

            start = h + 1;
        }

        if (_wholeGlobstar)
            return FirstDotSegment(input, 0, n) == n;

        if (_trailingGlobstar)
            return MatchTrailing(input, start);

        if (MatchEnd(input, start, n))
            return true;

        return _optionalSeparator && n > start && IsSeparator(input[n - 1]) && MatchEnd(input, start, n - 1);
    }

    /// <summary>
    /// Determines whether <paramref name="c"/> is a separator.
    /// </summary>
    /// <param name="c">The character.</param>
    /// <returns><see langword="true"/> if <paramref name="c"/> is <c>/</c>, or <c>\</c> for a Windows-style regex; otherwise, <see langword="false"/>.</returns>
    private bool IsSeparator(char c)
    {
        return c == '/' || (_windows && c == '\\');
    }

    /// <summary>
    /// Determines whether the input from <paramref name="start"/> to <paramref name="end"/> matches the leading segments, star and literals.
    /// </summary>
    /// <param name="input">The input.</param>
    /// <param name="start">The index the leading segments, star or literal start at.</param>
    /// <param name="end">The index the literals must end at.</param>
    /// <returns><see langword="true"/> if the range matches; otherwise, <see langword="false"/>.</returns>
    private bool MatchEnd(ReadOnlySpan<char> input, int start, int end)
    {
        if (_alternatives == null)
            return MatchTail(input, start, end, "");

        foreach (string alternative in _alternatives)
        {
            if (MatchTail(input, start, end, alternative))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Determines whether the input from <paramref name="start"/> to <paramref name="end"/> matches the leading segments,
    /// star and literal, followed by <paramref name="alternative"/>.
    /// </summary>
    /// <param name="input">The input.</param>
    /// <param name="start">The index the leading segments, star or literal start at.</param>
    /// <param name="end">The index the literals must end at.</param>
    /// <param name="alternative">The alternative that follows the literal.</param>
    /// <returns><see langword="true"/> if the range matches; otherwise, <see langword="false"/>.</returns>
    private bool MatchTail(ReadOnlySpan<char> input, int start, int end, string alternative)
    {
        int literalStart = end - _literal.Length - alternative.Length;
        if (literalStart < start || !LiteralAt(input, literalStart, _literal) || !LiteralAt(input, literalStart + _literal.Length, alternative))
            return false;

        if (_star)
        {
            // The star run takes the last segment up to the literal; leading segments take the rest.
            var run = input[start..literalStart];
            int separator = _windows ? run.LastIndexOfAny('/', '\\') : run.LastIndexOf('/');
            if (separator >= 0 && !_leadingSegments)
                return false;

            int starStart = start + separator + 1;
            if (starStart >= input.Length || input[starStart] == '.' || IsLineTerminator(input[starStart]))
                return false;

            return !_leadingSegments || FirstDotSegment(input, start, starStart) == starStart;
        }

        if (literalStart == start)
            return true;

        return _leadingSegments && IsSeparator(input[literalStart - 1]) && FirstDotSegment(input, start, literalStart) == literalStart;
    }

    /// <summary>
    /// Determines whether the input from <paramref name="start"/> matches the leading segments, literal and trailing globstar.
    /// </summary>
    /// <param name="input">The input.</param>
    /// <param name="start">The index the leading segments or literal start at.</param>
    /// <returns><see langword="true"/> if the input matches; otherwise, <see langword="false"/>.</returns>
    private bool MatchTrailing(ReadOnlySpan<char> input, int start)
    {
        int n = input.Length;
        int length = _literal.Length;
        if (!_leadingSegments && !LiteralAt(input, start, _literal))
            return false;

        // The literal must occur somewhere; checking that first avoids scanning for dot segments.
        if (_leadingSegments && (!_windows || _literal.IndexOf('/') < 0) && input[start..].IndexOf(_literal.AsSpan(), StringComparison.Ordinal) < 0)
            return false;

        int firstDot = FirstDotSegment(input, start, n);
        int lastDot = LastDotSegment(input, start, n);
        int last = Math.Min(firstDot, n - length);
        for (int i = start; i <= last; i++)
        {
            if (i > start)
            {
                if (!_leadingSegments)
                    return false;

                if (!IsSeparator(input[i - 1]))
                    continue;
            }

            if (!LiteralAt(input, i, _literal))
                continue;

            int after = i + length;
            if (after == n || (IsSeparator(input[after]) && lastDot <= after))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Determines whether <paramref name="input"/> has <paramref name="literal"/> at <paramref name="index"/>.
    /// </summary>
    /// <param name="input">The input.</param>
    /// <param name="index">The index to look at.</param>
    /// <param name="literal">The literal; for a Windows-style regex, a <c>/</c> in it matches either separator.</param>
    /// <returns><see langword="true"/> if <paramref name="literal"/> is at <paramref name="index"/>; otherwise, <see langword="false"/>.</returns>
    private bool LiteralAt(ReadOnlySpan<char> input, int index, string literal)
    {
        if (index < 0 || index + literal.Length > input.Length)
            return false;

        var slice = input.Slice(index, literal.Length);
        if (slice.SequenceEqual(literal.AsSpan()))
            return true;

        if (!_windows)
            return false;

        for (int i = 0; i < literal.Length; i++)
        {
            char c = literal[i];
            if (c != slice[i] && !(c == '/' && slice[i] == '\\'))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Finds the first segment in a range of the input that starts with a dot.
    /// </summary>
    /// <param name="input">The input.</param>
    /// <param name="start">The start of the range.</param>
    /// <param name="end">The end of the range, exclusive.</param>
    /// <returns>The first index in the range of a dot at the start of the input or after a separator, or <paramref name="end"/> if there is none.</returns>
    private int FirstDotSegment(ReadOnlySpan<char> input, int start, int end)
    {
        int i = start;
        while (i < end)
        {
            int k = input[i..end].IndexOf('.');
            if (k < 0)
                return end;

            i += k;
            if (i == 0 || IsSeparator(input[i - 1]))
                return i;

            i++;
        }

        return end;
    }

    /// <summary>
    /// Finds the last segment in a range of the input that starts with a dot.
    /// </summary>
    /// <param name="input">The input.</param>
    /// <param name="start">The start of the range.</param>
    /// <param name="end">The end of the range, exclusive.</param>
    /// <returns>The last index in the range of a dot at the start of the input or after a separator, or -1 if there is none.</returns>
    private int LastDotSegment(ReadOnlySpan<char> input, int start, int end)
    {
        int i = end;
        while (i > start)
        {
            int k = input[start..i].LastIndexOf('.');
            if (k < 0)
                return -1;

            int index = start + k;
            if (index == 0 || IsSeparator(input[index - 1]))
                return index;

            i = index;
        }

        return -1;
    }
}