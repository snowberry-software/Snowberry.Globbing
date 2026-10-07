namespace Snowberry.Globbing.Tests;

/// <summary>
/// Tests for options.noextglob.
/// Ported from: https://github.com/micromatch/picomatch/blob/master/test/options.noextglob.js
/// </summary>
public class OptionsNoExtglobTests
{
    private static readonly GlobOptions s_NoExtglob = new() { Extglobs = false };

    [Theory]
    // Extglob openers are literal text
    [InlineData("cz", "a@(z)", false)]
    [InlineData("az", "a@(z)", false)]
    [InlineData("cz", "a!(z)", false)]
    [InlineData("az", "a!(z)", false)]
    [InlineData("cz", "a*@(z)", false)]
    [InlineData("az", "a*@(z)", false)]
    [InlineData("az", "a*!(z)", false)]
    [InlineData("@(foo)", "@(foo)", true)]
    [InlineData("!(bar)", "!(bar)", true)]
    [InlineData("+(foo)", "+(foo)", true)]
    [InlineData("?(foo)", "?(foo)", true)]
    [InlineData("*(foo)", "*(foo)", true)]
    [InlineData("foo", "@(foo)", false)]
    [InlineData("bar", "!(bar)", false)]
    [InlineData("a/z", "a/!(z)", false)]
    [InlineData("a/b", "a/!(z)", false)]
    [InlineData("a/!(z)", "a/!(z)", true)]
    [InlineData("a/(dir)", "a/(dir)", true)]
    // + is literal before a plain group
    [InlineData("az", "a+(z)", false)]
    [InlineData("a+z", "a+(z)", true)]
    [InlineData("z", "+(z)", false)]
    [InlineData("fz", "+(z)", false)]
    // ? is one character before a plain group
    [InlineData("abz", "a?(z)", true)]
    [InlineData("az", "a?(z)", false)]
    [InlineData("azz", "a?(z)", true)]
    [InlineData("z", "?(z)", false)]
    [InlineData("zf", "?(z)", false)]
    [InlineData("fz", "?(z)", true)]
    [InlineData("ax", "?(a*|b)", false)]
    // * stays a wildcard and (z) is a plain group
    [InlineData("abz", "a**(z)", true)]
    [InlineData("az", "a**(z)", true)]
    [InlineData("abz", "a*(z)", true)]
    [InlineData("az", "a*(z)", true)]
    [InlineData("a.js.js", "*.*(js).js", true)]
    [InlineData("a.*.js", "*.*(js).js", false)]
    [InlineData("a.(js).js", "*.*(js).js", false)]
    [InlineData("z", "*(z)", true)]
    [InlineData("zf", "*(z)", false)]
    [InlineData("fz", "*(z)", true)]
    [InlineData("c/z/v", "c/*(z)/v", true)]
    [InlineData("c/a/v", "c/*(z)/v", false)]
    // Other glob features are unaffected
    [InlineData("foo", "*", true)]
    [InlineData("foo.txt", "*.txt", true)]
    [InlineData("foo", "f??", true)]
    [InlineData("foo", "[f]oo", true)]
    [InlineData("a/b/c", "**", true)]
    public void NoExtglob_MatchesExtglobSyntaxAsPlainGlob(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern, s_NoExtglob).Should().Be(expected);
    }

    [Theory]
    [InlineData("foo", "@(foo)", true)]
    [InlineData("foo", "!(bar)", true)]
    [InlineData("bar", "!(bar)", false)]
    [InlineData("foo", "+(foo)", true)]
    [InlineData("foofoo", "+(foo)", true)]
    [InlineData("foo", "?(foo)", true)]
    [InlineData("foo", "*(foo)", true)]
    [InlineData("foofoo", "*(foo)", true)]
    public void NoExtglob_ExtglobsEnabled_MatchExtglobs(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }
}
