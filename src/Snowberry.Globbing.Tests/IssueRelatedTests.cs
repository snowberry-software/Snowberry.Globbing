namespace Snowberry.Globbing.Tests;

/// <summary>
/// Tests for issue-related regression cases ported from picomatch.
/// </summary>
public class IssueRelatedTests
{

    [Theory]
    [InlineData("src/views/index.ts", "src/**/*{.ts,.tsx,.js,.jsx}", true)]
    [InlineData("src/views/index.tsx", "src/**/*{.ts,.tsx,.js,.jsx}", true)]
    [InlineData("src/views/index.js", "src/**/*{.ts,.tsx,.js,.jsx}", true)]
    [InlineData("src/views/index.jsx", "src/**/*{.ts,.tsx,.js,.jsx}", true)]
    public void ShouldMatchBraceExtensionsWithDotOption(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { Dot = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData(".view/index.ts", "src/**/*{.ts,.tsx,.js,.jsx}", false)]
    [InlineData("src/.view/index.ts", "src/**/*{.ts,.tsx,.js,.jsx}", true)]
    public void ShouldHandleDotDirectoriesWithBraces(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { Dot = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("foo", "!(foo/bar)", true)]
    [InlineData("foo/bar", "!(foo/bar)", false)]
    [InlineData("foo/baz", "!(foo/bar)", true)]
    public void ShouldSupportNegationExtglobWithSlashes(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("b", "[\\\\a-z]", true)]
    [InlineData("z", "[\\\\a-z]", true)]
    [InlineData("a", "[\\\\a-z]", true)]
    public void ShouldMatchCharactersInBracketRangeWithBackslash(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("ab", "a?", true)]
    [InlineData("abc", "a?c", true)]
    [InlineData("a/b", "a?b", false)]
    [InlineData("a\\b", "a?b", false)]
    public void QuestionMarkShouldNotMatchSlashes(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, new GlobbingOptions { Windows = true }));
    }

    [Theory]
    [InlineData("a/b", "a?b", false)]
    [InlineData("a\\b", "a?b", true)]
    public void QuestionMarkShouldMatchBackslashWhenWindowsDisabled(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, new GlobbingOptions { Windows = false }));
    }

    [Theory]
    [InlineData("a;b", "a;b", true)]
    [InlineData("a:b", "a:b", true)]
    [InlineData("a,b", "a,b", true)]
    public void ShouldMatchSpecialCharactersLiterally(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("~test", "~test", true)]
    [InlineData("~test", "~*", true)]
    [InlineData("test~", "*~", true)]
    public void ShouldMatchTildeLiterally(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("日本語", "*", true)]
    [InlineData("日本語/test", "日本語/*", true)]
    [InlineData("日本語/test.txt", "日本語/*.txt", true)]
    [InlineData("テスト", "*", true)]
    [InlineData("テスト/file.js", "テスト/**/*.js", true)]
    public void ShouldSupportJapaneseCharacters(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("日本語", "日本語", true)]
    [InlineData("日本語", "日本", false)]
    [InlineData("日本語abc", "日本語*", true)]
    [InlineData("abc日本語", "*日本語", true)]
    public void ShouldMatchJapaneseLiteralsCorrectly(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData(".dotfile", ".*", true)]
    [InlineData(".dotfile", "*", false)]
    [InlineData(".dotfile", "*", true)]
    public void ShouldHandleDotfilesCorrectly(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { Dot = expected || pattern == ".*" };
        bool actualExpected = pattern == ".*" || (pattern == "*" && expected);
        Assert.Equal(actualExpected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("test/.dotfile", "test/.*", true)]
    [InlineData("test/.dotfile", "test/*", false)]
    [InlineData("test/.dotfile", "**/*", false)]
    [InlineData("test/.dotfile", "**/*", true)]
    public void ShouldHandleNestedDotfilesCorrectly(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { Dot = expected || pattern.Contains(".*") };
        bool actualExpected = pattern.Contains(".*") || (expected && !pattern.Contains(".*"));
        Assert.Equal(actualExpected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("", "*", false)]
    [InlineData("", "**", false)]
    public void ShouldHandleEmptyStrings(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("foo/", "foo", false)]
    [InlineData("foo/", "foo/", true)]
    public void ShouldHandleTrailingSlashes(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("foo/", "foo/*", false)]
    [InlineData("foo/bar/", "foo/*", true)]
    public void ShouldHandleTrailingSlashesWithStar(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("(test)", "\\(test\\)", true)]
    [InlineData("[test]", "\\[test\\]", true)]
    [InlineData("{test}", "\\{test\\}", true)]
    [InlineData("test.txt", "test\\.txt", true)]
    [InlineData("test+txt", "test\\+txt", true)]
    [InlineData("test^txt", "test\\^txt", true)]
    [InlineData("test$txt", "test\\$txt", true)]
    public void ShouldMatchEscapedRegexCharacters(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a/b/c/d", "a/**/d", true)]
    [InlineData("a/b/c/d", "a/**/**/d", true)]
    [InlineData("a/b/c/d", "**/b/**/d", true)]
    [InlineData("a/b/c/d", "**/c/d", true)]
    [InlineData("a/b/c/d", "a/b/**/d", true)]
    public void ShouldMatchComplexNestedGlobstars(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a/b/c/d/e", "a/**/c/**/e", true)]
    [InlineData("a/x/c/y/e", "a/**/c/**/e", true)]
    [InlineData("a/x/y/c/z/e", "a/**/c/**/e", true)]
    public void ShouldMatchMultipleGlobstarsInPattern(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a", "{a,b,c}", true)]
    [InlineData("b", "{a,b,c}", true)]
    [InlineData("c", "{a,b,c}", true)]
    [InlineData("d", "{a,b,c}", false)]
    public void ShouldHandleBraceExpansion(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("{", "\\{", true)]
    [InlineData("}", "\\}", true)]
    [InlineData("{a,b}", "\\{a,b\\}", true)]
    public void ShouldHandleEscapedBraces(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a.js", "*.{js,ts}", true)]
    [InlineData("a.ts", "*.{js,ts}", true)]
    [InlineData("a.jsx", "*.{js,ts}", false)]
    [InlineData("a.js", "*.{js,jsx,ts,tsx}", true)]
    [InlineData("a.jsx", "*.{js,jsx,ts,tsx}", true)]
    public void ShouldHandleBraceExpansionWithExtensions(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("directory/.test.txt", "{file.txt,directory/**/*}", true)]
    [InlineData("directory/test.txt", "{file.txt,directory/**/*}", true)]
    public void Issue8_ShouldMatchWithBraces_DotTrue(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { Dot = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("directory/.test.txt", "{file.txt,directory/**/*}", false)]
    [InlineData("directory/test.txt", "{file.txt,directory/**/*}", true)]
    public void Issue8_ShouldMatchWithBraces_DotFalse(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("フォルダ/aaa.js", "フ*/**/*", true)]
    [InlineData("フォルダ/aaa.js", "フォ*/**/*", true)]
    [InlineData("フォルダ/aaa.js", "フォル*/**/*", true)]
    [InlineData("フォルダ/aaa.js", "フ*ル*/**/*", true)]
    [InlineData("フォルダ/aaa.js", "フォルダ/**/*", true)]
    public void Issue127_ShouldMatchJapaneseCharacters(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a/b-c/d/e/z.js", "a/b-*/**/z.js", true)]
    [InlineData("z.js", "z*", true)]
    [InlineData("z.js", "**/z*", true)]
    [InlineData("z.js", "**/z*.js", true)]
    [InlineData("z.js", "**/*.js", true)]
    [InlineData("foo", "**/foo", true)]
    public void Issue15_Comprehensive(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("zzjs", "z*.js", false)]
    [InlineData("zzjs", "*z.js", false)]
    public void Issue23_Comprehensive(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a/b/c/d/", "a/b/**/f", false)]
    [InlineData("a", "a/**", true)]
    [InlineData("a", "**", true)]
    [InlineData("a/", "**", true)]
    [InlineData("a/b/c/d", "**", true)]
    [InlineData("a/b/c/d/", "**", true)]
    [InlineData("a/b/c/d/", "**/**", true)]
    [InlineData("a/b/c/d/", "**/b/**", true)]
    [InlineData("a/b/c/d/", "a/b/**", true)]
    [InlineData("a/b/c/d/", "a/b/**/", true)]
    [InlineData("a/b/c/d/e.f", "a/b/**/**/*.*", true)]
    [InlineData("a/b/c/d/e.f", "a/b/**/*.*", true)]
    [InlineData("a/b/c/d/g/e.f", "a/b/**/d/**/*.*", true)]
    [InlineData("a/b/c/d/g/g/e.f", "a/b/**/d/**/*.*", true)]
    public void Issue24_Comprehensive(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a/b/c", "a/b**", false)]
    [InlineData("a/c/b", "a/**b", false)]
    public void Issue58_OnlyMatchNestedDirsWhenGlobstarAlone(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a/foo.js", "**/foo.js", true)]
    [InlineData("foo.js", "**/foo.js", true)]
    public void Issue79_Comprehensive_Default(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a/foo.js", "**/foo.js", true)]
    [InlineData("foo.js", "**/foo.js", true)]
    public void Issue79_Comprehensive_DotTrue(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { Dot = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

}
