using System.Linq;

namespace Snowberry.Globbing.Tests;

/// <summary>
/// Verifies that <see cref="GlobbingOptions.CompiledRegex"/> only affects performance, never
/// results: interpreted (<see langword="false"/>) and compiled (<see langword="true"/>) regexes
/// must produce identical matches for every pattern/input combination.
/// </summary>
public class CompiledRegexTests
{
    private static readonly string[] s_Patterns =
    [
        "*.js",
        "**/*.{js,ts,jsx,tsx}",
        "src/**/!(*.test|*.spec).{js,jsx,ts,tsx}",
        "!(*.md)",
        "test-[0-9][a-z].txt",
        "a/**/b/*.@(js|ts)",
        "+(foo|bar)/baz",
        "**/node_modules/**",
        "{a,b,c}/**/*.cs",
        "[[:alpha:]]*.log",
        "foo/**/bar/!(qux).js",
        "?(a|b)c*d.txt",
        "!*.md",
        "*"
    ];

    private static readonly string[] s_Inputs =
    [
        "app.js",
        "src/components/Button.tsx",
        "src/utils/helper.js",
        "src/utils/helper.test.js",
        "README.md",
        "test-3a.txt",
        "a/x/y/b/main.js",
        "foo/bar/qux.js",
        "foo/bar/baz.js",
        "node_modules/pkg/index.js",
        "b/deep/nested/file.cs",
        "alpha.log",
        "bc_d.txt",
        ".hidden"
    ];

    public static TheoryData<string> Patterns()
    {
        var data = new TheoryData<string>();
        foreach (string p in s_Patterns)
            data.Add(p);
        return data;
    }

    [Theory]
    [MemberData(nameof(Patterns))]
    public void InterpretedMatchesCompiledForAllInputs(string pattern)
    {
        var compiled = GlobMatcher.Create(pattern, new GlobbingOptions { CompiledRegex = true });
        var interpreted = GlobMatcher.Create(pattern, new GlobbingOptions { CompiledRegex = false });

        foreach (string input in s_Inputs)
            Assert.Equal(compiled(input), interpreted(input));
    }

    [Fact]
    public void GeneratedRegexSourceIsIdenticalRegardlessOfCompilation()
    {
        // CompiledRegex must not influence the generated pattern, only how it is compiled.
        foreach (string pattern in s_Patterns)
        {
            string a = GlobMatcher.MakeRe(pattern, new GlobbingOptions { CompiledRegex = true }).ToString();
            string b = GlobMatcher.MakeRe(pattern, new GlobbingOptions { CompiledRegex = false }).ToString();
            Assert.Equal(a, b);
        }
    }
}
