using System.Collections.Concurrent;

namespace Snowberry.Globbing;

/// <summary>
/// The bounded cache of the globs the static matching methods of <see cref="Glob"/> compile.
/// </summary>
/// <remarks>
/// Once the cache is full, a new glob replaces a randomly chosen one. Options are compared as records, so equal
/// <see cref="GlobOptions.IgnorePatterns"/> lists that are different instances are different keys.
/// </remarks>
internal static class GlobCache
{
    private const int c_Capacity = 256;

    private static readonly ConcurrentDictionary<(string Pattern, GlobOptions Options), Glob> s_Cache = new(GlobCacheKeyComparer.Instance);

    private static readonly (string Pattern, GlobOptions Options)[] s_Keys = new (string, GlobOptions)[c_Capacity];

    private static readonly object s_Lock = new();

    private static int s_Count;

    private static uint s_Seed = 2463534242;

    /// <summary>
    /// Gets the cached glob for <paramref name="pattern"/> and <paramref name="options"/>, compiling and caching it on a miss.
    /// </summary>
    /// <param name="pattern">The validated glob pattern.</param>
    /// <param name="options">The options.</param>
    /// <returns>The glob for the pattern and options.</returns>
    /// <exception cref="GlobParseException"><paramref name="pattern"/> or a <see cref="GlobOptions.IgnorePatterns"/> entry cannot be compiled.</exception>
    public static Glob GetOrAdd(string pattern, GlobOptions options)
    {
        var key = (pattern, options);
        if (s_Cache.TryGetValue(key, out var glob))
            return glob;

        glob = new Glob(pattern, options);
        lock (s_Lock)
        {
            if (s_Cache.TryGetValue(key, out var cached))
                return cached;

            if (s_Count < c_Capacity)
            {
                s_Keys[s_Count++] = key;
            }
            else
            {
                int slot = (int)(NextSeed() % c_Capacity);
                s_Cache.TryRemove(s_Keys[slot], out _);
                s_Keys[slot] = key;
            }

            s_Cache[key] = glob;
        }

        return glob;
    }

    /// <summary>
    /// Advances the xorshift generator that picks the entry to replace; called under the lock.
    /// </summary>
    /// <returns>The next pseudo-random value.</returns>
    private static uint NextSeed()
    {
        uint seed = s_Seed;
        seed ^= seed << 13;
        seed ^= seed >> 17;
        seed ^= seed << 5;
        s_Seed = seed;
        return seed;
    }
}
