using System;
using System.Buffers;
using Snowberry.Globbing.Utilities;

namespace Snowberry.Globbing.Syntax;

/// <summary>
/// The tokens and syntax tree of a glob pattern, in pooled buffers.
/// </summary>
/// <remarks>The tree owns its buffers; call <see cref="Dispose"/> once to return them to the pool.</remarks>
internal ref struct GlobSyntaxTree
{
    private ValueList<GlobToken> _tokens;
    private ValueList<SyntaxNode> _nodes;
    private int[]? _matches;

    /// <summary>
    /// Gets the index of the root <see cref="SyntaxKind.Sequence"/> in <see cref="Nodes"/>.
    /// </summary>
    public int Root { get; private set; }

    /// <summary>
    /// Gets the tokens of the pattern.
    /// </summary>
    public readonly ReadOnlySpan<GlobToken> Tokens => _tokens.AsSpan();

    /// <summary>
    /// Gets the syntax nodes, linked by index.
    /// </summary>
    public readonly ReadOnlySpan<SyntaxNode> Nodes => _nodes.AsSpan();

    /// <summary>
    /// Gets, for each token, the index of its paired delimiter token, or -1 if it is not a paired delimiter.
    /// </summary>
    /// <remarks>
    /// For a brace pair that is parsed as literal text, the <c>}</c> is reset to -1 while the <c>{</c> keeps the index of
    /// its <c>}</c>.
    /// </remarks>
    public readonly ReadOnlySpan<int> Matches => _matches.AsSpan(0, _tokens.Count);

    /// <summary>
    /// Lexes and parses <paramref name="pattern"/>.
    /// </summary>
    /// <param name="pattern">The pattern body, without a leading negation or <c>./</c>.</param>
    /// <param name="options">The options that enable or disable syntax.</param>
    /// <param name="source">The original pattern, reported in exceptions.</param>
    /// <param name="offset">The position of <paramref name="pattern"/> in <paramref name="source"/>, used for error offsets.</param>
    /// <param name="plain"><see langword="true"/> if the pattern has no structural syntax, so <c>|</c> is literal and no dot is a leading dot.</param>
    /// <returns>The tree; the caller disposes it.</returns>
    /// <exception cref="GlobParseException">The pattern is too deeply nested, or has unbalanced delimiters with <see cref="GlobOptions.StrictBrackets"/>. The buffers are returned to the pool before the exception propagates.</exception>
    public static GlobSyntaxTree Parse(ReadOnlySpan<char> pattern, GlobOptions options, string source, int offset, bool plain)
    {
        var tree = new GlobSyntaxTree
        {
            _tokens = new ValueList<GlobToken>(pattern.Length + 1),
            _nodes = new ValueList<SyntaxNode>(pattern.Length + 2),
        };

        GlobSyntaxParser parser = default;
        bool parsing = false;
        try
        {
            new GlobLexer(pattern, options, source, offset).Tokenize(ref tree._tokens);

            tree._matches = ArrayPool<int>.Shared.Rent(tree._tokens.Count);
            parser = new GlobSyntaxParser(tree._tokens.AsSpan(), pattern, options, source, offset, plain, tree._matches.AsSpan(0, tree._tokens.Count), tree._nodes);
            parsing = true;
            tree.Root = parser.Parse();
            tree._nodes = parser.Nodes;
            return tree;
        }
        catch
        {
            // The parser owns the node list once it starts and may have replaced its buffer, so release the current one.
            if (parsing)
                tree._nodes = parser.Nodes;

            tree.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Returns the buffers to the pool and resets the tree to its empty default state.
    /// </summary>
    public void Dispose()
    {
        _tokens.Dispose();
        _nodes.Dispose();
        if (_matches != null)
            ArrayPool<int>.Shared.Return(_matches);

        this = default;
    }
}