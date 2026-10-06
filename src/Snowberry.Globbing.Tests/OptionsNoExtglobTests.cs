namespace Snowberry.Globbing.Tests;

/// <summary>
/// Tests for options.noextglob.
/// Ported from: https://github.com/micromatch/picomatch/blob/master/test/options.noextglob.js
/// </summary>
public class OptionsNoExtglobTests
{
    // With Extglobs = false, * stays a wildcard and (js) is a plain group
    [Theory]
    [InlineData("a.js.js", "*.*(js).js", true)]
    [InlineData("a.*.js", "*.*(js).js", false)]
    [InlineData("a.(js).js", "*.*(js).js", false)]
    public void NoExtglob_StarBeforeParens_ActsAsWildcardFollowedByGroup(string input, string pattern, bool expected)
    {
        var options = new GlobOptions { Extglobs = false };
        Glob.IsMatch(input, pattern, options).Should().Be(expected);
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
    public void NoExtglob_Disabled_ExtglobsWork(string input, string pattern, bool expected)
    {
        Glob.IsMatch(input, pattern).Should().Be(expected);
    }

    [Theory]
    [InlineData("@(foo)", "@(foo)", true)]
    [InlineData("!(bar)", "!(bar)", true)]
    [InlineData("+(foo)", "+(foo)", true)]
    [InlineData("?(foo)", "?(foo)", true)]
    [InlineData("*(foo)", "*(foo)", true)]
    [InlineData("foo", "@(foo)", false)]
    [InlineData("bar", "!(bar)", false)]
    public void NoExtglob_Enabled_TreatedAsLiterals(string input, string pattern, bool expected)
    {
        var options = new GlobOptions { Extglobs = false };
        Glob.IsMatch(input, pattern, options).Should().Be(expected);
    }

    [Fact]
    public void NoExtglob_ShouldNotAffectOtherGlobFeatures()
    {
        var options = new GlobOptions { Extglobs = false };

        // Stars should still work
        Glob.IsMatch("foo", "*", options).Should().BeTrue();
        Glob.IsMatch("foo.txt", "*.txt", options).Should().BeTrue();

        // Question marks should still work
        Glob.IsMatch("foo", "f??", options).Should().BeTrue();

        // Brackets should still work
        Glob.IsMatch("foo", "[f]oo", options).Should().BeTrue();

        // Globstars should still work
        Glob.IsMatch("a/b/c", "**", options).Should().BeTrue();
    }

    [Fact]
    public void NoExtglob_ShouldMatchLiteralParens_Issue116()
    {
        var options = new GlobOptions { Extglobs = false };
        Glob.IsMatch("a/(dir)", "a/(dir)", options).Should().BeTrue();
    }

    [Theory]
    [InlineData("ax", "?(a*|b)", false)]
    public void NoExtglob_ShouldNotMatchExtglobPatterns(string input, string pattern, bool expected)
    {
        var options = new GlobOptions { Extglobs = false };
        Glob.IsMatch(input, pattern, options).Should().Be(expected);
    }

    [Theory]
    [InlineData("a/z", "a/!(z)", false)]
    [InlineData("a/b", "a/!(z)", false)]
    [InlineData("a/!(z)", "a/!(z)", true)]
    public void NoExtglob_ExclamationPatternPath(string input, string pattern, bool expected)
    {
        var options = new GlobOptions { Extglobs = false };
        Glob.IsMatch(input, pattern, options).Should().Be(expected);
    }

    [Theory]
    [InlineData("c/z/v", "c/*(z)/v", true)]
    [InlineData("c/a/v", "c/*(z)/v", false)]
    public void NoExtglob_StarParensPatternInMiddle(string input, string pattern, bool expected)
    {
        var options = new GlobOptions { Extglobs = false };
        Glob.IsMatch(input, pattern, options).Should().Be(expected);
    }

    [Theory]
    [InlineData("z", "?(z)", false)]
    [InlineData("zf", "?(z)", false)]
    [InlineData("fz", "?(z)", true)]
    public void NoExtglob_QuestionMarkParens(string input, string pattern, bool expected)
    {
        var options = new GlobOptions { Extglobs = false };
        Glob.IsMatch(input, pattern, options).Should().Be(expected);
    }

    [Theory]
    [InlineData("z", "+(z)", false)]
    [InlineData("fz", "+(z)", false)]
    public void NoExtglob_PlusParens(string input, string pattern, bool expected)
    {
        var options = new GlobOptions { Extglobs = false };
        Glob.IsMatch(input, pattern, options).Should().Be(expected);
    }

    [Theory]
    [InlineData("z", "*(z)", true)]
    [InlineData("zf", "*(z)", false)]
    [InlineData("fz", "*(z)", true)]
    public void NoExtglob_StarParens(string input, string pattern, bool expected)
    {
        var options = new GlobOptions { Extglobs = false };
        Glob.IsMatch(input, pattern, options).Should().Be(expected);
    }

    [Theory]
    [InlineData("cz", "a@(z)", false)]
    [InlineData("az", "a@(z)", false)]
    public void NoExtglob_AAtZ(string input, string pattern, bool expected)
    {
        var options = new GlobOptions { Extglobs = false };
        Glob.IsMatch(input, pattern, options).Should().Be(expected);
    }

    [Theory]
    [InlineData("cz", "a*@(z)", false)]
    [InlineData("az", "a*@(z)", false)]
    public void NoExtglob_AStarAtZ(string input, string pattern, bool expected)
    {
        var options = new GlobOptions { Extglobs = false };
        Glob.IsMatch(input, pattern, options).Should().Be(expected);
    }

    [Theory]
    [InlineData("cz", "a!(z)", false)]
    [InlineData("az", "a!(z)", false)]
    public void NoExtglob_AExclamationZ(string input, string pattern, bool expected)
    {
        var options = new GlobOptions { Extglobs = false };
        Glob.IsMatch(input, pattern, options).Should().Be(expected);
    }

    [Theory]
    [InlineData("abz", "a?(z)", true)]
    [InlineData("az", "a?(z)", false)]
    [InlineData("azz", "a?(z)", true)]
    public void NoExtglob_AQuestionZ(string input, string pattern, bool expected)
    {
        var options = new GlobOptions { Extglobs = false };
        Glob.IsMatch(input, pattern, options).Should().Be(expected);
    }

    [Theory]
    [InlineData("az", "a+(z)", false)]
    [InlineData("a+z", "a+(z)", true)]
    public void NoExtglob_APlusZ(string input, string pattern, bool expected)
    {
        var options = new GlobOptions { Extglobs = false };
        Glob.IsMatch(input, pattern, options).Should().Be(expected);
    }

    [Theory]
    [InlineData("abz", "a*(z)", true)]
    [InlineData("az", "a*(z)", true)]
    public void NoExtglob_AStarZ(string input, string pattern, bool expected)
    {
        var options = new GlobOptions { Extglobs = false };
        Glob.IsMatch(input, pattern, options).Should().Be(expected);
    }

    [Theory]
    [InlineData("abz", "a**(z)", true)]
    [InlineData("az", "a**(z)", true)]
    public void NoExtglob_ADoubleStarZ(string input, string pattern, bool expected)
    {
        var options = new GlobOptions { Extglobs = false };
        Glob.IsMatch(input, pattern, options).Should().Be(expected);
    }

    [Theory]
    [InlineData("az", "a*!(z)", false)]
    public void NoExtglob_AStarExclamationZ(string input, string pattern, bool expected)
    {
        var options = new GlobOptions { Extglobs = false };
        Glob.IsMatch(input, pattern, options).Should().Be(expected);
    }

}