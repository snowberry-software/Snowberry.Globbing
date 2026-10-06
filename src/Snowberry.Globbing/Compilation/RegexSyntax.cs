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

    /// <summary>Matches any character except a line terminator, the JavaScript meaning of <c>.</c>, independent of engine and newline mode.</summary>
    public const string c_AnyNonLineTerminator = "[^\\n\\r\\u2028\\u2029]";

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
        if (form == LiteralForm.Escaped || (form == LiteralForm.Plain && IsSpecial(c)))
            sb.Append('\\');
        sb.Append(c);
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