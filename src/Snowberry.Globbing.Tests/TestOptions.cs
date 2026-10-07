namespace Snowberry.Globbing.Tests;

/// <summary>
/// Option sets shared by the tests.
/// </summary>
internal static class TestOptions
{
    /// <summary>The default options with <see cref="GlobPathStyle.Posix"/> paths.</summary>
    public static readonly GlobOptions Posix = new() { PathStyle = GlobPathStyle.Posix };

    /// <summary>The default options with <see cref="GlobPathStyle.Windows"/> paths.</summary>
    public static readonly GlobOptions Windows = new() { PathStyle = GlobPathStyle.Windows };
}
