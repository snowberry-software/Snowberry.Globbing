namespace Snowberry.Globbing.Tests;

/// <summary>
/// Tests for brace expansion patterns ported from picomatch.
/// Note: Picomatch does not expand braces by default, but supports them with options.
/// </summary>
public class BracesTests
{

    [Theory]
    [InlineData("a", "{a,b,c}", true)]
    [InlineData("b", "{a,b,c}", true)]
    [InlineData("c", "{a,b,c}", true)]
    [InlineData("d", "{a,b,c}", false)]
    [InlineData("ab", "{a,b,c}", false)]
    public void ShouldMatchBasicBraceExpansion(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("aa", "a{b,c}", false)]
    [InlineData("ab", "a{b,c}", true)]
    [InlineData("ac", "a{b,c}", true)]
    [InlineData("ad", "a{b,c}", false)]
    public void ShouldMatchBraceWithPrefix(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("ax", "{a,b,c}x", true)]
    [InlineData("bx", "{a,b,c}x", true)]
    [InlineData("cx", "{a,b,c}x", true)]
    [InlineData("dx", "{a,b,c}x", false)]
    public void ShouldMatchBraceWithSuffix(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("abc", "a{b,c}c", true)]
    [InlineData("acc", "a{b,c}c", true)]
    [InlineData("adc", "a{b,c}c", false)]
    public void ShouldMatchBraceWithPrefixAndSuffix(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a", "{,a}", true)]
    [InlineData("", "{,a}", false)]
    [InlineData("b", "{,a}", false)]
    public void ShouldMatchBraceWithEmptyAlternative(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a", "{a,}", true)]
    [InlineData("", "{a,}", false)]
    [InlineData("b", "{a,}", false)]
    public void ShouldMatchBraceWithTrailingEmptyAlternative(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a", "{{a,b},{c,d}}", true)]
    [InlineData("b", "{{a,b},{c,d}}", true)]
    [InlineData("c", "{{a,b},{c,d}}", true)]
    [InlineData("d", "{{a,b},{c,d}}", true)]
    [InlineData("e", "{{a,b},{c,d}}", false)]
    public void ShouldMatchNestedBraces(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("ac", "a{b{c,d},e}", false)]
    [InlineData("abc", "a{b{c,d},e}", true)]
    [InlineData("abd", "a{b{c,d},e}", true)]
    [InlineData("ae", "a{b{c,d},e}", true)]
    [InlineData("af", "a{b{c,d},e}", false)]
    public void ShouldMatchDeeplyNestedBraces(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a.txt", "{*.txt,*.js}", true)]
    [InlineData("b.txt", "{*.txt,*.js}", true)]
    [InlineData("a.js", "{*.txt,*.js}", true)]
    [InlineData("b.js", "{*.txt,*.js}", true)]
    [InlineData("a.md", "{*.txt,*.js}", false)]
    public void ShouldMatchBraceWithStarPattern(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a/b.txt", "{a,b}/*.txt", true)]
    [InlineData("b/c.txt", "{a,b}/*.txt", true)]
    [InlineData("c/d.txt", "{a,b}/*.txt", false)]
    public void ShouldMatchBraceWithPathAndStar(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("foo/bar", "{foo,bar}/**", true)]
    [InlineData("foo/bar/baz", "{foo,bar}/**", true)]
    [InlineData("bar/baz", "{foo,bar}/**", true)]
    [InlineData("baz/bar", "{foo,bar}/**", false)]
    public void ShouldMatchBraceWithGlobstar(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("ab", "{a?,b?}", true)]
    [InlineData("ax", "{a?,b?}", true)]
    [InlineData("ba", "{a?,b?}", true)]
    [InlineData("bx", "{a?,b?}", true)]
    [InlineData("a", "{a?,b?}", false)]
    [InlineData("abc", "{a?,b?}", false)]
    public void ShouldMatchBraceWithQuestionMark(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("file.js", "*.{js,ts}", true)]
    [InlineData("file.ts", "*.{js,ts}", true)]
    [InlineData("file.jsx", "*.{js,ts}", false)]
    [InlineData("file.txt", "*.{js,ts}", false)]
    public void ShouldMatchFileExtensionWithBrace(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("src/file.js", "src/*.{js,ts,jsx,tsx}", true)]
    [InlineData("src/file.ts", "src/*.{js,ts,jsx,tsx}", true)]
    [InlineData("src/file.jsx", "src/*.{js,ts,jsx,tsx}", true)]
    [InlineData("src/file.tsx", "src/*.{js,ts,jsx,tsx}", true)]
    [InlineData("src/file.css", "src/*.{js,ts,jsx,tsx}", false)]
    public void ShouldMatchMultipleExtensionsWithBrace(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("src/file.js", "**/*.{js,ts}", true)]
    [InlineData("src/sub/file.ts", "**/*.{js,ts}", true)]
    [InlineData("file.js", "**/*.{js,ts}", true)]
    [InlineData("src/file.css", "**/*.{js,ts}", false)]
    public void ShouldMatchExtensionWithGlobstarAndBrace(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a/b", "{a,b}/{b,c}", true)]
    [InlineData("a/c", "{a,b}/{b,c}", true)]
    [InlineData("b/b", "{a,b}/{b,c}", true)]
    [InlineData("b/c", "{a,b}/{b,c}", true)]
    [InlineData("a/d", "{a,b}/{b,c}", false)]
    [InlineData("c/b", "{a,b}/{b,c}", false)]
    public void ShouldMatchBracesInMultiplePathSegments(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("src/components", "{src,lib}/{components,utils}", true)]
    [InlineData("src/utils", "{src,lib}/{components,utils}", true)]
    [InlineData("lib/components", "{src,lib}/{components,utils}", true)]
    [InlineData("lib/utils", "{src,lib}/{components,utils}", true)]
    [InlineData("src/models", "{src,lib}/{components,utils}", false)]
    public void ShouldMatchRealisticPathBraces(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    // Note: JS picomatch does NOT expand single-item braces {a} - they are treated literally
    [Theory]
    [InlineData("a", "{a}", false)]  // JS: {a} is treated literally, not expanded
    [InlineData("b", "{a}", false)]
    [InlineData("{a}", "{a}", true)] // Literal match
    public void ShouldMatchSingleItemBrace(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("{a,b}", "\\{a,b\\}", true)]
    [InlineData("a", "\\{a,b\\}", false)]
    [InlineData("b", "\\{a,b\\}", false)]
    public void ShouldMatchEscapedBracesLiterally(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("{", "\\{", true)]
    [InlineData("}", "\\}", true)]
    [InlineData("a", "\\{", false)]
    public void ShouldMatchSingleEscapedBrace(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("{a,b}", "{a,b}", true)]
    [InlineData("a", "{a,b}", false)]
    [InlineData("b", "{a,b}", false)]
    public void WithNobraceOptionBracesShouldMatchLiterally(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { NoBrace = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("{a}", "{a}", true)]
    [InlineData("a", "{a}", false)]
    public void WithNobraceOptionSingleItemBraceShouldMatchLiterally(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { NoBrace = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("abc", "a{b,c}c", true)]
    [InlineData("acc", "a{b,c}c", true)]
    [InlineData("adc", "a{b,c}c", false)]
    [InlineData("abcc", "a{b,c}c", false)]
    public void ShouldMatchComplexBracePattern(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("foobar", "{foo,bar}{bar,baz}", true)]
    [InlineData("foobaz", "{foo,bar}{bar,baz}", true)]
    [InlineData("barbar", "{foo,bar}{bar,baz}", true)]
    [InlineData("barbaz", "{foo,bar}{bar,baz}", true)]
    [InlineData("foofoo", "{foo,bar}{bar,baz}", false)]
    [InlineData("bazbar", "{foo,bar}{bar,baz}", false)]
    public void ShouldMatchConcatenatedBraces(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a.json", "{**/*.json,**/*.js}", true)]
    [InlineData("a.js", "{**/*.json,**/*.js}", true)]
    [InlineData("a/b.json", "{**/*.json,**/*.js}", true)]
    [InlineData("a/b.js", "{**/*.json,**/*.js}", true)]
    [InlineData("foo.md", "{**/foo.md,bar.md}", true)]
    [InlineData("a/foo.md", "{**/foo.md,bar.md}", true)]
    [InlineData("foo.md", "{bar.md,**/foo.md}", true)]
    [InlineData("a/foo.md", "{bar.md,**/foo.md}", true)]
    public void ShouldSupportBracesWithGlobstarsAtStartOfPattern(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

}
