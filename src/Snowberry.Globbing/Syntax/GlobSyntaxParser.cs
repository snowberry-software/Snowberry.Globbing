using System;
using Snowberry.Globbing.Utilities;

namespace Snowberry.Globbing.Syntax;

/// <summary>
/// Builds the syntax tree of a glob pattern from its tokens.
/// </summary>
/// <remarks>
/// A linear pre-pass pairs every opener with its closer, so an unbalanced <c>(</c>, <c>{</c> or extended glob opener is
/// parsed as literal text without backtracking.
/// </remarks>
internal ref struct GlobSyntaxParser
{
    /// <summary>The deepest supported nesting of groups, braces and extended globs; one level more fails with <see cref="GlobParseError.NestingTooDeep"/>.</summary>
    public const int c_MaxNestingDepth = 256;

    private readonly Span<int> _match;
    private readonly int _offset;
    private readonly GlobOptions _options;
    private readonly ReadOnlySpan<char> _pattern;
    private readonly bool _plain;
    private readonly string _source;

    private readonly ReadOnlySpan<GlobToken> _tokens;
    private ValueList<SyntaxNode> _nodes;

    /// <summary>
    /// Initializes a new instance of the <see cref="GlobSyntaxParser"/> struct.
    /// </summary>
    /// <param name="tokens">The tokens of the pattern.</param>
    /// <param name="pattern">The lexed pattern.</param>
    /// <param name="options">The options.</param>
    /// <param name="source">The original pattern, reported in exceptions.</param>
    /// <param name="offset">The position of <paramref name="pattern"/> in <paramref name="source"/>.</param>
    /// <param name="plain"><see langword="true"/> if the pattern has no structural syntax, so <c>|</c> is literal and dots never start a segment.</param>
    /// <param name="match">Storage with one element per token that receives, for each paired delimiter, the index of its partner, and -1 for every other token.</param>
    /// <param name="nodes">The empty list that receives the nodes, so the root is the first node added. The parser takes it over; read the result back from <see cref="Nodes"/>, since growing replaces its storage.</param>
    public GlobSyntaxParser(ReadOnlySpan<GlobToken> tokens, ReadOnlySpan<char> pattern, GlobOptions options, string source, int offset, bool plain, Span<int> match, ValueList<SyntaxNode> nodes)
    {
        _plain = plain;
        _tokens = tokens;
        _pattern = pattern;
        _options = options;
        _source = source;
        _offset = offset;
        _match = match;
        _nodes = nodes;
    }

    /// <summary>
    /// Parses all tokens into a root <see cref="SyntaxKind.Sequence"/>.
    /// </summary>
    /// <returns>The index of the root node.</returns>
    /// <exception cref="GlobParseException">A parenthesis, extended glob opener or <c>{</c> is unbalanced with <see cref="GlobOptions.StrictBrackets"/> set, or nesting is deeper than <see cref="c_MaxNestingDepth"/>.</exception>
    public int Parse()
    {
        MatchDelimiters();
        return ParseSequence(0, _tokens.Length, SequenceRole.Root, 0, inParens: false, inExtglob: false, inBrace: false, depth: 0);
    }

    /// <summary>
    /// Adds a node that spans <paramref name="token"/> and takes its value from it.
    /// </summary>
    /// <param name="kind">The kind of the node.</param>
    /// <param name="token">The token the node represents.</param>
    /// <param name="count">The count stored on the node, such as the number of stars.</param>
    /// <returns>The index of the new node.</returns>
    private int Add(SyntaxKind kind, GlobToken token, int count = 0)
    {
        var node = SyntaxNode.Create(kind, token.Start, token.Length);
        node.Value = token.Value;
        node.Count = count;
        return _nodes.Add(node);
    }

    /// <summary>
    /// Adds a <see cref="SyntaxKind.Literal"/> node, storing its <see cref="LiteralForm"/> in the node count.
    /// </summary>
    /// <param name="value">The literal character.</param>
    /// <param name="form">How the character was written in the pattern.</param>
    /// <param name="start">The position of the node in the lexed pattern.</param>
    /// <param name="length">The number of pattern characters the node spans.</param>
    /// <returns>The index of the new node.</returns>
    private int AddLiteral(char value, LiteralForm form, int start, int length)
    {
        var node = SyntaxNode.Create(SyntaxKind.Literal, start, length);
        node.Value = value;
        node.Count = (int)form;
        return _nodes.Add(node);
    }

    /// <summary>
    /// Pairs the closer at <paramref name="close"/> with the nearest open delimiter of the same kind. Openers of the
    /// other kind above it stay unmatched, so crossing delimiters such as <c>{a(b}</c> resolve to literals.
    /// </summary>
    /// <param name="stack">The token indexes of the currently open delimiters; truncated to below the matched opener.</param>
    /// <param name="close">The token index of the closer.</param>
    /// <param name="isBrace"><see langword="true"/> if the closer is a <c>}</c>; <see langword="false"/> if it is a <c>)</c>.</param>
    /// <returns><see langword="true"/> if a matching opener was found and paired; otherwise <see langword="false"/>.</returns>
    private readonly bool CloseNearest(ref ValueList<int> stack, int close, bool isBrace)
    {
        for (int s = stack.Count - 1; s >= 0; s--)
        {
            int open = stack[s];
            if (_tokens[open].Kind == GlobTokenKind.OpenBrace != isBrace)
                continue;

            _match[open] = close;
            _match[close] = open;
            stack.Truncate(s);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Creates, without throwing, the exception for a missing delimiter.
    /// </summary>
    /// <param name="error">The kind of error.</param>
    /// <param name="index">The position in the lexed pattern where the delimiter is missing.</param>
    /// <param name="type">The kind of delimiter that is missing, <c>opening</c> or <c>closing</c>.</param>
    /// <param name="ch">The missing delimiter character.</param>
    /// <returns>A <see cref="GlobParseException"/> positioned in the original pattern.</returns>
    private readonly GlobParseException Error(GlobParseError error, int index, string type, char ch)
    {
        return GlobParseException.MissingDelimiter(_source, error, _offset + index, type, ch);
    }

    /// <summary>
    /// Counts the tokens of a regex group prefix such as <c>?:</c>, <c>?=</c>, <c>?!</c>, <c>?&lt;=</c>, <c>?&lt;!</c> or
    /// <c>?&lt;name&gt;</c> at <paramref name="start"/>.
    /// </summary>
    /// <remarks>
    /// The prefix must be a <see cref="GlobTokenKind.Question"/> followed by literal tokens; a group name is one or more
    /// letters, digits or underscores.
    /// </remarks>
    /// <param name="start">The token index of the first token after the <c>(</c>.</param>
    /// <param name="end">The token index of the closing <c>)</c>.</param>
    /// <returns>The number of prefix tokens, or 0 if the group has no regex prefix.</returns>
    private readonly int GroupPrefixLength(int start, int end)
    {
        if (start + 1 >= end || _tokens[start].Kind != GlobTokenKind.Question || _tokens[start + 1].Kind != GlobTokenKind.Literal)
            return 0;

        char c = _tokens[start + 1].Value;
        if (c is ':' or '=' or '!')
            return 2;

        if (c != '<' || start + 2 >= end || _tokens[start + 2].Kind != GlobTokenKind.Literal)
            return 0;

        if (_tokens[start + 2].Value is '=' or '!')
            return 3;

        for (int i = start + 2; i < end && _tokens[i].Kind == GlobTokenKind.Literal; i++)
        {
            char n = _tokens[i].Value;
            if (n == '>')
                return i > start + 2 ? i - start + 1 : 0;

            if (!(char.IsLetterOrDigit(n) || n == '_'))
                return 0;
        }

        return 0;
    }

    /// <summary>
    /// Determines whether the tokens form a range <c>X..Y</c> or <c>X..Y..Z</c> of literal runs.
    /// </summary>
    /// <param name="start">The token index of the first token inside the braces.</param>
    /// <param name="end">The token index of the closing <c>}</c>.</param>
    /// <returns><see langword="true"/> if the tokens are two or three plain literal runs separated by <c>..</c>; otherwise <see langword="false"/>.</returns>
    private readonly bool IsRange(int start, int end)
    {
        int parts = 0;
        int i = start;
        while (true)
        {
            int run = i;
            while (i < end && _tokens[i].Kind == GlobTokenKind.Literal && _tokens[i].Form == LiteralForm.Plain)
                i++;

            if (i == run)
                return false;

            parts++;
            if (i == end)
                return parts is 2 or 3;

            if (i + 1 >= end || _tokens[i].Kind != GlobTokenKind.Dot || _tokens[i + 1].Kind != GlobTokenKind.Dot)
                return false;

            i += 2;
        }
    }

    /// <summary>
    /// Appends a child to a node's list of children, as its first child or as the sibling of the previous child.
    /// </summary>
    /// <param name="parent">The index of the parent node.</param>
    /// <param name="last">The index of the previous child, or -1 if there is none; updated to <paramref name="child"/>.</param>
    /// <param name="child">The index of the child node to append.</param>
    private void Link(int parent, ref int last, int child)
    {
        if (last < 0)
            _nodes[parent].FirstChild = child;
        else
            _nodes[last].Next = child;

        last = child;
    }

    /// <summary>
    /// Fills the matching-delimiter table so that each paired opener and closer holds the index of the other, and
    /// every other token holds -1. Extended glob openers pair with <c>)</c> like <c>(</c>.
    /// </summary>
    /// <exception cref="GlobParseException">
    /// With <see cref="GlobOptions.StrictBrackets"/> set: a <c>)</c> has no opener (<see cref="GlobParseError.MissingOpeningParenthesis"/>),
    /// or a <c>(</c>, extended glob opener or <c>{</c> stays unpaired (<see cref="GlobParseError.MissingClosingParenthesis"/>
    /// or <see cref="GlobParseError.MissingClosingBrace"/>). A <c>}</c> without an opener is never an error.
    /// </exception>
    private readonly void MatchDelimiters()
    {
        _match.Fill(-1);

        Span<int> initial = stackalloc int[32];
        var stack = new ValueList<int>(initial);
        try
        {
            for (int i = 0; i < _tokens.Length; i++)
            {
                switch (_tokens[i].Kind)
                {
                    case GlobTokenKind.OpenParen:
                    case GlobTokenKind.ExtglobOpen:
                    case GlobTokenKind.OpenBrace:
                        stack.Add(i);
                        break;

                    case GlobTokenKind.CloseParen:
                        if (!CloseNearest(ref stack, i, isBrace: false) && _options.StrictBrackets)
                            throw Error(GlobParseError.MissingOpeningParenthesis, _tokens[i].Start, "opening", '(');
                        break;

                    case GlobTokenKind.CloseBrace:
                        CloseNearest(ref stack, i, isBrace: true);
                        break;
                }
            }

            if (_options.StrictBrackets)
            {
                for (int i = 0; i < _tokens.Length; i++)
                {
                    var kind = _tokens[i].Kind;
                    if (_match[i] >= 0 || kind is not (GlobTokenKind.OpenParen or GlobTokenKind.ExtglobOpen or GlobTokenKind.OpenBrace))
                        continue;

                    throw kind == GlobTokenKind.OpenBrace
                        ? Error(GlobParseError.MissingClosingBrace, _tokens[i].Start, "closing", '}')
                        : Error(GlobParseError.MissingClosingParenthesis, _tokens[i].Start, "closing", ')');
                }
            }
        }
        finally
        {
            stack.Dispose();
        }
    }

    /// <summary>
    /// Parses a group or extended glob whose opener is at <paramref name="open"/>, splitting its content on <c>|</c>
    /// tokens that are not inside a nested paired delimiter.
    /// </summary>
    /// <param name="kind">The node kind to create, <see cref="SyntaxKind.Group"/> or <see cref="SyntaxKind.Extglob"/>.</param>
    /// <param name="open">The token index of the paired opener.</param>
    /// <param name="depth">The nesting depth of the sequence that contains the node; the alternatives are parsed one level deeper.</param>
    /// <param name="inExtglob">The <see cref="SyntaxNode.InExtglob"/> value of the alternatives.</param>
    /// <param name="inBrace"><see langword="true"/> to treat every dot in the alternatives as a leading dot, as inside a brace alternative.</param>
    /// <param name="firstToken">The token index where the content starts, or -1 to start right after <paramref name="open"/>.</param>
    /// <returns>The index of the node, whose children are one <see cref="SyntaxKind.Sequence"/> per alternative.</returns>
    /// <exception cref="GlobParseException">The nesting limit is exceeded (<see cref="GlobParseError.NestingTooDeep"/>).</exception>
    private int ParseAlternatives(SyntaxKind kind, int open, int depth, bool inExtglob, bool inBrace, int firstToken = -1)
    {
        int close = _match[open];
        var openToken = _tokens[open];
        var node = SyntaxNode.Create(kind, openToken.Start, _tokens[close].End - openToken.Start);
        node.Value = openToken.Value;
        int index = _nodes.Add(node);

        var role = kind == SyntaxKind.Extglob ? SequenceRole.ExtglobAlternative : SequenceRole.GroupAlternative;
        int start = firstToken < 0 ? open + 1 : firstToken;
        int last = -1;
        int alternative = 0;

        for (int i = start; i <= close; i++)
        {
            if (i < close && _tokens[i].Kind != GlobTokenKind.Pipe)
            {
                if (_match[i] > i)
                    i = _match[i];
                continue;
            }

            int child = ParseSequence(start, i, role, alternative++, inParens: true, inExtglob, inBrace, depth + 1);
            Link(index, ref last, child);
            start = i + 1;
        }

        return index;
    }

    /// <summary>
    /// Parses a parenthesized group, storing the length in characters of its regex prefix such as <c>?:</c> in
    /// <see cref="SyntaxNode.Count"/>; the prefix is not part of the first alternative.
    /// </summary>
    /// <param name="open">The token index of the <c>(</c>, which must be paired.</param>
    /// <param name="depth">The nesting depth of the sequence that contains the group.</param>
    /// <returns>The index of the <see cref="SyntaxKind.Group"/> node.</returns>
    /// <exception cref="GlobParseException">The nesting limit is exceeded (<see cref="GlobParseError.NestingTooDeep"/>).</exception>
    private int ParseGroup(int open, int depth)
    {
        int close = _match[open];
        int contentStart = open + 1;
        int prefixLength = GroupPrefixLength(contentStart, close);

        int group = ParseAlternatives(SyntaxKind.Group, open, depth, inExtglob: false, inBrace: false, firstToken: contentStart + prefixLength);
        _nodes[group].Count = prefixLength == 0 ? 0 : _tokens[contentStart + prefixLength - 1].End - _tokens[contentStart].Start;
        return group;
    }

    /// <summary>
    /// Parses the tokens in <paramref name="from"/> up to <paramref name="to"/> into a <see cref="SyntaxKind.Sequence"/>
    /// node, recursing into paired groups, extended globs and braces.
    /// </summary>
    /// <remarks>
    /// Unpaired openers and closers, commas, a <c>|</c> in a plain pattern, and braces without a comma or range become
    /// literals; the content of a literal brace is parsed in place. A <c>?</c> that starts the first alternative of a group
    /// or extended glob is literal.
    /// </remarks>
    /// <param name="from">The index of the first token.</param>
    /// <param name="to">The index after the last token.</param>
    /// <param name="role">What the sequence is, such as the root or one alternative of a group.</param>
    /// <param name="alternative">The zero-based position of the sequence among its alternatives.</param>
    /// <param name="inParens"><see langword="true"/> if the sequence is inside a group or extended glob.</param>
    /// <param name="inExtglob"><see langword="true"/> if the sequence is inside an extended glob.</param>
    /// <param name="inBrace"><see langword="true"/> if the sequence is inside a brace expression.</param>
    /// <param name="depth">The nesting depth of the sequence, 0 for the root, checked against <see cref="c_MaxNestingDepth"/>.</param>
    /// <returns>The index of the sequence node.</returns>
    /// <exception cref="GlobParseException"><paramref name="depth"/>, or the depth of a nested sequence, exceeds <see cref="c_MaxNestingDepth"/> (<see cref="GlobParseError.NestingTooDeep"/>).</exception>
    /// <exception cref="InvalidOperationException">A token has an unknown <see cref="GlobTokenKind"/>.</exception>
    private int ParseSequence(int from, int to, SequenceRole role, int alternative, bool inParens, bool inExtglob, bool inBrace, int depth)
    {
        if (depth > c_MaxNestingDepth)
            throw new GlobParseException(_source, GlobParseError.NestingTooDeep, _offset + (from < _tokens.Length ? _tokens[from].Start : _pattern.Length), $"The pattern nests groups, braces or extended globs more than {c_MaxNestingDepth} levels deep.");

        var sequence = SyntaxNode.Create(SyntaxKind.Sequence, from < _tokens.Length ? _tokens[from].Start : _pattern.Length, 0);
        sequence.Role = role;
        sequence.Count = alternative;
        sequence.InParens = inParens;
        sequence.InExtglob = inExtglob;
        int index = _nodes.Add(sequence);

        int last = -1;
        SyntaxKind? previous = null;

        for (int i = from; i < to; i++)
        {
            var token = _tokens[i];
            int child;

            switch (token.Kind)
            {
                case GlobTokenKind.Literal:
                    child = AddLiteral(token.Value, token.Form, token.Start, token.Length);
                    break;

                case GlobTokenKind.Star:
                    child = Add(SyntaxKind.Star, token, count: token.Length);
                    break;

                case GlobTokenKind.Question:
                    // A '?' that opens a group or extended glob is literal; "(?:"-style group prefixes are handled by ParseGroup.
                    child = role is SequenceRole.GroupAlternative or SequenceRole.ExtglobAlternative && alternative == 0 && previous == null
                        ? AddLiteral('?', LiteralForm.Plain, token.Start, 1)
                        : Add(SyntaxKind.Question, token);
                    break;

                case GlobTokenKind.Plus:
                    child = Add(SyntaxKind.Plus, token);
                    break;

                case GlobTokenKind.Slash:
                    child = Add(SyntaxKind.Separator, token);
                    break;

                case GlobTokenKind.Dot:
                    child = Add(SyntaxKind.Dot, token);
                    _nodes[child].Flag = !_plain && (inParens || inBrace || previous == SyntaxKind.Separator || (role == SequenceRole.Root && previous == null));
                    break;

                case GlobTokenKind.Pipe when !_plain:
                    child = Add(SyntaxKind.Pipe, token);
                    break;

                case GlobTokenKind.Comma:
                case GlobTokenKind.Pipe:
                case GlobTokenKind.CloseParen:
                case GlobTokenKind.CloseBrace:
                    child = AddLiteral(token.Value, LiteralForm.Plain, token.Start, 1);
                    break;

                case GlobTokenKind.CharClass:
                    child = Add(SyntaxKind.CharClass, token);
                    break;

                case GlobTokenKind.OpenParen:
                    if (_match[i] < 0)
                    {
                        child = AddLiteral('(', LiteralForm.Plain, token.Start, 1);
                        break;
                    }

                    child = ParseGroup(i, depth);
                    i = _match[i];
                    break;

                case GlobTokenKind.ExtglobOpen:
                    if (_match[i] < 0)
                    {
                        child = AddLiteral(token.Value, LiteralForm.Plain, token.Start, 1);
                        Link(index, ref last, child);
                        child = AddLiteral('(', LiteralForm.Plain, token.Start + 1, 1);
                        break;
                    }

                    child = ParseAlternatives(SyntaxKind.Extglob, i, depth, inExtglob: true, inBrace);
                    i = _match[i];
                    break;

                case GlobTokenKind.OpenBrace:
                    if (_match[i] >= 0 && TryParseBrace(i, depth, inParens, inExtglob, out child))
                    {
                        i = _match[i];
                        break;
                    }

                    // A brace without alternatives or a range matches literally; its content is parsed in place.
                    if (_match[i] >= 0)
                        _match[_match[i]] = -1;
                    child = AddLiteral('{', LiteralForm.Plain, token.Start, 1);
                    break;

                default:
                    throw new InvalidOperationException($"Unexpected token {token.Kind}.");
            }

            Link(index, ref last, child);
            previous = _nodes[child].Kind;
        }

        return index;
    }

    /// <summary>
    /// Tries to parse the paired brace at <paramref name="open"/> as a <see cref="SyntaxKind.BraceRange"/> or, if it
    /// has a comma outside nested paired delimiters, a <see cref="SyntaxKind.Brace"/> with one sequence per
    /// comma-separated alternative.
    /// </summary>
    /// <param name="open">The token index of the paired <c>{</c>.</param>
    /// <param name="depth">The nesting depth of the sequence that contains the brace; the alternatives are parsed one level deeper.</param>
    /// <param name="inParens">The <see cref="SyntaxNode.InParens"/> value of the containing sequence, passed on to the alternatives.</param>
    /// <param name="inExtglob">The <see cref="SyntaxNode.InExtglob"/> value of the containing sequence, passed on to the alternatives.</param>
    /// <param name="index">The index of the created node, or -1 if the brace is neither a range nor has alternatives.</param>
    /// <returns><see langword="true"/> if a node was created; <see langword="false"/> if the brace should be literal.</returns>
    /// <exception cref="GlobParseException">The nesting limit is exceeded (<see cref="GlobParseError.NestingTooDeep"/>).</exception>
    private bool TryParseBrace(int open, int depth, bool inParens, bool inExtglob, out int index)
    {
        int close = _match[open];

        if (IsRange(open + 1, close))
        {
            index = _nodes.Add(SyntaxNode.Create(SyntaxKind.BraceRange, _tokens[open].End, _tokens[close].Start - _tokens[open].End));
            return true;
        }

        bool hasComma = false;
        for (int i = open + 1; i < close && !hasComma; i++)
        {
            hasComma = _tokens[i].Kind == GlobTokenKind.Comma;
            if (_match[i] > i)
                i = _match[i];
        }

        if (!hasComma)
        {
            index = -1;
            return false;
        }

        index = _nodes.Add(SyntaxNode.Create(SyntaxKind.Brace, _tokens[open].Start, _tokens[close].End - _tokens[open].Start));
        int last = -1;
        int alternative = 0;
        int start = open + 1;

        for (int i = start; i <= close; i++)
        {
            if (i < close && _tokens[i].Kind != GlobTokenKind.Comma)
            {
                if (_match[i] > i)
                    i = _match[i];
                continue;
            }

            int child = ParseSequence(start, i, SequenceRole.BraceAlternative, alternative++, inParens, inExtglob, inBrace: true, depth + 1);
            Link(index, ref last, child);
            start = i + 1;
        }

        return true;
    }

    /// <summary>
    /// Gets the nodes built so far, in their current storage.
    /// </summary>
    public readonly ValueList<SyntaxNode> Nodes => _nodes;
}