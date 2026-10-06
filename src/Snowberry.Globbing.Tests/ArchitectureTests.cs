using ArchUnitNET.Domain;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnitV3;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Snowberry.Globbing.Tests;

public class ArchitectureTests
{
    private const string c_Root = "Snowberry.Globbing";
    private const string c_Compilation = "Snowberry.Globbing.Compilation";
    private const string c_Syntax = "Snowberry.Globbing.Syntax";
    private const string c_Utilities = "Snowberry.Globbing.Utilities";

    private static readonly Architecture s_Architecture = new ArchLoader().LoadAssemblies(typeof(Glob).Assembly).Build();

    [Fact]
    public void PublicTypes_ResideInTheRootNamespace()
    {
        Types().That().ArePublic()
            .Should().ResideInNamespace(c_Root)
            .Check(s_Architecture);
    }

    [Fact]
    public void TypesInSubNamespaces_AreInternal()
    {
        Types().That().ResideInNamespaceMatching(@"^Snowberry\.Globbing\..+")
            .Should().BeInternal()
            .Check(s_Architecture);
    }

    [Fact]
    public void PublicClasses_AreSealed()
    {
        Classes().That().ArePublic()
            .Should().BeSealed()
            .Check(s_Architecture);
    }

    [Fact]
    public void LibraryTypes_AreNotNested()
    {
        Types().That().ResideInNamespaceMatching(@"^Snowberry\.Globbing(\..+)?$").And().DoNotHaveNameMatching("<")
            .Should().NotBeNested()
            .Check(s_Architecture);
    }

    [Fact]
    public void Utilities_DependOnNoOtherLibraryNamespace()
    {
        Types().That().ResideInNamespace(c_Utilities)
            .Should().NotDependOnAnyTypesThat().ResideInNamespaceMatching(@"^Snowberry\.Globbing(\.(Syntax|Compilation))?$")
            .Check(s_Architecture);
    }

    [Fact]
    public void Syntax_DoesNotDependOnCompilation()
    {
        Types().That().ResideInNamespace(c_Syntax)
            .Should().NotDependOnAnyTypesThat().ResideInNamespace(c_Compilation)
            .Check(s_Architecture);
    }

    [Fact]
    public void SyntaxAndCompilation_DoNotDependOnTheMatcher()
    {
        Types().That().ResideInNamespace(c_Syntax).Or().ResideInNamespace(c_Compilation)
            .Should().NotDependOnAny(typeof(Glob), typeof(CompiledPattern), typeof(GlobMatch))
            .Check(s_Architecture);
    }
}