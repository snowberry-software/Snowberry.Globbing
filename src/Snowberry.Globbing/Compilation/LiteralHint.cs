using System;
using System.Text.RegularExpressions;
using Snowberry.Globbing.Syntax;
using Snowberry.Globbing.Utilities;

namespace Snowberry.Globbing.Compilation;

/// <summary>
/// Literal text that every input matching a pattern must contain, checked before running the regex.
/// </summary>
/// <remarks>
/// A hint only rejects inputs the regex would also reject; when it is present the regex still decides.
/// </remarks>
internal sealed class LiteralHint
{
    private const int c_MaxContainsLength = 64;

    private readonly string[]? _ordered;
    private readonly string? _prefix;
    private readonly string? _suffix;
    private readonly bool _windowsSeparators;

    /// <summary>
    /// Initializes a new instance of the <see cref="LiteralHint"/> class.
    /// </summary>
    /// <param name="prefix">The text a matching input starts with, or <see langword="null"/>.</param>
    /// <param name="suffix">The text a matching input ends with, optionally followed by one separator, or <see langword="null"/>.</param>
    /// <param name="ordered">
    /// The texts a matching input contains in this order without overlapping, after <paramref name="prefix"/> and before
    /// <paramref name="suffix"/>, or <see langword="null"/>.
    /// </param>
    /// <param name="windowsSeparators"><see langword="true"/> if <c>\</c> is also a separator after <paramref name="suffix"/>.</param>
    public LiteralHint(string? prefix, string? suffix, string[]? ordered, bool windowsSeparators)
    {
        _prefix = prefix;
        _suffix = suffix;
        _ordered = ordered;
        _windowsSeparators = windowsSeparators;
    }

    /// <summary>
    /// Gets a rough cost, in relative units, of rejecting a typical input with this hint and, when it passes, the regex.
    /// </summary>
    public int EstimatedRejectCost
    {
        get
        {
            const int c_MinSelectiveLength = 3;
            if ((_prefix?.Length ?? 0) >= c_MinSelectiveLength || (_suffix?.Length ?? 0) >= c_MinSelectiveLength)
                return 3;

            return _ordered != null && Array.Exists(_ordered, run => run.Length >= c_MinSelectiveLength) ? 8 : RegexRunCost;
        }
    }

    /// <summary>Gets a rough cost, in relative units, of running a typical pattern regex against a typical path.</summary>
    public static int RegexRunCost => 300;

    /// <summary>
    /// Finds the literal text every match of <paramref name="body"/> must contain.
    /// </summary>
    /// <remarks>
    /// Only runs of plain literals and dots directly in the root sequence count; any other node ends a run. The first run is a
    /// prefix if it starts the body and the last a suffix if it ends it, but only when the anchors mean start and end of input, that
    /// is without <see cref="GlobOptions.MatchSubstring"/> and <see cref="RegexOptions.Multiline"/>. The other runs are required in
    /// pattern order, without overlapping, between the prefix and the suffix. Whitespace and <c>#</c> end a run because
    /// <see cref="RegexOptions.IgnorePatternWhitespace"/> would change their meaning.
    /// </remarks>
    /// <param name="body">The pattern body, without a leading negation or <c>./</c>.</param>
    /// <param name="options">The options the body is compiled with.</param>
    /// <param name="source">The original pattern, reported in exceptions.</param>
    /// <param name="offset">The position of <paramref name="body"/> in <paramref name="source"/>.</param>
    /// <param name="plain"><see langword="true"/> if the body is compiled as plain text with wildcards.</param>
    /// <returns>
    /// The hint, or <see langword="null"/> if no literal run is found, if matching ignores case (<see cref="GlobOptions.IgnoreCase"/> or
    /// <see cref="RegexOptions.IgnoreCase"/>), if any literal is written raw as with <see cref="GlobOptions.Unescape"/>, or if the root
    /// sequence contains a top-level <c>|</c> alternation.
    /// </returns>
    /// <exception cref="GlobParseException"><paramref name="body"/> is too deeply nested, or has unbalanced delimiters with <see cref="GlobOptions.StrictBrackets"/>.</exception>
    public static LiteralHint? Find(ReadOnlySpan<char> body, GlobOptions options, string source, int offset, bool plain)
    {
        if (!AppliesTo(options))
            return null;

        var tree = GlobSyntaxTree.Parse(body, options, source, offset, plain);
        try
        {
            return Find(tree.Nodes, tree.Root, options);
        }
        finally
        {
            tree.Dispose();
        }
    }

    /// <summary>
    /// Finds the literal hint of an already parsed pattern body, as <see cref="Find(ReadOnlySpan{char}, GlobOptions, string, int, bool)"/> does.
    /// </summary>
    /// <param name="nodes">The nodes of the parsed body.</param>
    /// <param name="root">The index of the root sequence in <paramref name="nodes"/>.</param>
    /// <param name="options">The options the body is compiled with.</param>
    /// <returns>The hint, or <see langword="null"/> under the same conditions as the overload that parses the body.</returns>
    public static LiteralHint? Find(ReadOnlySpan<SyntaxNode> nodes, int root, GlobOptions options)
    {
        if (!CanUseLiterals(nodes, options))
            return null;

        // Anchors only pin the literal to the ends of the input when they mean start and end of input.
        bool anchored = !options.MatchSubstring && (options.RegexOptions & RegexOptions.Multiline) == 0;

        // The runs are collected in one buffer; only the prefix, suffix and ordered runs become strings.
        Span<char> textBuffer = stackalloc char[128];
        var texts = new ValueStringBuilder(textBuffer);
        Span<int> endBuffer = stackalloc int[16];
        var ends = new ValueList<int>(endBuffer);
        try
        {
            bool prefixRun = false;
            bool suffixRun = false;
            bool runAtStart = true;
            int runStart = 0;
            for (int i = nodes[root].FirstChild; i >= 0; i = nodes[i].Next)
            {
                ref readonly var node = ref nodes[i];
                if (node.Kind == SyntaxKind.Pipe)
                    return null;

                char c = node.Kind switch
                {
                    SyntaxKind.Dot => '.',
                    SyntaxKind.Literal when (LiteralForm)node.Count == LiteralForm.Plain && IsVerbatim(node.Value) => node.Value,
                    _ => '\0',
                };

                if (c != '\0')
                {
                    texts.Append(c);
                    continue;
                }

                if (texts.Length > runStart)
                {
                    ends.Add(texts.Length);
                    runStart = texts.Length;
                    prefixRun |= anchored && runAtStart;
                }

                runAtStart = false;
            }

            if (texts.Length > runStart)
            {
                ends.Add(texts.Length);
                prefixRun |= anchored && runAtStart;
                suffixRun = anchored;
            }

            // A long run is shortened; any part of it is required too.
            int first = prefixRun ? 1 : 0;
            int count = ends.Count - first - (suffixRun ? 1 : 0);
            if (!prefixRun && !suffixRun && count <= 0)
                return null;

            var all = texts.Slice(0);
            string? prefix = prefixRun ? all[..ends[0]].ToString() : null;
            string? suffix = null;
            if (suffixRun)
            {
                int last = ends.Count - 1;
                suffix = last == 0 && prefix != null ? prefix : all[(last == 0 ? 0 : ends[last - 1])..ends[last]].ToString();
            }

            string[]? ordered = null;
            if (count > 0)
            {
                ordered = new string[count];
                for (int k = 0; k < count; k++)
                {
                    int index = first + k;
                    int start = index == 0 ? 0 : ends[index - 1];
                    ordered[k] = all.Slice(start, Math.Min(ends[index] - start, c_MaxContainsLength)).ToString();
                }
            }

            return new LiteralHint(prefix, suffix, ordered, options.PathStyle == GlobPathStyle.Windows);
        }
        finally
        {
            texts.Dispose();
            ends.Dispose();
        }
    }

    /// <summary>
    /// Determines whether the literal text of a parsed pattern can be relied on: matching is case-sensitive and no node
    /// is verbatim regex text, which can quantify or alternate the text around it.
    /// </summary>
    /// <param name="nodes">The nodes of the parsed pattern.</param>
    /// <param name="options">The options the pattern is compiled with.</param>
    /// <returns><see langword="true"/> if literal text can be used; otherwise, <see langword="false"/>.</returns>
    public static bool CanUseLiterals(ReadOnlySpan<SyntaxNode> nodes, GlobOptions options)
    {
        return AppliesTo(options) && !ContainsVerbatimRegex(nodes, options, orGroup: false);
    }

    /// <summary>
    /// Determines whether any node is written as verbatim regex text: a raw literal, as with <see cref="GlobOptions.Unescape"/>,
    /// or a brace range written by <see cref="GlobOptions.BraceRangeExpander"/>.
    /// </summary>
    /// <param name="nodes">The nodes of the parsed pattern.</param>
    /// <param name="options">The options the pattern is compiled with.</param>
    /// <param name="orGroup"><see langword="true"/> to count a regex group as verbatim regex text too.</param>
    /// <returns><see langword="true"/> if a node is verbatim regex text; otherwise, <see langword="false"/>.</returns>
    public static bool ContainsVerbatimRegex(ReadOnlySpan<SyntaxNode> nodes, GlobOptions options, bool orGroup)
    {
        foreach (ref readonly var node in nodes)
        {
            if ((orGroup && node.Kind == SyntaxKind.Group)
                || (node.Kind == SyntaxKind.Literal && (LiteralForm)node.Count == LiteralForm.Raw)
                || (node.Kind == SyntaxKind.BraceRange && options.BraceRangeExpander != null))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Determines whether a literal <paramref name="c"/> can be part of literal text: it keeps its meaning under
    /// <see cref="RegexOptions.IgnorePatternWhitespace"/> and is not the <c>\0</c> that ends a run of <see cref="Find(ReadOnlySpan{SyntaxNode}, int, GlobOptions)"/>.
    /// </summary>
    /// <param name="c">The character.</param>
    /// <returns><see langword="false"/> for <c>\0</c>, whitespace and <c>#</c>; otherwise, <see langword="true"/>.</returns>
    public static bool IsVerbatim(char c)
    {
        return c != '\0' && c != '#' && !char.IsWhiteSpace(c);
    }

    /// <summary>
    /// Determines whether a literal hint can be used under <paramref name="options"/>.
    /// </summary>
    /// <param name="options">The options the pattern is compiled with.</param>
    /// <returns><see langword="false"/> if matching ignores case; otherwise, <see langword="true"/>.</returns>
    private static bool AppliesTo(GlobOptions options)
    {
        return !options.IgnoreCase && (options.RegexOptions & RegexOptions.IgnoreCase) == 0;
    }

    /// <summary>Gets a value indicating whether a matching input must start with literal text.</summary>
    public bool HasPrefix => _prefix != null;

    /// <summary>Gets a value indicating whether a matching input must contain literal text away from its ends.</summary>
    public bool HasOrdered => _ordered != null;

    /// <summary>
    /// Determines whether <paramref name="input"/> has the required literal text.
    /// </summary>
    /// <param name="input">The input passed to the regex.</param>
    /// <returns><see langword="false"/> if the regex cannot match <paramref name="input"/>; otherwise, <see langword="true"/>.</returns>
    public bool IsSatisfiedBy(string input)
    {
        // The string overloads are noticeably cheaper than their span counterparts for these short ordinal checks.
        if (_prefix != null && !input.StartsWith(_prefix, StringComparison.Ordinal))
            return false;

        return (_suffix == null && _ordered == null) || IsSatisfiedAfterPrefix(input);
    }

    /// <summary>
    /// Checks the suffix and the ordered runs of <paramref name="input"/>, which starts with the prefix.
    /// </summary>
    /// <param name="input">The input passed to the regex.</param>
    /// <returns><see langword="false"/> if the regex cannot match <paramref name="input"/>; otherwise, <see langword="true"/>.</returns>
    private bool IsSatisfiedAfterPrefix(string input)
    {
        int end = input.Length;
        if (_suffix != null)
        {
            if (input.EndsWith(_suffix, StringComparison.Ordinal))
                end -= _suffix.Length;
            else if (EndsWithSuffixAndSeparator(input.AsSpan()))
                end -= _suffix.Length + 1;
            else
                return false;
        }

        if (_ordered == null)
            return true;

        return _ordered.Length == 1 && _prefix == null && _suffix == null ? input.Contains(_ordered[0]) : ContainsInOrder(input, end);
    }

    /// <summary>
    /// Determines whether <paramref name="input"/> has the required literal text.
    /// </summary>
    /// <param name="input">The input passed to the regex.</param>
    /// <returns><see langword="false"/> if the regex cannot match <paramref name="input"/>; otherwise, <see langword="true"/>.</returns>
    public bool IsSatisfiedBy(ReadOnlySpan<char> input)
    {
        if (_prefix != null && !input.StartsWith(_prefix.AsSpan(), StringComparison.Ordinal))
            return false;

        int end = input.Length;
        if (_suffix != null)
        {
            if (input.EndsWith(_suffix.AsSpan(), StringComparison.Ordinal))
                end -= _suffix.Length;
            else if (EndsWithSuffixAndSeparator(input))
                end -= _suffix.Length + 1;
            else
                return false;
        }

        return _ordered == null || ContainsInOrder(input, end);
    }

    /// <summary>
    /// Determines whether the ordered runs occur in <paramref name="input"/>, in order and without overlapping, after the prefix and before <paramref name="end"/>.
    /// </summary>
    /// <param name="input">The input passed to the regex.</param>
    /// <param name="end">The position the suffix starts at, or the input length if there is no suffix.</param>
    /// <returns><see langword="true"/> if every run is found; otherwise, <see langword="false"/>.</returns>
    private bool ContainsInOrder(string input, int end)
    {
        int start = _prefix?.Length ?? 0;
        foreach (string run in _ordered!)
        {
            if (end - start < run.Length)
                return false;

            int index = input.IndexOf(run, start, end - start, StringComparison.Ordinal);
            if (index < 0)
                return false;

            start = index + run.Length;
        }

        return true;
    }

    /// <summary>
    /// Determines whether the ordered runs occur in <paramref name="input"/>, in order and without overlapping, after the prefix and before <paramref name="end"/>.
    /// </summary>
    /// <param name="input">The input passed to the regex.</param>
    /// <param name="end">The position the suffix starts at, or the input length if there is no suffix.</param>
    /// <returns><see langword="true"/> if every run is found; otherwise, <see langword="false"/>.</returns>
    private bool ContainsInOrder(ReadOnlySpan<char> input, int end)
    {
        int start = _prefix?.Length ?? 0;
        foreach (string run in _ordered!)
        {
            if (end - start < run.Length)
                return false;

            int index = input[start..end].IndexOf(run.AsSpan(), StringComparison.Ordinal);
            if (index < 0)
                return false;

            start += index + run.Length;
        }

        return true;
    }

    /// <summary>
    /// Determines whether <paramref name="input"/> is the required suffix followed by one trailing separator.
    /// </summary>
    /// <remarks>Compact forms such as <c>*.js</c> also match a trailing separator.</remarks>
    /// <param name="input">The input passed to the regex.</param>
    /// <returns><see langword="true"/> if <paramref name="input"/> ends in the suffix and a separator; otherwise, <see langword="false"/>.</returns>
    private bool EndsWithSuffixAndSeparator(ReadOnlySpan<char> input)
    {
        return input.Length > 0
            && (input[^1] == '/' || (_windowsSeparators && input[^1] == '\\'))
            && input[..^1].EndsWith(_suffix.AsSpan(), StringComparison.Ordinal);
    }
}