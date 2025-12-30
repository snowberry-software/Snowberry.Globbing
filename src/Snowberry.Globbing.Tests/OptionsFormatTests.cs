namespace Snowberry.Globbing.Tests;

/// <summary>
/// Tests for options.format.
/// Ported from: https://github.com/micromatch/picomatch/blob/master/test/options.format.js
/// </summary>
public class OptionsFormatTests
{
    [Fact]
    public void Format_ShouldTransformInputString()
    {
        // The format option allows transforming the input string before matching
        var options = new GlobbingOptions
        {
            Format = input => input.Replace("-", "")
        };

        Assert.True(GlobMatcher.IsMatch("abc", "abc", options));
        Assert.True(GlobMatcher.IsMatch("a-b-c", "abc", options));
        Assert.True(GlobMatcher.IsMatch("a--b--c", "abc", options));
    }

    [Fact]
    public void Format_ShouldWorkWithGlobPatterns()
    {
        var options = new GlobbingOptions
        {
            Format = input => input.ToLowerInvariant()
        };

        Assert.True(GlobMatcher.IsMatch("ABC", "abc", options));
        Assert.True(GlobMatcher.IsMatch("AbC", "abc", options));
        Assert.True(GlobMatcher.IsMatch("abc", "abc", options));
    }

    [Fact]
    public void Format_ShouldWorkWithWildcards()
    {
        var options = new GlobbingOptions
        {
            Format = input => input.Replace("_", "/")
        };

        Assert.True(GlobMatcher.IsMatch("foo_bar", "foo/bar", options));
        Assert.True(GlobMatcher.IsMatch("foo_bar_baz", "foo/*/baz", options));
        Assert.True(GlobMatcher.IsMatch("foo_bar_baz", "**/baz", options));
    }

    [Fact]
    public void Format_ShouldTransformBeforeMatching()
    {
        bool formatCalled = false;
        var options = new GlobbingOptions
        {
            Format = input =>
            {
                formatCalled = true;
                return input;
            }
        };

        GlobMatcher.IsMatch("foo", "foo", options);
        Assert.True(formatCalled);
    }

    [Fact]
    public void Format_ShouldBeCalledWithInputString()
    {
        string? receivedInput = null;
        var options = new GlobbingOptions
        {
            Format = input =>
            {
                receivedInput = input;
                return input;
            }
        };

        GlobMatcher.IsMatch("test-input", "test-input", options);
        Assert.Equal("test-input", receivedInput);
    }

    [Fact]
    public void Format_Null_ShouldNotTransform()
    {
        var options = new GlobbingOptions
        {
            Format = null
        };

        Assert.True(GlobMatcher.IsMatch("abc", "abc", options));
        Assert.False(GlobMatcher.IsMatch("ABC", "abc", options));
    }

    [Fact]
    public void Format_ShouldWorkWithExtglobs()
    {
        var options = new GlobbingOptions
        {
            Format = input => input.ToLowerInvariant()
        };

        Assert.True(GlobMatcher.IsMatch("FOO", "@(foo|bar)", options));
        Assert.True(GlobMatcher.IsMatch("BAR", "@(foo|bar)", options));
        Assert.True(GlobMatcher.IsMatch("FOO", "!(baz)", options));
    }
}
