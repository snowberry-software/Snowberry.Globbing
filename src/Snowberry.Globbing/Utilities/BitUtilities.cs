using System;
using System.Runtime.CompilerServices;

namespace Snowberry.Globbing.Utilities;

/// <summary>
/// Bit manipulation helpers.
/// </summary>
internal static class BitUtilities
{
    /// <summary>
    /// Sets bit <paramref name="index"/> of a bit set.
    /// </summary>
    /// <param name="bits">The bit set, 64 bits per word.</param>
    /// <param name="index">The non-negative bit index; the shift uses its low 6 bits.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetBit(Span<ulong> bits, int index)
    {
        bits[index >> 6] |= 1UL << index;
    }

    /// <summary>
    /// Sets bit <paramref name="index"/> of a bit set unless it is already set.
    /// </summary>
    /// <param name="bits">The bit set, 64 bits per word.</param>
    /// <param name="index">The non-negative bit index; the shift uses its low 6 bits.</param>
    /// <returns><see langword="true"/> if the bit was clear and is now set; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TrySetBit(Span<ulong> bits, int index)
    {
        ref ulong word = ref bits[index >> 6];
        ulong bit = 1UL << index;
        if ((word & bit) != 0)
            return false;

        word |= bit;
        return true;
    }

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
