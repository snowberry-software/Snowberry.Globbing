namespace Snowberry.Globbing.Tests;

/// <summary>
/// Tests for options.onMatch callback.
/// Ported from: https://github.com/micromatch/picomatch/blob/master/test/options.onMatch.js
/// </summary>
public class OptionsOnMatchTests
{
    [Fact]
    public void OnMatch_ShouldBeCalledWhenMatched()
    {
        bool called = false;
        var options = new GlobbingOptions
        {
            OnMatch = (result) =>
            {
                called = true;
            }
        };

        GlobMatcher.IsMatch("foo", "foo", options);
        Assert.True(called);
    }

    [Fact]
    public void OnMatch_ShouldNotBeCalledWhenNotMatched()
    {
        bool called = false;
        var options = new GlobbingOptions
        {
            OnMatch = (result) =>
            {
                called = true;
            }
        };

        GlobMatcher.IsMatch("foo", "bar", options);
        Assert.False(called);
    }

    [Fact]
    public void OnMatch_ShouldReceiveMatchedString()
    {
        string? receivedInput = null;
        var options = new GlobbingOptions
        {
            OnMatch = (result) =>
            {
                receivedInput = result.Input;
            }
        };

        GlobMatcher.IsMatch("test-file.txt", "*.txt", options);
        Assert.Equal("test-file.txt", receivedInput);
    }

    [Fact]
    public void OnMatch_ShouldReceiveGlob()
    {
        string? receivedGlob = null;
        var options = new GlobbingOptions
        {
            OnMatch = (result) =>
            {
                receivedGlob = result.Glob;
            }
        };

        GlobMatcher.IsMatch("foo", "f*", options);
        Assert.Equal("f*", receivedGlob);
    }

    [Fact]
    public void OnMatch_ShouldWorkWithWildcards()
    {
        int matchCount = 0;
        var options = new GlobbingOptions
        {
            OnMatch = (result) =>
            {
                matchCount++;
            }
        };

        Assert.True(GlobMatcher.IsMatch("foo.txt", "*.txt", options));
        Assert.Equal(1, matchCount);

        Assert.True(GlobMatcher.IsMatch("bar.txt", "*.txt", options));
        Assert.Equal(2, matchCount);
    }

    [Fact]
    public void OnMatch_ShouldWorkWithExtglobs()
    {
        var matched = new List<string>();
        var options = new GlobbingOptions
        {
            OnMatch = (result) =>
            {
                matched.Add(result.Input);
            }
        };

        GlobMatcher.IsMatch("foo", "@(foo|bar)", options);
        GlobMatcher.IsMatch("bar", "@(foo|bar)", options);
        GlobMatcher.IsMatch("baz", "@(foo|bar)", options);

        Assert.Equal(2, matched.Count);
        Assert.Contains("foo", matched);
        Assert.Contains("bar", matched);
    }

    [Fact]
    public void OnMatch_ShouldWorkWithGlobstars()
    {
        var matched = new List<string>();
        var options = new GlobbingOptions
        {
            OnMatch = (result) =>
            {
                matched.Add(result.Input);
            }
        };

        GlobMatcher.IsMatch("a/b/c.txt", "**/*.txt", options);
        GlobMatcher.IsMatch("a/b/c.js", "**/*.txt", options);

        Assert.Single(matched);
        Assert.Contains("a/b/c.txt", matched);
    }

    [Fact]
    public void OnMatch_Null_ShouldNotThrow()
    {
        var options = new GlobbingOptions
        {
            OnMatch = null
        };

        // Should not throw
        bool result = GlobMatcher.IsMatch("foo", "foo", options);
        Assert.True(result);
    }
}
