using System;

namespace Snowberry.Globbing.Compilation;

/// <summary>
/// A fixed-length run of <see cref="RegexFreeMatcher"/> text in which each position is a literal character or an ASCII character class.
/// </summary>
internal sealed class RegexFreeLiteral
{
    /// <summary>The kind of a literal position.</summary>
    public const byte c_Literal = 0;

    /// <summary>The kind of a class position that matches only the ASCII characters in its set.</summary>
    public const byte c_Class = 1;

    /// <summary>The kind of a class position that also matches every non-ASCII character.</summary>
    public const byte c_ClassWithNonAscii = 2;

    private readonly byte[]? _kinds;
    private readonly ulong[]? _sets;

    /// <summary>
    /// Initializes a new instance of the <see cref="RegexFreeLiteral"/> class.
    /// </summary>
    /// <param name="text">The text; a class position holds a placeholder character.</param>
    /// <param name="kinds">The kind of each position, or <see langword="null"/> if every position is literal.</param>
    /// <param name="sets">Two bit masks per position, for ASCII characters 0 to 63 and 64 to 127, or <see langword="null"/> if every position is literal.</param>
    public RegexFreeLiteral(string text, byte[]? kinds, ulong[]? sets)
    {
        Text = text;
        _kinds = kinds;
        _sets = sets;
    }

    /// <summary>Gets an empty literal.</summary>
    public static RegexFreeLiteral Empty { get; } = new("", null, null);

    /// <summary>Gets a value indicating whether a position is a character class.</summary>
    public bool HasClasses => _kinds != null;

    /// <summary>Gets the number of characters the literal matches.</summary>
    public int Length => Text.Length;

    /// <summary>Gets the text; a class position holds a placeholder character.</summary>
    public string Text { get; }

    /// <summary>
    /// Determines whether <paramref name="input"/> matches the literal at <paramref name="index"/>.
    /// </summary>
    /// <param name="input">The input.</param>
    /// <param name="index">The index to look at.</param>
    /// <param name="windows">Whether a literal <c>/</c> also matches <c>\</c>.</param>
    /// <returns><see langword="true"/> if the literal matches at <paramref name="index"/>; otherwise, <see langword="false"/>.</returns>
    public bool At(ReadOnlySpan<char> input, int index, bool windows)
    {
        string text = Text;
        if (index < 0 || index + text.Length > input.Length)
            return false;

        var slice = input.Slice(index, text.Length);
        if (_kinds == null)
        {
            if (slice.SequenceEqual(text.AsSpan()))
                return true;

            if (!windows)
                return false;
        }

        for (int i = 0; i < text.Length; i++)
        {
            char c = slice[i];
            byte kind = _kinds == null ? c_Literal : _kinds[i];
            if (kind == c_Literal)
            {
                char l = text[i];
                if (c != l && !(windows && l == '/' && c == '\\'))
                    return false;
            }
            else if (c < 128 ? ((_sets![(2 * i) + (c >> 6)] >> (c & 63)) & 1) == 0 : kind != c_ClassWithNonAscii)
            {
                return false;
            }
        }

        return true;
    }
}
