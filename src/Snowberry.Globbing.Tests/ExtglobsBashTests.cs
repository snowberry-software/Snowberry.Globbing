namespace Snowberry.Globbing.Tests;

/// <summary>
/// Extensive extglob tests from bash 4.3, 4.4 specifications.
/// Ported from: https://github.com/micromatch/picomatch/blob/master/test/extglobs-bash.js
/// </summary>
public class ExtglobsBashTests
{
    private readonly GlobbingOptions _bashOpts = new() { Bash = true, Windows = true };

    [Theory]
    [InlineData("", "*(0|1|3|5|7|9)", false)]
    [InlineData("137577991", "*(0|1|3|5|7|9)", true)]
    [InlineData("2468", "*(0|1|3|5|7|9)", false)]
    public void OddDigits(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, _bashOpts));
    }

    [Theory]
    [InlineData("***", "\\*\\*\\*", true)]
    public void EscapedStars(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, _bashOpts));
    }

    [Theory]
    [InlineData("0377", "+([0-7])", true)]
    [InlineData("07", "+([0-7])", true)]
    [InlineData("09", "+([0-7])", false)]
    public void OctalNumbers(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, _bashOpts));
    }

    [Theory]
    [InlineData("1", "0|[1-9]*([0-9])", true)]
    [InlineData("12", "0|[1-9]*([0-9])", true)]
    [InlineData("12abc", "0|[1-9]*([0-9])", false)]
    public void NumberAlternation(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, _bashOpts));
    }

    [Theory]
    [InlineData("-adobe-courier-bold-o-normal--12-120-75-75-/-70-iso8859-1", "-*-*-*-*-*-*-12-*-*-*-m-*-*-*", false)]
    [InlineData("-adobe-courier-bold-o-normal--12-120-75-75-m-70-iso8859-1", "-*-*-*-*-*-*-12-*-*-*-m-*-*-*", true)]
    [InlineData("-adobe-courier-bold-o-normal--12-120-75-75-X-70-iso8859-1", "-*-*-*-*-*-*-12-*-*-*-m-*-*-*", false)]
    public void AdobeFontPattern(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, _bashOpts));
    }

    [Theory]
    [InlineData("/dev/udp/129.22.8.102/45", "/dev\\/@(tcp|udp)\\/*\\/*", true)]
    public void DevicePaths(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, _bashOpts));
    }

    [Theory]
    [InlineData("a", "!(a)", false)]
    [InlineData("a", "!(a)*", false)]
    [InlineData("b", "!(a)", true)]
    [InlineData("aa", "!(a)", true)]
    [InlineData("ab", "!(a)", true)]
    public void SimpleNegation(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, _bashOpts));
    }

    [Theory]
    [InlineData("a", "(a)", true)]
    [InlineData("a", "(b)", false)]
    [InlineData("ab", "(a)b", true)]
    [InlineData("ab", "(a)(b)", true)]
    public void SimpleGrouping(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, _bashOpts));
    }

    [Theory]
    [InlineData("a", "*(a)", true)]
    [InlineData("aa", "*(a)", true)]
    [InlineData("aaa", "*(a)", true)]
    [InlineData("", "*(a)", false)]
    [InlineData("b", "*(a)", false)]
    public void ZeroOrMore(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, _bashOpts));
    }

    [Theory]
    [InlineData("a", "+(a)", true)]
    [InlineData("aa", "+(a)", true)]
    [InlineData("aaa", "+(a)", true)]
    [InlineData("", "+(a)", false)]
    [InlineData("b", "+(a)", false)]
    public void OneOrMore(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, _bashOpts));
    }

    [Theory]
    [InlineData("a", "?(a|b)", true)]
    [InlineData("b", "?(a|b)", true)]
    [InlineData("", "?(a)", false)]
    [InlineData("a", "?(a)", true)]
    [InlineData("aa", "?(a)", false)]
    public void ZeroOrOne(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, _bashOpts));
    }

    [Theory]
    [InlineData("a", "?", true)]
    [InlineData("a", "??", false)]
    [InlineData("ab", "??", true)]
    [InlineData("?a?b", "\\??\\?b", true)]
    public void QuestionMark(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, _bashOpts));
    }

    [Theory]
    [InlineData("a", "a!(b)*", true)]
    [InlineData("ab", "a!(b)*", false)]
    [InlineData("ac", "a!(b)*", true)]
    [InlineData("abc", "a!(b)*", false)]
    public void NegationAfterLiteral(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, _bashOpts));
    }

    [Theory]
    [InlineData("a", "a?(a|b)", true)]
    [InlineData("aa", "a?(a|b)", true)]
    [InlineData("ab", "a?(a|b)", true)]
    [InlineData("ac", "a?(a|b)", false)]
    [InlineData("a", "a?(x)", true)]
    [InlineData("ax", "a?(x)", true)]
    [InlineData("axx", "a?(x)", false)]
    public void OptionalAfterLiteral(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, _bashOpts));
    }

    [Theory]
    [InlineData("a((((b", "a(*b", true)]
    [InlineData("a((((b", "a(b", false)]
    [InlineData("a((b", "a(*b", true)]
    [InlineData("a(b", "a(*b", true)]
    [InlineData("a(b", "a(b", true)]
    [InlineData("a(b", "a\\(b", true)]
    public void ParenthesisPatterns(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, _bashOpts));
    }

    [Theory]
    [InlineData("a", "!(*.a|*.b|*.c)", true)]
    [InlineData("a.", "!(*.a|*.b|*.c)", true)]
    [InlineData("a.a", "!(*.a|*.b|*.c)", false)]
    [InlineData("a.b", "!(*.a|*.b|*.c)", false)]
    [InlineData("a.c", "!(*.a|*.b|*.c)", false)]
    [InlineData("a.d", "!(*.a|*.b|*.c)", true)]
    [InlineData("a.txt", "!(*.a|*.b|*.c)", true)]
    public void FileExtensionNegation(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, _bashOpts));
    }

    [Theory]
    [InlineData("a.a", "*.(a|b|@(ab|a*@(b))*(c)d)", true)]
    [InlineData("a.b", "*.(a|b|@(ab|a*@(b))*(c)d)", true)]
    [InlineData("a.c", "*.(a|b|@(ab|a*@(b))*(c)d)", false)]
    [InlineData("a.ab", "*.(a|b|@(ab|a*@(b))*(c)d)", false)]
    [InlineData("a.abcd", "*.(a|b|@(ab|a*@(b))*(c)d)", true)]
    public void FileExtensionAlternation(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, _bashOpts));
    }

    [Theory]
    [InlineData("a.", "*.!(a)", true)]
    [InlineData("a.", "*.!(a|b|c)", true)]
    [InlineData("a.a", "*.!(a)", false)]
    [InlineData("a.b", "*.!(a|b|c)", false)]
    [InlineData("a.d", "*.!(a|b|c)", true)]
    public void NegatedFileExtensions(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, _bashOpts));
    }

    [Theory]
    [InlineData("ab", "(a+|b)*", true)]
    [InlineData("abab", "(a+|b)*", true)]
    [InlineData("abcdef", "(a+|b)*", true)]
    [InlineData("123abc", "(a+|b)*", false)]
    public void ABPatternsWithStars(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, _bashOpts));
    }

    [Theory]
    [InlineData("ab", "(a+|b)+", true)]
    [InlineData("abab", "(a+|b)+", true)]
    [InlineData("123abc", "(a+|b)+", false)]
    [InlineData("abcdef", "(a+|b)+", false)]
    public void ABPatternsWithPlus(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, _bashOpts));
    }

    [Theory]
    [InlineData("ab", "ab*(e|f)", true)]
    [InlineData("abef", "ab*(e|f)", true)]
    [InlineData("abcdef", "ab*(e|f)", false)]
    public void ABStarEF(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, _bashOpts));
    }

    [Theory]
    [InlineData("ab", "ab**(e|f)", true)]
    [InlineData("abab", "ab**(e|f)", true)]
    [InlineData("abcdef", "ab**(e|f)", true)]
    [InlineData("abef", "ab**(e|f)", true)]
    [InlineData("123abc", "ab**(e|f)", false)]
    public void ABStarStarEF(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, _bashOpts));
    }

    [Theory]
    [InlineData("abcfefg", "ab**(e|f)g", true)]
    [InlineData("ab", "ab**(e|f)g", false)]
    [InlineData("abcdef", "ab**(e|f)g", false)]
    public void ABStarStarEFG(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, _bashOpts));
    }

    [Theory]
    [InlineData("abcdef", "ab***ef", true)]
    [InlineData("abef", "ab***ef", true)]
    [InlineData("abcfef", "ab***ef", true)]
    [InlineData("ab", "ab***ef", false)]
    public void ABStarStarStarEF(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, _bashOpts));
    }

    [Theory]
    [InlineData("abcdef", "ab*+(e|f)", true)]
    [InlineData("abef", "ab*+(e|f)", true)]
    [InlineData("abcfef", "ab*+(e|f)", true)]
    [InlineData("ab", "ab*+(e|f)", false)]
    public void ABStarPlusEF(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, _bashOpts));
    }

    [Theory]
    [InlineData("abcdef", "ab*d*(e|f)", true)]
    [InlineData("abd", "ab*d*(e|f)", true)]
    [InlineData("abef", "ab*d*(e|f)", false)]
    public void ABStarDStarEF(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, _bashOpts));
    }

    [Theory]
    [InlineData("abcdef", "ab*d+(e|f)", true)]
    [InlineData("abd", "ab*d+(e|f)", false)]
    public void ABStarDPlusEF(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, _bashOpts));
    }

    [Theory]
    [InlineData("abef", "ab?*(e|f)", true)]
    [InlineData("abcfef", "ab?*(e|f)", true)]
    [InlineData("abd", "ab?*(e|f)", true)]
    [InlineData("ab", "ab?*(e|f)", false)]
    [InlineData("abcdef", "ab?*(e|f)", false)]
    public void ABQMarkStarEF(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, _bashOpts));
    }

    [Theory]
    [InlineData("abd", "a(b*(foo|bar))d", true)]
    [InlineData("ab", "a(b*(foo|bar))d", false)]
    [InlineData("abcdef", "a(b*(foo|bar))d", false)]
    public void FooBarPattern(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, _bashOpts));
    }

    [Theory]
    [InlineData("123abc", "*?(a)bc", true)]
    public void BugFix_StarQMarkABC(string input, string pattern, bool expected)
    {
        // Bug in all versions up to and including bash-2.05b
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, _bashOpts));
    }

    [Theory]
    [InlineData("effgz", "@(b+(c)d|e*(f)g?|?(h)i@(j|k))", true)]
    [InlineData("efgz", "@(b+(c)d|e*(f)g?|?(h)i@(j|k))", true)]
    [InlineData("egz", "@(b+(c)d|e*(f)g?|?(h)i@(j|k))", true)]
    [InlineData("egz", "@(b+(c)d|e+(f)g?|?(h)i@(j|k))", false)]
    public void ComplexAlternation(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, _bashOpts));
    }

    [Theory]
    [InlineData("egzefffgzbcdij", "*(b+(c)d|e*(f)g?|?(h)i@(j|k))", true)]
    public void ComplexAlternationStar(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, _bashOpts));
    }

    [Theory]
    [InlineData("aaac", "*(@(a))a@(c)", true)]
    [InlineData("aac", "*(@(a))a@(c)", true)]
    [InlineData("ac", "*(@(a))a@(c)", true)]
    [InlineData("baaac", "*(@(a))a@(c)", false)]
    [InlineData("c", "*(@(a))a@(c)", false)]
    public void ABCCDPattern(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, _bashOpts));
    }

    [Theory]
    [InlineData("abbcd", "@(ab|a*(b))*(c)d", true)]
    [InlineData("abcd", "@(ab|a*(b))*(c)d", true)]
    [InlineData("acd", "@(ab|a*(b))*(c)d", true)]
    [InlineData("aaac", "@(ab|a*(b))*(c)d", false)]
    public void ABCDPattern(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, _bashOpts));
    }

    [Theory]
    [InlineData("abbcd", "?@(a|b)*@(c)d", true)]
    [InlineData("abcd", "?@(a|b)*@(c)d", true)]
    [InlineData("acd", "?@(a|b)*@(c)d", false)]
    public void QMarkABCDPattern(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, _bashOpts));
    }

    [Theory]
    [InlineData("abbcd", "@(ab|a*@(b))*(c)d", true)]
    [InlineData("abcd", "@(ab|a*@(b))*(c)d", true)]
    [InlineData("acd", "@(ab|a*@(b))*(c)d", false)]
    public void ABCDNestedPattern(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, _bashOpts));
    }

    [Theory]
    [InlineData("fofo", "*(fo|foo)", true)]
    [InlineData("fofoofoofofoo", "*(fo|foo)", true)]
    [InlineData("foo", "*(fo|foo)", true)]
    [InlineData("foofoofo", "*(fo|foo)", true)]
    [InlineData("fooofoofofooo", "*(fo|foo)", false)]
    [InlineData("foooofo", "*(fo|foo)", false)]
    public void FOFOOPattern(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, _bashOpts));
    }

    [Theory]
    [InlineData("ffo", "*(f*(o))", true)]
    [InlineData("fofo", "*(f*(o))", true)]
    [InlineData("fooofoofofooo", "*(f*(o))", true)]
    [InlineData("foooofo", "*(f*(o))", true)]
    [InlineData("foooofof", "*(f*(o))", true)]
    [InlineData("foob", "*(f*(o))", false)]
    [InlineData("foooofofx", "*(f*(o))", false)]
    public void FStarOPattern(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, _bashOpts));
    }

    [Theory]
    [InlineData("fofo", "*(f+(o))", true)]
    [InlineData("fofoofoofofoo", "*(f+(o))", true)]
    [InlineData("foo", "*(f+(o))", true)]
    [InlineData("foofoofo", "*(f+(o))", true)]
    [InlineData("fooofoofofooo", "*(f+(o))", true)]
    [InlineData("foooofo", "*(f+(o))", true)]
    [InlineData("foooofof", "*(f+(o))", false)]
    public void FPlusOPattern(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, _bashOpts));
    }

    [Theory]
    [InlineData("foooxfooxfoxfooox", "*(f*(o)x)", true)]
    [InlineData("foooxfooxfxfooox", "*(f*(o)x)", true)]
    [InlineData("foox", "*(f*(o)x)", true)]
    [InlineData("foo", "*(f*(o)x)", false)]
    [InlineData("foooxfooxofoxfooox", "*(f*(o)x)", false)]
    public void FStarOXPattern(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, _bashOpts));
    }

    [Theory]
    [InlineData("ofoofo", "*(of+(o))", true)]
    [InlineData("ofooofoofofooo", "*(of+(o))", false)]
    public void OFPlusOPattern(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, _bashOpts));
    }

    [Theory]
    [InlineData("fofo", "*(of+(o)|f)", true)]
    [InlineData("ofoofo", "*(of+(o)|f)", true)]
    [InlineData("ofooofoofofooo", "*(of+(o)|f)", true)]
    [InlineData("fofoofoofofoo", "*(of+(o)|f)", true)]
    [InlineData("foo", "*(of+(o)|f)", false)]
    public void OFPlusOOrFPattern(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, _bashOpts));
    }

    [Theory]
    [InlineData("ofoofo", "*(of|oof+(o))", true)]
    [InlineData("oofooofo", "*(of|oof+(o))", true)]
    [InlineData("ofooofoofofooo", "*(of|oof+(o))", false)]
    public void OFOrOOFPlusOPattern(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, _bashOpts));
    }

    [Theory]
    [InlineData("oxfoxoxfox", "*(oxf+(ox))", true)]
    [InlineData("oxfoxfox", "*(oxf+(ox))", false)]
    public void OXFPattern(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, _bashOpts));
    }

    [Theory]
    [InlineData("ofoooxoofxo", "*(*(of*(o)x)o)", true)]
    [InlineData("ofoooxoofxoofoooxoofxo", "*(*(of*(o)x)o)", true)]
    [InlineData("ofoooxoofxoofoooxoofxoo", "*(*(of*(o)x)o)", true)]
    [InlineData("ofoooxoofxoofoooxoofxooofxofxo", "*(*(of*(o)x)o)", true)]
    [InlineData("ofxoofxo", "*(*(of*(o)x)o)", true)]
    [InlineData("ooo", "*(*(of*(o)x)o)", true)]
    [InlineData("ofoooxoofxoofoooxoofxofo", "*(*(of*(o)x)o)", false)]
    public void NestedOFPattern(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, _bashOpts));
    }

    [Theory]
    [InlineData("foo", "@(foo|f|fo)*(f|of+(o))", true)]
    [InlineData("fofo", "@(foo|f|fo)*(f|of+(o))", true)]
    [InlineData("fofoofoofofoo", "@(foo|f|fo)*(f|of+(o))", true)]
    [InlineData("foofoofo", "@(foo|f|fo)*(f|of+(o))", true)]
    [InlineData("fooofoofofooo", "@(foo|f|fo)*(f|of+(o))", true)]
    [InlineData("ffffffo", "@(foo|f|fo)*(f|of+(o))", false)]
    public void AtFooOrFOrFoPattern(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, _bashOpts));
    }

    [Theory]
    [InlineData("foobb", "(foo)bb", true)]
    [InlineData("foo", "(foo)bb", false)]
    [InlineData("foob", "(foo)bb", false)]
    public void FoobbPattern(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, _bashOpts));
    }

    [Theory]
    [InlineData("ffffffo", "*(*(f)*(o))", true)]
    [InlineData("fffooofoooooffoofffooofff", "*(*(f)*(o))", true)]
    [InlineData("ffo", "*(*(f)*(o))", true)]
    [InlineData("fofo", "*(*(f)*(o))", true)]
    [InlineData("foo", "*(*(f)*(o))", true)]
    [InlineData("foob", "*(*(f)*(o))", false)]
    public void StarStarFOPattern(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, _bashOpts));
    }

    [Theory]
    [InlineData("foo", "!(foo)", false)]
    [InlineData("bar", "!(foo)", true)]
    [InlineData("foobar", "!(foo)", true)]
    public void ExclusionNotFoo(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, _bashOpts));
    }

    [Theory]
    [InlineData("f", "!(f)", false)]
    [InlineData("fff", "!(f)", true)]
    [InlineData("foo", "!(f)", true)]
    public void ExclusionNotF(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, _bashOpts));
    }

    [Theory]
    [InlineData("foo", "*(!(foo))", false)]
    [InlineData("bar", "*(!(foo))", true)]
    [InlineData("foobar", "*(!(foo))", true)]
    public void StarExclusionNotFoo(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, _bashOpts));
    }

    [Theory]
    [InlineData("f", "*(!(f))", false)]
    [InlineData("fff", "*(!(f))", true)]
    [InlineData("foo", "*(!(f))", true)]
    public void StarExclusionNotF(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, _bashOpts));
    }

    [Theory]
    [InlineData("f", "+(!(f))", false)]
    [InlineData("fff", "+(!(f))", true)]
    [InlineData("foo", "+(!(f))", true)]
    public void PlusExclusionNotF(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, _bashOpts));
    }

    [Theory]
    [InlineData("foo", "@(!(z*)|*x)", true)]
    [InlineData("foox", "@(!(z*)|*x)", true)]
    [InlineData("foot", "@(!(z*)|*x)", true)]
    [InlineData("zoot", "@(!(z*)|*x)", false)]
    [InlineData("zoox", "@(!(z*)|*x)", true)]
    public void AtNotZPattern(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, _bashOpts));
    }

    [Theory]
    [InlineData("moo.cow", "!(*.*)", false)]
    [InlineData("moo", "!(*.*)", true)]
    [InlineData("cow", "!(*.*)", true)]
    public void NotDotPattern(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, _bashOpts));
    }

    [Theory]
    [InlineData("moo.cow", "!(a*).!(b*)", true)]
    public void NotADotNotBPattern(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, _bashOpts));
    }

    [Theory]
    [InlineData("moo.cow", "!(*).!(*)", false)]
    public void NotStarDotNotStarPattern(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, _bashOpts));
    }

    [Theory]
    [InlineData("foob", "!(foo)b*", false)]
    [InlineData("foobb", "!(foo)b*", false)]
    [InlineData("bar", "!(foo)b*", true)]
    [InlineData("baz", "!(foo)b*", true)]
    public void NotFooBStarPattern(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, _bashOpts));
    }

}
