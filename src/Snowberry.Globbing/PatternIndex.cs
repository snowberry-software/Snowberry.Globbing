using System;
using System.Buffers;
using System.Collections.Generic;
using Snowberry.Globbing.Compilation;
using Snowberry.Globbing.Utilities;

namespace Snowberry.Globbing;

/// <summary>
/// Selects the patterns of a <see cref="Glob"/> that can match an input, so the others are skipped.
/// </summary>
/// <remarks>
/// Candidates are returned in pattern order, so testing them in order finds the same first match as testing every pattern.
/// </remarks>
internal sealed class PatternIndex
{
    /// <summary>
    /// The least estimated cost, in relative units, of the keyed patterns for which an index is built; below
    /// <see cref="LiteralHint.RegexRunCost"/>, so one keyed pattern that runs its regex is enough.
    /// </summary>
    public const int c_MinSkippedCost = 200;

    private const int c_StackSlotWords = 64;
    private const ulong c_Occupied = 1UL << 63;
    private const int c_MaxIndexedLength = 1024;
    private const ulong c_Multiplier = 0x9E37_79B9_7F4A_7C15UL;

    private readonly ulong[] _always;
    private readonly int[] _bucketPatterns;
    private readonly int[] _bucketStarts;
    private readonly CompiledPattern[] _compiled;
    private readonly Dictionary<int, int[]> _equal;
    private readonly ulong[] _equalLengths;
    private readonly int _shift;
    private readonly ulong[] _slotKeys;

    /// <summary>
    /// Initializes a new instance of the <see cref="PatternIndex"/> class.
    /// </summary>
    /// <param name="compiled">The patterns, in match order.</param>
    /// <param name="keys">The trigrams each pattern is filed under, or <see langword="null"/> for a pattern that is always a candidate.</param>
    /// <param name="distinctKeys">The number of distinct trigrams in <paramref name="keys"/>.</param>
    private PatternIndex(CompiledPattern[] compiled, ulong[]?[] keys, int distinctKeys)
    {
        _compiled = compiled;
        Words = (compiled.Length + 63) >> 6;
        _always = new ulong[Words];

        int slots = 4;
        while (slots < distinctKeys * 2)
            slots <<= 1;

        int log = 0;
        while ((1 << log) < slots)
            log++;

        _shift = 64 - log;
        _slotKeys = new ulong[slots];
        int[] counts = new int[slots];
        for (int i = 0; i < keys.Length; i++)
        {
            if (keys[i] is not { } grams)
            {
                BitUtilities.SetBit(_always, i);
                continue;
            }

            foreach (ulong gram in grams)
                counts[FindSlot(gram | c_Occupied, insert: true)]++;
        }

        _bucketStarts = new int[slots + 1];
        for (int s = 0; s < slots; s++)
            _bucketStarts[s + 1] = _bucketStarts[s] + counts[s];

        _bucketPatterns = new int[_bucketStarts[slots]];
        int[] next = (int[])_bucketStarts.Clone();
        for (int i = 0; i < keys.Length; i++)
        {
            if (keys[i] is not { } grams)
                continue;

            foreach (ulong gram in grams)
                _bucketPatterns[next[FindSlot(gram | c_Occupied, insert: false)]++] = i;
        }

        var equal = new Dictionary<int, List<int>>();
        _equalLengths = new ulong[(c_MaxIndexedLength >> 6) + 1];
        for (int i = 0; i < compiled.Length; i++)
        {
            string pattern = compiled[i].Pattern;
            int hash = Hash(pattern.AsSpan());
            if (!equal.TryGetValue(hash, out var list))
                equal[hash] = list = [];

            list.Add(i);
            BitUtilities.SetBit(_equalLengths, Math.Min(pattern.Length, c_MaxIndexedLength + 1) - 1);
        }

        _equal = new Dictionary<int, int[]>(equal.Count);
        foreach (var pair in equal)
            _equal[pair.Key] = [.. pair.Value];
    }

    /// <summary>
    /// Builds an index if it is estimated to save more than it costs.
    /// </summary>
    /// <param name="compiled">The patterns, in match order.</param>
    /// <param name="keyWindows">The key sets of each pattern, or <see langword="null"/> for a pattern without keys.</param>
    /// <returns>
    /// The index, or <see langword="null"/> for a single pattern or when the keyed patterns are estimated to cost less than
    /// <see cref="c_MinSkippedCost"/> per input.
    /// </returns>
    public static PatternIndex? Create(CompiledPattern[] compiled, ulong[][]?[] keyWindows)
    {
        if (compiled.Length < 2)
            return null;

        long skippedCost = 0;
        for (int i = 0; i < compiled.Length; i++)
        {
            if (keyWindows[i] != null)
                skippedCost += compiled[i].EstimatedRejectCost;
        }

        if (skippedCost < c_MinSkippedCost)
            return null;

        var gramCounts = new Dictionary<ulong, int>();
        var seen = new HashSet<ulong>();
        foreach (ulong[][]? windows in keyWindows)
        {
            if (windows == null)
                continue;

            seen.Clear();
            foreach (ulong[] window in windows)
            {
                foreach (ulong gram in window)
                {
                    if (seen.Add(gram))
                        gramCounts[gram] = gramCounts.TryGetValue(gram, out int count) ? count + 1 : 1;
                }
            }
        }

        ulong[]?[] keys = new ulong[]?[compiled.Length];
        var distinct = new HashSet<ulong>();
        for (int i = 0; i < compiled.Length; i++)
        {
            if (keyWindows[i] is not { } windows)
                continue;

            ulong[]? best = null;
            long bestScore = long.MaxValue;
            foreach (ulong[] window in windows)
            {
                long score = 0;
                foreach (ulong gram in window)
                    score += Score(gram, gramCounts[gram]);

                if (score < bestScore)
                {
                    bestScore = score;
                    best = window;
                }
            }

            keys[i] = best;
            distinct.UnionWith(best!);
        }

        return new PatternIndex(compiled, keys, distinct.Count);
    }

    /// <summary>
    /// Sets <paramref name="candidates"/> to the patterns whose regex can match <paramref name="text"/>.
    /// </summary>
    /// <remarks>Patterns equal to the input are added by <see cref="MarkEqual"/>.</remarks>
    /// <param name="text">The text the patterns are matched against.</param>
    /// <param name="candidates">A bit set of <see cref="Words"/> words, one bit per pattern.</param>
    private void FindCandidates(ReadOnlySpan<char> text, Span<ulong> candidates)
    {
        _always.AsSpan().CopyTo(candidates);
        if (text.Length < KeyGrams.c_GramLength)
            return;

        ulong[] slotKeys = _slotKeys;
        int words = (slotKeys.Length + 63) >> 6;
        ulong[]? rented = null;
        var expanded = words <= c_StackSlotWords ? stackalloc ulong[c_StackSlotWords] : (rented = ArrayPool<ulong>.Shared.Rent(words));
        expanded = expanded[..words];
        expanded.Clear();
        try
        {
            ulong gram = ((ulong)text[0] << 16) | text[1];
            int mask = slotKeys.Length - 1;
            for (int i = KeyGrams.c_GramLength - 1; i < text.Length; i++)
            {
                gram = KeyGrams.Append(gram, text[i]);
                ulong key = gram | c_Occupied;
                int slot = (int)((key * c_Multiplier) >> _shift);
                while (true)
                {
                    ulong stored = slotKeys[slot];
                    if (stored == key)
                    {
                        // Each bucket is added once, however often its trigram occurs.
                        if (BitUtilities.TrySetBit(expanded, slot))
                        {
                            for (int b = _bucketStarts[slot]; b < _bucketStarts[slot + 1]; b++)
                                BitUtilities.SetBit(candidates, _bucketPatterns[b]);
                        }

                        break;
                    }

                    if (stored == 0)
                        break;

                    slot = (slot + 1) & mask;
                }
            }
        }
        finally
        {
            if (rented != null)
                ArrayPool<ulong>.Shared.Return(rented);
        }
    }

    /// <summary>
    /// Sets <paramref name="candidates"/> to the patterns that can match <paramref name="target"/>, or equal the input or the normalized input.
    /// </summary>
    /// <param name="target">The text the patterns are matched against.</param>
    /// <param name="input">The original input.</param>
    /// <param name="normalized">The normalized input.</param>
    /// <param name="changed">Whether <paramref name="normalized"/> differs from <paramref name="input"/>.</param>
    /// <param name="candidates">A bit set of <see cref="Words"/> words, one bit per pattern.</param>
    public void FindCandidates(ReadOnlySpan<char> target, ReadOnlySpan<char> input, ReadOnlySpan<char> normalized, bool changed, Span<ulong> candidates)
    {
        FindCandidates(target, candidates);
        MarkEqual(input, candidates);
        if (changed)
            MarkEqual(normalized, candidates);
    }

    /// <summary>
    /// Adds to <paramref name="candidates"/> the first pattern equal to <paramref name="value"/>, if any.
    /// </summary>
    /// <param name="value">The input or the normalized input.</param>
    /// <param name="candidates">The candidate bit set.</param>
    private void MarkEqual(ReadOnlySpan<char> value, Span<ulong> candidates)
    {
        if (value.IsEmpty)
            return;

        int bucket = Math.Min(value.Length, c_MaxIndexedLength + 1) - 1;
        if ((_equalLengths[bucket >> 6] & (1UL << bucket)) == 0 || !_equal.TryGetValue(Hash(value), out int[]? indexes))
            return;

        foreach (int index in indexes)
        {
            if (value.SequenceEqual(_compiled[index].Pattern.AsSpan()))
            {
                BitUtilities.SetBit(candidates, index);
                return;
            }
        }
    }

    /// <summary>
    /// Computes an ordinal hash of <paramref name="value"/>.
    /// </summary>
    /// <param name="value">The text.</param>
    /// <returns>The hash.</returns>
    private static int Hash(ReadOnlySpan<char> value)
    {
        uint hash = 2166136261;
        foreach (char c in value)
            hash = (hash ^ c) * 16777619;

        return (int)hash;
    }

    /// <summary>
    /// Ranks a trigram as a key; lower is better.
    /// </summary>
    /// <param name="gram">The trigram.</param>
    /// <param name="patterns">The number of patterns that could be filed under it.</param>
    /// <returns>The score: fewer sharing patterns first, then trigrams with punctuation, which are rarer in paths.</returns>
    private static int Score(ulong gram, int patterns)
    {
        bool punctuation = !char.IsLetterOrDigit((char)(gram >> 32)) || !char.IsLetterOrDigit((char)(gram >> 16)) || !char.IsLetterOrDigit((char)gram);
        return (patterns * 2) + (punctuation ? 0 : 1);
    }

    /// <summary>
    /// Finds the slot of <paramref name="key"/>, claiming an empty one if requested.
    /// </summary>
    /// <param name="key">The trigram with <see cref="c_Occupied"/> set.</param>
    /// <param name="insert"><see langword="true"/> to claim an empty slot when <paramref name="key"/> is absent.</param>
    /// <returns>The slot.</returns>
    /// <exception cref="InvalidOperationException"><paramref name="key"/> is absent and <paramref name="insert"/> is <see langword="false"/>.</exception>
    private int FindSlot(ulong key, bool insert)
    {
        int mask = _slotKeys.Length - 1;
        int slot = (int)((key * c_Multiplier) >> _shift);
        while (_slotKeys[slot] != key)
        {
            if (_slotKeys[slot] == 0)
            {
                if (!insert)
                    throw new InvalidOperationException("The key was not inserted.");

                _slotKeys[slot] = key;
                break;
            }

            slot = (slot + 1) & mask;
        }

        return slot;
    }

    /// <summary>
    /// Gets the number of 64-bit words in a candidate bit set.
    /// </summary>
    public int Words { get; }
}
