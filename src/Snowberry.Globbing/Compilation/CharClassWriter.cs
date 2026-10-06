using System;
using Snowberry.Globbing.Syntax;
using Snowberry.Globbing.Utilities;

namespace Snowberry.Globbing.Compilation;

/// <summary>
/// Writes the regex for a bracket expression such as <c>[a-z]</c>, <c>[!.]</c> or <c>[[:alpha:]]</c>.
/// </summary>
internal static class CharClassWriter
{
    /// <summary>
    /// Writes the regex for the bracket expression with the content <paramref name="content"/>.
    /// </summary>
    /// <remarks>
    /// A leading <c>^</c>, or <c>!</c> unless <see cref="GlobOptions.BracketMode"/> is <see cref="GlobBracketMode.Literal"/>, negates the
    /// class, and a negated class without a POSIX class also excludes <c>/</c>. A backslash escape is passed to the regex as written.
    /// With <see cref="GlobOptions.PosixClasses"/>, a known POSIX class is expanded to its ranges and an unknown one is kept as literal members.
    /// An expression without regex metacharacters (a negation or any <c>-</c> counts as one) or POSIX classes is written, depending on <see cref="GlobOptions.BracketMode"/>, as
    /// a group that matches either the bracket text or one character of the class (<see cref="GlobBracketMode.Auto"/>), as the bracket
    /// text only (<see cref="GlobBracketMode.Literal"/>), or as the class only (<see cref="GlobBracketMode.CharacterClass"/>); any other
    /// expression is always written as the class.
    /// </remarks>
    /// <param name="content">The text between the brackets.</param>
    /// <param name="options">The options.</param>
    /// <param name="fragments">The regex fragments for <paramref name="options"/>.</param>
    /// <param name="atPatternStart"><see langword="true"/> if the expression starts the pattern; an expanded POSIX class then requires at least one more character.</param>
    /// <param name="sb">The builder that receives the regex.</param>
    public static void Write(ReadOnlySpan<char> content, GlobOptions options, RegexFragments fragments, bool atPatternStart, ref ValueStringBuilder sb)
    {
        bool literalMode = options.BracketMode == GlobBracketMode.Literal;
        bool negated = content.Length > 1 && (content[0] == '^' || (content[0] == '!' && !literalMode));

        Span<char> classBuffer = stackalloc char[64];
        var cls = new ValueStringBuilder(classBuffer);
        Span<char> textBuffer = stackalloc char[64];
        var text = new ValueStringBuilder(textBuffer);

        bool posix = false;
        bool expanded = false;
        bool hasSlash = false;
        int i = 0;

        if (negated)
        {
            cls.Append('^');
            text.Append(content[0]);
            i = 1;
        }

        for (; i < content.Length; i++)
        {
            char c = content[i];

            if (c == '\\' && i + 1 < content.Length)
            {
                c = content[++i];
                cls.Append('\\');
                cls.Append(c);
                text.Append(c);
                continue;
            }

            if (c == '[' && options.PosixClasses && i + 1 < content.Length && content[i + 1] == ':')
            {
                int close = content[(i + 2)..].IndexOf(":]".AsSpan());
                if (close >= 0)
                {
                    var name = content.Slice(i + 2, close);
                    posix = true;
                    string? ranges = PosixClasses.Ranges(name);
                    if (ranges != null)
                    {
                        cls.Append(ranges);
                        expanded = true;
                    }
                    else
                    {
                        foreach (char m in content.Slice(i, close + 4))
                            RegexSyntax.AppendClassMember(ref cls, m);
                    }

                    text.Append(content.Slice(i, close + 4));
                    i += close + 3;
                    continue;
                }
            }

            if (c == '-' && i > 0 && i < content.Length - 1)
                cls.Append('-');
            else
                RegexSyntax.AppendClassMember(ref cls, c);

            hasSlash |= c == '/';
            text.Append(c);
        }

        bool hasRegexChars = HasRegexChars(cls.Slice(0));
        if (negated && !posix && !hasSlash)
            cls.Append('/');

        if (atPatternStart && expanded)
            sb.Append(fragments.Chars.OneChar);

        // A class that contains a POSIX class is never also matched as literal text.
        if (options.BracketMode != GlobBracketMode.CharacterClass && !hasRegexChars && !posix)
        {
            if (!literalMode)
            {
                sb.Append('(');
                sb.Append(fragments.Capture);
            }

            sb.Append("\\[");
            foreach (char c in text.Slice(0))
                RegexSyntax.AppendLiteral(ref sb, c, LiteralForm.Plain);
            sb.Append("\\]");

            if (!literalMode)
            {
                sb.Append("|[");
                sb.Append(cls.Slice(0));
                sb.Append("])");
            }
        }
        else
        {
            sb.Append('[');
            sb.Append(cls.Slice(0));
            sb.Append(']');
        }

        cls.Dispose();
        text.Dispose();
    }

    /// <summary>
    /// Determines whether <paramref name="text"/> contains a character with regex meaning: <c>-</c>, a quantifier, <c>.</c>, <c>^</c>,
    /// <c>$</c>, a brace, parenthesis, bracket or <c>|</c>.
    /// </summary>
    /// <param name="text">The text to inspect.</param>
    /// <returns><see langword="true"/> if <paramref name="text"/> contains such a character; otherwise, <see langword="false"/>.</returns>
    private static bool HasRegexChars(ReadOnlySpan<char> text)
    {
        foreach (char c in text)
        {
            if (c is '-' or '*' or '+' or '?' or '.' or '^' or '$' or '{' or '}' or '(' or '|' or ')' or '[' or ']')
                return true;
        }

        return false;
    }
}
