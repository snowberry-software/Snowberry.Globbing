using System;
using System.Text.RegularExpressions;
using Snowberry.Globbing.Syntax;
using Snowberry.Globbing.Utilities;

namespace Snowberry.Globbing.Compilation;

/// <summary>
/// Compiles glob patterns to portable regex sources: common shapes such as <c>*.js</c> are written directly, everything
/// else is parsed into a <see cref="GlobSyntaxTree"/> and written by <see cref="RegexEmitter"/>.
/// </summary>
internal static class GlobCompiler
{
    /// <summary>
    /// Compiles <paramref name="pattern"/> to a regex source, as <see cref="CompileRegexSource"/> does with fast paths, plus the
    /// aids that let a matcher avoid that regex: a <see cref="LiteralHint"/> and, for a negated pattern, the source of its positive body.
    /// </summary>
    /// <param name="pattern">The non-empty glob pattern.</param>
    /// <param name="options">The options.</param>
    /// <param name="findKeys"><see langword="true"/> to also find the key sets of a pattern that is not negated, for <see cref="PatternIndex"/>.</param>
    /// <param name="deferShapeHint">
    /// <see langword="true"/> to leave out the hint of a compact shape that starts with <c>*</c>, which never has a prefix, and set
    /// <see cref="GlobCompilation.HintDeferred"/> instead; <see cref="FindDeferredHint"/> finds it.
    /// </param>
    /// <returns>
    /// The compilation. <see cref="GlobCompilation.PositiveSource"/> is set only for a negated pattern when neither
    /// <see cref="GlobOptions.MatchSubstring"/> nor <see cref="RegexOptions.Multiline"/> or <see cref="RegexOptions.RightToLeft"/> is set;
    /// the hint is then computed for that positive source, and is <see langword="null"/> for a negated pattern under those options.
    /// </returns>
    /// <exception cref="GlobParseException"><paramref name="pattern"/> is too long, too deeply nested, or has unbalanced delimiters with <see cref="GlobOptions.StrictBrackets"/>.</exception>
    public static GlobCompilation Compile(string pattern, GlobOptions options, bool findKeys = false, bool deferShapeHint = false)
    {
        pattern = Prepare(pattern, options);
        int offset = PatternPrefix.BodyStart(pattern.AsSpan(), options, out bool negated);

        // A negated whole-input match is "no line terminator and the body does not match", which lets the matcher run
        // the plain body, with its hint, instead of a lookahead. Under these options the outer anchors mean something else.
        bool positive = negated && !options.MatchSubstring && (options.RegexOptions & (RegexOptions.Multiline | RegexOptions.RightToLeft)) == 0;
        string source = EmitSource(pattern, offset, negated, options, fastPaths: true, analyze: !negated || positive, findKeys, deferShapeHint, out var hint, out bool hintDeferred, out string? positiveSource, out ulong[][]? keyWindows);
        return new GlobCompilation(source, positiveSource, hint, keyWindows, hintDeferred);
    }

    /// <summary>
    /// Finds the hint that <see cref="Compile"/> left out because <see cref="GlobCompilation.HintDeferred"/> is set.
    /// </summary>
    /// <param name="pattern">The glob pattern passed to <see cref="Compile"/>.</param>
    /// <param name="options">The options passed to <see cref="Compile"/>.</param>
    /// <returns>The hint, or <see langword="null"/> if there is none.</returns>
    public static LiteralHint? FindDeferredHint(string pattern, GlobOptions options)
    {
        pattern = Prepare(pattern, options);
        int offset = PatternPrefix.BodyStart(pattern.AsSpan(), options, out _);
        return LiteralHint.Find(pattern.AsSpan(offset), options, pattern, offset, plain: false);
    }

    /// <summary>
    /// Compiles <paramref name="pattern"/> to a regex source, anchored to the whole input unless <see cref="GlobOptions.MatchSubstring"/> is set.
    /// </summary>
    /// <param name="pattern">The non-empty glob pattern.</param>
    /// <param name="options">The options.</param>
    /// <param name="fastPaths"><see langword="true"/> to compile common patterns such as <c>*.js</c> to their compact forms and treat patterns without structural syntax as plain text with wildcards.</param>
    /// <returns>The regex source.</returns>
    /// <exception cref="GlobParseException"><paramref name="pattern"/> is too long, too deeply nested, or has unbalanced delimiters with <see cref="GlobOptions.StrictBrackets"/>.</exception>
    public static string CompileRegexSource(string pattern, GlobOptions options, bool fastPaths = true)
    {
        pattern = Prepare(pattern, options);
        int offset = PatternPrefix.BodyStart(pattern.AsSpan(), options, out bool negated);
        return EmitSource(pattern, offset, negated, options, fastPaths, analyze: false, findKeys: false, deferShapeHint: false, out _, out _, out _, out _);
    }

    /// <summary>
    /// Writes the regex for <paramref name="pattern"/>, without anchors or negation, to <paramref name="sb"/>.
    /// </summary>
    /// <param name="pattern">The pattern text, such as a body without a leading negation or <c>./</c>, or the text after a negated extended glob.</param>
    /// <param name="options">The options.</param>
    /// <param name="source">The original pattern, reported in exceptions.</param>
    /// <param name="offset">The position of <paramref name="pattern"/> in <paramref name="source"/>.</param>
    /// <param name="sb">The builder that receives the regex, including the optional trailing separator that <see cref="RegexEmitter.EmitRoot"/> or the shapes add.</param>
    /// <param name="allowShapes"><see langword="true"/> to compile common patterns such as <c>*.js</c> to their compact forms.</param>
    /// <param name="allowPlain"><see langword="true"/> to treat a pattern without structural syntax as plain text with wildcards.</param>
    /// <exception cref="GlobParseException">The pattern is too deeply nested, or has unbalanced delimiters with <see cref="GlobOptions.StrictBrackets"/>.</exception>
    public static void EmitBody(ReadOnlySpan<char> pattern, GlobOptions options, string source, int offset, ref ValueStringBuilder sb, bool allowShapes, bool allowPlain)
    {
        EmitBody(pattern, options, source, offset, ref sb, allowShapes, allowPlain, findHint: false, findKeys: false, deferShapeHint: false, out _, out _);
    }

    /// <summary>
    /// Writes the regex for <paramref name="pattern"/> as the public overload does and, if requested, finds its
    /// <see cref="LiteralHint"/> from the same parse.
    /// </summary>
    /// <param name="pattern">The pattern text.</param>
    /// <param name="options">The options.</param>
    /// <param name="source">The original pattern, reported in exceptions.</param>
    /// <param name="offset">The position of <paramref name="pattern"/> in <paramref name="source"/>.</param>
    /// <param name="sb">The builder that receives the regex.</param>
    /// <param name="allowShapes"><see langword="true"/> to compile common patterns such as <c>*.js</c> to their compact forms.</param>
    /// <param name="allowPlain"><see langword="true"/> to treat a pattern without structural syntax as plain text with wildcards.</param>
    /// <param name="findHint"><see langword="true"/> to find the literal hint of <paramref name="pattern"/>.</param>
    /// <param name="findKeys"><see langword="true"/> to find the key sets of <paramref name="pattern"/>.</param>
    /// <param name="deferShapeHint"><see langword="true"/> to leave out the hint of a compact shape that starts with <c>*</c>.</param>
    /// <param name="hintDeferred">Whether the hint was left out because of <paramref name="deferShapeHint"/>.</param>
    /// <param name="keyWindows">The key sets, or <see langword="null"/> if none were requested or found.</param>
    /// <returns>The hint, or <see langword="null"/> if none was requested or found.</returns>
    /// <exception cref="GlobParseException">The pattern is too deeply nested, or has unbalanced delimiters with <see cref="GlobOptions.StrictBrackets"/>.</exception>
    private static LiteralHint? EmitBody(ReadOnlySpan<char> pattern, GlobOptions options, string source, int offset, ref ValueStringBuilder sb, bool allowShapes, bool allowPlain, bool findHint, bool findKeys, bool deferShapeHint, out bool hintDeferred, out ulong[][]? keyWindows)
    {
        keyWindows = null;
        hintDeferred = false;
        bool plain = allowPlain && IsPlain(pattern);
        bool shape = allowShapes && TryEmitShape(pattern, options, ref sb);
        if (shape && !findKeys)
        {
            hintDeferred = findHint && deferShapeHint && pattern[0] == '*';
            return findHint && !hintDeferred ? LiteralHint.Find(pattern, options, source, offset, plain) : null;
        }

        var tree = GlobSyntaxTree.Parse(pattern, options, source, offset, plain);
        try
        {
            if (!shape)
                new RegexEmitter(tree.Nodes, pattern, options, source, offset, plain).EmitRoot(tree.Root, ref sb);

            if (findKeys)
                keyWindows = KeyGrams.Find(tree.Nodes, tree.Root, pattern, options);

            return findHint ? LiteralHint.Find(tree.Nodes, tree.Root, options) : null;
        }
        finally
        {
            tree.Dispose();
        }
    }

    /// <summary>
    /// Builds the complete regex source for the body of <paramref name="pattern"/>, with anchors and negation applied.
    /// </summary>
    /// <remarks>
    /// The body is wrapped in <c>(?:...)</c> and, unless <see cref="GlobOptions.MatchSubstring"/> is set, anchored by <c>^</c> and
    /// <see cref="RegexSyntax.c_EndOfInput"/>. A negated pattern wraps that in <c>^(?!...)</c> followed by any run of
    /// non-line-terminator characters and the end of input, so it matches only line-terminator-free inputs the body does not match.
    /// The compact shapes are tried only when the pattern starts with <c>.</c> or <c>*</c>.
    /// </remarks>
    /// <param name="pattern">The prepared pattern, including any leading negation.</param>
    /// <param name="offset">The position of the body in <paramref name="pattern"/>, as found by <see cref="PatternPrefix.BodyStart"/>.</param>
    /// <param name="negated"><see langword="true"/> if the pattern is negated.</param>
    /// <param name="options">The options.</param>
    /// <param name="fastPaths"><see langword="true"/> to compile common patterns such as <c>*.js</c> to their compact forms and treat patterns without structural syntax as plain text with wildcards; the fast paths are not used for negated patterns.</param>
    /// <param name="analyze"><see langword="true"/> to also find the literal hint of the body and, for a negated pattern without
    /// <see cref="GlobOptions.MatchSubstring"/>, the positive source <c>^(?:body)</c> plus <see cref="RegexSyntax.c_EndOfInput"/>, which the
    /// negated source contains verbatim.</param>
    /// <param name="findKeys"><see langword="true"/> to find the key sets when <paramref name="analyze"/> is set.</param>
    /// <param name="deferShapeHint"><see langword="true"/> to leave out the hint of a compact shape that starts with <c>*</c>.</param>
    /// <param name="hint">The literal hint, or <see langword="null"/> if <paramref name="analyze"/> is <see langword="false"/> or none is found.</param>
    /// <param name="hintDeferred">Whether the hint was left out because of <paramref name="deferShapeHint"/>.</param>
    /// <param name="positiveSource">The positive source of a negated pattern, or <see langword="null"/>.</param>
    /// <param name="keyWindows">The key sets of a pattern that is not negated when <paramref name="findKeys"/> is set, or <see langword="null"/>.</param>
    /// <returns>The regex source.</returns>
    /// <exception cref="GlobParseException">The pattern is too deeply nested, or has unbalanced delimiters with <see cref="GlobOptions.StrictBrackets"/>.</exception>
    private static string EmitSource(string pattern, int offset, bool negated, GlobOptions options, bool fastPaths, bool analyze, bool findKeys, bool deferShapeHint, out LiteralHint? hint, out bool hintDeferred, out string? positiveSource, out ulong[][]? keyWindows)
    {
        bool shapeCandidate = fastPaths && pattern[0] is '.' or '*';
        var body = pattern.AsSpan(offset);

        Span<char> buffer = stackalloc char[256];
        var sb = new ValueStringBuilder(buffer);

        if (negated)
            sb.Append("^(?!");
        int positiveStart = sb.Length;
        if (!options.MatchSubstring)
            sb.Append('^');
        sb.Append("(?:");

        hint = EmitBody(body, options, pattern, offset, ref sb, allowShapes: shapeCandidate && !negated, allowPlain: fastPaths && !negated, findHint: analyze, findKeys: findKeys && analyze && !negated, deferShapeHint, out hintDeferred, out keyWindows);

        sb.Append(')');
        if (!options.MatchSubstring)
            sb.Append(RegexSyntax.c_EndOfInput);

        positiveSource = null;
        if (negated)
        {
            if (analyze && !options.MatchSubstring)
                positiveSource = sb.Slice(positiveStart).ToString();

            sb.Append(')');
            sb.Append(RegexSyntax.c_AnyNonLineTerminator);
            sb.Append('*');
            sb.Append(RegexSyntax.c_EndOfInput);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Determines whether <paramref name="pattern"/> has no structural syntax: no separator, group, bracket, brace, quote or
    /// escape, and no leading <c>*</c> or <c>!</c>. In such a pattern <c>|</c> is literal and no trailing separator is matched.
    /// </summary>
    /// <param name="pattern">The pattern body.</param>
    /// <returns><see langword="true"/> if <paramref name="pattern"/> is non-empty and has no structural syntax; otherwise, <see langword="false"/>.</returns>
    private static bool IsPlain(ReadOnlySpan<char> pattern)
    {
        return !pattern.IsEmpty && pattern[0] is not ('*' or '!') && pattern.IndexOfAny("/()[]{}\"\\".AsSpan()) < 0;
    }

    /// <summary>
    /// Replaces redundant spellings of simpler patterns (<c>***</c>, <c>**/**</c> and <c>**/**/**</c>) and checks the pattern length.
    /// </summary>
    /// <param name="pattern">The glob pattern.</param>
    /// <param name="options">The options, which supply <see cref="GlobOptions.MaxPatternLength"/>.</param>
    /// <returns>The pattern to compile, which is <paramref name="pattern"/> unless it is a redundant spelling.</returns>
    /// <exception cref="GlobParseException"><paramref name="pattern"/> is longer than <see cref="GlobOptions.MaxPatternLength"/>.</exception>
    private static string Prepare(string pattern, GlobOptions options)
    {
        pattern = pattern switch
        {
            "***" => "*",
            "**/**" or "**/**/**" => "**",
            _ => pattern,
        };

        int max = options.MaxPatternLength;
        if (pattern.Length > max)
            throw new GlobParseException(pattern, GlobParseError.PatternTooLong, max, $"The pattern is {pattern.Length} characters long, which exceeds the maximum of {max}.");

        return pattern;
    }

    /// <summary>
    /// Writes the compact forms of <c>*</c>, <c>**</c>, <c>*.*</c>, <c>**/*</c> and similar patterns, optionally followed by
    /// a literal extension such as <c>.js</c>. Unless <see cref="GlobOptions.StrictSlashes"/> is set, an optional trailing separator follows.
    /// </summary>
    /// <param name="pattern">The pattern, without a leading negation or <c>./</c>.</param>
    /// <param name="options">The options.</param>
    /// <param name="sb">The builder that receives the regex; it is left unchanged when this method returns <see langword="false"/>.</param>
    /// <returns><see langword="true"/> if <paramref name="pattern"/> is a known shape and was written; otherwise, <see langword="false"/>.</returns>
    private static bool TryEmitShape(ReadOnlySpan<char> pattern, GlobOptions options, ref ValueStringBuilder sb)
    {
        int start = sb.Length;
        var f = RegexFragments.For(options);
        if (!TryEmitShapeCore(pattern, options, f, beforeDot: false, ref sb))
        {
            sb.Length = start;
            return false;
        }

        if (!options.StrictSlashes)
            sb.Append(f.OptionalSlash);
        return true;
    }

    /// <summary>
    /// Writes the compact form of <paramref name="pattern"/> to <paramref name="sb"/> when it is one of the known shapes, without the trailing separator.
    /// </summary>
    /// <remarks>
    /// A pattern is a known shape when it is <c>*</c>, <c>.*</c>, <c>*.*</c>, <c>*/*</c>, <c>**</c>, <c>**/*</c>, <c>**/*.*</c> or <c>**/.*</c>,
    /// or when it is a known shape followed by <c>.</c> and a non-empty extension of ASCII letters, digits and underscores, which can repeat
    /// as in <c>*.tar.gz</c>. <c>**</c> directly before an extension is not a shape. Without <see cref="GlobOptions.Globstar"/>, <c>**</c> is written as a single star.
    /// </remarks>
    /// <param name="pattern">The pattern, without a leading negation or <c>./</c>.</param>
    /// <param name="options">The options.</param>
    /// <param name="f">The regex fragments for <paramref name="options"/>.</param>
    /// <param name="beforeDot"><see langword="true"/> if a literal dot follows <paramref name="pattern"/>, so that a final <c>*</c> that starts a segment cannot be empty.</param>
    /// <param name="sb">The builder that receives the regex; it may hold partial output when this method returns <see langword="false"/>.</param>
    /// <returns><see langword="true"/> if <paramref name="pattern"/> is a known shape and was written; otherwise, <see langword="false"/>.</returns>
    private static bool TryEmitShapeCore(ReadOnlySpan<char> pattern, GlobOptions options, RegexFragments f, bool beforeDot, ref ValueStringBuilder sb)
    {
        var chars = f.Chars;
        string nodot = f.ShapeStartGuard;
        string slashDot = f.SegmentGuard;
        string star = f.ShapeStar;
        string globstar = options.Globstar ? f.Globstar : star;
        string guardedStar = beforeDot ? f.GuardedShapeStarBeforeDot : f.GuardedShapeStar;

        switch (pattern)
        {
            case "*" when guardedStar.Length > 0:
                sb.Append(guardedStar);
                return true;
            case "*":
                sb.Append(nodot); sb.Append(chars.OneChar); sb.Append(star);
                return true;
            case ".*":
                sb.Append(chars.DotLiteral); sb.Append(chars.OneChar); sb.Append(star);
                return true;
            case "*.*":
                sb.Append(nodot); sb.Append(star); sb.Append(chars.DotLiteral); sb.Append(chars.OneChar); sb.Append(star);
                return true;
            case "*/*" when guardedStar.Length > 0:
                sb.Append(nodot); sb.Append(star); sb.Append(chars.SlashLiteral); sb.Append(guardedStar);
                return true;
            case "*/*":
                sb.Append(nodot); sb.Append(star); sb.Append(chars.SlashLiteral); sb.Append(chars.OneChar); sb.Append(slashDot); sb.Append(star);
                return true;
            case "**":
                sb.Append(nodot); sb.Append(globstar);
                return true;
            case "**/*" when guardedStar.Length > 0:
                AppendLeadingSegments(ref sb, nodot, globstar, f, options); sb.Append(guardedStar);
                return true;
            case "**/*":
                AppendLeadingSegments(ref sb, nodot, globstar, f, options); sb.Append(slashDot); sb.Append(chars.OneChar); sb.Append(star);
                return true;
            case "**/*.*":
                AppendLeadingSegments(ref sb, nodot, globstar, f, options); sb.Append(slashDot); sb.Append(star); sb.Append(chars.DotLiteral); sb.Append(chars.OneChar); sb.Append(star);
                return true;
            case "**/.*":
                AppendLeadingSegments(ref sb, nodot, globstar, f, options); sb.Append(chars.DotLiteral); sb.Append(chars.OneChar); sb.Append(star);
                return true;
        }

        int dot = pattern.LastIndexOf('.');
        if (dot < 0 || dot == pattern.Length - 1)
            return false;

        var extension = pattern[(dot + 1)..];
        foreach (char c in extension)
        {
            if (c is not ((>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9') or '_'))
                return false;
        }

        // "**" directly before an extension ("**.js") acts as a single star, which the full parser handles.
        var stem = pattern[..dot];
        if (stem is "**" || !TryEmitShapeCore(stem, options, f, beforeDot: true, ref sb))
            return false;

        sb.Append(chars.DotLiteral);
        sb.Append(extension);
        return true;

        static void AppendLeadingSegments(ref ValueStringBuilder sb, string nodot, string globstar, RegexFragments f, GlobOptions options)
        {
            if (options.Globstar && f.SegmentLoops)
            {
                sb.Append(f.LeadingSegments);
                return;
            }

            var chars = f.Chars;
            sb.Append("(?:");
            sb.Append(nodot);
            sb.Append(globstar);
            sb.Append(chars.SlashLiteral);
            sb.Append(")?");
        }
    }
}