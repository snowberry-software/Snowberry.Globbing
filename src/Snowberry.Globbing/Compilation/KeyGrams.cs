using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Snowberry.Globbing.Syntax;

namespace Snowberry.Globbing.Compilation;

/// <summary>
/// Finds key sets of a pattern: sets of three-character substrings (trigrams) of which every input the pattern matches
/// contains at least one.
/// </summary>
/// <remarks>Keys come from runs of literals, dots and simple bracket expressions in the root sequence.</remarks>
internal static class KeyGrams
{
    /// <summary>The number of characters in a trigram.</summary>
    public const int c_GramLength = 3;

    private const ulong c_GramMask = 0xFFFF_FFFF_FFFF;
    private const int c_MaxGramsPerWindow = 128;
    private const int c_MaxWindowElements = 3;
    private const int c_MaxWindows = 64;

    private static readonly string[][] s_AsciiLiterals = CreateAsciiLiterals();

    /// <summary>
    /// Finds the key sets of a parsed pattern body.
    /// </summary>
    /// <param name="nodes">The nodes of the parsed body.</param>
    /// <param name="root">The index of the root sequence in <paramref name="nodes"/>.</param>
    /// <param name="pattern">The parsed body text.</param>
    /// <param name="options">The options the body is compiled with.</param>
    /// <returns>
    /// The key sets, or <see langword="null"/> if there are none, if matching ignores case, if a brace range is written by
    /// <see cref="GlobOptions.BraceRangeExpander"/>, if a literal is written raw, or if the root sequence has a top-level <c>|</c>.
    /// </returns>
    public static ulong[][]? Find(ReadOnlySpan<SyntaxNode> nodes, int root, ReadOnlySpan<char> pattern, GlobOptions options)
    {
        if (!LiteralHint.CanUseLiterals(nodes, options))
            return null;

        var elements = new List<string[]>();
        var windows = new List<ulong[]>();
        for (int i = nodes[root].FirstChild; i >= 0; i = nodes[i].Next)
        {
            ref readonly var node = ref nodes[i];
            if (node.Kind == SyntaxKind.Pipe)
                return null;

            string[]? alternatives = node.Kind switch
            {
                SyntaxKind.Dot => Literal('.'),
                SyntaxKind.Literal when (LiteralForm)node.Count == LiteralForm.Plain && LiteralHint.IsVerbatim(node.Value) => Literal(node.Value),
                SyntaxKind.CharClass => ClassAlternatives(pattern.Slice(node.Start + 1, node.Length - 2), options),
                _ => null,
            };

            if (alternatives != null)
                elements.Add(alternatives);
            else
                Flush(elements, windows);
        }

        Flush(elements, windows);
        return windows.Count > 0 ? [.. windows] : null;
    }

    /// <summary>
    /// Packs three characters into a trigram.
    /// </summary>
    /// <param name="first">The first character.</param>
    /// <param name="second">The second character.</param>
    /// <param name="third">The third character.</param>
    /// <returns>The trigram, one character per 16 bits.</returns>
    public static ulong Pack(char first, char second, char third)
    {
        return ((ulong)first << 32) | ((ulong)second << 16) | third;
    }

    /// <summary>
    /// Appends <paramref name="c"/> to a trigram, dropping its first character.
    /// </summary>
    /// <param name="gram">The trigram, packed as by <see cref="Pack"/>.</param>
    /// <param name="c">The character to append.</param>
    /// <returns>The trigram of the last two characters of <paramref name="gram"/> and <paramref name="c"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong Append(ulong gram, char c)
    {
        return ((gram << 16) | c) & c_GramMask;
    }

    /// <summary>
    /// Returns the texts a bracket expression matches, as <see cref="CharClassWriter.Write"/> compiles it.
    /// </summary>
    /// <param name="content">The text between the brackets.</param>
    /// <param name="options">The options.</param>
    /// <returns>
    /// The alternatives, or <see langword="null"/> unless the content is only ASCII letters, digits, <c>.</c>, <c>_</c>
    /// and <c>-</c>, without negation and without <see cref="GlobOptions.RegexQuantifiers"/>.
    /// </returns>
    private static string[]? ClassAlternatives(ReadOnlySpan<char> content, GlobOptions options)
    {
        if (content.IsEmpty || options.RegexQuantifiers)
            return null;

        bool hasRegexChars = false;
        var chars = new SortedSet<char>();
        for (int i = 0; i < content.Length; i++)
        {
            char c = content[i];
            if (!IsSimple(c))
                return null;

            if (c is '.' or '-')
                hasRegexChars = true;

            if (c == '-' && i > 0 && i < content.Length - 1)
            {
                char low = content[i - 1];
                char high = content[i + 1];
                if (low == '-' || high == '-' || high < low || (i >= 2 && content[i - 2] == '-'))
                    return null;

                for (char r = low; r <= high; r++)
                    chars.Add(r);

                i++;
                continue;
            }

            chars.Add(c);
        }

        bool literal = options.BracketMode != GlobBracketMode.CharacterClass && !hasRegexChars;
        bool oneChar = !literal || options.BracketMode != GlobBracketMode.Literal;
        var alternatives = new List<string>();
        if (literal)
            alternatives.Add(string.Concat("[", content.ToString(), "]"));

        if (oneChar)
        {
            foreach (char c in chars)
                alternatives.Add(new string(c, 1));
        }

        return [.. alternatives];

        static bool IsSimple(char c) => c is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9') or '.' or '_' or '-';
    }

    /// <summary>
    /// Adds one key set per start position of <paramref name="elements"/> to <paramref name="windows"/>, then clears <paramref name="elements"/>.
    /// </summary>
    /// <param name="elements">The alternatives of each element of a run.</param>
    /// <param name="windows">The key sets found so far.</param>
    private static void Flush(List<string[]> elements, List<ulong[]> windows)
    {
        if (elements.Count == 0)
            return;

        HashSet<ulong>? grams = null;
        for (int start = 0; start < elements.Count && windows.Count < c_MaxWindows; start++)
        {
            if (start + c_GramLength <= elements.Count && IsSingleChar(elements[start]) && IsSingleChar(elements[start + 1]) && IsSingleChar(elements[start + 2]))
            {
                windows.Add([Pack(elements[start][0][0], elements[start + 1][0][0], elements[start + 2][0][0])]);
                continue;
            }

            for (int length = 1; length <= c_MaxWindowElements && start + length <= elements.Count; length++)
            {
                long product = 1;
                for (int k = start; k < start + length && product <= c_MaxGramsPerWindow; k++)
                    product *= elements[k].Length;

                if (product > c_MaxGramsPerWindow)
                    break;

                // Every text is at least a trigram long exactly when the shortest one is.
                if (ShortestLength(elements, start, start + length) < c_GramLength)
                    continue;

                grams ??= [];
                grams.Clear();
                Collect(elements, start, start + length, 0, grams);
                windows.Add([.. grams]);
                break;
            }
        }

        elements.Clear();
    }

    /// <summary>
    /// Adds the last trigram of every text that elements <paramref name="from"/> to <paramref name="to"/> can match after the text before them.
    /// </summary>
    /// <param name="elements">The elements of the run.</param>
    /// <param name="from">The first element still to append.</param>
    /// <param name="to">The element after the window.</param>
    /// <param name="last">The last three characters of the text before <paramref name="from"/>, packed as by <see cref="Pack"/>.</param>
    /// <param name="grams">The trigrams found; every text must be at least a trigram long.</param>
    private static void Collect(List<string[]> elements, int from, int to, ulong last, HashSet<ulong> grams)
    {
        if (from == to)
        {
            grams.Add(last);
            return;
        }

        foreach (string alternative in elements[from])
        {
            ulong next = last;
            foreach (char c in alternative.Length > c_GramLength ? alternative.AsSpan(alternative.Length - c_GramLength) : alternative.AsSpan())
                next = Append(next, c);

            Collect(elements, from + 1, to, next, grams);
        }
    }

    /// <summary>
    /// Gets the length of the shortest text that elements <paramref name="from"/> to <paramref name="to"/> can match.
    /// </summary>
    /// <param name="elements">The elements of the run.</param>
    /// <param name="from">The first element of the window.</param>
    /// <param name="to">The element after the window.</param>
    /// <returns>The sum of the shortest alternative of each element.</returns>
    private static int ShortestLength(List<string[]> elements, int from, int to)
    {
        int total = 0;
        for (int k = from; k < to; k++)
        {
            int shortest = int.MaxValue;
            foreach (string alternative in elements[k])
                shortest = Math.Min(shortest, alternative.Length);

            total += shortest;
        }

        return total;
    }

    /// <summary>
    /// Returns the alternatives of a literal character.
    /// </summary>
    /// <param name="c">The character.</param>
    /// <returns>A one-element array; shared, and so never modified, for ASCII characters.</returns>
    private static string[] Literal(char c)
    {
        return c < s_AsciiLiterals.Length ? s_AsciiLiterals[c] : [new string(c, 1)];
    }

    /// <summary>
    /// Determines whether an element matches exactly one character.
    /// </summary>
    /// <param name="alternatives">The alternatives of the element.</param>
    /// <returns><see langword="true"/> for a single one-character alternative; otherwise, <see langword="false"/>.</returns>
    private static bool IsSingleChar(string[] alternatives)
    {
        return alternatives.Length == 1 && alternatives[0].Length == 1;
    }

    /// <summary>
    /// Creates the shared alternatives of the ASCII characters.
    /// </summary>
    /// <returns>One one-element array per ASCII character.</returns>
    private static string[][] CreateAsciiLiterals()
    {
        string[][] literals = new string[128][];
        for (int c = 0; c < literals.Length; c++)
            literals[c] = [new string((char)c, 1)];

        return literals;
    }
}
