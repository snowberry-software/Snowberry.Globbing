using System;

namespace Snowberry.Globbing.Utilities;

/// <summary>
/// Path helpers for matching inputs.
/// </summary>
internal static class PathUtilities
{
    private static readonly char[] s_PosixSeparators = ['/'];
    private static readonly char[] s_WindowsSeparators = ['/', '\\'];

    /// <summary>
    /// Gets the last path segment of <paramref name="path"/>, ignoring one trailing separator.
    /// </summary>
    /// <remarks>
    /// Only a single trailing separator is ignored, so <c>a//</c> yields an empty string, and a root such as <c>/</c>
    /// also yields an empty string. A path without separators is returned unchanged.
    /// </remarks>
    /// <param name="path">The path.</param>
    /// <param name="windows"><see langword="true"/> to treat both <c>/</c> and <c>\</c> as separators; otherwise, only <c>/</c>.</param>
    /// <returns>The file name, for example <c>b</c> for <c>a/b</c> and for <c>a/b/</c>.</returns>
    public static string BaseName(string path, bool windows = false)
    {
        char[] separators = windows ? s_WindowsSeparators : s_PosixSeparators;
        var span = path.AsSpan();
        int lastSepIndex = span.LastIndexOfAny(separators);

        if (lastSepIndex == -1)
            return path;

        if (lastSepIndex == path.Length - 1 && path.Length > 1)
        {
            int secondLastSepIndex = span[..lastSepIndex].LastIndexOfAny(separators);
            if (secondLastSepIndex == -1)
                return path[..lastSepIndex];

            return span.Slice(secondLastSepIndex + 1, lastSepIndex - secondLastSepIndex - 1).ToString();
        }

        return span[(lastSepIndex + 1)..].ToString();
    }

    /// <summary>
    /// Converts backslashes to forward slashes.
    /// </summary>
    /// <param name="str">The string to convert.</param>
    /// <returns>The string with every backslash replaced by a forward slash.</returns>
    public static string ToPosixSlashes(string str)
    {
        return str.Replace('\\', '/');
    }
}