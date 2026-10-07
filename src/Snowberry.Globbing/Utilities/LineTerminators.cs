using System;

namespace Snowberry.Globbing.Utilities;

/// <summary>
/// The line terminators: line feed, carriage return, line separator and paragraph separator, the set
/// <c>RegexSyntax.c_LineTerminators</c> writes as regex text.
/// </summary>
internal static class LineTerminators
{
    private static readonly char[] s_All = [(char)0x0A, (char)0x0D, (char)0x2028, (char)0x2029];

    /// <summary>
    /// Determines whether <paramref name="c"/> is a line terminator.
    /// </summary>
    /// <param name="c">The character.</param>
    /// <returns><see langword="true"/> if <paramref name="c"/> is a line terminator; otherwise, <see langword="false"/>.</returns>
    public static bool Is(char c)
    {
        return c is (char)0x0A or (char)0x0D or (char)0x2028 or (char)0x2029;
    }

    /// <summary>
    /// Determines whether <paramref name="input"/> contains a line terminator.
    /// </summary>
    /// <param name="input">The input to inspect.</param>
    /// <returns><see langword="true"/> if <paramref name="input"/> contains a line terminator; otherwise, <see langword="false"/>.</returns>
    public static bool Any(ReadOnlySpan<char> input)
    {
        return input.IndexOfAny(s_All) >= 0;
    }
}
