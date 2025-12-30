using System;
using System.Text;

namespace Snowberry.Globbing.Utilities;

internal static class StringBuilderPool
{
    private const int c_DefaultCapacity = 256;
    private const int c_MaxCapacity = 4096;
    private const int c_MaxPoolSize = 4;

    [ThreadStatic]
    private static StringBuilder?[]? t_Pool;

    [ThreadStatic]
    private static int t_PoolCount;

    public static StringBuilder Rent()
    {
        if (t_Pool != null && t_PoolCount > 0)
        {
            var sb = t_Pool[--t_PoolCount];
            t_Pool[t_PoolCount] = null;
            sb!.Clear();
            return sb;
        }

        return new StringBuilder(c_DefaultCapacity);
    }

    public static void Return(StringBuilder sb)
    {
        if (sb == null || sb.Capacity > c_MaxCapacity)
            return;

        t_Pool ??= new StringBuilder[c_MaxPoolSize];

        if (t_PoolCount < c_MaxPoolSize)
            t_Pool[t_PoolCount++] = sb;
    }

    public static string Build(Action<StringBuilder> buildAction)
    {
        var sb = Rent();
        try
        {
            buildAction(sb);
#if NET5_0_OR_GREATER
            return string.Create(sb.Length, sb, static (span, builder) =>
            {
                foreach (var chunk in builder.GetChunks())
                {
                    chunk.Span.CopyTo(span);
                    span = span[chunk.Length..];
                }
            });
#else
            return sb.ToString();
#endif
        }
        finally
        {
            Return(sb);
        }
    }
}
