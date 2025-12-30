namespace Snowberry.Globbing.Tests;

/// <summary>
/// Tests for options.noextglob.
/// Ported from: https://github.com/micromatch/picomatch/blob/master/test/options.noextglob.js
/// </summary>
public class OptionsNoExtglobTests
{
    [Theory]
    [InlineData("a.js.js", "*.*(js).js", true)]
    [InlineData("a.md.js", "*.*(js).js", false)]
    public void NoExtglob_Disabled_ShouldMatchExtglobs(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
    }

    // With noextglob, *(js) is NOT an extglob, so the * is still a glob wildcard
    // Pattern *.*(js).js becomes: [^/]*?\.[^/]*?(js)\.js in regex
    // This means: anything-dot-anything-capturing(js)-dot-js
    [Theory]
    [InlineData("a.js.js", "*.*(js).js", true)]    // Works: a.js.js -> [a].[js].js
    [InlineData("a.*.js", "*.*(js).js", false)]    // Fails: * literal not in pattern, (js) group needs "js"
    [InlineData("a.(js).js", "*.*(js).js", false)] // Fails: parentheses don't match the capturing group
    public void NoExtglob_Enabled_ShouldTreatAsLiteral(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { NoExtglob = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
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
    // Note: Empty string does NOT match ?(foo) or *(foo) in picomatch without special options
    public void NoExtglob_Disabled_ExtglobsWork(string input, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern));
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
        var options = new GlobbingOptions { NoExtglob = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Fact]
    public void NoExtglob_ShouldNotAffectOtherGlobFeatures()
    {
        var options = new GlobbingOptions { NoExtglob = true };

        // Stars should still work
        Assert.True(GlobMatcher.IsMatch("foo", "*", options));
        Assert.True(GlobMatcher.IsMatch("foo.txt", "*.txt", options));

        // Question marks should still work
        Assert.True(GlobMatcher.IsMatch("foo", "f??", options));

        // Brackets should still work
        Assert.True(GlobMatcher.IsMatch("foo", "[f]oo", options));

        // Globstars should still work
        Assert.True(GlobMatcher.IsMatch("a/b/c", "**", options));
    }

    [Fact]
    public void NoExtglob_ShouldMatchLiteralParens_Issue116()
    {
        var options = new GlobbingOptions { NoExtglob = true };
        Assert.True(GlobMatcher.IsMatch("a/(dir)", "a/(dir)", options));
    }

    [Theory]
    [InlineData("ax", "?(a*|b)", false)]
    public void NoExtglob_ShouldNotMatchExtglobPatterns(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { NoExtglob = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("a.j.js", "*.*(j).js", true)]
    [InlineData("a.md.js", "*.*(j).js", false)]
    public void NoExtglob_StarJDotJs(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { NoExtglob = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("a/z", "a/!(z)", false)]
    [InlineData("a/b", "a/!(z)", false)]
    [InlineData("a/!(z)", "a/!(z)", true)]
    public void NoExtglob_ExclamationPatternPath(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { NoExtglob = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("c/a/v", "c/!(z)/v", false)]
    [InlineData("c/z/v", "c/!(z)/v", false)]
    public void NoExtglob_ExclamationPatternInMiddle(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { NoExtglob = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("c/z/v", "c/@(z)/v", false)]
    [InlineData("c/a/v", "c/@(z)/v", false)]
    public void NoExtglob_AtPatternInMiddle(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { NoExtglob = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("c/z/v", "c/+(z)/v", false)]
    [InlineData("c/a/v", "c/+(z)/v", false)]
    public void NoExtglob_PlusPatternInMiddle(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { NoExtglob = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("c/z/v", "c/*(z)/v", true)]
    [InlineData("c/a/v", "c/*(z)/v", false)]
    public void NoExtglob_StarParensPatternInMiddle(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { NoExtglob = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("c/z/v", "?(z)", false)]
    [InlineData("z", "?(z)", false)]
    [InlineData("zf", "?(z)", false)]
    [InlineData("fz", "?(z)", true)]
    public void NoExtglob_QuestionMarkParens(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { NoExtglob = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("c/z/v", "+(z)", false)]
    [InlineData("z", "+(z)", false)]
    [InlineData("zf", "+(z)", false)]
    [InlineData("fz", "+(z)", false)]
    public void NoExtglob_PlusParens(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { NoExtglob = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("c/z/v", "*(z)", false)]
    [InlineData("z", "*(z)", true)]
    [InlineData("zf", "*(z)", false)]
    [InlineData("fz", "*(z)", true)]
    public void NoExtglob_StarParens(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { NoExtglob = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("cz", "a@(z)", false)]
    [InlineData("abz", "a@(z)", false)]
    [InlineData("az", "a@(z)", false)]
    public void NoExtglob_AAtZ(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { NoExtglob = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("cz", "a*@(z)", false)]
    [InlineData("abz", "a*@(z)", false)]
    [InlineData("az", "a*@(z)", false)]
    public void NoExtglob_AStarAtZ(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { NoExtglob = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("cz", "a!(z)", false)]
    [InlineData("abz", "a!(z)", false)]
    [InlineData("az", "a!(z)", false)]
    public void NoExtglob_AExclamationZ(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { NoExtglob = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("cz", "a?(z)", false)]
    [InlineData("abz", "a?(z)", true)]
    [InlineData("az", "a?(z)", false)]
    [InlineData("azz", "a?(z)", true)]
    public void NoExtglob_AQuestionZ(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { NoExtglob = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("cz", "a+(z)", false)]
    [InlineData("abz", "a+(z)", false)]
    [InlineData("az", "a+(z)", false)]
    [InlineData("azz", "a+(z)", false)]
    [InlineData("a+z", "a+(z)", true)]
    public void NoExtglob_APlusZ(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { NoExtglob = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("cz", "a*(z)", false)]
    [InlineData("abz", "a*(z)", true)]
    [InlineData("az", "a*(z)", true)]
    public void NoExtglob_AStarZ(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { NoExtglob = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("cz", "a**(z)", false)]
    [InlineData("abz", "a**(z)", true)]
    [InlineData("az", "a**(z)", true)]
    public void NoExtglob_ADoubleStarZ(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { NoExtglob = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

    [Theory]
    [InlineData("cz", "a*!(z)", false)]
    [InlineData("abz", "a*!(z)", false)]
    [InlineData("az", "a*!(z)", false)]
    public void NoExtglob_AStarExclamationZ(string input, string pattern, bool expected)
    {
        var options = new GlobbingOptions { NoExtglob = true };
        Assert.Equal(expected, GlobMatcher.IsMatch(input, pattern, options));
    }

}
