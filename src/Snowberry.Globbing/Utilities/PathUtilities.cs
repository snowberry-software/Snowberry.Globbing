using System;

namespace Snowberry.Globbing.Utilities;

/// <summary>
/// Path helpers for matching inputs.
/// </summary>
internal static class PathUtilities
{
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
        var name = BaseName(path.AsSpan(), windows);
        return name.Length == path.Length ? path : name.ToString();
    }

    /// <summary>
    /// Gets the last path segment of <paramref name="path"/>, ignoring one trailing separator, as a slice of it.
    /// </summary>
    /// <remarks>Behaves like <see cref="BaseName(string, bool)"/>.</remarks>
    /// <param name="path">The path.</param>
    /// <param name="windows"><see langword="true"/> to treat both <c>/</c> and <c>\</c> as separators; otherwise, only <c>/</c>.</param>
    /// <returns>The file name, a slice of <paramref name="path"/>.</returns>
    public static ReadOnlySpan<char> BaseName(ReadOnlySpan<char> path, bool windows)
    {
        int lastSepIndex = windows ? path.LastIndexOfAny('/', '\\') : path.LastIndexOf('/');
        if (lastSepIndex == -1)
            return path;

        if (lastSepIndex == path.Length - 1 && path.Length > 1)
        {
            var head = path[..lastSepIndex];
            int secondLastSepIndex = windows ? head.LastIndexOfAny('/', '\\') : head.LastIndexOf('/');
            return head[(secondLastSepIndex + 1)..];
        }

        return path[(lastSepIndex + 1)..];
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

    /// <summary>
    /// Copies <paramref name="source"/> to <paramref name="destination"/>, converting backslashes to forward slashes.
    /// </summary>
    /// <param name="source">The characters to convert.</param>
    /// <param name="destination">The buffer to write to, at least as long as <paramref name="source"/>.</param>
    public static void ToPosixSlashes(ReadOnlySpan<char> source, Span<char> destination)
    {
#if NET8_0_OR_GREATER
        source.Replace(destination, '\\', '/');
#else
        for (int i = 0; i < source.Length; i++)
        {
            char c = source[i];
            destination[i] = c == '\\' ? '/' : c;
        }
#endif
    }
}