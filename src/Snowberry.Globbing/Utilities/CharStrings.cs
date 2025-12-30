using System.Runtime.CompilerServices;

namespace Snowberry.Globbing.Utilities;

/// <summary>
/// Provides cached single-character strings to avoid repeated allocations.
/// </summary>
internal static class CharStrings
{
    private static readonly string[] s_AsciiStrings = new string[128];

    static CharStrings()
    {
        for (int i = 0; i < 128; i++)
            s_AsciiStrings[i] = ((char)i).ToString();
    }

    /// <summary>
    /// Gets the cached string representation of a character.
    /// For ASCII characters, returns a cached instance.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Get(char c)
    {
        if (c < 128)
            return s_AsciiStrings[c];

        return c.ToString();
    }
}
