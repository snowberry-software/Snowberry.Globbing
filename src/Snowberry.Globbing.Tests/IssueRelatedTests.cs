namespace Snowberry.Globbing.Tests;

/// <summary>
/// Tests for issue-related regression cases ported from picomatch.
/// </summary>
public class IssueRelatedTests
{
    [Theory]
    [InlineData("src/views/index.ts", "src/**/*{.ts,.tsx,.js,.jsx}", true)]
    [InlineData("src/views/index.jsx", "src/**/*{.ts,.tsx,.js,.jsx}", true)]
    [InlineData(".view/index.ts", "src/**/*{.ts,.tsx,.js,.jsx}", false)]
    [InlineData("src/.view/index.ts", "src/**/*{.ts,.tsx,.js,.jsx}", true)]
    public void ShouldMatchBraceExtensionsWithDotOption(string input, string pattern, bool expected)
    {
        var options = new GlobOptions { MatchDotFiles = true };
        Glob.IsMatch(input, pattern, options).Should().Be(expected);
    }

    [Theory]
    [InlineData("foo", "!(foo/bar)", true)]
    [InlineData("foo/bar", "!(foo/bar)", false)]
    [InlineData("foo/baz", "!(foo/bar)", true)]
    public void ShouldSupportNegationExtglobWithSlashes(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("z", "[\\\\a-z]", true)]
    [InlineData("a", "[\\\\a-z]", true)]
    public void ShouldMatchCharactersInBracketRangeWithBackslash(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("ab", "a?", true)]
    [InlineData("abc", "a?c", true)]
    [InlineData("a/b", "a?b", false)]
    [InlineData("a\\b", "a?b", false)]
    public void QuestionMarkShouldNotMatchSlashes(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern, new GlobOptions { PathStyle = GlobPathStyle.Windows }).Should().Be(expected);
    }

    [Theory]
    [InlineData("a/b", "a?b", false)]
    [InlineData("a\\b", "a?b", true)]
    public void QuestionMarkShouldMatchBackslashWhenWindowsDisabled(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern, new GlobOptions { PathStyle = GlobPathStyle.Posix }).Should().Be(expected);
    }

    [Theory]
    [InlineData("~test", "~test", true)]
    [InlineData("test~", "*~", true)]
    public void ShouldMatchTildeLiterally(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("日本語", "*", true)]
    [InlineData("日本語/test", "日本語/*", true)]
    [InlineData("テスト/file.js", "テスト/**/*.js", true)]
    public void ShouldSupportJapaneseCharacters(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("日本語", "日本語", true)]
    [InlineData("日本語", "日本", false)]
    [InlineData("日本語abc", "日本語*", true)]
    [InlineData("abc日本語", "*日本語", true)]
    public void ShouldMatchJapaneseLiteralsCorrectly(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData(".dotfile", ".*", true, true)]
    [InlineData(".dotfile", "*", false, false)]
    [InlineData(".dotfile", "*", true, true)]
    [InlineData("test/.dotfile", "test/.*", true, true)]
    [InlineData("test/.dotfile", "test/*", false, false)]
    [InlineData("test/.dotfile", "**/*", false, false)]
    [InlineData("test/.dotfile", "**/*", true, true)]
    public void ShouldHandleDotfilesCorrectly(string input, string pattern, bool matchDotFiles, bool expected)
    {
        var options = new GlobOptions { MatchDotFiles = matchDotFiles };
        Glob.IsMatch(input, pattern, options).Should().Be(expected);
    }

    [Theory]
    [InlineData("", "**", false)]
    public void ShouldHandleEmptyStrings(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("foo/", "foo/", true)]
    [InlineData("foo/", "foo/*", false)]
    [InlineData("foo/bar/", "foo/*", true)]
    public void ShouldHandleTrailingSlashes(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a/b/c/d", "a/**/d", true)]
    [InlineData("a/b/c/d", "a/**/**/d", true)]
    [InlineData("a/b/c/d", "**/b/**/d", true)]
    public void ShouldMatchComplexNestedGlobstars(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a/b/c/d/e", "a/**/c/**/e", true)]
    [InlineData("a/x/y/c/z/e", "a/**/c/**/e", true)]
    public void ShouldMatchMultipleGlobstarsInPattern(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a.js", "*.{js,ts}", true)]
    [InlineData("a.jsx", "*.{js,ts}", false)]
    [InlineData("a.jsx", "*.{js,jsx,ts,tsx}", true)]
    public void ShouldHandleBraceExpansionWithExtensions(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("directory/.test.txt", "{file.txt,directory/**/*}", true, true)]
    [InlineData("directory/test.txt", "{file.txt,directory/**/*}", true, true)]
    [InlineData("directory/.test.txt", "{file.txt,directory/**/*}", false, false)]
    [InlineData("directory/test.txt", "{file.txt,directory/**/*}", false, true)]
    public void Issue8_ShouldMatchWithBraces(string input, string pattern, bool matchDotFiles, bool expected)
    {
        var options = new GlobOptions { MatchDotFiles = matchDotFiles };
        Glob.IsMatch(input, pattern, options).Should().Be(expected);
    }

    [Theory]
    [InlineData("フォルダ/aaa.js", "フ*/**/*", true)]
    [InlineData("フォルダ/aaa.js", "フ*ル*/**/*", true)]
    [InlineData("フォルダ/aaa.js", "フォルダ/**/*", true)]
    public void Issue127_ShouldMatchJapaneseCharacters(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("z.js", "**/z*", true)]
    [InlineData("z.js", "**/z*.js", true)]
    [InlineData("z.js", "**/*.js", true)]
    [InlineData("foo", "**/foo", true)]
    public void Issue15_LeadingGlobstar_MatchesRootLevelFile(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a/b/c", "a/b**", false)]
    [InlineData("a/c/b", "a/**b", false)]
    public void Issue58_OnlyMatchNestedDirsWhenGlobstarAlone(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("a/foo.js", "**/foo.js", true)]
    [InlineData("foo.js", "**/foo.js", true)]
    public void Issue79_LeadingGlobstar_MatchesWithAndWithoutDirectory(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

}