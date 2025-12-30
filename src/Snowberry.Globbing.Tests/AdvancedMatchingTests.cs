namespace Snowberry.Globbing.Tests;

public class AdvancedMatchingTests
{
    [Theory]
    [InlineData("foo/bar/baz.js", "**/*.js", true)]
    [InlineData("foo/bar.js", "**/*.js", true)]
    [InlineData("a.js", "**/*.js", true)]
    [InlineData("a.md", "**/*.js", false)]
    public void TestGlobstarPatterns(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a.a", "*.!(*a)", false)]
    [InlineData("a.b", "*.!(*a)", true)]
    [InlineData("foo.bar", "!(foo).!(bar)", false)]
    public void TestNegatedExtglobs(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a.a", "!(abc)", true)]
    [InlineData("abc", "!(abc)", false)]
    public void TestNegatedExtglobs_State(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a", "a*(z)", true)]
    [InlineData("az", "a*(z)", true)]
    [InlineData("azzz", "a*(z)", true)]
    [InlineData("a", "a+(z)", false)]
    [InlineData("az", "a+(z)", true)]
    [InlineData("azzz", "a+(z)", true)]
    public void TestExtglobPatterns(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("foo/bar", "*/*", true)]
    [InlineData("foo/bar/baz", "*/*/*", true)]
    [InlineData("foo", "*/*", false)]
    public void TestSlashPatterns(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a.js", "*.js", true)]
    [InlineData("test.js", "*.js", true)]
    [InlineData("a.md", "*.js", false)]
    public void TestWildcardPatterns(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Fact]
    public void TestBraceExpansion()
    {
        var matcher = GlobMatcher.Create("a.{js,md}");

        Assert.True(matcher("a.js"));
        Assert.True(matcher("a.md"));
        Assert.False(matcher("a.txt"));
    }

    [Fact]
    public void TestComplexPattern()
    {
        var matcher = GlobMatcher.Create("**/*.{js,ts}");

        Assert.True(matcher("foo.js"));
        Assert.True(matcher("foo.ts"));
        Assert.True(matcher("bar/foo.js"));
        Assert.True(matcher("bar/baz/foo.ts"));
        Assert.False(matcher("foo.md"));
    }

    [Fact]
    public void TestWindowsPathsOption()
    {
        // Windows path support - normalize backslashes
        var matcher = GlobMatcher.Create("foo/bar.js", new GlobbingOptions { Windows = true });

        Assert.True(matcher("foo/bar.js"));
        // Note: Backslash normalization would require format function
    }

    [Fact]
    public void TestPosixPathsOption()
    {
        var matcher = GlobMatcher.Create("foo/bar.js", new GlobbingOptions { Windows = false });

        Assert.True(matcher("foo/bar.js"));
    }

    [Fact]
    public void TestMultipleStars()
    {
        var matcher = GlobMatcher.Create("foo/*/bar/*.js");

        Assert.True(matcher("foo/a/bar/test.js"));
        Assert.True(matcher("foo/baz/bar/app.js"));
        Assert.False(matcher("foo/bar/test.js"));
        Assert.False(matcher("foo/a/b/bar/test.js"));
    }

    [Fact]
    public void TestBashOption()
    {
        var matcher = GlobMatcher.Create("*", new GlobbingOptions { Bash = true });

        Assert.True(matcher("foo"));
        Assert.True(matcher("bar"));
    }

    [Fact]
    public void TestNoglobstarOption()
    {
        var matcher = GlobMatcher.Create("**", new GlobbingOptions { NoGlobstar = true });

        // With noglobstar, ** should behave like *
        Assert.True(matcher("foo"));
        Assert.False(matcher("foo/bar"));
    }

    [Fact]
    public void TestNoextglobOption()
    {
        var matcher = GlobMatcher.Create("test.js", new GlobbingOptions { NoExtglob = true });

        // With NoExtglob disabled, patterns are treated literally
        Assert.True(matcher("test.js"));
        Assert.False(matcher("app.js"));
    }

    [Fact]
    public void TestContainsOption()
    {
        var matcher = GlobMatcher.Create("bar", new GlobbingOptions { Contains = true });

        Assert.True(matcher("foobar"));
        Assert.True(matcher("barbaz"));
        Assert.True(matcher("foobarbaz"));
        Assert.False(matcher("foo"));
    }

    [Fact]
    public void TestOnMatchCallback()
    {
        int matchCount = 0;
        var options = new GlobbingOptions
        {
            OnMatch = (result) => { matchCount++; }
        };

        var matcher = GlobMatcher.Create("*.js", options);
        matcher("test.js");
        matcher("app.js");
        matcher("test.md"); // This won't match

        Assert.Equal(2, matchCount);
    }

    [Fact]
    public void TestOnResultCallback()
    {
        int resultCount = 0;
        var options = new GlobbingOptions
        {
            OnResult = (result) => { resultCount++; }
        };

        var matcher = GlobMatcher.Create("*.js", options);
        matcher("test.js");
        matcher("app.js");
        matcher("test.md");

        Assert.Equal(3, resultCount);
    }

    [Theory]
    [InlineData("app.ts", true)]
    [InlineData("index.ts", true)]
    [InlineData("src/app.ts", true)]
    [InlineData("src/utils/helper.ts", true)]
    [InlineData("deep/nested/path/file.ts", true)]
    [InlineData("app.d.ts", false)]
    [InlineData("index.d.ts", false)]
    [InlineData("src/types.d.ts", false)]
    [InlineData("src/utils/helper.d.ts", false)]
    [InlineData("deep/nested/path/types.d.ts", false)]
    [InlineData("app.js", false)]
    [InlineData("src/app.js", false)]
    [InlineData("readme.md", false)]
    public void TestGlobstarWithNegatedExtglob_ExcludesDeclarationFiles(string input, bool expected)
    {
        // Pattern "**/!(*.d).ts" matches .ts files but excludes .d.ts declaration files
        Assert.Equal(expected, GlobMatcher.IsMatch(input, "**/!(*.d).ts"));
    }

    [Fact]
    public void TestGlobstarWithNegatedExtglob_ExcludesDeclarationFiles_WithMatcher()
    {
        // Pattern "**/!(*.d).ts" matches .ts files but excludes .d.ts declaration files
        var matcher = GlobMatcher.Create("**/!(*.d).ts");

        // Should match regular TypeScript files
        Assert.True(matcher("app.ts"));
        Assert.True(matcher("index.ts"));
        Assert.True(matcher("src/app.ts"));
        Assert.True(matcher("src/utils/helper.ts"));
        Assert.True(matcher("deep/nested/path/file.ts"));

        // Should NOT match TypeScript declaration files (.d.ts)
        Assert.False(matcher("app.d.ts"));
        Assert.False(matcher("index.d.ts"));
        Assert.False(matcher("src/types.d.ts"));
        Assert.False(matcher("src/utils/helper.d.ts"));

        // Should NOT match non-.ts files
        Assert.False(matcher("app.js"));
        Assert.False(matcher("readme.md"));
    }
}
