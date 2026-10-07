namespace Snowberry.Globbing.Utilities;

/// <summary>
/// Bit manipulation helpers.
/// </summary>
internal static class BitUtilities
{
    /// <summary>
    /// Returns the index of the lowest set bit of <paramref name="value"/>.
    /// </summary>
    /// <param name="value">A non-zero value.</param>
    /// <returns>The number of trailing zero bits.</returns>
    public static int TrailingZeroCount(ulong value)
    {
#if NET5_0_OR_GREATER
        return System.Numerics.BitOperations.TrailingZeroCount(value);
#else
        int count = 0;
        if ((value & 0xFFFF_FFFF) == 0)
        {
            count += 32;
            value >>= 32;
        }

        if ((value & 0xFFFF) == 0)
        {
            count += 16;
            value >>= 16;
        }

        if ((value & 0xFF) == 0)
        {
            count += 8;
            value >>= 8;
        }

        if ((value & 0xF) == 0)
        {
            count += 4;
            value >>= 4;
        }

        if ((value & 0x3) == 0)
        {
            count += 2;
            value >>= 2;
        }

        return count + (int)(~value & 1);
#endif
    }
}
