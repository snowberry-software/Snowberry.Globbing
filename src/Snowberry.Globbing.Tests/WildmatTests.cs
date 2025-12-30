namespace Snowberry.Globbing.Tests;

/// <summary>
/// Tests for wildmat patterns.
/// Ported from: https://github.com/micromatch/picomatch/blob/master/test/wildmat.js
/// </summary>
public class WildmatTests
{
    // POSIX option needed for [!...] bracket negation
    private static readonly GlobbingOptions PosixOptions = new() { Posix = true };

    [Theory]
    [InlineData("foo", "foo", true)]
    [InlineData("foo", "bar", false)]
    [InlineData("", "", true)]
    [InlineData("foo/bar/baz/to", "**/t[o]", true)]
    public void Wildmat_BasicPatterns(string input, string pattern, bool expected)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            Assert.Throws<ArgumentException>(() => GlobMatcher.IsMatch(input, pattern));
            return;
        }

        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("foo", "*", true)]
    [InlineData("foo", "f*", true)]
    [InlineData("foo", "*o", true)]
    [InlineData("foo", "*oo", true)]
    [InlineData("foo", "fo*", true)]
    [InlineData("foo", "f*o", true)]
    [InlineData("foo", "f**", true)]
    [InlineData("foo", "**o", true)]
    [InlineData("foo", "**oo", true)]
    [InlineData("foo", "fo**", true)]
    [InlineData("foo", "f**o", true)]
    public void Wildmat_Stars(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("foo", "?oo", true)]
    [InlineData("foo", "f?o", true)]
    [InlineData("foo", "fo?", true)]
    [InlineData("foo", "???", true)]
    [InlineData("foo", "????", false)]
    [InlineData("foo", "??", false)]
    public void Wildmat_QuestionMarks(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("foo", "[f]oo", true)]
    [InlineData("foo", "f[o]o", true)]
    [InlineData("foo", "fo[o]", true)]
    [InlineData("foo", "[^b]oo", true)]
    [InlineData("foo", "f[^b]o", true)]
    [InlineData("foo", "fo[^b]", true)]
    [InlineData("moo", "[^f]oo", true)]
    [InlineData("foo", "[^f]oo", false)]
    public void Wildmat_Brackets(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, PosixOptions));
    }

    [Theory]
    [InlineData("foobar", "[^f]*", false)]
    [InlineData("moobar", "[^f]*", true)]
    public void Wildmat_BracketsWithStar(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, PosixOptions));
    }

    [Theory]
    [InlineData("a/b", "a/b", true)]
    [InlineData("a/b", "a/*", true)]
    [InlineData("a/b", "*/b", true)]
    [InlineData("a/b", "*/*", true)]
    [InlineData("a/b/c", "*/*/*", true)]
    [InlineData("a/b/c", "**", true)]
    [InlineData("a/b/c", "a/**", true)]
    [InlineData("a/b/c", "**/c", true)]
    [InlineData("a/b/c", "a/**/c", true)]
    public void Wildmat_PathPatterns(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("a", "[a-z]", true)]
    [InlineData("z", "[a-z]", true)]
    [InlineData("A", "[a-z]", false)]
    [InlineData("m", "[a-z]", true)]
    [InlineData("5", "[0-9]", true)]
    [InlineData("a", "[0-9]", false)]
    public void Wildmat_CharacterRanges(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("foo", "[abc]oo", false)]
    [InlineData("aoo", "[abc]oo", true)]
    [InlineData("boo", "[abc]oo", true)]
    [InlineData("coo", "[abc]oo", true)]
    public void Wildmat_CharacterClasses(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData("foo", "!foo", false)]
    [InlineData("bar", "!foo", true)]
    public void Wildmat_Negation(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData(".hidden", ".*", true)]
    [InlineData(".hidden", "*", false)]
    public void Wildmat_DotFiles_DefaultBehavior(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    [Theory]
    [InlineData(".hidden", "*", true)]
    public void Wildmat_DotFiles_WithDotOption(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { Dot = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    // Non-matching cases
    [InlineData("-adobe-courier-bold-o-normal--12-120-75-75-/-70-iso8859-1", "-*-*-*-*-*-*-12-*-*-*-m-*-*-*", false)]
    [InlineData("-adobe-courier-bold-o-normal--12-120-75-75-X-70-iso8859-1", "-*-*-*-*-*-*-12-*-*-*-m-*-*-*", false)]
    [InlineData("ab/cXd/efXg/hi", "*X*i", false)]
    [InlineData("ab/cXd/efXg/hi", "*Xg*i", false)]
    [InlineData("abcd/abcdefg/abcdefghijk/abcdefghijklmnop.txtz", "**/*a*b*g*n*t", false)]
    [InlineData("foo", "*/*/*", false)]
    [InlineData("foo", "fo", false)]
    [InlineData("foo/bar", "*/*/*", false)]
    [InlineData("foo/bar", "foo?bar", false)]
    [InlineData("foo/bb/aa/rr", "*/*/*", false)]
    [InlineData("foo/bba/arr", "foo*", false)]
    [InlineData("foo/bba/arr", "foo**", false)]
    [InlineData("foo/bba/arr", "foo/*", false)]
    [InlineData("foo/bba/arr", "foo/**arr", false)]
    [InlineData("foo/bba/arr", "foo/**z", false)]
    [InlineData("foo/bba/arr", "foo/*arr", false)]
    [InlineData("foo/bba/arr", "foo/*z", false)]
    [InlineData("XXX/adobe/courier/bold/o/normal//12/120/75/75/X/70/iso8859/1", "XXX/*/*/*/*/*/*/12/*/*/*/m/*/*/*", false)]
    // Matching cases
    [InlineData("-adobe-courier-bold-o-normal--12-120-75-75-m-70-iso8859-1", "-*-*-*-*-*-*-12-*-*-*-m-*-*-*", true)]
    [InlineData("ab/cXd/efXg/hi", "**/*X*/**/*i", true)]
    [InlineData("ab/cXd/efXg/hi", "*/*X*/*/*i", true)]
    [InlineData("abcd/abcdefg/abcdefghijk/abcdefghijklmnop.txt", "**/*a*b*g*n*t", true)]
    [InlineData("abcXdefXghi", "*X*i", true)]
    [InlineData("foo", "foo", true)]
    [InlineData("foo/bar", "foo/*", true)]
    [InlineData("foo/bar", "foo/bar", true)]
    [InlineData("foo/bar", "foo[/]bar", true)]
    [InlineData("foo/bb/aa/rr", "**/**/**", true)]
    [InlineData("foo/bba/arr", "*/*/*", true)]
    [InlineData("foo/bba/arr", "foo/**", true)]
    public void Wildmat_Recursion(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }
}
