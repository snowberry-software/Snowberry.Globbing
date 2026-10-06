namespace Snowberry.Globbing.Tests;

public class RealWorldScenariosTests
{
    private static readonly GlobOptions s_Posix = new() { PathStyle = GlobPathStyle.Posix };
    private static readonly GlobOptions s_Windows = new() { PathStyle = GlobPathStyle.Windows };

    private static readonly Glob s_WebBuild = new(
        ["src/**/*.{js,jsx,ts,tsx}", "*.json"],
        s_Windows with { IgnorePatterns = ["**/*.test.*", "**/*.spec.*", "**/__tests__/**", "**/node_modules/**", "**/*.d.ts"] });

    private static readonly Glob s_CSharpSources = new(
        ["*.cs", "*.csproj"],
        s_Windows with { MatchFileNameOnly = true, IgnoreCase = true, IgnorePatterns = ["*.Designer.cs", "AssemblyInfo.cs"] });

    private static readonly Glob s_SourcesOnly = new(
        "**",
        s_Posix with { IgnorePatterns = ["**/{bin,obj}/**", "!**/*.{cs,csproj,sln}"] });

    [Theory]
    [InlineData("src/app.tsx", true)]
    [InlineData(@"src\components\Button.jsx", true)]
    [InlineData(@"src\utils\helpers.ts", true)]
    [InlineData("package.json", true)]
    [InlineData(@"config\app.json", false)]
    [InlineData(@"src\styles\main.css", false)]
    [InlineData(@"src\.cache\chunk.js", false)]
    [InlineData(@"src\app.test.tsx", false)]
    [InlineData("src/utils/api.spec.ts", false)]
    [InlineData(@"src\__tests__\setup.js", false)]
    [InlineData(@"src\types\index.d.ts", false)]
    [InlineData(@"src\node_modules\react\index.js", false)]
    public void WebBuild_IncludesSourcesAndExcludesTestsAndDependencies(string input, bool expected)
    {
        s_WebBuild.IsMatch(input).Should().Be(expected);
    }

    [Theory]
    [InlineData(@"src\App\Program.cs", true)]
    [InlineData(@"src\App\App.csproj", true)]
    [InlineData("src/App/Main.CS", true)]
    // Matching only the file name means directories such as obj cannot be excluded.
    [InlineData(@"obj\Debug\Generated.cs", true)]
    [InlineData(@"src\App\Form1.Designer.cs", false)]
    [InlineData(@"src\App\FORM1.DESIGNER.CS", false)]
    [InlineData(@"src\App\Properties\AssemblyInfo.cs", false)]
    [InlineData(@"src\App\.hidden.cs", false)]
    [InlineData(@"src\App\readme.md", false)]
    public void FileNameOnly_AppliesIgnoreCaseAndIgnorePatternsToTheFileName(string input, bool expected)
    {
        s_CSharpSources.IsMatch(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("App.sln", true)]
    [InlineData("src/App/Program.cs", true)]
    [InlineData("src/App/App.csproj", true)]
    [InlineData("src/App/appsettings.json", false)]
    [InlineData("README.md", false)]
    [InlineData("bin/Release/App.cs", false)]
    [InlineData("src/App/obj/Debug/App.AssemblyInfo.cs", false)]
    [InlineData(".vs/App/state.cs", false)]
    public void NegatedIgnorePattern_IgnoresEverythingWithoutAListedExtension(string input, bool expected)
    {
        s_SourcesOnly.IsMatch(input).Should().Be(expected);
    }

    [Fact]
    public void Monorepo_FilterKeepsSelectedPackageSourcesInOrder()
    {
        var glob = new Glob("packages/{core,utils}/src/**/*.ts", s_Posix with { IgnorePatterns = ["**/*.test.ts"] });
        string[] files =
        [
            "packages/core/src/index.ts",
            "packages/docs/src/index.ts",
            "packages/utils/src/deep/nested/helpers.ts",
            "packages/core/src/index.test.ts",
            "packages/core/tests/index.ts",
            "packages/utils/src/types.d.ts",
            "tools/build.ts"
        ];
        string[] expected =
        [
            "packages/core/src/index.ts",
            "packages/utils/src/deep/nested/helpers.ts",
            "packages/utils/src/types.d.ts"
        ];

        glob.Filter(files).Should().Equal(expected);
    }
}
