namespace Snowberry.Globbing.Tests;

/// <summary>
/// Tests for dotfile matching ported from picomatch.
/// </summary>
public class DotfilesTests
{

    [Theory]
    [InlineData(".a", "*", false)]
    [InlineData(".ab", "*", false)]
    [InlineData(".abc", "*", false)]
    [InlineData(".gitignore", "*", false)]
    [InlineData("a", "*", true)]
    [InlineData("ab", "*", true)]
    [InlineData("abc", "*", true)]
    public void StarShouldNotMatchDotfilesWithoutDotOption(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData(".a", "**", false)]
    [InlineData(".ab", "**", false)]
    [InlineData(".abc", "**", false)]
    [InlineData(".gitignore", "**", false)]
    [InlineData("a", "**", true)]
    [InlineData("ab", "**", true)]
    [InlineData("abc", "**", true)]
    public void GlobstarShouldNotMatchDotfilesWithoutDotOption(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData(".a", "?", false)]
    [InlineData(".ab", "??", false)]
    public void QuestionMarkShouldNotMatchDotfilesWithoutDotOption(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a/.b", "a/*", false)]
    [InlineData("a/.bc", "a/*", false)]
    [InlineData("a/.gitignore", "a/*", false)]
    [InlineData("a/b", "a/*", true)]
    [InlineData("a/bc", "a/*", true)]
    public void StarShouldNotMatchDotfilesInPathWithoutDotOption(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a/.b", "a/**", false)]
    [InlineData("a/b/.c", "a/**", false)]
    [InlineData("a/b/.c/d", "a/**", false)]
    [InlineData("a/b/c", "a/**", true)]
    public void GlobstarShouldNotMatchDotfilesInPathWithoutDotOption(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a/.b", "*/*", false)]
    [InlineData("a/.bc", "*/*", false)]
    [InlineData("a/b", "*/*", true)]
    [InlineData("a/bc", "*/*", true)]
    public void StarSlashStarShouldNotMatchDotfilesWithoutDotOption(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a/.b/c", "**/c", false)]
    [InlineData("a/b/c", "**/c", true)]
    public void GlobstarPatternShouldNotMatchThroughDotDirectoryWithoutDotOption(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData(".a", "*", true)]
    [InlineData(".ab", "*", true)]
    [InlineData(".abc", "*", true)]
    [InlineData(".gitignore", "*", true)]
    [InlineData("a", "*", true)]
    [InlineData("ab", "*", true)]
    [InlineData("abc", "*", true)]
    public void StarShouldMatchDotfilesWithDotOption(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { Dot = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData(".a", "**", true)]
    [InlineData(".ab", "**", true)]
    [InlineData(".abc", "**", true)]
    [InlineData(".gitignore", "**", true)]
    [InlineData("a", "**", true)]
    public void GlobstarShouldMatchDotfilesWithDotOption(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { Dot = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData(".a", "?a", true)]
    [InlineData(".ab", "???", true)]
    public void QuestionMarkShouldMatchDotfilesWithDotOption(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { Dot = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("a/.b", "a/*", true)]
    [InlineData("a/.bc", "a/*", true)]
    [InlineData("a/.gitignore", "a/*", true)]
    [InlineData("a/b", "a/*", true)]
    public void StarShouldMatchDotfilesInPathWithDotOption(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { Dot = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("a/.b", "a/**", true)]
    [InlineData("a/b/.c", "a/**", true)]
    [InlineData("a/b/.c/d", "a/**", true)]
    [InlineData("a/b/c", "a/**", true)]
    public void GlobstarShouldMatchDotfilesInPathWithDotOption(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { Dot = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("a/.b", "*/*", true)]
    [InlineData("a/.bc", "*/*", true)]
    [InlineData("a/b", "*/*", true)]
    public void StarSlashStarShouldMatchDotfilesWithDotOption(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { Dot = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("a/.b/c", "**/c", true)]
    [InlineData("a/b/c", "**/c", true)]
    public void GlobstarPatternShouldMatchThroughDotDirectoryWithDotOption(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { Dot = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData(".a", ".*", true)]
    [InlineData(".ab", ".*", true)]
    [InlineData(".abc", ".*", true)]
    [InlineData(".gitignore", ".*", true)]
    [InlineData("a", ".*", false)]
    public void ExplicitDotPatternShouldMatchDotfiles(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData(".gitignore", ".gitignore", true)]
    [InlineData(".gitignore", ".git*", true)]
    [InlineData(".gitattributes", ".git*", true)]
    [InlineData("gitignore", ".git*", false)]
    public void ExplicitDotPatternWithNameShouldMatch(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a/.b", "a/.*", true)]
    [InlineData("a/.bc", "a/.*", true)]
    [InlineData("a/b", "a/.*", false)]
    public void ExplicitDotPatternInPathShouldMatch(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a/.b", "**/.*", true)]
    [InlineData("a/b/.c", "**/.*", true)]
    [InlineData("a/b/c", "**/.*", false)]
    public void GlobstarWithExplicitDotPatternShouldMatch(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData(".a", ".?", true)]
    [InlineData(".ab", ".??", true)]
    [InlineData("a", ".?", false)]
    public void ExplicitDotWithQuestionMarkShouldMatchDotfiles(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a.b", "*.*", true)]
    [InlineData("a.b.c", "*.*", true)]
    [InlineData("a", "*.*", false)]
    public void DotInMiddleShouldBeMatched(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a.js", "*.js", true)]
    [InlineData("file.test.js", "*.js", true)]
    [InlineData(".js", "*.js", false)]
    public void FileExtensionMatchingShouldWork(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData(".js", "*.js", true)]
    public void FileExtensionMatchingShouldMatchDotfilesWithDotOption(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { Dot = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("..", "*", false)]
    [InlineData("..", "**", false)]
    [InlineData("a/..", "*/*", false)]
    [InlineData("a/../b", "*/*/*", false)]
    public void DoubleDotShouldNotBeMatchedByDefault(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("..", "*", false)]
    [InlineData("..", "**", false)]
    [InlineData("a/..", "*/*", false)]
    public void DoubleDotShouldBeMatchedWithDotOption(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { Dot = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("..", "..", true)]
    [InlineData("a/..", "a/..", true)]
    [InlineData("a/../b", "a/../b", true)]
    public void ExplicitDoubleDotPatternShouldMatch(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData(".git/config", ".git/*", true)]
    [InlineData(".git/hooks/pre-commit", ".git/**", true)]
    [InlineData(".github/workflows/ci.yml", ".github/**/*.yml", true)]
    public void HiddenDirectoryShouldBeMatchedByExplicitPattern(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData(".git/config", "*/*", false)]
    [InlineData(".git/config", "**/*", false)]
    public void HiddenDirectoryShouldNotBeMatchedByDefaultPattern(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData(".git/config", "*/*", true)]
    [InlineData(".git/config", "**/*", true)]
    public void HiddenDirectoryShouldBeMatchedWithDotOption(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { Dot = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData(".dot", "**/*dot", true)]
    [InlineData(".dot", "*dot", true)]
    [InlineData(".dot", "?dot", true)]
    [InlineData(".dotfile.js", ".*.js", true)]
    [InlineData("/a/b/.dot", "/**/*dot", true)]
    [InlineData("/a/b/.dot", "**/*dot", true)]
    [InlineData("/a/b/.dot", "**/.[d]ot", true)]
    [InlineData("/a/b/.dot", "**/?dot", true)]
    [InlineData("/a/b/.dot", "/**/.[d]ot", true)]
    [InlineData("/a/b/.dot", "/**/?dot", true)]
    [InlineData("a/b/.dot", "**/*dot", true)]
    [InlineData("a/b/.dot", "**/.[d]ot", true)]
    [InlineData("a/b/.dot", "**/?dot", true)]
    public void ShouldMatchDotfilesWithDotOptionTrue(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { Dot = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("a/b/.dot", "**/*dot", false)]
    [InlineData("a/b/.dot", "**/?dot", false)]
    public void ShouldNotMatchDotfilesWithDotOptionFalse(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { Dot = false };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("a/b/.dot", "**/*dot")]
    [InlineData("a/b/.dot", "**/?dot")]
    public void ShouldNotMatchDotfilesWithoutDotOptionAndNoDotInPattern(string input, string pattern, bool expected = false)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("/aaa/.git/foo", "/aaa/**/*")]
    [InlineData("/aaa/bbb/.git", "/aaa/bbb/*")]
    [InlineData("/aaa/bbb/.git", "/aaa/bbb/**")]
    [InlineData("/aaa/bbb/ccc/.git", "/aaa/bbb/**")]
    [InlineData("aaa/bbb/.git", "aaa/bbb/**")]
    public void ShouldNotMatchGitDirectoriesWithoutDotOption(string input, string pattern, bool expected = false)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("/aaa/bbb/", "/aaa/bbb/**", true)]
    [InlineData("/aaa/bbb/foo", "/aaa/bbb/**", true)]
    public void ShouldMatchNonDotDirectoriesWithGlobstar(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("/aaa/.git/foo", "/aaa/**/*", true)]
    [InlineData("/aaa/bbb/.git", "/aaa/bbb/*", true)]
    [InlineData("/aaa/bbb/.git", "/aaa/bbb/**", true)]
    [InlineData("/aaa/bbb/ccc/.git", "/aaa/bbb/**", true)]
    [InlineData("aaa/bbb/.git", "aaa/bbb/**", true)]
    public void ShouldMatchGitDirectoriesWithDotOption(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { Dot = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("foo", "*", true)]
    [InlineData("foo/bar", "*/*", true)]
    [InlineData(".foo", "*")]
    [InlineData(".foo/bar", "*/*")]
    [InlineData(".foo/.bar", "*/*")]
    [InlineData("foo/.bar", "*/*")]
    [InlineData("foo/.bar/baz", "*/*/*")]
    public void ShouldNotMatchDotfilesWithSingleStarsByDefault(string input, string pattern, bool expected = false)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("../test.js", "../*.js", true)]
    [InlineData("../.test.js", "../*.js", true)]
    public void ShouldWorkWithDotsInPath(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { Dot = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("../.test.js", "../*.js")]
    public void ShouldNotMatchDotfilesInPathWithoutDotOption(string input, string pattern, bool expected = false)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData(".foo", "**/**")]
    [InlineData(".foo", "**")]
    [InlineData(".foo", "**/*")]
    [InlineData("bar/.foo", "**/*")]
    [InlineData(".bar", "**/*")]
    [InlineData("foo/.bar", "**/*")]
    [InlineData("foo/.bar", "**/*a*")]
    public void ShouldNotMatchDotfilesWithGlobstarByDefault(string input, string pattern, bool expected = false)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("foo", "**/.*a*")]
    [InlineData(".bar", "**/.*a*", true)]
    [InlineData("foo/.bar", "**/.*a*", true)]
    [InlineData(".foo", "**/.*", true)]
    public void ShouldMatchDotfilesWhenLeadingDotIsInPattern(string input, string pattern, bool expected = false)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("foo", ".*a*")]
    [InlineData(".bar", ".*a*", true)]
    [InlineData("bar", ".*a*")]
    [InlineData("foo", ".b*")]
    [InlineData(".bar", ".b*", true)]
    [InlineData("bar", ".b*")]
    [InlineData("foo", ".*r")]
    [InlineData(".bar", ".*r", true)]
    [InlineData("bar", ".*r")]
    public void ShouldMatchDotFilesWithExplicitDotPatterns(string input, string pattern, bool expected = false)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData(".dot", "**/*dot")]
    [InlineData(".dot", "**/?dot")]
    [InlineData(".dot", "*/*dot")]
    [InlineData(".dot", "*/?dot")]
    [InlineData(".dot", "*dot")]
    [InlineData(".dot", "/*dot")]
    [InlineData(".dot", "/?dot")]
    [InlineData("/.dot", "**/*dot")]
    [InlineData("/.dot", "**/?dot")]
    [InlineData("/.dot", "*/*dot")]
    [InlineData("/.dot", "*/?dot")]
    [InlineData("/.dot", "/*dot")]
    [InlineData("/.dot", "/?dot")]
    [InlineData("abc/.dot", "*/*dot")]
    [InlineData("abc/.dot", "*/?dot")]
    [InlineData("abc/.dot", "abc/*dot")]
    [InlineData("abc/abc/.dot", "**/*dot")]
    [InlineData("abc/abc/.dot", "**/?dot")]
    public void ShouldNotMatchDotWhenNotExplicitlyDefined(string input, string pattern, bool expected = false)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData(".dot", "?dot")]
    [InlineData("/.dot", "/?dot")]
    [InlineData("abc/.dot", "abc/?dot")]
    public void ShouldNotMatchLeadingDotsWithQuestionMarks(string input, string pattern, bool expected = false)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("../../b", "**/../*")]
    [InlineData("../../b", "*/../*")]
    [InlineData("../../b", "../*")]
    [InlineData("../abc", "*/../*")]
    [InlineData("../c/d", "**/../*")]
    [InlineData("../c/d", "*/../*")]
    [InlineData("../c/d", "../*")]
    [InlineData("abc", "**/../*")]
    [InlineData("abc", "*/../*")]
    [InlineData("abc", "../*")]
    [InlineData("abc/../abc", "../*")]
    [InlineData("abc/../", "**/../*")]
    public void ShouldNotMatchDoubleDotsThatAreNotMatched(string input, string pattern, bool expected = false)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("..", "..", true)]
    [InlineData("../b", "../*", true)]
    [InlineData("../../b", "../../*", true)]
    [InlineData("../../..", "../../..", true)]
    [InlineData("../abc", "**/../*", true)]
    [InlineData("../abc", "../*", true)]
    [InlineData("abc/../abc", "**/../*", true)]
    [InlineData("abc/../abc", "*/../*", true)]
    public void ShouldMatchDoubleDotsWhenDefinedInPattern(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("/.dot", "**/.[d]ot", true)]
    [InlineData("aaa/.dot", "**/.[d]ot", true)]
    [InlineData("aaa/bbb/.dot", "**/.[d]ot", true)]
    [InlineData("aaa/.dot", "*/.[d]ot", true)]
    [InlineData(".dot", ".[d]ot", true)]
    [InlineData("/.dot", "/.[d]ot", true)]
    public void ShouldMatchDotsDefinedInBrackets(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("/.dot", "**/.dot*", true)]
    [InlineData("aaa/bbb/.dot", "**/.dot*", true)]
    [InlineData("aaa/.dot", "*/.dot*", true)]
    [InlineData(".aaa.bbb", ".*.*", true)]
    [InlineData(".aaa.bbb", ".*.bbb", true)]
    [InlineData(".dotfile.js", ".*.js", true)]
    [InlineData(".dot", ".*ot", true)]
    [InlineData(".dot.bbb.ccc", ".*ot.*.*", true)]
    [InlineData(".dot", ".d?t", true)]
    [InlineData(".dot", ".dot*", true)]
    [InlineData("/.dot", "/.dot*", true)]
    public void ShouldMatchDotWhenExplicitlyDefined(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData(".abc/.abc", "**/.abc/**")]
    public void ShouldNotMatchNestedDotFolders(string input, string pattern, bool expected = false)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData(".abc", "**/.abc/**", true)]
    [InlineData(".abc/", "**/.abc/**", true)]
    [InlineData(".abc/abc", "**/.abc/**", true)]
    [InlineData(".abc/abc/b", "**/.abc/**", true)]
    [InlineData("abc/.abc/b", "**/.abc/**", true)]
    [InlineData("abc/abc/.abc", "**/.abc", true)]
    [InlineData("abc/abc/.abc", "**/.abc/**", true)]
    [InlineData("abc/abc/.abc/", "**/.abc/**", true)]
    [InlineData("abc/abc/.abc/abc", "**/.abc/**", true)]
    [InlineData("abc/abc/.abc/c/d", "**/.abc/**", true)]
    [InlineData("abc/abc/.abc/c/d/e", "**/.abc/**", true)]
    public void ShouldMatchLeadingDotsInRootPath(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData(".editorconfig", ".editorconfig", true)]
    [InlineData(".eslintrc", ".eslintrc", true)]
    [InlineData(".eslintrc.js", ".eslintrc.*", true)]
    [InlineData(".eslintrc.json", ".eslintrc.*", true)]
    [InlineData(".prettierrc", ".prettierrc", true)]
    public void CommonDotfilesShouldBeMatched(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData(".env", ".env", true)]
    [InlineData(".env.local", ".env*", true)]
    [InlineData(".env.development", ".env*", true)]
    [InlineData(".env.production", ".env*", true)]
    public void EnvFilesShouldBeMatched(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

}
