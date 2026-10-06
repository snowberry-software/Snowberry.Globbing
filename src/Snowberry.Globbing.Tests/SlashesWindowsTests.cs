namespace Snowberry.Globbing.Tests;

/// <summary>
/// Tests for Windows path handling (backslash support) ported from picomatch.
/// </summary>
public class SlashesWindowsTests
{
    [Theory]
    [InlineData("a\\b", "a/b", true)]
    [InlineData("a\\a", "a/b", false)]
    [InlineData("b\\c", "a/b", false)]
    [InlineData("a\\b", "(a/b)", true)]
    [InlineData("a\\a", "(a/b)", false)]
    [InlineData("a\\a", "a/(a|c)", true)]
    [InlineData("a\\b", "a/(a|c)", false)]
    [InlineData("a\\b", "a/(a|b|c)", true)]
    [InlineData("a\\a", "a/[b-c]", false)]
    [InlineData("a\\b", "a/[b-c]", true)]
    [InlineData("a\\x", "a/[b-c]", false)]
    [InlineData("a\\x", "a/[a-z]", true)]
    [InlineData("a", "*", true)]
    [InlineData("a\\a", "*", false)]
    [InlineData("x\\y", "*", false)]
    [InlineData("a", "*/*", false)]
    [InlineData("a\\a", "*/*", true)]
    [InlineData("a\\a\\a", "*/*", false)]
    [InlineData("a\\a", "*/*/*", false)]
    [InlineData("a\\a\\a", "*/*/*", true)]
    [InlineData("a\\a\\a\\a", "*/*/*", false)]
    [InlineData("a", "a/*", false)]
    [InlineData("a\\b", "a/*", true)]
    [InlineData("a\\a\\a", "a/*", false)]
    [InlineData("x\\y", "a/*", false)]
    [InlineData("a\\a\\a", "a/*/a", true)]
    [InlineData("a\\a\\b", "a/*/a", false)]
    [InlineData("a\\b\\a", "a/*/a", true)]
    [InlineData("a\\a\\a\\a", "a/*/a", false)]
    [InlineData("a\\a", "a/**", true)]
    [InlineData("a\\x\\y\\z", "a/**", true)]
    [InlineData("a\\a", "a/**/*", true)]
    [InlineData("a\\x\\y\\z", "a/**/*", true)]
    [InlineData("a.txt", "a*.txt", true)]
    [InlineData("a\\b.txt", "a*.txt", false)]
    [InlineData("a\\x\\y.txt", "a*.txt", false)]
    [InlineData("a.txt", "a/**/*.txt", false)]
    [InlineData("a\\b.txt", "a/**/*.txt", true)]
    [InlineData("a\\x\\y.txt", "a/**/*.txt", true)]
    [InlineData("a\\x\\y\\z", "a/**/*.txt", false)]
    [InlineData("a.txt", "a/*.txt", false)]
    [InlineData("a\\b.txt", "a/*.txt", true)]
    [InlineData("a\\x\\y.txt", "a/*.txt", false)]
    [InlineData("a\\b.txt", "a/*/*.txt", false)]
    [InlineData("a\\x\\y.txt", "a/*/*.txt", true)]
    [InlineData("a", "!a/b", true)]
    [InlineData("a\\b", "!a/b", false)]
    [InlineData("b\\a", "!a/b", true)]
    [InlineData("a\\b", "!*/c", true)]
    [InlineData("a\\c", "!*/c", false)]
    [InlineData("b\\c", "!*/c", false)]
    [InlineData("a\\b", "!a/(b)", false)]
    [InlineData("a\\c", "!a/(b)", true)]
    [InlineData("a", "!(a/b)", true)]
    [InlineData("a\\b", "!(a/b)", false)]
    [InlineData("a\\c", "!(a/b)", true)]
    public void ShouldMatchWindowsSeparators(string input, string pattern, bool expected)
    {
        var options = new GlobOptions { PathStyle = GlobPathStyle.Windows };
        Glob.IsMatch(input, pattern, options).Should().Be(expected);
    }

    [Theory]
    [InlineData("a\\b", "a/b", false)]
    [InlineData("a\\c", "a/b", false)]
    [InlineData("a\\a", "a/(a|c)", false)]
    [InlineData("a\\c", "a/(a|b|c)", false)]
    [InlineData("a\\x", "a/[b-c]", false)]
    [InlineData("a\\b", "a/[a-z]", false)]
    [InlineData("a\\b", "a/**", false)]
    [InlineData("a\\x\\y\\z", "a/**", false)]
    [InlineData("a\\b", "*", true)]
    public void ShouldNotTreatBackslashAsSeparatorInPosixStyle(string input, string pattern, bool expected)
    {
        var options = new GlobOptions { PathStyle = GlobPathStyle.Posix };
        Glob.IsMatch(input, pattern, options).Should().Be(expected);
    }
}