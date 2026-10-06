using System;
using Snowberry.Globbing.Utilities;

namespace Snowberry.Globbing.Syntax;

/// <summary>
/// Splits a glob pattern into <see cref="GlobToken"/>s.
/// </summary>
/// <remarks>
/// Escapes, quotes and bracket expressions are resolved here, so the parser only sees structural tokens, literal
/// characters and whole bracket expressions. Characters between double quotes are literal, and the quotes themselves are
/// dropped unless <see cref="GlobOptions.KeepQuotes"/> is set; an unclosed quote makes the rest of the pattern literal.
/// With <see cref="GlobOptions.Extglobs"/> set, an extended glob opener is an operator directly followed by a <c>(</c> that
/// does not start a <c>(?</c> regex group; for <c>!</c> only <c>(?:</c>, <c>(?=</c>, <c>(?!</c> and <c>(?&lt;</c> count
/// as regex groups, and a <c>?(</c> directly after a <c>(</c> token is not an opener.
/// </remarks>
internal ref struct GlobLexer
{
    private readonly ReadOnlySpan<char> _pattern;
    private readonly GlobOptions _options;
    private readonly string _source;
    private readonly int _offset;

    /// <summary>
    /// Initializes a new instance of the <see cref="GlobLexer"/> struct.
    /// </summary>
    /// <param name="pattern">The pattern to lex, without a leading negation or <c>./</c>.</param>
    /// <param name="options">The options that enable or disable syntax.</param>
    /// <param name="source">The original pattern, reported in exceptions.</param>
    /// <param name="offset">The position of <paramref name="pattern"/> in <paramref name="source"/>, used for error offsets.</param>
    public GlobLexer(ReadOnlySpan<char> pattern, GlobOptions options, string source, int offset)
    {
        _pattern = pattern;
        _options = options;
        _source = source;
        _offset = offset;
    }

    /// <summary>
    /// Appends the tokens of the pattern to <paramref name="tokens"/>.
    /// </summary>
    /// <param name="tokens">The list that receives the tokens.</param>
    /// <exception cref="GlobParseException">A <c>[</c> is not closed or a <c>]</c> outside quotes has no opening <c>[</c>, with <see cref="GlobOptions.BracketExpressions"/> and <see cref="GlobOptions.StrictBrackets"/> set.</exception>
    public readonly void Tokenize(ref ValueList<GlobToken> tokens)
    {
        var p = _pattern;
        bool quoted = false;
        int i = 0;

        while (i < p.Length)
        {
            char c = p[i];

            if (quoted)
            {
                if (c == '"')
                {
                    quoted = false;
                    if (_options.KeepQuotes)
                        tokens.Add(new GlobToken(GlobTokenKind.Literal, '"', LiteralForm.Plain, i, 1));
                }
                else
                {
                    tokens.Add(new GlobToken(GlobTokenKind.Literal, c, LiteralForm.Plain, i, 1));
                }

                i++;
                continue;
            }

            switch (c)
            {
                case '\\':
                    i = LexEscape(ref tokens, i);
                    continue;

                case '"':
                    quoted = true;
                    if (_options.KeepQuotes)
                        tokens.Add(new GlobToken(GlobTokenKind.Literal, '"', LiteralForm.Plain, i, 1));
                    i++;
                    continue;

                case '[':
                    i = LexBracket(ref tokens, i);
                    continue;

                case ']':
                    if (_options.BracketExpressions && _options.StrictBrackets)
                        throw Error(GlobParseError.MissingOpeningBracket, i, "opening", '[');
                    tokens.Add(new GlobToken(GlobTokenKind.Literal, ']', LiteralForm.Plain, i, 1));
                    i++;
                    continue;

                case '*':
                    i = LexStar(ref tokens, i);
                    continue;

                case '?':
                    if (IsExtglobOpener(i) && !PreviousIs(ref tokens, GlobTokenKind.OpenParen))
                    {
                        tokens.Add(new GlobToken(GlobTokenKind.ExtglobOpen, '?', LiteralForm.Plain, i, 2));
                        i += 2;
                        continue;
                    }

                    tokens.Add(new GlobToken(GlobTokenKind.Question, '?', LiteralForm.Plain, i, 1));
                    i++;
                    continue;

                case '+':
                case '@':
                    if (IsExtglobOpener(i))
                    {
                        tokens.Add(new GlobToken(GlobTokenKind.ExtglobOpen, c, LiteralForm.Plain, i, 2));
                        i += 2;
                        continue;
                    }

                    tokens.Add(c == '+'
                        ? new GlobToken(GlobTokenKind.Plus, '+', LiteralForm.Plain, i, 1)
                        : new GlobToken(GlobTokenKind.Literal, '@', LiteralForm.Plain, i, 1));
                    i++;
                    continue;

                case '!':
                    // "!(?=", "!(?!", "!(?<" and "!(?:" start a regex group, not a negated extended glob.
                    if (_options.Extglobs && At(i + 1) == '(' && (At(i + 2) != '?' || At(i + 3) is not ('!' or '=' or '<' or ':')))
                    {
                        tokens.Add(new GlobToken(GlobTokenKind.ExtglobOpen, '!', LiteralForm.Plain, i, 2));
                        i += 2;
                        continue;
                    }

                    tokens.Add(new GlobToken(GlobTokenKind.Literal, '!', LiteralForm.Plain, i, 1));
                    i++;
                    continue;
            }

            tokens.Add(new GlobToken(c switch
            {
                '/' => GlobTokenKind.Slash,
                '.' => GlobTokenKind.Dot,
                ',' => GlobTokenKind.Comma,
                '|' => GlobTokenKind.Pipe,
                '(' => GlobTokenKind.OpenParen,
                ')' => GlobTokenKind.CloseParen,
                '{' when _options.BraceExpansion => GlobTokenKind.OpenBrace,
                '}' when _options.BraceExpansion => GlobTokenKind.CloseBrace,
                _ => GlobTokenKind.Literal,
            }, c, LiteralForm.Plain, i, 1));
            i++;
        }
    }

    /// <summary>
    /// Lexes the backslash escape at <paramref name="i"/>, appending a literal token for the escaped character, with
    /// <see cref="LiteralForm.Raw"/> if <see cref="GlobOptions.Unescape"/> is set and <see cref="LiteralForm.Escaped"/> otherwise.
    /// </summary>
    /// <remarks>
    /// An escaped separator (unless <see cref="GlobOptions.BashCompatibility"/> is set) or an escaped dot is not
    /// literal: the backslash is skipped so the next iteration lexes the separator or dot itself. A trailing backslash is
    /// a <see cref="LiteralForm.Plain"/> literal.
    /// </remarks>
    /// <param name="tokens">The list that receives the token.</param>
    /// <param name="i">The position of the backslash.</param>
    /// <returns>The index of the next unread character.</returns>
    private readonly int LexEscape(ref ValueList<GlobToken> tokens, int i)
    {
        if (i + 1 >= _pattern.Length)
        {
            tokens.Add(new GlobToken(GlobTokenKind.Literal, '\\', LiteralForm.Plain, i, 1));
            return i + 1;
        }

        char next = _pattern[i + 1];

        // An escaped separator is still a separator, and an escaped dot is a dot.
        if ((next == '/' && !_options.BashCompatibility) || next == '.')
            return i + 1;

        tokens.Add(new GlobToken(GlobTokenKind.Literal, next, _options.Unescape ? LiteralForm.Raw : LiteralForm.Escaped, i, 2));
        return i + 2;
    }

    /// <summary>
    /// Lexes the <c>[</c> at <paramref name="i"/>, appending one character class token for the whole bracket expression,
    /// or a literal <c>[</c> if no closing <c>]</c> exists or <see cref="GlobOptions.BracketExpressions"/> is not set.
    /// </summary>
    /// <param name="tokens">The list that receives the token.</param>
    /// <param name="i">The position of the <c>[</c>.</param>
    /// <returns>The index of the next unread character.</returns>
    /// <exception cref="GlobParseException">The bracket expression is not closed, with <see cref="GlobOptions.BracketExpressions"/> and <see cref="GlobOptions.StrictBrackets"/> set.</exception>
    private readonly int LexBracket(ref ValueList<GlobToken> tokens, int i)
    {
        int end = _options.BracketExpressions ? FindBracketEnd(i) : -1;
        if (end < 0)
        {
            if (_options.BracketExpressions && _options.StrictBrackets)
                throw Error(GlobParseError.MissingClosingBracket, i, "closing", ']');

            tokens.Add(new GlobToken(GlobTokenKind.Literal, '[', LiteralForm.Plain, i, 1));
            return i + 1;
        }

        tokens.Add(new GlobToken(GlobTokenKind.CharClass, '[', LiteralForm.Plain, i, end - i + 1));
        return end + 1;
    }

    /// <summary>
    /// Lexes the <c>*</c> at <paramref name="i"/>, appending either an extended glob opener (<c>*(</c>) or a single star
    /// token that covers the run of consecutive stars, up to a star that opens an extended glob.
    /// </summary>
    /// <param name="tokens">The list that receives the token.</param>
    /// <param name="i">The position of the first <c>*</c>.</param>
    /// <returns>The index of the next unread character.</returns>
    private readonly int LexStar(ref ValueList<GlobToken> tokens, int i)
    {
        if (IsStarExtglobOpener(i))
        {
            tokens.Add(new GlobToken(GlobTokenKind.ExtglobOpen, '*', LiteralForm.Plain, i, 2));
            return i + 2;
        }

        int end = i + 1;
        while (end < _pattern.Length && _pattern[end] == '*' && !IsStarExtglobOpener(end))
            end++;

        tokens.Add(new GlobToken(GlobTokenKind.Star, '*', LiteralForm.Plain, i, end - i));
        return end;
    }

    /// <summary>
    /// Finds the <c>]</c> that closes the bracket expression opened at <paramref name="open"/>.
    /// </summary>
    /// <remarks>
    /// A <c>]</c> directly after the <c>[</c> or its negation <c>!</c> or <c>^</c> is a member, a backslash skips the next
    /// character, and with <see cref="GlobOptions.PosixClasses"/> set a <c>[:</c> skips to the next <c>:]</c>. If no
    /// closing <c>]</c> follows, <c>[!]</c> and <c>[^]</c> close at their <c>]</c> as a class of the negation character.
    /// </remarks>
    /// <param name="open">The position of the <c>[</c>.</param>
    /// <returns>The position of the closing <c>]</c>, or -1 if the expression is not closed.</returns>
    private readonly int FindBracketEnd(int open)
    {
        var p = _pattern;
        int i = open + 1;
        bool negated = i < p.Length && p[i] is '!' or '^';
        if (negated)
            i++;

        // A ']' right after the opening (or the negation) is a member, not the end.
        if (i < p.Length && p[i] == ']')
            i++;

        while (i < p.Length)
        {
            char c = p[i];
            if (c == '\\')
            {
                i += 2;
                continue;
            }

            if (c == '[' && _options.PosixClasses && At(i + 1) == ':')
            {
                int close = p[(i + 2)..].IndexOf(":]".AsSpan());
                if (close >= 0)
                {
                    i += close + 4;
                    continue;
                }
            }

            if (c == ']')
                return i;

            i++;
        }

        // "[!]" and "[^]" without a later ']' are a class of the negation character itself.
        return negated && open + 2 < p.Length && p[open + 2] == ']' ? open + 2 : -1;
    }

    /// <summary>
    /// Determines whether the <c>?</c>, <c>+</c> or <c>@</c> at <paramref name="i"/> opens an extended glob, that is,
    /// extended globs are enabled and it is directly followed by a <c>(</c> that does not start a <c>(?</c> regex group.
    /// </summary>
    /// <param name="i">The position of the operator character.</param>
    /// <returns><see langword="true"/> if an extended glob opens at <paramref name="i"/>; otherwise <see langword="false"/>.</returns>
    private readonly bool IsExtglobOpener(int i)
    {
        return _options.Extglobs && At(i + 1) == '(' && At(i + 2) != '?';
    }

    /// <summary>
    /// Determines whether the <c>*</c> at <paramref name="i"/> opens an extended glob, that is, extended globs are
    /// enabled and it is followed by <c>(</c> and at least one more character that is not <c>?</c>.
    /// </summary>
    /// <param name="i">The position of the <c>*</c>.</param>
    /// <returns><see langword="true"/> if <c>*(</c> at <paramref name="i"/> opens an extended glob; otherwise <see langword="false"/>.</returns>
    private readonly bool IsStarExtglobOpener(int i)
    {
        return _options.Extglobs && At(i + 1) == '(' && i + 2 < _pattern.Length && _pattern[i + 2] != '?';
    }

    /// <summary>
    /// Reads a pattern character, tolerating positions past the end.
    /// </summary>
    /// <param name="i">The position to read.</param>
    /// <returns>The character at <paramref name="i"/>, or <c>\0</c> if <paramref name="i"/> is past the end of the pattern.</returns>
    private readonly char At(int i)
    {
        return i < _pattern.Length ? _pattern[i] : '\0';
    }

    /// <summary>
    /// Determines whether the most recently appended token has the given kind.
    /// </summary>
    /// <param name="tokens">The tokens lexed so far.</param>
    /// <param name="kind">The kind to look for.</param>
    /// <returns><see langword="true"/> if <paramref name="tokens"/> is not empty and its last token is of kind <paramref name="kind"/>; otherwise <see langword="false"/>.</returns>
    private static bool PreviousIs(ref ValueList<GlobToken> tokens, GlobTokenKind kind)
    {
        return tokens.Count > 0 && tokens[tokens.Count - 1].Kind == kind;
    }

    /// <summary>
    /// Creates, without throwing, the exception for a missing bracket delimiter.
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
}