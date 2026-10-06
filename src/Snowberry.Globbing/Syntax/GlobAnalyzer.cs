using System;
using System.Collections.Generic;
using Snowberry.Globbing.Utilities;

namespace Snowberry.Globbing.Syntax;

/// <summary>
/// Describes the structure of a glob pattern from the same syntax tree the compiler uses.
/// </summary>
internal static class GlobAnalyzer
{
    /// <summary>
    /// Analyzes <paramref name="pattern"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="GlobOptions.StrictBrackets"/> is ignored, so unbalanced delimiters are reported as literal text. Segments
    /// are split at the top-level separators of the body. The base path ends at the last top-level separator before the
    /// first top-level glob node, and a pattern rooted at <c>/</c> keeps <c>/</c> as its base. With
    /// <see cref="GlobOptions.Unescape"/> set, escaping backslashes are removed from the base path and glob part.
    /// </remarks>
    /// <param name="pattern">The glob pattern.</param>
    /// <param name="options">The options that enable or disable syntax.</param>
    /// <returns>The structure of the pattern.</returns>
    /// <exception cref="GlobParseException">The pattern nests groups, braces or extended globs more than <see cref="GlobSyntaxParser.c_MaxNestingDepth"/> levels deep.</exception>
    public static GlobInfo Analyze(string pattern, GlobOptions options)
    {
        // Analysis reports unbalanced delimiters as literal text instead of rejecting them.
        if (options.StrictBrackets)
            options = options with { StrictBrackets = false };

        int start = PatternPrefix.BodyStart(pattern.AsSpan(), options, out bool negated);
        var body = pattern.AsSpan(start);

        var tree = GlobSyntaxTree.Parse(body, options, pattern, start, plain: false);
        try
        {
            var nodes = tree.Nodes;
            var features = GlobFeatures.None;
            for (int i = 0; i < nodes.Length; i++)
                features |= FeaturesOf(in nodes[i]);

            var tokens = tree.Tokens;
            var matches = tree.Matches;
            for (int i = 0; i < tokens.Length && (features & GlobFeatures.Braces) == 0; i++)
            {
                if (tokens[i].Kind == GlobTokenKind.OpenBrace && matches[i] >= 0)
                    features |= GlobFeatures.Braces;
            }

            if (!options.Extglobs)
                features &= ~GlobFeatures.Extglob;

            var segments = new List<string>();
            int segmentStart = 0;
            int baseEnd = -1;
            bool globSeen = false;
            ref readonly var root = ref nodes[tree.Root];

            for (int i = root.FirstChild; i >= 0; i = nodes[i].Next)
            {
                ref readonly var node = ref nodes[i];
                if (node.Kind == SyntaxKind.Separator)
                {
                    segments.Add(body[segmentStart..node.Start].ToString());
                    segmentStart = node.End;
                    if (!globSeen)
                        baseEnd = node.Start;
                }
                else if (!globSeen && (FeaturesOf(in node) & GlobFeatures.Glob) != 0)
                {
                    globSeen = true;
                }
            }

            segments.Add(body[segmentStart..].ToString());

            bool isGlob = (features & GlobFeatures.Glob) != 0;
            string basePath;
            string globPart;
            if (!isGlob)
            {
                basePath = body.ToString();
                globPart = "";
            }
            else if (baseEnd < 0)
            {
                basePath = "";
                globPart = body.ToString();
            }
            else
            {
                // A pattern rooted at "/" keeps "/" as its base.
                basePath = body[..Math.Max(baseEnd, 1)].ToString();
                globPart = body[(baseEnd + 1)..].ToString();
            }

            if (options.Unescape)
            {
                basePath = RemoveEscapes(basePath);
                globPart = RemoveEscapes(globPart);
            }

            bool negatedExtglob = root.FirstChild >= 0 && nodes[root.FirstChild].Kind == SyntaxKind.Extglob && nodes[root.FirstChild].Value == '!';

            return new GlobInfo(
                pattern,
                pattern[..start],
                basePath,
                globPart,
                isGlob,
                negated,
                negatedExtglob,
                (features & GlobFeatures.Braces) != 0,
                (features & GlobFeatures.Brackets) != 0,
                (features & GlobFeatures.Extglob) != 0,
                (features & GlobFeatures.Globstar) != 0,
                segments);
        }
        finally
        {
            tree.Dispose();
        }
    }

    /// <summary>
    /// Maps a syntax node to the glob features it contributes.
    /// </summary>
    /// <param name="node">The node to classify.</param>
    /// <returns>The features of <paramref name="node"/>, or <see cref="GlobFeatures.None"/> if it is plain text.</returns>
    private static GlobFeatures FeaturesOf(in SyntaxNode node)
    {
        return node.Kind switch
        {
            SyntaxKind.Star => node.Count >= 2 ? GlobFeatures.Glob | GlobFeatures.Globstar : GlobFeatures.Glob,
            SyntaxKind.Question or SyntaxKind.Group => GlobFeatures.Glob,
            SyntaxKind.CharClass => GlobFeatures.Glob | GlobFeatures.Brackets,
            SyntaxKind.Brace or SyntaxKind.BraceRange => GlobFeatures.Glob | GlobFeatures.Braces,
            SyntaxKind.Extglob => GlobFeatures.Glob | GlobFeatures.Extglob,
            _ => GlobFeatures.None,
        };
    }

    /// <summary>
    /// Removes escaping backslashes outside bracket expressions.
    /// </summary>
    /// <remarks>
    /// A bracket expression runs from an unescaped <c>[</c> to the next unescaped <c>]</c> and is copied unchanged. A
    /// trailing backslash is kept.
    /// </remarks>
    /// <param name="text">The text to unescape.</param>
    /// <returns>The text without escaping backslashes, or <paramref name="text"/> itself if it has none.</returns>
    private static string RemoveEscapes(string text)
    {
        if (text.IndexOf('\\') < 0)
            return text;

        Span<char> buffer = stackalloc char[256];
        var sb = new ValueStringBuilder(buffer);
        bool inBracket = false;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (inBracket)
            {
                sb.Append(c);
                if (c == '\\' && i + 1 < text.Length)
                    sb.Append(text[++i]);
                else if (c == ']')
                    inBracket = false;
                continue;
            }

            if (c == '\\' && i + 1 < text.Length)
            {
                sb.Append(text[++i]);
                continue;
            }

            inBracket = c == '[';
            sb.Append(c);
        }

        return sb.ToString();
    }
}
