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
    private readonly string? _prefix;
    private readonly string? _suffix;
    private readonly string? _contains;
    private readonly bool _windowsSeparators;

    /// <summary>
    /// Initializes a new instance of the <see cref="LiteralHint"/> class.
    /// </summary>
    /// <param name="prefix">The text a matching input starts with, or <see langword="null"/>.</param>
    /// <param name="suffix">The text a matching input ends with, optionally followed by one separator, or <see langword="null"/>.</param>
    /// <param name="contains">The text a matching input contains, or <see langword="null"/>.</param>
    /// <param name="windowsSeparators"><see langword="true"/> if <c>\</c> is also a separator after <paramref name="suffix"/>.</param>
    public LiteralHint(string? prefix, string? suffix, string? contains, bool windowsSeparators)
    {
        _prefix = prefix;
        _suffix = suffix;
        _contains = contains;
        _windowsSeparators = windowsSeparators;
    }

    /// <summary>
    /// Finds the literal text every match of <paramref name="body"/> must contain.
    /// </summary>
    /// <remarks>
    /// Only runs of plain literals and dots directly in the root sequence count; any other node ends a run. The first run is a
    /// prefix if it starts the body and the last a suffix if it ends it, but only when the anchors mean start and end of input, that
    /// is without <see cref="GlobOptions.MatchSubstring"/> and <see cref="RegexOptions.Multiline"/>. The longest run is also required
    /// anywhere in the input unless it is already the prefix or suffix. Whitespace and <c>#</c> end a run because
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
        // Case-insensitive regex matching does not agree with ordinal case folding for every character.
        var regexOptions = options.RegexOptions;
        if (options.IgnoreCase || (regexOptions & RegexOptions.IgnoreCase) != 0)
            return null;

        // Anchors only pin the literal to the ends of the input when they mean start and end of input.
        bool anchored = !options.MatchSubstring && (regexOptions & RegexOptions.Multiline) == 0;

        string? prefix = null;
        string? suffix = null;
        string? longest = null;
        var tree = GlobSyntaxTree.Parse(body, options, source, offset, plain);
        try
        {
            var nodes = tree.Nodes;

            // A character written raw, as with GlobOptions.Unescape, can quantify or alternate the text around it.
            foreach (ref readonly var node in nodes)
            {
                if (node.Kind == SyntaxKind.Literal && (LiteralForm)node.Count == LiteralForm.Raw)
                    return null;
            }

            Span<char> buffer = stackalloc char[64];
            var run = new ValueStringBuilder(buffer);
            bool runAtStart = true;

            for (int i = nodes[tree.Root].FirstChild; i >= 0; i = nodes[i].Next)
            {
                ref readonly var node = ref nodes[i];
                if (node.Kind == SyntaxKind.Pipe)
                {
                    run.Dispose();
                    return null;
                }

                char c = node.Kind switch
                {
                    SyntaxKind.Dot => '.',
                    SyntaxKind.Literal when (LiteralForm)node.Count == LiteralForm.Plain && IsVerbatim(node.Value) => node.Value,
                    _ => '\0',
                };

                if (c != '\0')
                {
                    run.Append(c);
                    continue;
                }

                Close(ref run, runAtStart, atEnd: false);
                runAtStart = false;
            }

            Close(ref run, runAtStart, atEnd: true);
            run.Dispose();
        }
        finally
        {
            tree.Dispose();
        }

        if (prefix == null && suffix == null && longest == null)
            return null;

        // The prefix and suffix already check the longest run when it is one of them.
        string? contains = longest == prefix || longest == suffix ? null : longest;
        return new LiteralHint(prefix, suffix, contains, options.PathStyle == GlobPathStyle.Windows);

        void Close(ref ValueStringBuilder run, bool isPrefix, bool atEnd)
        {
            if (run.Length == 0)
                return;

            string text = run.Slice(0).ToString();
            run.Length = 0;
            if (anchored && isPrefix)
                prefix = text;
            if (anchored && atEnd)
                suffix = text;
            if (longest == null || text.Length > longest.Length)
                longest = text;
        }
    }

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

        if (_suffix != null && !input.EndsWith(_suffix, StringComparison.Ordinal) && !EndsWithSuffixAndSeparator(input.AsSpan()))
            return false;

        return _contains == null || input.Contains(_contains);
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

        if (_suffix != null && !input.EndsWith(_suffix.AsSpan(), StringComparison.Ordinal) && !EndsWithSuffixAndSeparator(input))
            return false;

        return _contains == null || input.IndexOf(_contains.AsSpan(), StringComparison.Ordinal) >= 0;
    }

    /// <summary>
    /// Determines whether <paramref name="c"/> keeps its literal meaning under <see cref="RegexOptions.IgnorePatternWhitespace"/>.
    /// </summary>
    /// <param name="c">The character.</param>
    /// <returns><see langword="false"/> for whitespace and <c>#</c>; otherwise, <see langword="true"/>.</returns>
    private static bool IsVerbatim(char c)
    {
        return c != '#' && !char.IsWhiteSpace(c);
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
