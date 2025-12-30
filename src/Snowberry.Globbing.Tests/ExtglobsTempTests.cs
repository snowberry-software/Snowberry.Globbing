namespace Snowberry.Globbing.Tests;

/// <summary>
/// Additional extglob tests ported from bash spec and minimatch.
/// These are temporary tests collected from various sources.
/// Ported from: https://github.com/micromatch/picomatch/blob/master/test/extglobs-temp.js
/// </summary>
public class ExtglobsTempTests
{
    private readonly GlobbingOptions _windowsOpts = new() { Windows = true };

    [Theory]
    [InlineData("bar", true)]
    [InlineData("f", true)]
    [InlineData("fa", true)]
    [InlineData("fb", true)]
    [InlineData("ff", true)]
    [InlineData("fff", true)]
    [InlineData("fo", true)]
    [InlineData("foo", false)]
    [InlineData("foo/bar", false)]
    [InlineData("foobar", true)]
    [InlineData("foot", true)]
    [InlineData("foox", true)]
    [InlineData("o", true)]
    [InlineData("of", true)]
    [InlineData("ooo", true)]
    [InlineData("ox", true)]
    [InlineData("x", true)]
    [InlineData("xx", true)]
    public void NegationExtglob_ExcludeFoo(string input, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, "!(foo)", _windowsOpts));
    }

    [Theory]
    [InlineData("bar", false)]
    [InlineData("f", false)]
    [InlineData("fa", false)]
    [InlineData("foo", true)]
    [InlineData("foobar", false)]
    public void NegationExtglob_DoubleNegation(string input, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, "!(!(foo))", _windowsOpts));
    }

    [Theory]
    [InlineData("bar", true)]
    [InlineData("f", true)]
    [InlineData("foo", false)]
    [InlineData("foobar", true)]
    public void NegationExtglob_TripleNegation(string input, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, "!(!(!(foo)))", _windowsOpts));
    }

    [Theory]
    [InlineData("bar", false)]
    [InlineData("f", false)]
    [InlineData("foo", true)]
    [InlineData("foobar", false)]
    public void NegationExtglob_QuadNegation(string input, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, "!(!(!(!(foo))))", _windowsOpts));
    }

    [Theory]
    [InlineData("bar", true)]
    [InlineData("f", true)]
    [InlineData("foo", false)]
    [InlineData("foobar", false)]
    [InlineData("foot", false)]
    [InlineData("foox", false)]
    [InlineData("o", true)]
    [InlineData("x", true)]
    public void NegationWithStar_ExcludeFooPrefix(string input, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, "!(foo)*", _windowsOpts));
    }

    [Theory]
    [InlineData("bar", false)]
    [InlineData("f", false)]
    [InlineData("foo", true)]
    [InlineData("foobar", true)]
    [InlineData("foot", true)]
    [InlineData("foox", true)]
    [InlineData("o", false)]
    public void NegationWithStar_DoubleNegationFoo(string input, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, "!(!(foo))*", _windowsOpts));
    }

    [Theory]
    [InlineData("bar", true)]
    [InlineData("f", false)]
    [InlineData("fa", false)]
    [InlineData("fb", false)]
    [InlineData("ff", false)]
    [InlineData("fff", false)]
    [InlineData("fo", true)]
    [InlineData("foo", false)]
    [InlineData("foobar", false)]
    [InlineData("o", true)]
    [InlineData("x", true)]
    public void ComplexNegation_ExcludeFNotO(string input, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, "!(f!(o))", _windowsOpts));
    }

    [Theory]
    [InlineData("bar", true)]
    [InlineData("f", true)]
    [InlineData("fo", false)]
    [InlineData("foo", true)]
    [InlineData("foobar", true)]
    public void NegationGroup_ExcludeFO(string input, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, "!(f(o))", _windowsOpts));
    }

    [Theory]
    [InlineData("bar", true)]
    [InlineData("f", false)]
    [InlineData("fa", true)]
    [InlineData("fb", true)]
    [InlineData("ff", true)]
    [InlineData("foo", true)]
    [InlineData("foobar", true)]
    [InlineData("o", true)]
    [InlineData("ooo", true)]
    public void StarExtglob_NotF(string input, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, "*(!(f))", _windowsOpts));
    }

    [Theory]
    [InlineData("bar", false)]
    [InlineData("foo", true)]
    [InlineData("foofoo", true)]
    [InlineData("foobar", false)]
    public void StarExtglob_Foo(string input, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, "*((foo))", _windowsOpts));
    }

    [Theory]
    [InlineData("bar", true)]
    [InlineData("f", false)]
    [InlineData("foo", true)]
    [InlineData("foobar", true)]
    [InlineData("o", true)]
    public void PlusExtglob_NotF(string input, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, "+(!(f))", _windowsOpts));
    }

    [Theory]
    [InlineData("bar", true)]
    [InlineData("foo", true)]
    [InlineData("foox", true)]
    [InlineData("foot", true)]
    [InlineData("x", true)]
    [InlineData("xx", true)]
    [InlineData("zoot", false)]
    public void AtExtglob_NotZOrX(string input, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, "@(!(z*)|*x)", _windowsOpts));
    }

    [Theory]
    [InlineData("foo/bar", true)]
    public void AtExtglob_NotZOrXWithPath(string input, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, "@(!(z*/*)|*x)", _windowsOpts));
    }

    [Theory]
    [InlineData("ffffffo", true)]
    [InlineData("ffo", true)]
    [InlineData("fofo", true)]
    [InlineData("foo", true)]
    [InlineData("foofoofo", true)]
    [InlineData("fooofoofofooo", true)]
    [InlineData("foooofo", true)]
    [InlineData("foooofof", true)]
    [InlineData("foob", false)]
    [InlineData("foooofofx", false)]
    public void StarFO_ZeroOrMore(string input, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, "*(*(f)*(o))", _windowsOpts));
    }

    [Theory]
    [InlineData("ffffffo", true)]
    [InlineData("ffo", true)]
    [InlineData("fofo", true)]
    [InlineData("foo", true)]
    [InlineData("foooofof", true)]
    [InlineData("foob", false)]
    [InlineData("ofoofo", false)]
    public void StarFO_ZeroOrMoreFO(string input, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, "*(f*(o))", _windowsOpts));
    }

    [Theory]
    [InlineData("fofo", true)]
    [InlineData("foo", true)]
    [InlineData("foofoofo", true)]
    [InlineData("fooofoofofooo", true)]
    [InlineData("foooofo", true)]
    [InlineData("ffffffo", false)]
    [InlineData("ffo", false)]
    [InlineData("foooofof", false)]
    public void StarFPlusO_OneOrMoreO(string input, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, "*(f+(o))", _windowsOpts));
    }

    [Theory]
    [InlineData("foooxfooxfoxfooox", true)]
    [InlineData("foooxfooxfxfooox", true)]
    [InlineData("foox", true)]
    [InlineData("foo", false)]
    [InlineData("foooxfooxofoxfooox", false)]
    public void StarFOX_ZeroOrMoreFOX(string input, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, "*(f*(o)x)", _windowsOpts));
    }

    [Theory]
    [InlineData("ofoofo", true)]
    [InlineData("ofooofoofofooo", false)]
    public void StarOfPlusO(string input, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, "*(of+(o))", _windowsOpts));
    }

    [Theory]
    [InlineData("fofo", true)]
    [InlineData("fofoofoofofoo", true)]
    [InlineData("ofoofo", true)]
    [InlineData("ofooofoofofooo", true)]
    [InlineData("foo", false)]
    public void StarOfPlusOOrF(string input, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, "*(of+(o)|f)", _windowsOpts));
    }

    [Theory]
    [InlineData("ofoofo", true)]
    [InlineData("oofooofo", true)]
    [InlineData("ofooofoofofooo", false)]
    public void StarOfOrOofPlusO(string input, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, "*(of|oof+(o))", _windowsOpts));
    }

    [Theory]
    [InlineData("oxfoxoxfox", true)]
    [InlineData("oxfoxfox", false)]
    public void StarOxfPlusOx(string input, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, "*(oxf+(ox))", _windowsOpts));
    }

    [Theory]
    [InlineData("ofoooxoofxo", true)]
    [InlineData("ofoooxoofxoofoooxoofxo", true)]
    [InlineData("ofoooxoofxoofoooxoofxoo", true)]
    [InlineData("ofoooxoofxoofoooxoofxooofxofxo", true)]
    [InlineData("ofxoofxo", true)]
    [InlineData("ooo", true)]
    [InlineData("ofoooxoofxoofoooxoofxofo", false)]
    public void StarOfOXO_Complex(string input, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, "*(*(of*(o)x)o)", _windowsOpts));
    }

    [Theory]
    [InlineData("fofoofoofofoo", true)]
    [InlineData("fofo", true)]
    [InlineData("foo", true)]
    [InlineData("foofoofo", true)]
    [InlineData("ffffffo", false)]
    [InlineData("fooofoofofooo", false)]
    [InlineData("foooofo", false)]
    public void StarFoOrFoo_Backtracking(string input, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, "*(fo|foo)", _windowsOpts));
    }

    [Theory]
    [InlineData("foo", false)]
    [InlineData("bar", true)]
    [InlineData("baz", true)]
    [InlineData("foobar", true)]
    public void StarNotFoo(string input, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, "*(!(foo))", _windowsOpts));
    }

    [Theory]
    [InlineData("moo.cow", false)]
    [InlineData("moo", true)]
    [InlineData("cow", true)]
    public void NotDotPattern(string input, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, "!(*.*)", _windowsOpts));
    }

    [Theory]
    [InlineData("moo.cow", true)]
    public void NotADotNotB(string input, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, "!(a*).!(b*)", _windowsOpts));
    }

    [Theory]
    [InlineData("moo.cow", false)]
    public void NotStarDotNotStar(string input, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, "!(*).!(*)", _windowsOpts));
    }

    [Theory]
    [InlineData("effgz", true)]
    [InlineData("efgz", true)]
    [InlineData("egz", true)]
    public void AtExtglob_BracketAlternation(string input, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, "@(b+(c)d|e*(f)g?|?(h)i@(j|k))", _windowsOpts));
    }

    [Theory]
    [InlineData("egz", false)]
    public void AtExtglob_BracketAlternationPlusF(string input, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, "@(b+(c)d|e+(f)g?|?(h)i@(j|k))", _windowsOpts));
    }

    [Theory]
    [InlineData("egzefffgzbcdij", true)]
    public void StarExtglob_BracketAlternation(string input, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, "*(b+(c)d|e*(f)g?|?(h)i@(j|k))", _windowsOpts));
    }

    [Theory]
    [InlineData("/dev/udp/129.22.8.102/45", true)]
    public void DevPath_TcpOrUdp(string input, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, "/dev/@(tcp|udp)/*/*", _windowsOpts));
    }

    [Theory]
    [InlineData("12", true)]
    [InlineData("0", false)]
    [InlineData("1", false)]
    [InlineData("12abc", false)]
    [InlineData("555", false)]
    public void NumberRange_1To6FollowedByDigit(string input, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, "[1-6]([0-9])", _windowsOpts));
    }

    [Theory]
    [InlineData("12", true)]
    [InlineData("1", true)]
    [InlineData("555", true)]
    [InlineData("0", false)]
    [InlineData("12abc", false)]
    public void NumberRange_1To6StarDigits(string input, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, "[1-6]*([0-9])", _windowsOpts));
    }

    [Theory]
    [InlineData("07", true)]
    [InlineData("0377", true)]
    [InlineData("09", false)]
    public void OctalDigits(string input, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, "+([0-7])", _windowsOpts));
    }

    [Theory]
    [InlineData("a", true)]
    [InlineData("abc", true)]
    [InlineData("abcd", false)]
    [InlineData("abcde", false)]
    public void PlusAOrAbc(string input, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, "+(a|abc)", _windowsOpts));
    }

    [Theory]
    [InlineData("f", true)]
    [InlineData("def", true)]
    [InlineData("cdef", false)]
    [InlineData("bcdef", false)]
    public void PlusFOrDef(string input, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, "+(f|def)", _windowsOpts));
    }

    [Theory]
    [InlineData("abcd", true)]
    [InlineData("a", false)]
    [InlineData("ab", false)]
    [InlineData("abc", false)]
    public void StarAOrBFollowedByCd(string input, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, "*(a|b)cd", _windowsOpts));
    }

    [Theory]
    [InlineData("ab", true)]
    [InlineData("abab", true)]
    [InlineData("abcdef", true)]
    [InlineData("accdef", true)]
    [InlineData("123abc", false)]
    public void GroupAPlusOrB_Star(string input, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, "(a+|b)*", _windowsOpts));
    }

    [Theory]
    [InlineData("ab", true)]
    [InlineData("abab", true)]
    [InlineData("abcdef", false)]
    [InlineData("123abc", false)]
    public void GroupAPlusOrB_Plus(string input, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, "(a+|b)+", _windowsOpts));
    }

    [Theory]
    [InlineData("abd", true)]
    [InlineData("ab", false)]
    [InlineData("abcdef", false)]
    public void GroupBStarFooOrBar_D(string input, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, "a(b*(foo|bar))d", _windowsOpts));
    }

    [Theory]
    [InlineData("ab", true)]
    [InlineData("abef", true)]
    [InlineData("abcdef", false)]
    [InlineData("abcfefg", false)]
    public void AbStarEOrF(string input, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, "ab*(e|f)", _windowsOpts));
    }

    [Theory]
    [InlineData("ab", true)]
    [InlineData("abab", true)]
    [InlineData("abcdef", true)]
    [InlineData("abcfefg", true)]
    [InlineData("abef", true)]
    [InlineData("abd", true)]
    [InlineData("123abc", false)]
    [InlineData("accdef", false)]
    public void AbStarStarEOrF(string input, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, "ab**(e|f)", _windowsOpts));
    }

    [Theory]
    [InlineData("abcfefg", true)]
    [InlineData("ab", false)]
    [InlineData("abcdef", false)]
    public void AbStarStarEOrFG(string input, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, "ab**(e|f)g", _windowsOpts));
    }

    [Theory]
    [InlineData("abcdef", true)]
    [InlineData("abef", true)]
    [InlineData("abcfef", true)]
    [InlineData("ab", false)]
    [InlineData("abcfefg", false)]
    public void AbStarStarStarEf(string input, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, "ab***ef", _windowsOpts));
    }

    [Theory]
    [InlineData("abcdef", true)]
    [InlineData("abef", true)]
    [InlineData("abcfef", true)]
    [InlineData("ab", false)]
    public void AbStarPlusEOrF(string input, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, "ab*+(e|f)", _windowsOpts));
    }

    [Theory]
    [InlineData("abcdef", true)]
    [InlineData("abd", true)]
    [InlineData("abef", false)]
    public void AbStarDStarEOrF(string input, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, "ab*d*(e|f)", _windowsOpts));
    }

    [Theory]
    [InlineData("abcdef", true)]
    [InlineData("abd", false)]
    public void AbStarDPlusEOrF(string input, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, "ab*d+(e|f)", _windowsOpts));
    }

    [Theory]
    [InlineData("abef", true)]
    [InlineData("abcfef", true)]
    [InlineData("abd", true)]
    [InlineData("ab", false)]
    [InlineData("abcdef", false)]
    public void AbQMarkStarEOrF(string input, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, "ab?*(e|f)", _windowsOpts));
    }

    [Theory]
    [InlineData("123abc", true)]
    public void BugFix_StarQMarkAbc(string input, bool expected)
    {
        // Bug in all versions up to and including bash-2.05b
        Assert.Equal(expected, GlobMatcher.IsMatch(input, "*?(a)bc", _windowsOpts));
    }

    [Theory]
    [InlineData("a.c", true)]
    public void PosixWithExtglob_PlusAlphaDot(string input, bool expected)
    {
        var opts = new GlobbingOptions { Posix = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, "+([[:alpha:].])", opts));
    }

    [Theory]
    [InlineData("a.c", true)]
    public void PosixWithExtglob_StarAlphaDot(string input, bool expected)
    {
        var opts = new GlobbingOptions { Posix = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, "*([[:alpha:].])", opts));
    }

    [Theory]
    [InlineData("a.b", true)]
    [InlineData("a,b", true)]
    [InlineData("a:b", true)]
    [InlineData("a-b", true)]
    [InlineData("a;b", true)]
    [InlineData("a b", true)]
    [InlineData("a_b", true)]
    public void PosixWithExtglob_AtNotAlnum(string input, bool expected)
    {
        var opts = new GlobbingOptions { Posix = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, "a@([^[:alnum:]])b", opts));
    }

    [Theory]
    [InlineData("a.b", true)]
    [InlineData("a,b", true)]
    [InlineData("a:b", true)]
    [InlineData("a-b", true)]
    [InlineData("a;b", true)]
    [InlineData("a b", true)]
    [InlineData("a_b", true)]
    public void ExtglobWithCharClass_AtPunctuation(string input, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, "a@([-.,:; _])b", _windowsOpts));
    }

    [Theory]
    [InlineData("a.b", true)]
    [InlineData("a,b", false)]
    [InlineData("a-b", false)]
    public void ExtglobWithCharClass_AtDot(string input, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, "a@([.])b", _windowsOpts));
    }

    [Theory]
    [InlineData("a.b", false)]
    [InlineData("a,b", true)]
    [InlineData("a:b", true)]
    [InlineData("a-b", true)]
    public void ExtglobWithCharClass_AtNotDot(string input, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, "a@([^.])b", _windowsOpts));
    }

    [Theory]
    [InlineData("aaac", true)]
    [InlineData("aac", true)]
    [InlineData("ac", true)]
    [InlineData("baaac", false)]
    [InlineData("c", false)]
    [InlineData("foo", false)]
    public void StarAtAAc(string input, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, "*(@(a))a@(c)", _windowsOpts));
    }

    [Theory]
    [InlineData("abbcd", true)]
    [InlineData("abcd", true)]
    [InlineData("acd", true)]
    [InlineData("aaac", false)]
    [InlineData("baaac", false)]
    public void AtAbOrStarBCd(string input, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, "@(ab|a*(b))*(c)d", _windowsOpts));
    }

    [Theory]
    [InlineData("abbcd", true)]
    [InlineData("abcd", true)]
    [InlineData("acd", false)]
    [InlineData("aaac", false)]
    public void QMarkAtAOrBStarCd(string input, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, "?@(a|b)*@(c)d", _windowsOpts));
    }

    [Theory]
    [InlineData("abbcd", true)]
    [InlineData("abcd", true)]
    [InlineData("acd", false)]
    public void AtAbOrStarAtBCd(string input, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, "@(ab|a*@(b))*(c)d", _windowsOpts));
    }

    [Theory]
    [InlineData("aac", false)]
    public void StarAtABAtC(string input, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, "*(@(a))b@(c)", _windowsOpts));
    }

    [Theory]
    [InlineData("f", false)]
    [InlineData("fff", true)]
    [InlineData("foo", true)]
    [InlineData("ooo", true)]
    public void StarNotF_SingleAndMultiple(string input, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, "*(!(f))", _windowsOpts));
    }

    [Theory]
    [InlineData("f", false)]
    [InlineData("fff", true)]
    [InlineData("foo", true)]
    [InlineData("ooo", true)]
    public void PlusNotF_SingleAndMultiple(string input, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, "+(!(f))", _windowsOpts));
    }

    [Theory]
    [InlineData("foo", false)]
    [InlineData("foobar", true)]
    [InlineData("bar", true)]
    public void NotFoo_Simple(string input, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, "!(foo)", _windowsOpts));
    }

    [Theory]
    [InlineData("foo", true)]
    [InlineData("foobar", true)]
    [InlineData("bar", true)]
    public void NotX_Simple(string input, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, "!(x)", _windowsOpts));
    }

    [Theory]
    [InlineData("foo", true)]
    [InlineData("bar", true)]
    [InlineData("foobar", true)]
    public void NotXStar_Simple(string input, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, "!(x)*", _windowsOpts));
    }

    [Theory]
    [InlineData("foot", true)]
    [InlineData("foox", true)]
    [InlineData("zoox", true)]
    [InlineData("zoot", false)]
    public void AtNotZOrX_Complex(string input, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, "@(!(z*)|*x)", _windowsOpts));
    }

    [Theory]
    [InlineData("foobb", true)]
    [InlineData("foo", false)]
    [InlineData("foob", false)]
    public void GroupFooBb(string input, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, "(foo)bb", _windowsOpts));
    }

    [Theory]
    [InlineData("foo", true)]
    [InlineData("fofo", true)]
    [InlineData("fofoofoofofoo", true)]
    [InlineData("foofoofo", true)]
    [InlineData("fooofoofofooo", true)]
    [InlineData("ffffffo", false)]
    public void AtFooOrFOrFoStarFOrOfPlusO(string input, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, "@(foo|f|fo)*(f|of+(o))", _windowsOpts));
    }

}
