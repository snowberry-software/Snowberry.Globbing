namespace Snowberry.Globbing.Tests;

/// <summary>
/// Extglob tests for minimatch compatibility.
/// Similar to bash extglob tests but without the bash option.
/// Ported from: https://github.com/micromatch/picomatch/blob/master/test/extglobs-minimatch.js
/// </summary>
public class ExtglobsMinimatchTests
{
    private readonly GlobOptions _opts = new() { PathStyle = GlobPathStyle.Windows };

    [Theory]
    [InlineData("*(a|b[)", "*(a|b\\[)", false)]
    [InlineData("a", "*(a|b\\[)", true)]
    [InlineData("b[", "*(a|b\\[)", true)]
    public void EscapedExtglobSyntax(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern, _opts).Should().Be(expected);
    }

    [Theory]
    [InlineData("foo/bar", "!(foo)", false)] // negation extglob does not match across a path separator
    [InlineData("bar", "!(foo)", true)]
    public void ExclusionNotFoo(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern, _opts).Should().Be(expected);
    }

    [Theory]
    [InlineData("\\a\\b\\c", "abc", false)]
    [InlineData("\\a\\b\\c", "/a/b/c", true)]
    public void BackslashNotMatching(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern, _opts).Should().Be(expected);
    }

    [Theory]
    [InlineData("a.a", "!(*.[a-b]*)", false)]
    [InlineData("a.a", "!(*[a-b].[a-b]*)", false)]
    [InlineData("c.c", "!(*.[a-b]*)", true)]
    public void CharClassNegation(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern, _opts).Should().Be(expected);
    }

    [Theory]
    [InlineData("a.a", "!*.(a|b)", false)]
    [InlineData("a.a", "!*.(a|b)*", false)]
    [InlineData("a.c", "!*.(a|b)", true)]
    public void NotStarDotAOrB(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern, _opts).Should().Be(expected);
    }

    [Theory]
    [InlineData("a.a", "(a|d).(a|b)*", true)]
    [InlineData("c.a", "(a|d).(a|b)*", false)]
    public void AlternationDotExtension(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern, _opts).Should().Be(expected);
    }

    [Theory]
    [InlineData("a.", "*.+(b|d)", false)]
    [InlineData("a.b", "*.+(b|d)", true)]
    [InlineData("a.d", "*.+(b|d)", true)]
    public void StarDotPlusBOrD(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern, _opts).Should().Be(expected);
    }

    [Theory]
    [InlineData("bar", false)]
    [InlineData("foo", true)]
    [InlineData("foobar", false)]
    public void NegationExtglob_DoubleNegation(string input, bool expected)
    {
        Glob.IsMatch(input, "!(!(foo))", _opts).Should().Be(expected);
    }

    [Theory]
    [InlineData("bar", true)]
    [InlineData("foo", false)]
    public void NegationExtglob_TripleNegation(string input, bool expected)
    {
        Glob.IsMatch(input, "!(!(!(foo)))", _opts).Should().Be(expected);
    }

    [Theory]
    [InlineData("bar", true)]
    [InlineData("f", true)]
    [InlineData("foo", false)]
    [InlineData("foobar", false)]
    [InlineData("x", true)]
    public void NegationWithStar_ExcludeFooPrefix(string input, bool expected)
    {
        Glob.IsMatch(input, "!(foo)*", _opts).Should().Be(expected);
    }

    [Theory]
    [InlineData("bar", false)]
    [InlineData("f", false)]
    [InlineData("foo", true)]
    [InlineData("foobar", true)]
    public void NegationWithStar_DoubleNegationFoo(string input, bool expected)
    {
        Glob.IsMatch(input, "!(!(foo))*", _opts).Should().Be(expected);
    }

    [Theory]
    [InlineData("bar", true)]
    [InlineData("f", false)]
    [InlineData("fa", false)]
    [InlineData("fo", true)]
    [InlineData("foo", false)]
    [InlineData("foobar", false)]
    public void ComplexNegation_ExcludeFNotO(string input, bool expected)
    {
        Glob.IsMatch(input, "!(f!(o))", _opts).Should().Be(expected);
    }

    [Theory]
    [InlineData("bar", true)]
    [InlineData("f", true)]
    [InlineData("fo", false)]
    [InlineData("foo", true)]
    [InlineData("foobar", true)]
    public void NegationGroup_ExcludeFO(string input, bool expected)
    {
        Glob.IsMatch(input, "!(f(o))", _opts).Should().Be(expected);
    }

    [Theory]
    [InlineData("bar", false)]
    [InlineData("foo", true)]
    [InlineData("foofoo", true)]
    [InlineData("foobar", false)]
    public void StarExtglob_DoubleParenFoo(string input, bool expected)
    {
        Glob.IsMatch(input, "*((foo))", _opts).Should().Be(expected);
    }

    [Theory]
    [InlineData("foo/bar", true)]
    [InlineData("z/a", false)]
    public void AtExtglob_NotZOrXWithPath(string input, bool expected)
    {
        Glob.IsMatch(input, "@(!(z*/*)|*x)", _opts).Should().Be(expected);
    }

    [Theory]
    [InlineData("/dev/udp/129.22.8.102/45", true)]
    [InlineData("/dev/foo/1/2", false)]
    public void DevPath_TcpOrUdp_UnescapedSlashes(string input, bool expected)
    {
        Glob.IsMatch(input, "/dev/@(tcp|udp)/*/*", _opts).Should().Be(expected);
    }

    [Theory]
    [InlineData("12", true)]
    [InlineData("1", false)]
    [InlineData("12abc", false)]
    [InlineData("555", false)]
    public void NumberRange_1To6FollowedByDigit(string input, bool expected)
    {
        Glob.IsMatch(input, "[1-6]([0-9])", _opts).Should().Be(expected);
    }

    [Theory]
    [InlineData("12", true)]
    [InlineData("555", true)]
    [InlineData("0", false)]
    [InlineData("12abc", false)]
    public void NumberRange_1To6StarDigits(string input, bool expected)
    {
        Glob.IsMatch(input, "[1-6]*([0-9])", _opts).Should().Be(expected);
    }

    [Theory]
    [InlineData("a", true)]
    [InlineData("abc", true)]
    [InlineData("abcd", false)]
    public void PlusAOrAbc(string input, bool expected)
    {
        Glob.IsMatch(input, "+(a|abc)", _opts).Should().Be(expected);
    }

    [Theory]
    [InlineData("f", true)]
    [InlineData("def", true)]
    [InlineData("cdef", false)]
    public void PlusFOrDef(string input, bool expected)
    {
        Glob.IsMatch(input, "+(f|def)", _opts).Should().Be(expected);
    }

    [Theory]
    [InlineData("abcd", true)]
    [InlineData("a", false)]
    [InlineData("ab", false)]
    [InlineData("abc", false)]
    public void StarAOrBFollowedByCd(string input, bool expected)
    {
        Glob.IsMatch(input, "*(a|b)cd", _opts).Should().Be(expected);
    }

    [Theory]
    [InlineData("a.c", true)]
    [InlineData("a-c", false)]
    public void PosixWithExtglob_PlusAlphaDot(string input, bool expected)
    {
        var opts = new GlobOptions { PosixClasses = true };
        Glob.IsMatch(input, "+([[:alpha:].])", opts).Should().Be(expected);
    }

    [Theory]
    [InlineData("a.c", true)]
    [InlineData("a-c", false)]
    public void PosixWithExtglob_StarAlphaDot(string input, bool expected)
    {
        var opts = new GlobOptions { PosixClasses = true };
        Glob.IsMatch(input, "*([[:alpha:].])", opts).Should().Be(expected);
    }

    [Theory]
    [InlineData("a.b", true)]
    [InlineData("a b", true)]
    [InlineData("a_b", true)]
    [InlineData("acb", false)]
    public void PosixWithExtglob_AtNotAlnum(string input, bool expected)
    {
        var opts = new GlobOptions { PosixClasses = true };
        Glob.IsMatch(input, "a@([^[:alnum:]])b", opts).Should().Be(expected);
    }

    [Theory]
    [InlineData("a.b", true)]
    [InlineData("a b", true)]
    [InlineData("a_b", true)]
    [InlineData("acb", false)]
    public void ExtglobWithCharClass_AtPunctuation(string input, bool expected)
    {
        Glob.IsMatch(input, "a@([-.,:; _])b", _opts).Should().Be(expected);
    }

    [Theory]
    [InlineData("a.b", true)]
    [InlineData("a,b", false)]
    [InlineData("a-b", false)]
    public void ExtglobWithCharClass_AtDot(string input, bool expected)
    {
        Glob.IsMatch(input, "a@([.])b", _opts).Should().Be(expected);
    }

    [Theory]
    [InlineData("a.b", false)]
    [InlineData("a,b", true)]
    [InlineData("a-b", true)]
    public void ExtglobWithCharClass_AtNotDot(string input, bool expected)
    {
        Glob.IsMatch(input, "a@([^.])b", _opts).Should().Be(expected);
    }

    [Theory]
    [InlineData("aac", false)]
    [InlineData("aabc", true)]
    public void StarAtABAtC(string input, bool expected)
    {
        Glob.IsMatch(input, "*(@(a))b@(c)", _opts).Should().Be(expected);
    }

}