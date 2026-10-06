using System;

namespace Snowberry.Globbing.Compilation;

/// <summary>
/// The POSIX character classes such as <c>[:alpha:]</c>.
/// </summary>
internal static class PosixClasses
{
    /// <summary>
    /// Gets the regex ranges of the POSIX character class <paramref name="name"/>, such as <c>a-zA-Z</c> for <c>alpha</c>.
    /// </summary>
    /// <param name="name">The class name between <c>[:</c> and <c>:]</c>.</param>
    /// <returns>The ranges, or <see langword="null"/> if <paramref name="name"/> is not a POSIX class.</returns>
    public static string? Ranges(ReadOnlySpan<char> name)
    {
        return name switch
        {
            "alnum" => "a-zA-Z0-9",
            "alpha" => "a-zA-Z",
            "ascii" => "\\x00-\\x7F",
            "blank" => " \\t",
            "cntrl" => "\\x00-\\x1F\\x7F",
            "digit" => "0-9",
            "graph" => "\\x21-\\x7E",
            "lower" => "a-z",
            "print" => "\\x20-\\x7E ",
            "punct" => "\\-!\"#$%&'()\\*+,./:;<=>?@[\\\\\\]^_`{|}~",
            "space" => " \\t\\r\\n\\v\\f",
            "upper" => "A-Z",
            "word" => "A-Za-z0-9_",
            "xdigit" => "A-Fa-f0-9",
            _ => null,
        };
    }
}
