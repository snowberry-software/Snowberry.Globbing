using Snowberry.Globbing.Compilation;

namespace Snowberry.Globbing.Tests;

/// <summary>
/// The regex source compiled for a pattern, and whether the pattern is negated.
/// </summary>
/// <param name="Output">The anchored regex source.</param>
/// <param name="Negated"><see langword="true"/> if the pattern starts with a negating <c>!</c>.</param>
internal sealed record TestCompilation(string Output, bool Negated);

/// <summary>
/// Entry points into the internal compiler for tests that assert on the generated regex.
/// </summary>
internal static class TestHelpers
{
    /// <summary>
    /// Compiles <paramref name="pattern"/> without the compact forms of common patterns.
    /// </summary>
    /// <param name="pattern">The glob pattern.</param>
    /// <param name="options">The options, or <see langword="null"/> for <see cref="GlobOptions.Default"/>.</param>
    /// <returns>The regex source and whether the pattern is negated.</returns>
    public static TestCompilation Parse(string pattern, GlobOptions? options = null)
    {
        string source = GlobCompiler.CompileRegexSource(pattern, options ?? GlobOptions.Default, fastPaths: false);
        return new TestCompilation(source, source.StartsWith("^(?!", StringComparison.Ordinal));
    }
}