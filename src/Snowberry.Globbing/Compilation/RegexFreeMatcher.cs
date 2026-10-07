using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Snowberry.Globbing.Compilation;

/// <summary>
/// Decides without running a regex whether an input matches one of the regex shapes the emitter writes for the most
/// common globs, such as <c>*.js</c>, <c>*.min.*.js</c>, <c>**/*.js</c>, <c>*.{js,ts}</c>, <c>src/**</c>, <c>src/**/*.cs</c>,
/// <c>**/bin/**</c>, <c>**</c>, <c>*-*.js</c>, <c>*.*</c>, plain literals and ASCII bracket expressions such as <c>*.[jt]s</c> or <c>?</c>,
/// including the <see cref="GlobOptions.MatchDotFiles"/> forms of a star that starts the input.
/// </summary>
/// <remarks>
/// The shape is recognized from the regex source; any other source, or a regex option that changes its meaning, runs the regex.
/// Literals are at most 256 characters, so matching is linear in the input; it ignores <see cref="GlobOptions.MatchTimeout"/>.
/// </remarks>
internal sealed class RegexFreeMatcher
{
    private const string c_End = ")$(?!\\n)";
    private const int c_MaxInnerLiterals = 8;
    private const int c_MaxLiteralLength = 256;
    private const string c_Start = "^(?:";

    private static readonly RegexFreeVocabulary s_Posix = new(windows: false);
    private static readonly RegexFreeVocabulary s_Windows = new(windows: true);

    private const int c_MaxTails = 64;

    private readonly string? _headLiteral;
    private readonly string[]? _inner;
    private readonly bool _leadingSegments;
    private readonly string _literal;
    private readonly bool _optionalSeparator;
    private readonly bool _star;
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
    /// <param name="wholeGlobstar">Whether the regex is a whole-input globstar.</param>
    /// <param name="star">Whether a star run comes before the literal.</param>
    /// <param name="starDots">Whether the star run may start with a dot unless its segment is <c>.</c> or <c>..</c>.</param>
    /// <param name="starMayBeEmpty">Whether the star run may be empty or start with a line terminator, and only must not start with a dot.</param>
    /// <param name="lastStarGuarded">Whether the character after the last inner literal must exist and not be a line terminator.</param>
    /// <param name="starConsumes">Whether the star run takes the first character of its segment even when a literal follows.</param>
    /// <param name="inner">The literals, in order, that the star run contains, each followed by another star run, or <see langword="null"/> if there are none.</param>
    /// <param name="tails">The alternatives for the text that ends the shape; the first is the literal before a trailing globstar.</param>
    /// <param name="trailingGlobstar">Whether a trailing globstar follows the literal.</param>
    /// <param name="optionalSeparator">Whether one separator may end the input.</param>
    private RegexFreeMatcher(
        bool windows,
        string? headLiteral,
        bool leadingSegments,
        bool wholeGlobstar,
        bool star,
        bool starDots,
        bool starMayBeEmpty,
        bool lastStarGuarded,
        bool starConsumes,
        string[]? inner,
        RegexFreeLiteral[] tails,
        bool trailingGlobstar,
        bool optionalSeparator)
    {
        _windows = windows;
        _headLiteral = headLiteral;
        _leadingSegments = leadingSegments;
        _wholeGlobstar = wholeGlobstar;
        _star = star;
        _starDots = starDots;
        _starMayBeEmpty = starMayBeEmpty;
        _lastStarGuarded = lastStarGuarded;
        _starConsumes = starConsumes;
        _inner = inner;
        _tails = tails;
        _literal = tails[0].Text;
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
            if (!TryParseLiteral(source, ref q, end, windows, out var head))
                return null;

            if (At(source, q, v.MiddleGlobstar))
            {
                if (head.HasClasses)
                    return null;

                headLiteral = head.Text;
                leading = true;
                p = q + v.MiddleGlobstar.Length;
            }
        }

        int bound = -1;
        bool starDots = false;
        bool starMayBeEmpty = false;
        int starLength = whole ? 0 : v.StarLengthAt(source, p, out bound, out starDots, out starMayBeEmpty);
        bool star = starLength > 0;

        // Only the first star sees the start of the input, where the dot-segment guard applies.
        if (starDots && (leading || headLiteral != null))
            return null;

        bool starConsumes = star && At(source, p, v.ConsumingStarStart);
        p += starLength;

        var literal = RegexFreeLiteral.Empty;
        if (!whole && !TryParseLiteral(source, ref p, end, windows, out literal))
            return null;

        // A star bounded by a character, as in [^/-]*-, is a plain star only when that character and another star follow it.
        List<string>? inner = null;
        int middle;
        bool guarded = false;
        while (star && literal.Length > 0 && (middle = v.MiddleStarLengthAt(source, p, out int nextBound, out bool nextGuarded)) > 0)
        {
            // Only the last star may be guarded.
            if (guarded || literal.HasClasses || (bound >= 0 && (literal.Length != 1 || literal.Text[0] != bound)))
                return null;

            (inner ??= []).Add(literal.Text);
            bound = nextBound;
            guarded = nextGuarded;
            p += middle;
            if (!TryParseLiteral(source, ref p, end, windows, out literal))
                return null;
        }

        if (bound >= 0)
            return null;

        RegexFreeLiteral[] tails = [literal];
        bool grouped = false;
        while (!whole && p < end && source[p] == '(' && !At(source, p, v.TrailingGlobstar))
        {
            if (!TryParseAlternatives(source, ref p, end, windows, out var alternatives) || !TryParseLiteral(source, ref p, end, windows, out var after))
                return null;

            grouped = true;
            if (tails.Length * alternatives.Length > c_MaxTails)
                return null;

            var product = new RegexFreeLiteral[tails.Length * alternatives.Length];
            for (int i = 0; i < tails.Length; i++)
            {
                for (int j = 0; j < alternatives.Length; j++)
                    product[(i * alternatives.Length) + j] = RegexFreeLiteral.Concat(RegexFreeLiteral.Concat(tails[i], alternatives[j]), after);
            }

            tails = product;
        }

        bool trailing = !whole && !star && !grouped && !literal.HasClasses && literal.Length > 0 && At(source, p, v.TrailingGlobstar);
        if (trailing)
            p += v.TrailingGlobstar.Length;

        bool optionalSeparator = !trailing && end - p == v.Separator.Length + 1 && At(source, p, v.Separator) && source[end - 1] == '?';
        if (optionalSeparator)
            p = end;

        if (p != end)
            return null;

        // Matching costs up to the input length times the literal length.
        if (literal.Length > c_MaxLiteralLength || headLiteral?.Length > c_MaxLiteralLength || Array.Exists(tails, t => t.Length > 2 * c_MaxLiteralLength)
            || (inner != null && (inner.Count > c_MaxInnerLiterals || inner.Exists(i => i.Length > c_MaxLiteralLength || HasSlash(i)))))
            return null;

        // A star run does not take a separator, so the text after it must not have one.
        if (star && Array.Exists(tails, t => HasSlash(t.Text)))
            return null;

        // The end-of-input alternative of a middle globstar must stay unable to match, so the rest needs a character.
        if (headLiteral != null && !star && Array.Exists(tails, t => t.Length == 0))
            return null;

        return new RegexFreeMatcher(windows, headLiteral, leading, whole, star, starDots, starMayBeEmpty, guarded, starConsumes, inner?.ToArray(), tails, trailing, optionalSeparator);
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
    private static bool TryParseAlternatives(string source, ref int p, int end, bool windows, out RegexFreeLiteral[] alternatives)
    {
        alternatives = [];
        if (!At(source, p, "(?:"))
            return false;

        int q = p + 3;
        var list = new List<RegexFreeLiteral>();
        while (true)
        {
            if (!TryParseLiteral(source, ref q, end, windows, out var alternative))
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
    /// <param name="literal">The literal read, possibly empty.</param>
    /// <returns><see langword="true"/> if the text read is literal; otherwise, <see langword="false"/>.</returns>
    private static bool TryParseLiteral(string source, ref int p, int end, bool windows, out RegexFreeLiteral literal)
    {
        literal = RegexFreeLiteral.Empty;
        var sb = new StringBuilder();
        List<byte>? kinds = null;
        List<ulong>? sets = null;
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

                kinds?.Add(RegexFreeLiteral.c_Literal);
                sets?.Add(0);
                sets?.Add(0);
                sb.Append(escaped);
                q += 2;
                continue;
            }

            if (windows && At(source, q, RegexFreeVocabulary.c_WindowsSeparator))
            {
                if (q + RegexFreeVocabulary.c_WindowsSeparator.Length < source.Length && source[q + RegexFreeVocabulary.c_WindowsSeparator.Length] == '?')
                    break;

                kinds?.Add(RegexFreeLiteral.c_Literal);
                sets?.Add(0);
                sets?.Add(0);
                sb.Append('/');
                q += RegexFreeVocabulary.c_WindowsSeparator.Length;
                continue;
            }

            // A class with a quantifier, such as a star, ends the run.
            int r = q;
            if (c == '[' && TryParseClass(source, ref r, end, windows, out ulong low, out ulong high, out bool nonAscii) && !(r < end && source[r] is '?' or '*' or '+' or '{'))
            {
                q = r;
                if (kinds == null)
                {
                    kinds = [.. new byte[sb.Length]];
                    sets = [.. new ulong[2 * sb.Length]];
                }

                kinds.Add(nonAscii ? RegexFreeLiteral.c_ClassWithNonAscii : RegexFreeLiteral.c_Class);
                sets!.Add(low);
                sets.Add(high);
                sb.Append('\0');
                continue;
            }

            if (c is '^' or '$' or '.' or '|' or '?' or '*' or '+' or '(' or ')' or '[' or ']' or '{' or '}')
                break;

            kinds?.Add(RegexFreeLiteral.c_Literal);
            sets?.Add(0);
            sets?.Add(0);
            sb.Append(c);
            q++;
        }

        // A quantifier would apply to the last character read.
        if (q < end && source[q] is '?' or '*' or '+' or '{')
            return false;

        literal = sb.Length == 0 ? RegexFreeLiteral.Empty : new RegexFreeLiteral(sb.ToString(), kinds?.ToArray(), sets?.ToArray());
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
            if (member < 128 && char.IsLetterOrDigit(member))
                return false;

            q++;
        }

        q++;
        return member < 128;
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
        var tails = _tails;
        if (tails.Length == 1)
            return MatchTail(input, start, end, tails[0]);

        foreach (var tail in _tails)
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
            else if ((_starConsumes && starStart == literalStart) || starStart >= input.Length || IsLineTerminator(input[starStart])
                || (_starDots ? IsDotSegment(input, starStart) : input[starStart] == '.'))
            {
                return false;
            }

            if (_inner != null && !ContainsInner(input, _starConsumes ? starStart + 1 : starStart, literalStart))
                return false;

            return !_leadingSegments || FirstDotSegment(input, start, starStart) == starStart;
        }

        if (literalStart == start)
            return true;

        return _leadingSegments && IsSeparator(input[literalStart - 1]) && FirstDotSegment(input, start, literalStart) == literalStart;
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
                if (_lastStarGuarded && i == inner.Length - 1 && (after >= input.Length || IsLineTerminator(input[after])))
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