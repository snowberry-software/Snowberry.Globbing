using System;

namespace Snowberry.Globbing.Syntax;

/// <summary>
/// Reads the prefix of a glob pattern that is not part of its body: a leading <c>./</c> and negation.
/// </summary>
internal static class PatternPrefix
{
    /// <summary>
    /// Finds where the body of <paramref name="pattern"/> starts, after one leading <c>./</c>, the negating <c>!</c> and any further <c>./</c>.
    /// </summary>
    /// <param name="pattern">The pattern.</param>
    /// <param name="options">The options; negation is recognized only with <see cref="GlobOptions.Negation"/>.</param>
    /// <param name="negated">When this method returns, <see langword="true"/> if an odd number of <c>!</c> negate the pattern.</param>
    /// <returns>The position of the body in <paramref name="pattern"/>.</returns>
    public static int BodyStart(ReadOnlySpan<char> pattern, GlobOptions options, out bool negated)
    {
        int start = pattern.StartsWith("./".AsSpan()) ? 2 : 0;

        negated = false;
        if (options.Negation)
        {
            int bangs = CountNegation(pattern[start..], options);
            negated = bangs % 2 == 1;
            start += bangs;
        }

        while (pattern[start..].StartsWith("./".AsSpan()))
            start += 2;

        return start;
    }

    /// <summary>
    /// Reads a character without bounds failures.
    /// </summary>
    /// <param name="s">The text to read.</param>
    /// <param name="i">The zero-based position.</param>
    /// <returns>The character at <paramref name="i"/>, or <c>'\0'</c> if <paramref name="i"/> is past the end of <paramref name="s"/>.</returns>
    private static char At(ReadOnlySpan<char> s, int i)
    {
        return i < s.Length ? s[i] : '\0';
    }

    /// <summary>
    /// Counts the leading <c>!</c> that negate the pattern; a <c>!</c> that opens <c>!(...)</c> is not counted.
    /// </summary>
    /// <remarks>
    /// A first <c>!</c> followed by a regex group such as <c>(?!...)</c>, <c>(?=...)</c>, <c>(?&lt;...)</c> or <c>(?:...)</c> still
    /// negates the pattern, because that group is not an extended glob. A later <c>!</c> is not counted when it is followed by
    /// <c>(</c> without <c>?</c>, whatever <see cref="GlobOptions.Extglobs"/> says.
    /// </remarks>
    /// <param name="pattern">The pattern, after any leading <c>./</c>.</param>
    /// <param name="options">The options; with <see cref="GlobOptions.Extglobs"/>, a first <c>!</c> that opens <c>!(...)</c> is not a negation.</param>
    /// <returns>The number of leading <c>!</c> that negate the pattern.</returns>
    private static int CountNegation(ReadOnlySpan<char> pattern, GlobOptions options)
    {
        if (pattern.IsEmpty || pattern[0] != '!')
            return 0;

        if (options.Extglobs && At(pattern, 1) == '(' && (At(pattern, 2) != '?' || At(pattern, 3) is not ('!' or '=' or '<' or ':')))
            return 0;

        int count = 1;
        while (At(pattern, count) == '!' && (At(pattern, count + 1) != '(' || At(pattern, count + 2) == '?'))
            count++;

        return count;
    }
}