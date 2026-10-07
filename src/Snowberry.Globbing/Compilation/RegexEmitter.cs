using System;
using Snowberry.Globbing.Syntax;
using Snowberry.Globbing.Utilities;

namespace Snowberry.Globbing.Compilation;

/// <summary>
/// Writes the regex for a glob syntax tree.
/// </summary>
/// <remarks>
/// Decisions are local to a node and its neighbors in the same sequence: whether a <c>*</c> starts a path segment,
/// whether <c>**</c> spans whole segments, whether <c>+</c> follows something it can quantify, and so on. The one exception is
/// <c>!(...)</c>, which also inspects the pattern text that follows it.
/// </remarks>
internal readonly ref struct RegexEmitter
{
    private readonly bool _boundStars;
    private readonly RegexFragments _f;
    private readonly ReadOnlySpan<SyntaxNode> _nodes;
    private readonly int _offset;
    private readonly GlobOptions _options;
    private readonly ReadOnlySpan<char> _pattern;
    private readonly bool _plain;
    private readonly string _source;

    /// <summary>
    /// Initializes a new instance of the <see cref="RegexEmitter"/> struct.
    /// </summary>
    /// <param name="nodes">The syntax tree.</param>
    /// <param name="pattern">The lexed pattern the nodes refer to.</param>
    /// <param name="options">The options.</param>
    /// <param name="source">The original pattern, reported in exceptions.</param>
    /// <param name="offset">The position of <paramref name="pattern"/> in <paramref name="source"/>.</param>
    /// <param name="plain"><see langword="true"/> if the pattern has no structural syntax; such patterns never match a trailing separator.</param>
    public RegexEmitter(ReadOnlySpan<SyntaxNode> nodes, ReadOnlySpan<char> pattern, GlobOptions options, string source, int offset, bool plain)
    {
        _plain = plain;
        _nodes = nodes;
        _pattern = pattern;
        _options = options;
        _f = RegexFragments.For(options);
        _source = source;
        _offset = offset;
        _boundStars = CanBoundStars(nodes, options);
    }

    /// <summary>
    /// Determines whether <paramref name="text"/> is a dot followed by at least one character and no backslash, separator or further dot, such as <c>.ts</c>.
    /// </summary>
    /// <param name="text">The pattern text that follows a negated extended glob.</param>
    /// <returns><see langword="true"/> if <paramref name="text"/> is a literal suffix; otherwise, <see langword="false"/>.</returns>
    private static bool IsLiteralSuffix(ReadOnlySpan<char> text)
    {
        if (text.Length < 2 || text[0] != '.')
            return false;

        foreach (char c in text[1..])
        {
            if (c is '\\' or '/' or '.')
                return false;
        }

        return true;
    }

    /// <summary>
    /// Writes the regex for the root sequence at <paramref name="root"/>, without anchors, followed by an optional trailing separator
    /// when the sequence ends in a single-segment star or a bracket expression, unless the pattern is plain or
    /// <see cref="GlobOptions.StrictSlashes"/> is set.
    /// </summary>
    /// <param name="root">The index of the root sequence.</param>
    /// <param name="sb">The builder that receives the regex.</param>
    /// <returns><see langword="true"/> if an optional trailing separator was written; otherwise, <see langword="false"/>.</returns>
    public bool EmitRoot(int root, ref ValueStringBuilder sb)
    {
        var last = EmitSequence(root, ref sb);
        if (_plain || _options.StrictSlashes || last is not (EmittedKind.Star or EmittedKind.CharClass))
            return false;

        sb.Append(_f.OptionalSlash);
        return true;
    }

    /// <summary>
    /// Writes the child sequences of <paramref name="parent"/> separated by <c>|</c>.
    /// </summary>
    /// <param name="parent">The index of the brace, group or extended glob node that owns the alternatives.</param>
    /// <param name="sb">The builder that receives the regex.</param>
    private void EmitAlternatives(int parent, ref ValueStringBuilder sb)
    {
        for (int alternative = _nodes[parent].FirstChild; alternative >= 0; alternative = _nodes[alternative].Next)
        {
            if (alternative != _nodes[parent].FirstChild)
                sb.Append('|');

            EmitSequence(alternative, ref sb);
        }
    }

    /// <summary>
    /// Writes the regex for an extended glob such as <c>@(a|b)</c>, <c>?(a)</c>, <c>+(a)</c>, <c>*(a)</c> or <c>!(a)</c>.
    /// </summary>
    /// <remarks>
    /// <c>@(...)</c> is a group that captures with <see cref="GlobOptions.CaptureGroups"/>. The others are a non-capturing group with
    /// the matching quantifier, or the negative lookahead of <see cref="EmitNegationClose"/>, wrapped in a capture group with
    /// <see cref="GlobOptions.CaptureGroups"/>.
    /// </remarks>
    /// <param name="node">The extended glob node.</param>
    /// <param name="index">The index of <paramref name="node"/>, which owns the alternatives.</param>
    /// <param name="atPatternStart"><see langword="true"/> if the extended glob starts the root sequence; every form except <c>@(...)</c> then requires at least one more character.</param>
    /// <param name="sb">The builder that receives the regex.</param>
    private void EmitExtglob(in SyntaxNode node, int index, bool atPatternStart, ref ValueStringBuilder sb)
    {
        if (node.Value == '@')
        {
            EmitGroup(index, _f.Capture.AsSpan(), ref sb);
            return;
        }

        if (atPatternStart)
            sb.Append(_f.Chars.OneChar);

        if (_options.CaptureGroups)
            sb.Append('(');

        sb.Append(node.Value == '!' ? "(?:(?!(?:" : "(?:");
        EmitAlternatives(index, ref sb);
        if (node.Value == '!')
        {
            EmitNegationClose(in node, ref sb);
        }
        else
        {
            sb.Append(')');
            sb.Append(node.Value);
        }

        if (_options.CaptureGroups)
            sb.Append(')');
    }

    /// <summary>
    /// Closes a <c>!(...)</c> and writes the text it matches. The negative lookahead must reject the excluded text as a whole, so
    /// when the alternatives contain <c>*</c> and the rest of the pattern is a literal suffix such as <c>.ts</c> in <c>!(*.d).ts</c>,
    /// the suffix is compiled into the lookahead; otherwise the lookahead is anchored to the end of input when the alternatives
    /// span segments or nothing but closing parentheses follows.
    /// </summary>
    /// <remarks>
    /// The text after the lookahead is <see cref="RegexFragments.Globstar"/> when the alternatives contain <c>/</c> and
    /// <see cref="RegexFragments.Star"/> otherwise, except in the unanchored case, which uses a non-capturing <see cref="GlobChars.SegmentRun"/>.
    /// The rest of the pattern is the remaining pattern text, not only the rest of the enclosing sequence.
    /// </remarks>
    /// <param name="node">The negated extended glob node.</param>
    /// <param name="sb">The builder that receives the regex.</param>
    private void EmitNegationClose(in SyntaxNode node, ref ValueStringBuilder sb)
    {
        var inner = _pattern.Slice(node.Start + 2, node.Length - 3);
        var remaining = _pattern[node.End..];
        bool spansSegments = inner.Length > 1 && inner.IndexOf('/') >= 0;
        string star = spansSegments ? _f.Globstar : _f.Star;

        if (inner.IndexOf('*') >= 0 && IsLiteralSuffix(remaining))
        {
            sb.Append(')');
            GlobCompiler.EmitBody(remaining, _options, _source, _offset + node.End, ref sb, allowShapes: false, allowPlain: false);
            sb.Append(')');
            sb.Append(star);
            sb.Append(')');
            return;
        }

        if (spansSegments || remaining.IsEmpty || remaining.Trim(')').IsEmpty)
        {
            sb.Append(')');
            sb.Append(RegexSyntax.c_EndOfInput);
            sb.Append("))");
            sb.Append(star);
            return;
        }

        sb.Append("))");
        sb.Append(_f.Chars.SegmentRun);
        sb.Append(')');
    }

    /// <summary>
    /// Writes a group of the alternatives of <paramref name="index"/>, opened with <paramref name="prefix"/>.
    /// </summary>
    /// <param name="index">The index of the node that owns the alternatives.</param>
    /// <param name="prefix">The text after <c>(</c>, such as <c>?:</c>.</param>
    /// <param name="sb">The builder that receives the regex.</param>
    private void EmitGroup(int index, ReadOnlySpan<char> prefix, ref ValueStringBuilder sb)
    {
        sb.Append('(');
        sb.Append(prefix);
        EmitAlternatives(index, ref sb);
        sb.Append(')');
    }

    /// <summary>
    /// Writes the regex for each node of the sequence at <paramref name="sequence"/>.
    /// </summary>
    /// <remarks>
    /// A separator followed by a star that plans as <see cref="StarForm.TrailingGlobstar"/> or <see cref="StarForm.MiddleGlobstar"/> is
    /// written by that globstar form. A brace is written as a group that captures with <see cref="GlobOptions.CaptureGroups"/>; a regex group keeps its
    /// prefix, such as <c>?:</c> or <c>?&lt;name&gt;</c>, as written.
    /// </remarks>
    /// <param name="sequence">The index of the sequence node.</param>
    /// <param name="sb">The builder that receives the regex.</param>
    /// <returns>The kind of the last thing written, or <see cref="EmittedKind.Nothing"/> if the sequence is empty.</returns>
    /// <exception cref="InvalidOperationException">The sequence contains a node of a kind that cannot appear in a sequence.</exception>
    private EmittedKind EmitSequence(int sequence, ref ValueStringBuilder sb)
    {
        var rendered = EmittedKind.Nothing;
        bool chained = false;
        int prev = -1;
        int prevPrev = -1;

        for (int i = _nodes[sequence].FirstChild; i >= 0;)
        {
            ref readonly var node = ref _nodes[i];
            int last = i;
            int beforeLast = prev;
            rendered = EmittedKind.Other;

            switch (node.Kind)
            {
                case SyntaxKind.Literal:
                    RegexSyntax.AppendLiteral(ref sb, node.Value, (LiteralForm)node.Count);
                    break;

                case SyntaxKind.Pipe:
                    sb.Append('|');
                    break;

                case SyntaxKind.Separator:
                    if (node.Next >= 0 && _nodes[node.Next].Kind == SyntaxKind.Star)
                    {
                        var plan = PlanStar(sequence, node.Next, prev: i, prevPrev: prev);
                        if (plan.Form is StarForm.TrailingGlobstar or StarForm.MiddleGlobstar)
                        {
                            EmitStar(in plan, ref chained, ref sb);
                            last = plan.Last;
                            beforeLast = plan.BeforeLast;
                            rendered = EmittedKind.Globstar;
                            break;
                        }
                    }

                    sb.Append(_f.Chars.SlashLiteral);
                    break;

                case SyntaxKind.Dot:
                    sb.Append(_f.Chars.DotLiteral);
                    break;

                case SyntaxKind.Star:
                    var starPlan = PlanStar(sequence, i, prev, prevPrev);
                    rendered = EmitStar(in starPlan, ref chained, ref sb);
                    last = starPlan.Last;
                    beforeLast = starPlan.BeforeLast;
                    break;

                case SyntaxKind.Question:
                    if (IsQuantifiable(prev, groupsOnly: true))
                    {
                        // Lazy and greedy forms match the same inputs; the lazy one can hang the .NET interpreter.
                        if (_options.CaptureGroups || _nodes[prev].Kind != SyntaxKind.Extglob || _nodes[prev].Value is not ('?' or '+' or '*'))
                            sb.Append('?');
                    }
                    else if (IsSegmentStart(sequence, prev))
                        sb.Append(_f.SegmentStartQmark);
                    else
                        sb.Append(_f.Chars.Qmark);
                    break;

                case SyntaxKind.Plus:
                    if (IsQuantifiable(prev, groupsOnly: false) || (_nodes[sequence].InParens && prev >= 0 && _nodes[prev].Kind is not (SyntaxKind.Plus or SyntaxKind.Extglob)))
                        sb.Append('+');
                    else
                        sb.Append(_f.Chars.PlusLiteral);
                    break;

                case SyntaxKind.CharClass:
                    CharClassWriter.Write(_pattern.Slice(node.Start + 1, node.Length - 2), _options, _f, IsPatternStart(sequence, prev), ref sb);
                    rendered = EmittedKind.CharClass;
                    break;

                case SyntaxKind.Brace:
                    EmitGroup(i, _f.Capture.AsSpan(), ref sb);
                    break;

                case SyntaxKind.BraceRange:
                    BraceRangeWriter.Write(_pattern.Slice(node.Start, node.Length), _options, ref sb);
                    break;

                case SyntaxKind.Extglob:
                    EmitExtglob(in node, i, atPatternStart: IsPatternStart(sequence, prev), ref sb);
                    break;

                case SyntaxKind.Group:
                    EmitGroup(i, _pattern.Slice(node.Start + 1, node.Count), ref sb);
                    break;

                default:
                    throw new InvalidOperationException($"Unexpected syntax node {node.Kind}.");
            }

            prevPrev = beforeLast;
            prev = last;
            i = _nodes[last].Next;
        }

        return rendered;
    }

    /// <summary>
    /// Writes the regex for a star run in the form chosen by <paramref name="plan"/>.
    /// </summary>
    /// <remarks>
    /// A star or globstar that starts a segment is guarded by <see cref="GlobChars.NoDotsSlash"/> with <see cref="GlobOptions.MatchDotFiles"/> and by
    /// <see cref="RegexFragments.DotGuard"/> otherwise; with <see cref="GlobOptions.BashCompatibility"/> only <see cref="StarForm.BashStar"/> gets a guard, <see cref="RegexFragments.DotGuard"/>.
    /// <see cref="StarForm.TrailingGlobstar"/> also matches the end of input unless <see cref="GlobOptions.StrictSlashes"/> is set, and
    /// <see cref="StarForm.MiddleGlobstar"/> matches a single separator, or the end of input when <see cref="StarPlan.MoreAfter"/> is set.
    /// </remarks>
    /// <param name="plan">The plan produced by <see cref="PlanStar"/>.</param>
    /// <param name="chained">
    /// On entry, whether the previous star of the sequence is followed by a literal run and this star; on return, whether this star
    /// is followed by a literal run and another star, as <see cref="TryAppendBoundedStar"/> decides.
    /// </param>
    /// <param name="sb">The builder that receives the regex.</param>
    /// <returns><see cref="EmittedKind.Star"/> for <see cref="StarForm.Star"/>, <see cref="StarForm.BashStar"/> and <see cref="StarForm.Quantifier"/>; otherwise, <see cref="EmittedKind.Globstar"/>.</returns>
    private EmittedKind EmitStar(in StarPlan plan, ref bool chained, ref ValueStringBuilder sb)
    {
        var chars = _f.Chars;
        bool previousChained = chained;
        chained = false;
        string segmentPrefix = _options.BashCompatibility ? "" : _f.SegmentGuard;

        switch (plan.Form)
        {
            case StarForm.Quantifier:
                sb.Append('*');
                return EmittedKind.Star;

            case StarForm.BashStar:
                if (plan.SegmentStart)
                    sb.Append(_f.DotGuard);
                sb.Append(_f.BashStar);
                return EmittedKind.Star;

            case StarForm.WholeGlobstar:
                sb.Append(_f.Globstar);
                return EmittedKind.Globstar;

            case StarForm.TrailingGlobstar:
                sb.Append(_options.StrictSlashes ? _f.StrictTrailingGlobstar : _f.TrailingGlobstar);
                return EmittedKind.Globstar;

            case StarForm.MiddleGlobstar:
                sb.Append(plan.MoreAfter ? _f.MiddleGlobstarOrEnd : _f.MiddleGlobstar);
                return EmittedKind.Globstar;

            case StarForm.LeadingGlobstar:
                sb.Append(plan.SegmentStart ? _f.LeadingGlobstar : _f.AlternativeLeadingGlobstar);
                return EmittedKind.Globstar;

            case StarForm.Globstar:
                if (plan.SegmentStart)
                    sb.Append(segmentPrefix);
                sb.Append(_f.Globstar);
                return EmittedKind.Globstar;

            default:
                if (plan.SegmentStart && !plan.AfterLeadingDot && plan.OneChar && segmentPrefix.Length > 0 && !_options.MatchDotFiles)
                {
                    sb.Append(plan.BeforeDot && !_options.CaptureGroups ? chars.SegmentFirstChar : chars.OneCharNoDot);
                }
                else
                {
                    if (plan.AfterLeadingDot)
                        sb.Append(chars.NoDotSlash);
                    else if (plan.SegmentStart)
                        sb.Append(segmentPrefix);

                    if (plan.OneChar && (plan.SegmentStart || plan.AfterLeadingDot))
                        sb.Append(chars.OneChar);
                }

                if (!_boundStars || !TryAppendBoundedStar(plan.Last, previousChained, out chained, ref sb))
                    sb.Append(_f.Star);
                return EmittedKind.Star;
        }
    }

    /// <summary>
    /// Determines whether single-segment stars may be written in the bounded form of <see cref="TryAppendBoundedStar"/>.
    /// </summary>
    /// <remarks>
    /// Not with <see cref="GlobOptions.CaptureGroups"/>, <see cref="GlobOptions.BashCompatibility"/>, a regex group,
    /// a raw literal or a brace range written by <see cref="GlobOptions.BraceRangeExpander"/>.
    /// </remarks>
    /// <param name="nodes">The syntax tree.</param>
    /// <param name="options">The options.</param>
    /// <returns><see langword="true"/> if stars may be bounded; otherwise, <see langword="false"/>.</returns>
    private static bool CanBoundStars(ReadOnlySpan<SyntaxNode> nodes, GlobOptions options)
    {
        return !options.CaptureGroups && !options.BashCompatibility && !LiteralHint.ContainsVerbatimRegex(nodes, options, orGroup: true);
    }

    /// <summary>
    /// Writes a single-segment star that stops at the first occurrence of the literal run that follows it, when another
    /// single-segment star follows that run.
    /// </summary>
    /// <remarks>
    /// <c>[^/]*abc[^/]*</c> becomes <c>[^/a]*(?:a(?!bc)[^/a]*)*abc[^/]*</c>, which matches the same strings.
    /// A run of more than one character is bounded only in a chain of at least three stars.
    /// </remarks>
    /// <param name="last">The index of the last node of the star run.</param>
    /// <param name="previousChained">Whether the previous star of the sequence is followed by a literal run and this star.</param>
    /// <param name="chained">Set to whether this star is followed by a literal run and another star.</param>
    /// <param name="sb">The builder that receives the regex.</param>
    /// <returns><see langword="true"/> if the bounded form was written; otherwise, <see langword="false"/> and nothing was written.</returns>
    private bool TryAppendBoundedStar(int last, bool previousChained, out bool chained, ref ValueStringBuilder sb)
    {
        int first = _nodes[last].Next;
        int i = SkipBoundingLiterals(first, out int count);
        chained = count > 0 && i >= 0 && _nodes[i].Kind == SyntaxKind.Star;
        if (!chained)
            return false;

        if (count > 1 && !previousChained)
        {
            int after = SkipBoundingLiterals(_nodes[i].Next, out int nextCount);
            if (nextCount == 0 || after < 0 || _nodes[after].Kind != SyntaxKind.Star)
                return false;
        }

        char head = LiteralChar(in _nodes[first]);
        AppendStarUntil(head, ref sb);
        if (count > 1)
        {
            sb.Append("(?:");
            RegexSyntax.AppendLiteral(ref sb, head, LiteralForm.Plain);
            sb.Append("(?!");
            for (int j = _nodes[first].Next; j != i; j = _nodes[j].Next)
                RegexSyntax.AppendLiteral(ref sb, LiteralChar(in _nodes[j]), LiteralForm.Plain);
            sb.Append(')');
            AppendStarUntil(head, ref sb);
            sb.Append(")*");
        }

        return true;
    }

    /// <summary>
    /// Writes a run of characters other than a separator and <paramref name="head"/>, such as <c>[^/a]*</c>.
    /// </summary>
    /// <param name="head">The character the run stops before.</param>
    /// <param name="sb">The builder that receives the regex.</param>
    private void AppendStarUntil(char head, ref ValueStringBuilder sb)
    {
        sb.Append(_f.Chars.NotSeparatorOpen);
        RegexSyntax.AppendClassMember(ref sb, head);
        sb.Append("]*");
    }

    /// <summary>
    /// Finds the end of the run of literals accepted by <see cref="IsBoundingLiteral"/> that starts at <paramref name="first"/>.
    /// </summary>
    /// <param name="first">The index of the first node, or a negative value if there is none.</param>
    /// <param name="count">Set to the number of literals in the run.</param>
    /// <returns>The index of the node after the run, or a negative value if the run ends the sequence.</returns>
    private int SkipBoundingLiterals(int first, out int count)
    {
        count = 0;
        int i = first;
        for (; i >= 0 && IsBoundingLiteral(in _nodes[i]); i = _nodes[i].Next)
            count++;

        return i;
    }

    /// <summary>
    /// Determines whether <paramref name="node"/> is a literal character that can bound a star: a plain literal or a dot that is not a
    /// separator, whitespace or <c>#</c>.
    /// </summary>
    /// <param name="node">The node.</param>
    /// <returns><see langword="true"/> if the node can bound a star; otherwise, <see langword="false"/>.</returns>
    private static bool IsBoundingLiteral(in SyntaxNode node)
    {
        if (node.Kind == SyntaxKind.Dot)
            return true;

        return node.Kind == SyntaxKind.Literal && (LiteralForm)node.Count == LiteralForm.Plain
            && node.Value is not ('/' or '\\' or '#') && !char.IsWhiteSpace(node.Value);
    }

    /// <summary>
    /// Gets the character a node accepted by <see cref="IsBoundingLiteral"/> matches.
    /// </summary>
    /// <param name="node">The node.</param>
    /// <returns>The character.</returns>
    private static char LiteralChar(in SyntaxNode node)
    {
        return node.Kind == SyntaxKind.Dot ? '.' : node.Value;
    }

    /// <summary>
    /// Determines whether a <c>**</c> directly after <paramref name="prev"/> in <paramref name="seq"/> may span whole segments.
    /// </summary>
    /// <remarks>
    /// It may when it follows a separator, group or extended glob, or starts a sequence; at the start of a regex group alternative,
    /// only the first alternative qualifies.
    /// </remarks>
    /// <param name="seq">The sequence node that contains the star.</param>
    /// <param name="prev">The index of the node before the star, or a negative value if the star starts the sequence.</param>
    /// <returns><see langword="true"/> if the globstar may span segments; otherwise, <see langword="false"/>.</returns>
    private bool GlobstarAllowed(in SyntaxNode seq, int prev)
    {
        if (prev < 0)
            return seq.Role != SequenceRole.GroupAlternative || seq.Count == 0;

        return _nodes[prev].Kind is SyntaxKind.Separator or SyntaxKind.Group or SyntaxKind.Extglob;
    }

    /// <summary>
    /// Determines whether <paramref name="index"/> refers to a node that is written as a literal dot.
    /// </summary>
    /// <param name="index">The node index; a negative value means no node.</param>
    /// <returns><see langword="true"/> if the node is a dot or an escaped or plain <c>.</c> literal; otherwise, <see langword="false"/>.</returns>
    private bool IsDotLiteral(int index)
    {
        if (index < 0)
            return false;

        ref readonly var node = ref _nodes[index];
        return node.Kind == SyntaxKind.Dot || (node.Kind == SyntaxKind.Literal && node.Value == '.' && (LiteralForm)node.Count != LiteralForm.Raw);
    }

    /// <summary>
    /// Determines whether <paramref name="index"/> refers to a node of kind <paramref name="kind"/>.
    /// </summary>
    /// <param name="index">The node index; a negative value means no node.</param>
    /// <param name="kind">The expected kind.</param>
    /// <returns><see langword="true"/> if the node exists and has the expected kind; otherwise, <see langword="false"/>.</returns>
    private bool IsKind(int index, SyntaxKind kind)
    {
        return index >= 0 && _nodes[index].Kind == kind;
    }

    /// <summary>
    /// Determines whether a node at the given position is the first node of the whole pattern.
    /// </summary>
    /// <param name="sequence">The index of the containing sequence.</param>
    /// <param name="prev">The index of the preceding node, or a negative value if there is none.</param>
    /// <returns><see langword="true"/> if there is no preceding node and the sequence is the root; otherwise, <see langword="false"/>.</returns>
    private bool IsPatternStart(int sequence, int prev)
    {
        return prev < 0 && _nodes[sequence].Role == SequenceRole.Root;
    }

    /// <summary>
    /// Determines whether the node at <paramref name="prev"/> ends in something a following <c>?</c> or <c>+</c> can quantify.
    /// </summary>
    /// <remarks>
    /// An extended glob such as <c>*(a)</c>, <c>+(a)</c> or <c>?(a)</c> already ends in a quantifier: a following <c>?</c> makes it
    /// lazy, and a <c>+</c> would nest unless <see cref="GlobOptions.CaptureGroups"/> wraps the extended glob in a group.
    /// </remarks>
    /// <param name="prev">The index of the preceding node, or a negative value if there is none.</param>
    /// <param name="groupsOnly">
    /// <see langword="true"/> to accept only groups and extended globs, as a following <c>?</c> requires; <see langword="false"/> to also
    /// accept bracket expressions, braces and brace ranges, but extended globs only when they are <c>@(...)</c> or <c>!(...)</c> or <see cref="GlobOptions.CaptureGroups"/> is set.
    /// </param>
    /// <returns><see langword="true"/> if the preceding node can be quantified; otherwise, <see langword="false"/>.</returns>
    private bool IsQuantifiable(int prev, bool groupsOnly)
    {
        if (prev < 0)
            return false;

        return _nodes[prev].Kind switch
        {
            SyntaxKind.Extglob => groupsOnly || _options.CaptureGroups || _nodes[prev].Value is '@' or '!',
            SyntaxKind.Group => true,
            SyntaxKind.CharClass or SyntaxKind.Brace or SyntaxKind.BraceRange => !groupsOnly,
            _ => false,
        };
    }

    /// <summary>
    /// Determines whether a node at the given position starts a path segment.
    /// </summary>
    /// <param name="sequence">The index of the containing sequence.</param>
    /// <param name="prev">The index of the preceding node, or a negative value if there is none.</param>
    /// <returns><see langword="true"/> if the node starts the pattern or follows a separator; otherwise, <see langword="false"/>.</returns>
    private bool IsSegmentStart(int sequence, int prev)
    {
        return IsPatternStart(sequence, prev) || IsKind(prev, SyntaxKind.Separator);
    }

    /// <summary>
    /// Determines whether the node after a globstar lets it keep spanning segments instead of acting as a single star.
    /// </summary>
    /// <param name="next">The index of the node after the globstar.</param>
    /// <returns><see langword="true"/> if the node is a separator, group, brace, brace range or <c>@(...)</c>; otherwise, <see langword="false"/>.</returns>
    private bool KeepsGlobstar(int next)
    {
        ref readonly var node = ref _nodes[next];
        return node.Kind switch
        {
            SyntaxKind.Separator or SyntaxKind.Group or SyntaxKind.Brace or SyntaxKind.BraceRange => true,
            SyntaxKind.Extglob => node.Value == '@',
            _ => false,
        };
    }

    /// <summary>
    /// Decides how the star run at <paramref name="star"/> is written: as a single-segment wildcard, a quantifier, a bash star, or one
    /// of the globstar forms when <c>**</c> spans whole path segments.
    /// </summary>
    /// <remarks>
    /// In a plain pattern every star is <see cref="StarForm.Star"/>. Otherwise a run of exactly two stars is a globstar when
    /// <see cref="GlobOptions.Globstar"/> is set and <see cref="GlobstarAllowed"/> agrees. With <see cref="GlobOptions.BashCompatibility"/>,
    /// every run is <see cref="StarForm.BashStar"/> except a globstar that starts a segment and ends the pattern or precedes a separator.
    /// A globstar merges with following <c>/**</c> runs and becomes, in order of precedence, <see cref="StarForm.WholeGlobstar"/>,
    /// <see cref="StarForm.TrailingGlobstar"/>, <see cref="StarForm.MiddleGlobstar"/>, <see cref="StarForm.LeadingGlobstar"/>,
    /// <see cref="StarForm.Globstar"/> when it ends the sequence or <see cref="KeepsGlobstar"/> agrees, and <see cref="StarForm.Star"/>
    /// otherwise. Any other run is <see cref="StarForm.Quantifier"/> after a bracket expression, group or extended glob with
    /// <see cref="GlobOptions.RegexQuantifiers"/>, and <see cref="StarForm.Star"/> otherwise.
    /// </remarks>
    /// <param name="sequence">The index of the sequence that contains the star.</param>
    /// <param name="star">The index of the star node.</param>
    /// <param name="prev">The index of the node before the star in the sequence, or a negative value if there is none.</param>
    /// <param name="prevPrev">The index of the node before <paramref name="prev"/>, or a negative value if there is none.</param>
    /// <returns>The chosen form and the nodes it consumes, which can extend past <paramref name="star"/> when repeated globstars are merged.</returns>
    private StarPlan PlanStar(int sequence, int star, int prev, int prevPrev)
    {
        ref readonly var seq = ref _nodes[sequence];
        ref readonly var node = ref _nodes[star];
        bool atStart = seq.Role == SequenceRole.Root && prev < 0;
        bool afterSeparator = prev >= 0 && _nodes[prev].Kind == SyntaxKind.Separator;
        bool segmentStart = atStart || afterSeparator;
        bool afterLeadingDot = prev >= 0 && _nodes[prev].Kind == SyntaxKind.Dot && _nodes[prev].IsLeadingDot;
        int next = node.Next;
        bool rootEnd = seq.Role == SequenceRole.Root && next < 0;

        // A plain pattern has no segments to span; its stars are single-segment wildcards.
        if (_plain)
            return new StarPlan(StarForm.Star, star, prev);

        bool globstar = node.Count == 2 && _options.Globstar && GlobstarAllowed(in seq, prev);
        if (_options.BashCompatibility && !(globstar && segmentStart && (rootEnd || IsKind(next, SyntaxKind.Separator))))
            return new StarPlan(StarForm.BashStar, star, prev, segmentStart);

        if (globstar)
        {
            int last = star;
            int beforeLast = prev;

            // "a/**/**/b" is the same as "a/**/b".
            while (IsKind(next, SyntaxKind.Separator) && IsKind(_nodes[next].Next, SyntaxKind.Star) && _nodes[_nodes[next].Next].Count == 2)
            {
                int candidate = _nodes[next].Next;
                int after = _nodes[candidate].Next;
                if (!(after < 0 ? seq.Role == SequenceRole.Root : _nodes[after].Kind == SyntaxKind.Separator))
                    break;

                beforeLast = next;
                last = candidate;
                next = after;
            }

            rootEnd = seq.Role == SequenceRole.Root && next < 0;
            bool separatorNotFirst = afterSeparator && !(seq.Role == SequenceRole.Root && prevPrev < 0);
            bool afterStar = IsKind(prevPrev, SyntaxKind.Star);

            if (atStart && rootEnd)
                return new StarPlan(StarForm.WholeGlobstar, last, beforeLast);

            if (separatorNotFirst && !afterStar && rootEnd)
                return new StarPlan(StarForm.TrailingGlobstar, last, beforeLast, SegmentStart: true);

            if (separatorNotFirst && IsKind(next, SyntaxKind.Separator))
                return new StarPlan(StarForm.MiddleGlobstar, next, last, SegmentStart: true, MoreAfter: _nodes[next].Next >= 0 || seq.Role != SequenceRole.Root);

            if ((atStart || (seq.Role == SequenceRole.BraceAlternative && prev < 0)) && IsKind(next, SyntaxKind.Separator))
            {
                // Only an anchored pattern start can use the optional form; elsewhere "nothing" would match mid-input.
                return new StarPlan(StarForm.LeadingGlobstar, next, last, SegmentStart: atStart && !_options.MatchSubstring);
            }

            if (next < 0 || KeepsGlobstar(next))
                return new StarPlan(StarForm.Globstar, last, beforeLast, segmentStart);

            return new StarPlan(StarForm.Star, last, beforeLast, segmentStart);
        }

        if (_options.RegexQuantifiers && prev >= 0 && _nodes[prev].Kind is SyntaxKind.CharClass or SyntaxKind.Group or SyntaxKind.Extglob)
            return new StarPlan(StarForm.Quantifier, star, prev);

        bool oneChar = node.Count == 1 && !(IsKind(next, SyntaxKind.Extglob) && _nodes[next].Value == '*');
        return new StarPlan(StarForm.Star, star, prev, segmentStart, afterLeadingDot, oneChar, false, IsDotLiteral(next));
    }
}