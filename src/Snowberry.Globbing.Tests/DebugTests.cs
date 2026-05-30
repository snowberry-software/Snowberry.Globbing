namespace Snowberry.Globbing.Tests;

/// <summary>
/// Tests documenting specific pattern behaviors, compared against JS picomatch.
/// </summary>
public class DebugTests
{
    [Theory]
    // a@([.])b matches a literal dot via extglob+bracket
    [InlineData("a.b", "a@([.])b", true)]
    [InlineData("a,b", "a@([.])b", false)]
    // [a]+ matches one or more 'a' characters
    [InlineData("a", "[a]+", true)]
    [InlineData("aa", "[a]+", true)]
    [InlineData("aaa", "[a]+", true)]
    // !(foo/bar) - negation extglob with slash inside
    [InlineData("foo", "!(foo/bar)", true)]
    [InlineData("foo/bar", "!(foo/bar)", false)]
    [InlineData("foo/baz", "!(foo/bar)", true)]
    // (a+|b)* - parentheses without extglob prefix behave regex-like
    [InlineData("ab", "(a+|b)*", true)]
    [InlineData("abab", "(a+|b)*", true)]
    [InlineData("abcdef", "(a+|b)*", true)]
    [InlineData("123abc", "(a+|b)*", false)]
    // \* matches a literal star
    [InlineData("*", "\\*", true)]
    [InlineData("\\*", "\\*", true)]
    [InlineData("**", "\\*", false)]
    [InlineData("a", "\\*", false)]
    // A single double-quote matches itself
    [InlineData("\"", "\"", true)]
    // foo[/]bar matches foo/bar
    [InlineData("foo/bar", "foo[/]bar", true)]
    [InlineData("foobar", "foo[/]bar", false)]
    // a/**/ requires a trailing slash
    [InlineData("a/b/c/d/", "a/**/", true)]
    [InlineData("a/", "a/**/", true)]
    [InlineData("a/b/c/d", "a/**/", false)]
    // !(+) / !(?) - the +/? inside is a literal, not a quantifier
    [InlineData("cbz", "c!(+)z", true)]
    [InlineData("cbz", "c!(?)z", true)]
    // /!(*.d).{ts,tsx} - negation excludes files ending in .d before .ts/.tsx
    [InlineData("/file.d.ts", "/!(*.d).{ts,tsx}", false)]
    [InlineData("/file.ts", "/!(*.d).{ts,tsx}", true)]
    [InlineData("/file.d.ts", "/!(*.d).@(ts)", false)]
    [InlineData("/file.ts", "/!(*.d).@(ts)", true)]
    // [!a] - bracket negation uses ! as caret; literal [!a] also matches itself
    [InlineData("[!a]", "[!a]", true)]
    [InlineData("a", "[!a]", true)]
    [InlineData("b", "[!a]", false)]
    [InlineData("b", "[^a]", true)]
    [InlineData("a", "[^a]", false)]
    public void Matches_DefaultOptions(string input, string pattern, bool expected)
        => Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));

    [Theory]
    // +([[:alpha:].]) matches one or more letters or dots
    [InlineData("a.c", "+([[:alpha:].])", true)]
    // [[:alpha:]]+ - the + after a POSIX class works as a quantifier
    [InlineData("abc", "[[:alpha:]]+", true)]
    [InlineData("a", "[[:alpha:]]+", true)]
    public void Matches_PosixOptions(string input, string pattern, bool expected)
        => Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, new GlobbingOptions { Posix = true }));

    [Theory]
    // With noextglob, @(foo) is literal text, not an extglob
    [InlineData("@(foo)", "@(foo)", true)]
    [InlineData("foo", "@(foo)", false)]
    public void Matches_NoExtglobOptions(string input, string pattern, bool expected)
        => Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, new GlobbingOptions { NoExtglob = true }));

    [Theory]
    // With Windows=false, backslash is the escape character; \\ matches a literal backslash
    [InlineData("\\", "\\\\", true)]
    [InlineData("a\\b", "a\\\\b", true)]
    public void Matches_PosixSlashes(string input, string pattern, bool expected)
        => Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, new GlobbingOptions { Windows = false }));
}
