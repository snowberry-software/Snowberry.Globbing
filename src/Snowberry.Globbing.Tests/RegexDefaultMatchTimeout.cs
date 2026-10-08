using System.Runtime.CompilerServices;

namespace Snowberry.Globbing.Tests;

/// <summary>
/// Configures the process-wide default regex match timeout before any regex is created, so tests can tell a regex built
/// with the default apart from one built with <see cref="Regex.InfiniteMatchTimeout"/>.
/// </summary>
/// <remarks>
/// .NET reads <c>REGEX_DEFAULT_MATCH_TIMEOUT</c> once, when the first regex is created, and accepts only a
/// <see cref="TimeSpan"/> set as app domain data, so it cannot come from <c>runtimeconfig.json</c>. The value is long
/// enough that no test relying on the default can reach it.
/// </remarks>
internal static class RegexDefaultMatchTimeout
{
    /// <summary>The process-wide default regex match timeout of the test process.</summary>
    public static readonly TimeSpan Value = TimeSpan.FromMinutes(30);

    /// <summary>Sets the process-wide default regex match timeout.</summary>
    [ModuleInitializer]
    internal static void Initialize()
    {
        AppDomain.CurrentDomain.SetData("REGEX_DEFAULT_MATCH_TIMEOUT", Value);
    }
}
