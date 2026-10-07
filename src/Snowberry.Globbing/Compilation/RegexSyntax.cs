using Snowberry.Globbing.Syntax;
using Snowberry.Globbing.Utilities;

namespace Snowberry.Globbing.Compilation;

/// <summary>
/// Portable regex atoms and escaping. Everything written here is valid in .NET, JavaScript and PostgreSQL.
/// </summary>
internal static class RegexSyntax
{
    /// <summary>Matches any character, including line terminators.</summary>
    public const string c_AnyChar = "[\\s\\S]";

    /// <summary>
    /// The line terminators as character class members: line feed, carriage return, line separator and paragraph separator;
    /// <see cref="LineTerminators"/> holds the same set as characters.
    /// </summary>
    public const string c_LineTerminators = "\\n\\r\\u2028\\u2029";

    /// <summary>Matches any character except a line terminator, the JavaScript meaning of <c>.</c>, independent of engine and newline mode.</summary>
    public const string c_AnyNonLineTerminator = "[^" + c_LineTerminators + "]";

    /// <summary>Matches the end of input without <see cref="System.Text.RegularExpressions.RegexOptions.Multiline"/>. Unlike a bare <c>$</c> in .NET, it does not match before a trailing line feed.</summary>
    public const string c_EndOfInput = "$(?!\\n)";

    /// <summary>
    /// Writes <paramref name="c"/> as a literal member of a character class, escaping <c>\</c>, <c>]</c>, <c>^</c>, <c>-</c> and <c>[</c>.
    /// </summary>
    /// <param name="sb">The builder that receives the text.</param>
    /// <param name="c">The character.</param>
    public static void AppendClassMember(ref ValueStringBuilder sb, char c)
    {
        if (c is '\\' or ']' or '^' or '-' or '[')
            sb.Append('\\');
        sb.Append(c);
    }

    /// <summary>
    /// Writes <paramref name="c"/> as regex text.
    /// </summary>
    /// <param name="sb">The builder that receives the text.</param>
    /// <param name="c">The character.</param>
    /// <param name="form">
    /// <see cref="LiteralForm.Plain"/> to escape the character only if it has a regex meaning, <see cref="LiteralForm.Escaped"/> to always
    /// write a backslash before it, or <see cref="LiteralForm.Raw"/> to write it verbatim.
    /// </param>
    public static void AppendLiteral(ref ValueStringBuilder sb, char c, LiteralForm form)
    {
        if (form == LiteralForm.Escaped ? KeepsEscape(c) : form == LiteralForm.Plain && IsSpecial(c))
            sb.Append('\\');
        sb.Append(c);
    }

    /// <summary>
    /// Determines whether <paramref name="c"/> is an ASCII letter or digit; escaped, such a character is a class or escape such as <c>\d</c> rather than a literal.
    /// </summary>
    /// <param name="c">The character.</param>
    /// <returns><see langword="true"/> if <paramref name="c"/> is an ASCII letter or digit; otherwise, <see langword="false"/>.</returns>
    public static bool IsAsciiLetterOrDigit(char c)
    {
        return c < 128 && char.IsLetterOrDigit(c);
    }

    /// <summary>
    /// Determines whether an escaped <paramref name="c"/> is written with its backslash.
    /// </summary>
    /// <remarks>An ASCII character other than <c>_</c> keeps it, so <c>\d</c> stays regex syntax; <c>_</c> and non-ASCII characters are written as themselves.</remarks>
    /// <param name="c">The escaped character.</param>
    /// <returns><see langword="true"/> if the backslash is written; otherwise, <see langword="false"/>.</returns>
    public static bool KeepsEscape(char c)
    {
        return c < 0x80 && c != '_';
    }

    /// <summary>
    /// Determines whether <paramref name="c"/> has a regex meaning, or is the <c>/</c> that is escaped for portability.
    /// </summary>
    /// <param name="c">The character.</param>
    /// <returns><see langword="true"/> if <paramref name="c"/> must be escaped to match literally; otherwise, <see langword="false"/>.</returns>
    private static bool IsSpecial(char c)
    {
        return c is '\\' or '^' or '$' or '.' or '|' or '?' or '*' or '+' or '(' or ')' or '[' or ']' or '{' or '}' or '/';
    }
}