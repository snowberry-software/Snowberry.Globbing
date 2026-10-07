using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Snowberry.Globbing.Syntax;

namespace Snowberry.Globbing.Compilation;

/// <summary>
/// Finds key sets of a pattern: sets of three-character substrings (trigrams) of which every input the pattern matches
/// contains at least one.
/// </summary>
/// <remarks>Keys come from runs of literals, dots and simple bracket expressions in the root sequence.</remarks>
internal static class KeyGrams
{
    private const int c_GramLength = 3;
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
        if (options.IgnoreCase || (options.RegexOptions & RegexOptions.IgnoreCase) != 0)
            return null;

        foreach (ref readonly var node in nodes)
        {
            if (LiteralHint.IsVerbatimRegex(in node, options))
                return null;
        }

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
                SyntaxKind.Literal when (LiteralForm)node.Count == LiteralForm.Plain && IsVerbatim(node.Value) => Literal(node.Value),
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
    /// Packs three characters of <paramref name="text"/> into a trigram.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="index">The position of the first character.</param>
    /// <returns>The trigram, one character per 16 bits.</returns>
    public static ulong Pack(ReadOnlySpan<char> text, int index)
    {
        return ((ulong)text[index] << 32) | ((ulong)text[index + 1] << 16) | text[index + 2];
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
                windows.Add([((ulong)elements[start][0][0] << 32) | ((ulong)elements[start + 1][0][0] << 16) | elements[start + 2][0][0]]);
                continue;
            }

            for (int length = 1; length <= c_MaxWindowElements && start + length <= elements.Count; length++)
            {
                long product = 1;
                for (int k = start; k < start + length && product <= c_MaxGramsPerWindow; k++)
                    product *= elements[k].Length;

                if (product > c_MaxGramsPerWindow)
                    break;

                grams ??= [];
                grams.Clear();
                if (Collect(elements, start, start + length, string.Empty, grams))
                {
                    windows.Add([.. grams]);
                    break;
                }
            }
        }

        elements.Clear();
    }

    /// <summary>
    /// Adds the last trigram of every text that elements <paramref name="from"/> to <paramref name="to"/> can match after <paramref name="prefix"/>.
    /// </summary>
    /// <param name="elements">The elements of the run.</param>
    /// <param name="from">The first element still to append.</param>
    /// <param name="to">The element after the window.</param>
    /// <param name="prefix">The text of the elements before <paramref name="from"/>.</param>
    /// <param name="grams">The trigrams found.</param>
    /// <returns><see langword="false"/> if some text is shorter than a trigram; otherwise, <see langword="true"/>.</returns>
    private static bool Collect(List<string[]> elements, int from, int to, string prefix, HashSet<ulong> grams)
    {
        if (from == to)
        {
            if (prefix.Length < c_GramLength)
                return false;

            grams.Add(Pack(prefix.AsSpan(), prefix.Length - c_GramLength));
            return true;
        }

        foreach (string alternative in elements[from])
        {
            if (!Collect(elements, from + 1, to, prefix + alternative, grams))
                return false;
        }

        return true;
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
    /// Determines whether a literal keeps its meaning under <see cref="RegexOptions.IgnorePatternWhitespace"/>, as <see cref="LiteralHint"/> requires.
    /// </summary>
    /// <param name="c">The character.</param>
    /// <returns><see langword="false"/> for <c>\0</c>, whitespace and <c>#</c>; otherwise, <see langword="true"/>.</returns>
    private static bool IsVerbatim(char c)
    {
        return c != '\0' && c != '#' && !char.IsWhiteSpace(c);
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
