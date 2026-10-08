using System;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Snowberry.Globbing.Utilities;
using static Snowberry.Globbing.Compilation.RegexFreeVocabulary;

namespace Snowberry.Globbing.Compilation;

/// <summary>
/// Decides without running a regex whether an input matches one of the regex shapes the emitter writes for the most
/// common globs, such as <c>*.js</c>, <c>*.min.*.js</c>, <c>**/*.js</c>, <c>*.{js,ts}</c>, <c>src/**</c>, <c>src/**/*.cs</c>,
/// <c>**/bin/**</c>, <c>**</c>, <c>*-*.js</c>, <c>*.*</c>, plain literals and ASCII bracket expressions such as <c>*.[jt]s</c> or <c>?</c>,
/// including the <see cref="GlobOptions.MatchDotFiles"/> forms of a star that starts the input.
/// </summary>
/// <remarks>
/// The shape is recognized from the regex source; any other source, or a regex option that changes its meaning, runs the regex.
/// Literals are at most <see cref="c_MaxLiteralLength"/> characters, so matching is linear in the input; it ignores <see cref="GlobOptions.MatchTimeout"/>.
/// </remarks>
internal sealed class RegexFreeMatcher
{
    private const string c_End = ")$(?!\\n)";
    private const int c_MaxInnerLiterals = 8;
    private const int c_MaxLiteralLength = 256;
    private const int c_MaxTails = 64;
    private const string c_Start = "^(?:";

    private static readonly RegexFreeVocabulary s_Posix = new(windows: false);
    private static readonly RegexFreeVocabulary s_Windows = new(windows: true);

    private readonly string? _headLiteral;
    private readonly string[]? _inner;
    private readonly bool _dotLeading;
    private readonly bool _leadingSegments;
    private readonly string _literal;
    private readonly bool _optionalSeparator;
    private readonly bool _searchLiteralFirst;
    private readonly bool _star;
    private readonly RegexFreeLiteral? _starClass;
    private readonly bool _starClassOneChar;
    private readonly bool _starConsumes;
    private readonly bool _starDots;
    private readonly bool _starMayBeEmpty;
    private readonly bool _lastStarGuarded;
    private readonly RegexFreeLiteral[] _tails;
    private readonly bool _trailingGlobstar;
    private readonly bool _wholeGlobstar;
    private readonly bool _windows;

    /// <summary>
    /// Initializes a new instance of the <see cref="RegexFreeMatcher"/> class.
    /// </summary>
    /// <param name="windows">Whether <c>\</c> is a separator too, and a <c>/</c> in the literals stands for either separator.</param>
    /// <param name="headLiteral">The literal before a middle globstar, or <see langword="null"/> if there is none.</param>
    /// <param name="leadingSegments">Whether segments that do not start with a dot may come before the star or literal.</param>
    /// <param name="dotLeading">Whether the leading segments, if any, only must not be <c>.</c> or <c>..</c>.</param>
    /// <param name="wholeGlobstar">Whether the regex is a whole-input globstar.</param>
    /// <param name="star">Whether a star run comes before the literal.</param>
    /// <param name="starDots">Whether the star run may start with a dot unless its segment is <c>.</c> or <c>..</c>.</param>
    /// <param name="starMayBeEmpty">Whether the star run may be empty or start with a line terminator, and only must not start with a dot.</param>
    /// <param name="lastStarGuarded">Whether the character after the last inner literal must exist and not be a line terminator.</param>
    /// <param name="starConsumes">Whether the star run takes the first character of its segment even when a literal follows.</param>
    /// <param name="starClass">The class the first character of the star run must be in, or <see langword="null"/> if the run starts with a plain star.</param>
    /// <param name="starClassOneChar">Whether the first character of a <paramref name="starClass"/> run must not be a line terminator either.</param>
    /// <param name="inner">The literals, in order, that the star run contains, each followed by another star run, or <see langword="null"/> if there are none.</param>
    /// <param name="tails">The alternatives for the text that ends the shape; the first is the literal before a trailing globstar.</param>
    /// <param name="trailingGlobstar">Whether a trailing globstar follows the literal.</param>
    /// <param name="literalHasSlash">Whether the literal before a trailing globstar matches a separator.</param>
    /// <param name="optionalSeparator">Whether one separator may end the input.</param>
    private RegexFreeMatcher(
        bool windows,
        string? headLiteral,
        bool leadingSegments,
        bool dotLeading,
        bool wholeGlobstar,
        bool star,
        bool starDots,
        bool starMayBeEmpty,
        bool lastStarGuarded,
        bool starConsumes,
        RegexFreeLiteral? starClass,
        bool starClassOneChar,
        string[]? inner,
        RegexFreeLiteral[] tails,
        bool trailingGlobstar,
        bool literalHasSlash,
        bool optionalSeparator)
    {
        _windows = windows;
        _headLiteral = headLiteral;
        _leadingSegments = leadingSegments;
        _dotLeading = dotLeading;
        _wholeGlobstar = wholeGlobstar;
        _star = star;
        _starDots = starDots;
        _starMayBeEmpty = starMayBeEmpty;
        _lastStarGuarded = lastStarGuarded;
        _starConsumes = starConsumes;
        _starClass = starClass;
        _starClassOneChar = starClassOneChar;
        _inner = inner;
        _tails = tails;
        _literal = tails[0].Text;
        _trailingGlobstar = trailingGlobstar;
        _searchLiteralFirst = leadingSegments && (!windows || !literalHasSlash);
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

        if (!source.StartsWith(c_Start, StringComparison.Ordinal) || !source.EndsWith(c_End, StringComparison.Ordinal))
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
    /// <remarks>The shape is read without allocating; the literals are decoded only once the whole shape is recognized.</remarks>
    /// <param name="source">The regex source, which starts with <c>^(?:</c> and ends with <c>)$(?!\n)</c>.</param>
    /// <param name="v">The regex text of the shape parts.</param>
    /// <returns>The matcher, or <see langword="null"/> if <paramref name="source"/> has no supported shape.</returns>
    private static RegexFreeMatcher? TryParse(string source, RegexFreeVocabulary v)
    {
        int end = source.Length - c_End.Length;
        int p = c_Start.Length;
        if (!TryParseLead(source, ref p, end, v, out var head, out bool hasHead, out bool leading, out bool dotLeading, out bool whole))
            return null;

        int bound = -1;
        bool starDots = false;
        bool starMayBeEmpty = false;
        bool starConsumes = false;
        int starLength = whole ? 0 : v.StarLengthAt(source, p, out bound, out starDots, out starMayBeEmpty, out starConsumes);
        bool star = starLength > 0;
        starConsumes &= star;

        // The dot-segment guard sees the start of the input, so only segments that are not . or .. may come before it.
        if (starDots && leading && !dotLeading)
            return null;

        p += starLength;
        RegexFreeLiteral? starClass = null;
        bool starClassOneChar = false;
        if (!whole && !star && TryParseStarClass(source, ref p, end, v, out starClass, out starClassOneChar, ref bound))
        {
            star = true;
            starConsumes = true;
        }

        RegexFreeLiteralRun literal = default;
        if (!whole && !TryParseLiteral(source, ref p, end, v, out literal))
            return null;

        var inner = star && literal.Length > 0 ? stackalloc RegexFreeLiteralRun[c_MaxInnerLiterals] : default;
        if (!TryParseInner(source, ref p, end, v, star, inner, ref literal, ref bound, out int innerCount, out bool guarded) || bound >= 0)
            return null;

        bool groups = !whole && p < end && source[p] == '(';
        var parts = new ValueList<RegexFreeLiteralRun>(groups ? stackalloc RegexFreeLiteralRun[16] : default);
        var sizes = new ValueList<int>(groups ? stackalloc int[8] : default);
        try
        {
            if (!TryParseGroups(source, ref p, end, v, whole, literal, ref parts, ref sizes, out int tailCount, out int maxTailLength, out bool tailSlash, out bool emptyTail))
                return null;

            bool trailing = !whole && !star && sizes.Count == 0 && !dotLeading && !literal.HasClasses && literal.Length > 0 && At(source, p, v.TrailingGlobstar);
            if (trailing)
                p += v.TrailingGlobstar.Length;

            bool optionalSeparator = !trailing && end - p == v.Separator.Length + 1 && At(source, p, v.Separator) && source[end - 1] == '?';
            if (optionalSeparator)
                p = end;

            if (p != end)
                return null;

            // Matching costs up to the input length times the literal length.
            if (literal.Length > c_MaxLiteralLength || head.Length > c_MaxLiteralLength || maxTailLength > 2 * c_MaxLiteralLength)
                return null;

            // A star run does not take a separator, so the text after it must not have one.
            if (star && tailSlash)
                return null;

            // The end-of-input alternative of a middle globstar must stay unable to match, so the rest needs a character.
            if (hasHead && !star && emptyTail)
                return null;

            string[]? innerTexts = null;
            if (innerCount > 0)
            {
                innerTexts = new string[innerCount];
                for (int i = 0; i < innerCount; i++)
                    innerTexts[i] = DecodeText(source, end, v, inner[i]);
            }

            var tails = new RegexFreeLiteral[tailCount];
            for (int k = 0; k < tailCount; k++)
                tails[k] = DecodeTail(source, end, v, literal, parts.AsSpan(), sizes.AsSpan(), k);

            return new RegexFreeMatcher(
                v.Windows,
                hasHead ? DecodeText(source, end, v, head) : null,
                leading,
                dotLeading,
                whole,
                star,
                starDots,
                starMayBeEmpty,
                guarded,
                starConsumes,
                starClass,
                starClassOneChar,
                innerTexts,
                tails,
                trailing,
                literal.HasSlash,
                optionalSeparator);
        }
        finally
        {
            parts.Dispose();
            sizes.Dispose();
        }
    }

    /// <summary>
    /// Reads what comes before the star or literal: a leading globstar, a whole-input globstar, or a literal followed by a middle globstar.
    /// </summary>
    /// <param name="source">The regex source.</param>
    /// <param name="p">The index to read from; on return, the index after what was read.</param>
    /// <param name="end">The index the shape ends at.</param>
    /// <param name="v">The regex text of the shape parts.</param>
    /// <param name="head">The literal before a middle globstar, if <paramref name="hasHead"/> is set.</param>
    /// <param name="hasHead">Whether a literal and a middle globstar were read.</param>
    /// <param name="leading">Whether segments may come before the star or literal.</param>
    /// <param name="dotLeading">Whether the leading globstar is the one of <see cref="GlobOptions.MatchDotFiles"/>.</param>
    /// <param name="whole">Whether the source is a whole-input globstar.</param>
    /// <returns><see langword="true"/> if the text read is supported; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryParseLead(string source, ref int p, int end, RegexFreeVocabulary v, out RegexFreeLiteralRun head, out bool hasHead, out bool leading, out bool dotLeading, out bool whole)
    {
        head = default;
        hasHead = false;
        leading = false;
        dotLeading = false;
        whole = false;
        int dotGlobstar;
        if (At(source, p, v.LeadingGlobstar))
        {
            leading = true;
            p += v.LeadingGlobstar.Length;
        }
        else if ((dotGlobstar = v.DotLeadingGlobstarLengthAt(source, p)) > 0)
        {
            leading = true;
            dotLeading = true;
            p += dotGlobstar;
        }
        else if (At(source, p, v.WholeGlobstar))
        {
            whole = true;
            p += v.WholeGlobstar.Length;
        }
        else
        {
            int q = p;
            if (!TryParseLiteral(source, ref q, end, v, out var first))
                return false;

            if (At(source, q, v.MiddleGlobstar))
            {
                if (first.HasClasses)
                    return false;

                head = first;
                hasHead = true;
                leading = true;
                p = q + v.MiddleGlobstar.Length;
            }
        }

        return true;
    }

    /// <summary>
    /// Reads a class that starts the star run and takes its first character, as in <c>[a-c]*</c>.
    /// </summary>
    /// <param name="source">The regex source.</param>
    /// <param name="p">The index to read from; on success, the index after the star.</param>
    /// <param name="end">The index the shape ends at.</param>
    /// <param name="v">The regex text of the shape parts.</param>
    /// <param name="starClass">The class, or <see langword="null"/> if there is none.</param>
    /// <param name="oneChar">Whether the class is preceded by a guard that the character is not a line terminator.</param>
    /// <param name="bound">On success, the character the star after the class stops before, or <c>-1</c>.</param>
    /// <returns><see langword="true"/> if a class and a star were read; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryParseStarClass(string source, ref int p, int end, RegexFreeVocabulary v, out RegexFreeLiteral? starClass, out bool oneChar, ref int bound)
    {
        starClass = null;
        int q = p;
        oneChar = At(source, q, v.OneChar);
        if (oneChar)
            q += v.OneChar.Length;

        int r = q;
        int classStar = 0;
        int classBound = -1;
        if (q < end && source[q] == '[' && TryParseClass(source, ref r, end, v.Windows, out ulong low, out ulong high, out bool nonAscii)
            && (classStar = v.MiddleStarLengthAt(source, r, out classBound, out bool classGuarded)) > 0 && !classGuarded)
        {
            starClass = new RegexFreeLiteral("\0", [nonAscii ? RegexFreeLiteral.c_ClassWithNonAscii : RegexFreeLiteral.c_Class], [low, high]);
            bound = classBound;
            p = r + classStar;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Reads the inner literals of a star run, each followed by another star; a star bounded by a character, as in
    /// <c>[^/-]*-</c>, is a plain star only when that character and another star follow it.
    /// </summary>
    /// <param name="source">The regex source.</param>
    /// <param name="p">The index after <paramref name="literal"/>; on success, the index after the last literal.</param>
    /// <param name="end">The index the shape ends at.</param>
    /// <param name="v">The regex text of the shape parts.</param>
    /// <param name="star">Whether a star run comes before <paramref name="literal"/>.</param>
    /// <param name="inner">The storage for the inner literals, of <see cref="c_MaxInnerLiterals"/> elements when <paramref name="star"/> is set and <paramref name="literal"/> is not empty.</param>
    /// <param name="literal">The literal after the star; on success, the literal after the last star.</param>
    /// <param name="bound">The character the star stops before, or <c>-1</c>; on success, that of the last star.</param>
    /// <param name="count">The number of inner literals.</param>
    /// <param name="guarded">Whether the last star is guarded.</param>
    /// <returns><see langword="true"/> if the literals and stars are supported; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryParseInner(string source, ref int p, int end, RegexFreeVocabulary v, bool star, Span<RegexFreeLiteralRun> inner, ref RegexFreeLiteralRun literal, ref int bound, out int count, out bool guarded)
    {
        count = 0;
        guarded = false;
        int middle;
        while (star && literal.Length > 0 && (middle = v.MiddleStarLengthAt(source, p, out int nextBound, out bool nextGuarded)) > 0)
        {
            // Only the last star may be guarded.
            if (guarded || literal.HasClasses || (bound >= 0 && (literal.Length != 1 || literal.First != bound)))
                return false;

            if (count == c_MaxInnerLiterals || literal.Length > c_MaxLiteralLength || literal.HasSlash)
                return false;

            inner[count++] = literal;
            bound = nextBound;
            guarded = nextGuarded;
            p += middle;
            if (!TryParseLiteral(source, ref p, end, v, out literal))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Reads the groups of literal alternatives after the literal; each group adds its alternatives, then the literal after it, to <paramref name="parts"/>.
    /// </summary>
    /// <param name="source">The regex source.</param>
    /// <param name="p">The index after the literal; on success, the index after the last group.</param>
    /// <param name="end">The index the shape ends at.</param>
    /// <param name="v">The regex text of the shape parts.</param>
    /// <param name="whole">Whether the source is a whole-input globstar, which has no groups.</param>
    /// <param name="literal">The literal before the first group.</param>
    /// <param name="parts">The list the alternatives and literals are added to.</param>
    /// <param name="sizes">The list the number of alternatives of each group is added to.</param>
    /// <param name="tailCount">The number of combinations of the alternatives.</param>
    /// <param name="maxTailLength">The length of the longest combination.</param>
    /// <param name="tailSlash">Whether a combination matches a separator.</param>
    /// <param name="emptyTail">Whether a combination is empty.</param>
    /// <returns><see langword="true"/> if the groups are supported; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryParseGroups(string source, ref int p, int end, RegexFreeVocabulary v, bool whole, RegexFreeLiteralRun literal, ref ValueList<RegexFreeLiteralRun> parts, ref ValueList<int> sizes, out int tailCount, out int maxTailLength, out bool tailSlash, out bool emptyTail)
    {
        tailCount = 1;
        maxTailLength = literal.Length;
        tailSlash = literal.HasSlash;
        emptyTail = literal.Length == 0;
        while (!whole && p < end && source[p] == '(' && !At(source, p, v.TrailingGlobstar))
        {
            if (!TryParseAlternatives(source, ref p, end, v, ref parts, out int count, out int maxLength, out bool anyEmpty, out bool anySlash)
                || !TryParseLiteral(source, ref p, end, v, out var after))
                return false;

            if (tailCount * count > c_MaxTails)
                return false;

            tailCount *= count;
            parts.Add(after);
            sizes.Add(count);
            maxTailLength += maxLength + after.Length;
            tailSlash |= anySlash || after.HasSlash;
            emptyTail &= anyEmpty && after.Length == 0;
        }

        return true;
    }

    /// <summary>
    /// Decodes a literal run without character classes.
    /// </summary>
    /// <param name="source">The regex source.</param>
    /// <param name="end">The index the shape ends at.</param>
    /// <param name="v">The regex text of the shape parts.</param>
    /// <param name="run">The run, of at most <see cref="c_MaxLiteralLength"/> characters.</param>
    /// <returns>The text the run matches.</returns>
    private static string DecodeText(string source, int end, RegexFreeVocabulary v, RegexFreeLiteralRun run)
    {
        Span<char> text = stackalloc char[run.Length];
        Decode(source, end, v, run, text, null, null, 0);
        return text.ToString();
    }

    /// <summary>
    /// Decodes one alternative for the text that ends the shape: the literal, then for each group one of its alternatives and the literal after it.
    /// </summary>
    /// <param name="source">The regex source.</param>
    /// <param name="end">The index the shape ends at.</param>
    /// <param name="v">The regex text of the shape parts.</param>
    /// <param name="literal">The literal before the first group.</param>
    /// <param name="parts">For each group, its alternatives and then the literal after it.</param>
    /// <param name="sizes">The number of alternatives of each group.</param>
    /// <param name="index">The index of the alternative; the alternatives of the last group vary fastest.</param>
    /// <returns>The decoded alternative, of at most twice <see cref="c_MaxLiteralLength"/> characters.</returns>
    private static RegexFreeLiteral DecodeTail(string source, int end, RegexFreeVocabulary v, RegexFreeLiteralRun literal, ReadOnlySpan<RegexFreeLiteralRun> parts, ReadOnlySpan<int> sizes, int index)
    {
        // The chosen alternative of each group, as an index into the parts.
        var chosen = sizes.Length == 0 ? default : sizes.Length <= 16 ? stackalloc int[16] : new int[sizes.Length];
        int length = literal.Length;
        bool classes = literal.HasClasses;
        int rest = index;
        int groupEnd = parts.Length;
        for (int g = sizes.Length - 1; g >= 0; g--)
        {
            int size = sizes[g];
            int first = groupEnd - 1 - size;
            int pick = first + (rest % size);
            rest /= size;
            chosen[g] = pick;
            length += parts[pick].Length + parts[groupEnd - 1].Length;
            classes |= parts[pick].HasClasses || parts[groupEnd - 1].HasClasses;
            groupEnd = first;
        }

        if (length == 0)
            return RegexFreeLiteral.Empty;

        Span<char> text = stackalloc char[length];
        byte[]? kinds = classes ? new byte[length] : null;
        ulong[]? sets = classes ? new ulong[2 * length] : null;
        int offset = Decode(source, end, v, literal, text, kinds, sets, 0);
        groupEnd = 0;
        for (int g = 0; g < sizes.Length; g++)
        {
            int after = groupEnd + sizes[g];
            offset = Decode(source, end, v, parts[chosen[g]], text, kinds, sets, offset);
            offset = Decode(source, end, v, parts[after], text, kinds, sets, offset);
            groupEnd = after + 1;
        }

        return new RegexFreeLiteral(text.ToString(), kinds, sets);
    }

    /// <summary>
    /// Writes the characters, and for a class its kind and sets, that a literal run matches.
    /// </summary>
    /// <param name="source">The regex source.</param>
    /// <param name="end">The index the shape ends at.</param>
    /// <param name="v">The regex text of the shape parts.</param>
    /// <param name="run">The run, as read by <see cref="TryParseLiteral"/>.</param>
    /// <param name="text">The text; a class position gets a placeholder character.</param>
    /// <param name="kinds">The kind of each position, or <see langword="null"/> if the run has no classes.</param>
    /// <param name="sets">Two bit masks per position, or <see langword="null"/> if the run has no classes.</param>
    /// <param name="offset">The position to write the first character to.</param>
    /// <returns>The position after the last character written.</returns>
    private static int Decode(string source, int end, RegexFreeVocabulary v, RegexFreeLiteralRun run, Span<char> text, byte[]? kinds, ulong[]? sets, int offset)
    {
        string separator = v.Separator;
        char separatorStart = separator[0];
        int q = run.Start;
        while (q < run.End)
        {
            char c = source[q];
            if (c == separatorStart && At(source, q, separator))
            {
                text[offset++] = '/';
                q += separator.Length;
            }
            else if (c == '\\')
            {
                text[offset++] = source[q + 1];
                q += 2;
            }
            else if (c == '[')
            {
                _ = TryParseClass(source, ref q, end, v.Windows, out ulong low, out ulong high, out bool nonAscii);
                kinds![offset] = nonAscii ? RegexFreeLiteral.c_ClassWithNonAscii : RegexFreeLiteral.c_Class;
                sets![2 * offset] = low;
                sets[(2 * offset) + 1] = high;
                text[offset++] = '\0';
            }
            else
            {
                text[offset++] = c;
                q++;
            }
        }

        return offset;
    }

    /// <summary>
    /// Reads a group of literal alternatives, <c>(?:a|b)</c>, and adds them to <paramref name="parts"/>.
    /// </summary>
    /// <param name="source">The regex source.</param>
    /// <param name="p">The index of the group; on success, the index after it.</param>
    /// <param name="end">The index the shape ends at.</param>
    /// <param name="v">The regex text of the shape parts.</param>
    /// <param name="parts">The list the alternatives are added to.</param>
    /// <param name="count">The number of alternatives.</param>
    /// <param name="maxLength">The length of the longest alternative.</param>
    /// <param name="anyEmpty">Whether an alternative is empty.</param>
    /// <param name="anySlash">Whether an alternative matches a separator.</param>
    /// <returns><see langword="true"/> if the group has only literal alternatives; otherwise, <see langword="false"/>.</returns>
    private static bool TryParseAlternatives(string source, ref int p, int end, RegexFreeVocabulary v, ref ValueList<RegexFreeLiteralRun> parts, out int count, out int maxLength, out bool anyEmpty, out bool anySlash)
    {
        count = 0;
        maxLength = 0;
        anyEmpty = false;
        anySlash = false;
        if (!At(source, p, "(?:"))
            return false;

        int q = p + 3;
        while (true)
        {
            if (!TryParseLiteral(source, ref q, end, v, out var alternative))
                return false;

            parts.Add(alternative);
            count++;
            maxLength = Math.Max(maxLength, alternative.Length);
            anyEmpty |= alternative.Length == 0;
            anySlash |= alternative.HasSlash;
            if (q >= end)
                return false;

            if (source[q] == ')')
                break;

            if (source[q] != '|')
                return false;

            q++;
        }

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
    /// <param name="v">The regex text of the shape parts.</param>
    /// <param name="literal">The run read, possibly empty.</param>
    /// <returns><see langword="true"/> if the text read is literal; otherwise, <see langword="false"/>.</returns>
    private static bool TryParseLiteral(string source, ref int p, int end, RegexFreeVocabulary v, out RegexFreeLiteralRun literal)
    {
        literal = default;
        int length = 0;
        char first = '\0';
        bool classes = false;
        bool slash = false;
        string separator = v.Separator;
        char separatorStart = separator[0];
        int q = p;
        while (q < end)
        {
            char c = source[q];
            char matched;
            if (c == separatorStart && At(source, q, separator))
            {
                int after = q + separator.Length;
                if (after < source.Length && source[after] == '?')
                    break;

                matched = '/';
                q = after;
            }
            else if (c == '\\')
            {
                if (q + 1 >= end)
                    return false;

                char escaped = source[q + 1];
                if (RegexSyntax.IsAsciiLetterOrDigit(escaped) || (v.Windows && escaped is '/' or '\\'))
                    return false;

                matched = escaped;
                q += 2;
            }
            else
            {
                // A class with a quantifier, such as a star, ends the run.
                int r = q;
                if (c == '[' && TryParseClass(source, ref r, end, v.Windows, out _, out _, out _) && !(r < end && source[r] is '?' or '*' or '+' or '{'))
                {
                    q = r;
                    classes = true;
                    matched = '\0';
                }
                else if (c is '^' or '$' or '.' or '|' or '?' or '*' or '+' or '(' or ')' or '[' or ']' or '{' or '}')
                {
                    break;
                }
                else
                {
                    matched = c;
                    q++;
                }
            }

            if (length == 0)
                first = matched;

            slash |= matched == '/';
            length++;
        }

        // A quantifier would apply to the last character read.
        if (q < end && source[q] is '?' or '*' or '+' or '{')
            return false;

        literal = new RegexFreeLiteralRun(p, q, length, first, classes, slash);
        p = q;
        return true;
    }

    /// <summary>
    /// Reads a character class of ASCII characters and ranges, such as <c>[a-z_]</c> or <c>[^./]</c>, that does not match a separator.
    /// </summary>
    /// <param name="source">The regex source.</param>
    /// <param name="p">The index of the <c>[</c>; on success, the index after the class.</param>
    /// <param name="end">The index the shape ends at.</param>
    /// <param name="windows">Whether <c>\</c> is a separator too.</param>
    /// <param name="low">The ASCII characters 0 to 63 the class matches, one bit each.</param>
    /// <param name="high">The ASCII characters 64 to 127 the class matches, one bit each.</param>
    /// <param name="nonAscii">Whether the class matches every non-ASCII character.</param>
    /// <returns><see langword="true"/> if the class has a supported form; otherwise, <see langword="false"/>.</returns>
    private static bool TryParseClass(string source, ref int p, int end, bool windows, out ulong low, out ulong high, out bool nonAscii)
    {
        low = 0;
        high = 0;
        nonAscii = false;
        int q = p + 1;
        bool negated = q < end && source[q] == '^';
        if (negated)
            q++;

        bool any = false;
        while (q < end && source[q] != ']')
        {
            if (!TryReadClassMember(source, ref q, end, out char first))
                return false;

            char last = first;
            if (q + 1 < end && source[q] == '-' && source[q + 1] != ']')
            {
                q++;
                if (!TryReadClassMember(source, ref q, end, out last) || last < first)
                    return false;
            }

            for (int c = first; c <= last; c++)
            {
                if (c < 64)
                    low |= 1UL << c;
                else
                    high |= 1UL << (c - 64);
            }

            any = true;
        }

        if (q >= end || !any)
            return false;

        if (negated)
        {
            low = ~low;
            high = ~high;
            nonAscii = true;
        }

        if (((low >> '/') & 1) != 0 || (windows && ((high >> ('\\' - 64)) & 1) != 0))
            return false;

        p = q + 1;
        return true;
    }

    /// <summary>
    /// Reads one ASCII member of a character class: a character, or a backslash and a character that is not a letter or digit.
    /// </summary>
    /// <param name="source">The regex source.</param>
    /// <param name="q">The index of the member; on success, the index after it.</param>
    /// <param name="end">The index the shape ends at.</param>
    /// <param name="member">The character the member stands for.</param>
    /// <returns><see langword="true"/> if the member is a supported ASCII character; otherwise, <see langword="false"/>.</returns>
    private static bool TryReadClassMember(string source, ref int q, int end, out char member)
    {
        member = source[q];
        if (member == '[')
            return false;

        if (member == '\\')
        {
            if (q + 1 >= end)
                return false;

            member = source[q + 1];
            if (RegexSyntax.IsAsciiLetterOrDigit(member))
                return false;

            q++;
        }

        q++;
        return member < 128;
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
        var tails = _tails;
        if (tails.Length == 1)
            return MatchTail(input, start, end, tails[0]);

        foreach (var tail in tails)
        {
            if (MatchTail(input, start, end, tail))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Determines whether the input from <paramref name="start"/> to <paramref name="end"/> matches the leading segments,
    /// star and inner literals, followed by <paramref name="tail"/>.
    /// </summary>
    /// <param name="input">The input.</param>
    /// <param name="start">The index the leading segments, star or tail start at.</param>
    /// <param name="end">The index the tail must end at.</param>
    /// <param name="tail">The text that ends the shape.</param>
    /// <returns><see langword="true"/> if the range matches; otherwise, <see langword="false"/>.</returns>
    private bool MatchTail(ReadOnlySpan<char> input, int start, int end, RegexFreeLiteral tail)
    {
        int literalStart = end - tail.Length;
        if (literalStart < start || !tail.At(input, literalStart, _windows))
            return false;

        if (_star)
        {
            // The star run takes the last segment up to the literal; leading segments take the rest.
            var run = input[start..literalStart];
            int separator = _windows ? run.LastIndexOfAny('/', '\\') : run.LastIndexOf('/');
            if (separator >= 0 && !_leadingSegments)
                return false;

            int starStart = start + separator + 1;
            if (_starMayBeEmpty)
            {
                if (starStart < input.Length && input[starStart] == '.')
                    return false;
            }
            else if ((_starConsumes && starStart == literalStart) || starStart >= input.Length
                || (_starClass != null
                    ? !_starClass.At(input, starStart, _windows) || (_starClassOneChar && LineTerminators.Is(input[starStart]))
                    : LineTerminators.Is(input[starStart]) || (_starDots ? IsDotSegment(input, starStart) : input[starStart] == '.')))
            {
                return false;
            }

            if (_inner != null && !ContainsInner(input, _starConsumes ? starStart + 1 : starStart, literalStart))
                return false;

            return !_leadingSegments || LeadingSegmentsEnd(input, start, starStart) == starStart;
        }

        if (literalStart == start)
            return true;

        return _leadingSegments && IsSeparator(input[literalStart - 1]) && LeadingSegmentsEnd(input, start, literalStart) == literalStart;
    }

    /// <summary>
    /// Finds where the leading segments between <paramref name="start"/> and <paramref name="end"/> stop matching.
    /// </summary>
    /// <param name="input">The input.</param>
    /// <param name="start">The index the segments start at.</param>
    /// <param name="end">The index after the separator that ends the last segment.</param>
    /// <returns>The index of the first segment that starts with a dot or, for a leading globstar of <see cref="GlobOptions.MatchDotFiles"/>, is <c>.</c> or <c>..</c>; otherwise, <paramref name="end"/>.</returns>
    private int LeadingSegmentsEnd(ReadOnlySpan<char> input, int start, int end)
    {
        if (!_dotLeading)
            return FirstDotSegment(input, start, end);

        int i = start;
        while (i < end)
        {
            i = FirstDotSegment(input, i, end);
            if (i == end || IsDotSegment(input, i))
                return i;

            i++;
        }

        return end;
    }

    /// <summary>
    /// Determines whether the segment at <paramref name="index"/> is <c>.</c> or <c>..</c>.
    /// </summary>
    /// <param name="input">The input.</param>
    /// <param name="index">The index the segment starts at.</param>
    /// <returns><see langword="true"/> if one or two dots at <paramref name="index"/> are followed by a separator or the end of the input; otherwise, <see langword="false"/>.</returns>
    private bool IsDotSegment(ReadOnlySpan<char> input, int index)
    {
        if (input[index] != '.')
            return false;

        int k = index + 1;
        if (k < input.Length && input[k] == '.')
            k++;

        return k == input.Length || IsSeparator(input[k]);
    }

    /// <summary>
    /// Determines whether the inner literals occur in order, without overlapping, between <paramref name="from"/> and <paramref name="to"/>.
    /// </summary>
    /// <param name="input">The input.</param>
    /// <param name="from">The index the first inner literal may start at.</param>
    /// <param name="to">The index the last inner literal must end by.</param>
    /// <returns><see langword="true"/> if every inner literal fits; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool ContainsInner(ReadOnlySpan<char> input, int from, int to)
    {
        string[] inner = _inner!;
        for (int i = 0; i < inner.Length; i++)
        {
            string literal = inner[i];
            while (true)
            {
                if (to - from < literal.Length)
                    return false;

                int index = input[from..to].IndexOf(literal.AsSpan(), StringComparison.Ordinal);
                if (index < 0)
                    return false;

                int after = from + index + literal.Length;

                // A guarded last star needs a non-line-terminator after the literal, so later occurrences are tried.
                if (_lastStarGuarded && i == inner.Length - 1 && (after >= input.Length || LineTerminators.Is(input[after])))
                {
                    from += index + 1;
                    continue;
                }

                from = after;
                break;
            }
        }

        return true;
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
        if (_searchLiteralFirst && input[start..].IndexOf(_literal.AsSpan(), StringComparison.Ordinal) < 0)
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