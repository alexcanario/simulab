using System.Reflection;
using System.Xml.Linq;

namespace Simulab.ArchitectureTests;

/// <summary>AC13, AC14 (F-3): the building blocks stay where the profile puts them, and each module owns its schema.</summary>
public class BuildingBlockBoundaryTests
{
    private static Assembly SharedKernel => typeof(Simulab.SharedKernel.Entities.Entity).Assembly;

    private static Assembly Persistence => typeof(Simulab.Persistence.ModuleDbContext).Assembly;

    private static IReadOnlyList<string> References(Assembly assembly) =>
        [.. assembly.GetReferencedAssemblies().Select(reference => reference.Name!)];

    [Fact]
    public void SharedKernel_DoesNotDependOnEfCoreOrAspNet()
    {
        var references = References(SharedKernel);

        references.Should().NotBeEmpty("the rule must have looked at something");
        references.Should().NotContain(name =>
            name.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal)
            || name.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal)
            || name.StartsWith("Npgsql", StringComparison.Ordinal));
    }

    [Fact]
    public void Persistence_ReferencesOnlySharedKernelAmongSolutionProjects()
    {
        var solutionReferences = References(Persistence).Where(name => name.StartsWith("Simulab.", StringComparison.Ordinal));

        solutionReferences.Should().Equal("Simulab.SharedKernel");
    }

    /// <summary>F-39 BR7: the new block carries ASP.NET, so its solution references stay at SharedKernel.</summary>
    [Fact]
    public void ApiResults_ReferencesOnlySharedKernelAmongSolutionProjects()
    {
        var solutionReferences = References(typeof(Simulab.ApiResults.ApiProblem).Assembly)
            .Where(name => name.StartsWith("Simulab.", StringComparison.Ordinal));

        solutionReferences.Should().Equal("Simulab.SharedKernel");
    }

    [Fact]
    public void ModuleContexts_EachDeclareTheirOwnSchema()
    {
        var contexts = ModuleContextTypes().ToList();

        contexts.Should().NotBeEmpty("the rule must have found at least one module context");
        var schemas = contexts.Select(SchemaOf).ToList();
        schemas.Should().OnlyContain(schema => !string.IsNullOrWhiteSpace(schema));
        schemas.Should().OnlyHaveUniqueItems("two modules never share a schema");
    }

    [Fact]
    public void ModuleContexts_AllInheritTheBaseContext()
    {
        var contexts = DbContextTypes().ToList();

        contexts.Should().NotBeEmpty("the rule must have found at least one context");
        contexts.Should().OnlyContain(type => type.IsAssignableTo(typeof(Simulab.Persistence.ModuleDbContext)));
    }

    // The sample context of the persistence tests stands in for a module until the first module arrives (F-4).
    private static IEnumerable<Type> DbContextTypes() =>
        SolutionAssemblies.All.Append(SampleAssembly())
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => !type.IsAbstract && type.IsAssignableTo(typeof(Microsoft.EntityFrameworkCore.DbContext)));

    private static IEnumerable<Type> ModuleContextTypes() =>
        DbContextTypes().Where(type => type.IsAssignableTo(typeof(Simulab.Persistence.ModuleDbContext)));

    private static Assembly SampleAssembly() => typeof(Simulab.Persistence.Tests.SampleContext).Assembly;

    private static string SchemaOf(Type contextType) =>
        (string)contextType.GetProperty("Schema", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(contextType))!;

    [Fact]
    public void EveryProductionProject_IsListedInTheSolution()
    {
        var solution = XDocument.Load(Path.Combine(SolutionAssemblies.RepositoryRoot(), "Simulab.slnx"));
        var listed = solution.Descendants("Project").Select(project => project.Attribute("Path")!.Value.Replace('\\', '/')).ToList();

        listed.Should().Contain("src/BuildingBlocks/Simulab.Persistence/Simulab.Persistence.csproj");
        listed.Should().Contain("src/BuildingBlocks/Simulab.Email/Simulab.Email.csproj");
        listed.Should().Contain("src/BuildingBlocks/Simulab.Jobs/Simulab.Jobs.csproj");
        listed.Should().Contain("src/BuildingBlocks/Simulab.ApiResults/Simulab.ApiResults.csproj"); // F-39
    }
}
