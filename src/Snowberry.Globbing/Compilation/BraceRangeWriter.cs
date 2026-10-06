using System;
using System.Collections.Generic;
using System.Globalization;
using Snowberry.Globbing.Syntax;
using Snowberry.Globbing.Utilities;

namespace Snowberry.Globbing.Compilation;

/// <summary>
/// Writes the regex for a brace range such as <c>{1..10}</c>, <c>{01..10..2}</c> or <c>{a..f}</c>.
/// </summary>
internal static class BraceRangeWriter
{
    private const int c_MaxRangeValues = 4096;

    /// <summary>
    /// Writes a brace range: the regex returned by a custom <see cref="GlobOptions.BraceRangeExpander"/>, written verbatim; an
    /// alternation of the values of a numeric range; a character class between two single characters in either order, without a step;
    /// or the literal text, braces included, otherwise.
    /// </summary>
    /// <param name="content">The text between the braces, two or three parts separated by <c>..</c>.</param>
    /// <param name="options">The options.</param>
    /// <param name="sb">The builder that receives the regex.</param>
    public static void Write(ReadOnlySpan<char> content, GlobOptions options, ref ValueStringBuilder sb)
    {
        var parts = new List<string>(3);
        int start = 0;
        while (true)
        {
            int dots = content[start..].IndexOf("..".AsSpan());
            if (dots < 0)
            {
                parts.Add(content[start..].ToString());
                break;
            }

            parts.Add(content.Slice(start, dots).ToString());
            start += dots + 2;
        }

        if (options.BraceRangeExpander != null)
        {
            sb.Append(options.BraceRangeExpander(parts));
            return;
        }

        if (TryWriteNumeric(parts, ref sb))
            return;

        if (parts.Count == 2 && parts[0].Length == 1 && parts[1].Length == 1)
        {
            char lo = parts[0][0];
            char hi = parts[1][0];
            if (lo > hi)
                (lo, hi) = (hi, lo);

            sb.Append('[');
            RegexSyntax.AppendClassMember(ref sb, lo);
            sb.Append('-');
            RegexSyntax.AppendClassMember(ref sb, hi);
            sb.Append(']');
            return;
        }

        sb.Append("\\{");
        foreach (char c in content)
            RegexSyntax.AppendLiteral(ref sb, c, LiteralForm.Plain);
        sb.Append("\\}");
    }

    /// <summary>
    /// Writes a numeric range as an alternation of its values, such as <c>(?:1|2|3)</c>, if the parts describe one.
    /// </summary>
    /// <remarks>
    /// The sign of the step is ignored; the values run from the start toward the end. When either bound has a leading zero, every
    /// value is zero-padded to the digit count of the longer bound. Negative values are written with an escaped <c>-</c>.
    /// </remarks>
    /// <param name="parts">The range parts: start, end and an optional step.</param>
    /// <param name="sb">The builder that receives the regex; it is left unchanged if the method returns <see langword="false"/>.</param>
    /// <returns>
    /// <see langword="true"/> if the bounds and step are 64-bit integers, the step is not zero and the range has at most
    /// <see cref="c_MaxRangeValues"/> values, so it was written; otherwise, <see langword="false"/>.
    /// </returns>
    private static bool TryWriteNumeric(List<string> parts, ref ValueStringBuilder sb)
    {
        if (!long.TryParse(parts[0], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long from)
            || !long.TryParse(parts[1], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long to))
        {
            return false;
        }

        long step = 1;
        if (parts.Count == 3 && (!long.TryParse(parts[2], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out step) || step == 0))
            return false;

        // Decimal holds every 64-bit bound and step exactly, so no arithmetic here can overflow.
        decimal stride = Math.Abs((decimal)step);
        decimal count = Math.Floor(Math.Abs((decimal)to - from) / stride) + 1;
        if (count > c_MaxRangeValues)
            return false;

        // "01..10" pads every value to the width of the longest bound.
        bool padded = HasLeadingZero(parts[0]) || HasLeadingZero(parts[1]);
        int width = padded ? Math.Max(parts[0].TrimStart('-').Length, parts[1].TrimStart('-').Length) : 0;
        decimal direction = to >= from ? stride : -stride;

        sb.Append("(?:");
        for (int i = 0; i < count; i++)
        {
            decimal value = from + (i * direction);
            if (i > 0)
                sb.Append('|');

            string digits = Math.Abs(value).ToString(CultureInfo.InvariantCulture).PadLeft(width, '0');
            if (value < 0)
                sb.Append("\\-");
            sb.Append(digits);
        }

        sb.Append(')');
        return true;

        static bool HasLeadingZero(string s)
        {
            string digits = s.TrimStart('-');
            return digits.Length > 1 && digits[0] == '0';
        }
    }
}
