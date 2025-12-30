using System;
using System.Diagnostics.CodeAnalysis;
using Snowberry.Globbing.Models;

#if NET8_0_OR_GREATER
using System.Buffers;
#endif

namespace Snowberry.Globbing.Utilities;

/// <summary>
/// Utility functions.
/// </summary>
public static class Utils
{
    private static readonly char[] s_PosixSeparators = ['/'];
    private static readonly char[] s_WindowsSeparators = ['/', '\\'];

    /// <summary>
    /// Check if a string has regex characters.
    /// </summary>
    /// <param name="str">The string to check for regex special characters.</param>
    /// <returns><see langword="true"/> if the string contains regex special characters; otherwise, <see langword="false"/>.</returns>
    public static bool HasRegexChars(string str)
    {
#if NET7_0_OR_GREATER
        return Constants.s_RegexSpecialChars.IsMatch(str.AsSpan());
#else
        return Constants.s_RegexSpecialChars.IsMatch(str);
#endif
    }

    /// <summary>
    /// Escape regex special characters.
    /// </summary>
    /// <param name="str">The string containing characters to escape.</param>
    /// <returns>The string with regex special characters escaped.</returns>
    public static string EscapeRegex(string str)
    {
        return Constants.s_RegexSpecialCharsGlobal.Replace(str, "\\$1");
    }

    /// <summary>
    /// Convert backslashes to forward slashes.
    /// </summary>
    /// <param name="str">The string with backslashes to convert.</param>
    /// <returns>The string with all backslashes converted to forward slashes.</returns>
    public static string ToPosixSlashes(string str)
    {
        return str.Replace('\\', '/');
    }

    /// <summary>
    /// Remove backslashes from a string.
    /// </summary>
    /// <param name="str">The string to remove backslashes from.</param>
    /// <returns>The string with backslashes removed.</returns>
    public static string RemoveBackslashes(string str)
    {
        return Constants.s_RegexRemoveBackslash.Replace(str, match =>
        {
            return match.Value == "\\" ? "" : match.Value;
        });
    }

    /// <summary>
    /// Escape the last occurrence of a character.
    /// </summary>
    /// <param name="input">The input string to search.</param>
    /// <param name="ch">The character to escape.</param>
    /// <param name="lastIdx">Optional starting index for the search. If <see langword="null"/>, starts from the end of the string.</param>
    /// <returns>The string with the last occurrence of the character escaped, or the original string if the character is not found.</returns>
    public static string EscapeLast(string input, char ch, int? lastIdx = null)
    {
        int idx = input.LastIndexOf(ch, lastIdx ?? (input.Length - 1));
        if (idx == -1)
            return input;
        if (idx > 0 && input[idx - 1] == '\\')
            return EscapeLast(input, ch, idx - 1);

        return StringBuilderPool.Build(sb =>
        {
            sb.Append(input, 0, idx)
              .Append('\\')
              .Append(input, idx, input.Length - idx);
        });
    }

    /// <summary>
    /// Remove prefix from input.
    /// </summary>
    /// <param name="input">The input string to process.</param>
    /// <param name="state">The parse state to update with the removed prefix.</param>
    /// <param name="output">The output string after removing the prefix.</param>
    /// <returns>Whether the prefix was removed.</returns>
    public static bool RemovePrefix(ReadOnlySpan<char> input, ParseState state, [NotNullWhen(true)] out string? output)
    {
        output = null;
        if (input.StartsWith("./"))
        {
            state.Prefix = "./";
            output = input[2..].ToString();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Wrap output with anchors.
    /// </summary>
    /// <param name="input">The regex pattern to wrap.</param>
    /// <param name="state">The parse state containing negation information.</param>
    /// <param name="options">The options controlling anchor behavior.</param>
    /// <returns>The wrapped regex pattern with appropriate anchors and negation.</returns>
    public static string WrapOutput(string input, ParseState state, GlobbingOptions options)
    {
        if (options.Contains && !state.Negated)
            return input;

        bool hasPrefix = !options.Contains;
        bool hasSuffix = !options.Contains;

        if (!state.Negated)
        {
            if (!hasPrefix && !hasSuffix)
                return input;

            return StringBuilderPool.Build(sb =>
            {
                if (hasPrefix)
                    sb.Append('^');
                sb.Append("(?:").Append(input).Append(')');
                if (hasSuffix)
                    sb.Append('$');
            });
        }

        return StringBuilderPool.Build(sb =>
        {
            sb.Append("(?:^(?!");
            if (hasPrefix)
                sb.Append('^');
            sb.Append("(?:").Append(input).Append(')');
            if (hasSuffix)
                sb.Append('$');
            sb.Append(").*$)");
        });
    }

    /// <summary>
    /// Get basename from a path (optimized to avoid unnecessary allocations).
    /// </summary>
    /// <param name="path">The file path to extract the basename from.</param>
    /// <param name="windows">If <see langword="true"/>, treats both forward and backslashes as path separators; otherwise, only forward slashes.</param>
    /// <returns>The basename (filename with extension) from the path.</returns>
    public static string BaseName(string path, bool windows = false)
    {
        char[] separators = windows ? s_WindowsSeparators : s_PosixSeparators;
        var span = path.AsSpan();
        int lastSepIndex = span.LastIndexOfAny(separators);

        if (lastSepIndex == -1)
            return path;

        if (lastSepIndex == path.Length - 1 && path.Length > 1)
        {
            // Path ends with separator, find second-to-last segment
            int secondLastSepIndex = span[..lastSepIndex].LastIndexOfAny(separators);
            if (secondLastSepIndex == -1)
                return path[..lastSepIndex];

            return span.Slice(secondLastSepIndex + 1, lastSepIndex - secondLastSepIndex - 1).ToString();
        }

        return span[(lastSepIndex + 1)..].ToString();
    }
}
