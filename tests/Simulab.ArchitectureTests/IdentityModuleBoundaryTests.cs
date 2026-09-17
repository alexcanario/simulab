using System.Reflection;
using System.Xml.Linq;

namespace Simulab.ArchitectureTests;

/// <summary>AC14 (F-4): the five projects of the Identity module keep the profile's direction of dependency.</summary>
public class IdentityModuleBoundaryTests
{
    private static Assembly Domain => typeof(Simulab.Identity.Domain.Entities.User).Assembly;

    private static Assembly Application => typeof(Simulab.Identity.Application.Registration.RegisterUserHandler).Assembly;

    private static Assembly Contracts => typeof(Simulab.Identity.Contracts.IdentityErrorCodes).Assembly;

    private static Assembly Web => typeof(Simulab.Web.Resources.SharedResources).Assembly;

    private static IReadOnlyList<string> References(Assembly assembly) =>
        [.. assembly.GetReferencedAssemblies().Select(reference => reference.Name!)];

    private static bool IsEfCoreOrAspNetCore(string name) =>
        name.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal)
        || name.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal)
        || name.StartsWith("Npgsql", StringComparison.Ordinal);

    [Theory]
    [InlineData("Domain")]
    [InlineData("Application")]
    public void InnerLayers_DoNotDependOnEfCoreOrAspNetCore(string layer)
    {
        // Microsoft.Extensions.Identity.* is neither: it carries IdentityUser and UserManager, which the
        // profile puts in the domain and the use cases (ADR-0001, decision 12).
        var references = References(layer == "Domain" ? Domain : Application);

        references.Should().NotBeEmpty("the rule must have looked at something");
        references.Should().NotContain(name => IsEfCoreOrAspNetCore(name));
    }

    [Fact]
    public void Contracts_ReferencesNoSolutionProjectBesidesSharedKernel()
    {
        // The compiler drops a reference no type uses, so the assertion is about what may appear, not
        // about what must: contracts are records, and today they need nothing from SharedKernel.
        var solutionReferences = References(Contracts)
            .Where(name => name.StartsWith("Simulab.", StringComparison.Ordinal))
            .ToList();

        solutionReferences.Should().OnlyContain(name => name == "Simulab.SharedKernel");
    }

    [Fact]
    public void Web_ReferencesContractsAndNoModuleProject()
    {
        var moduleReferences = References(Web)
            .Where(name => name.StartsWith("Simulab.Identity", StringComparison.Ordinal))
            .ToList();

        // The Web talks to a module through its contracts only.
        moduleReferences.Should().Equal("Simulab.Identity.Contracts");
    }

    [Fact]
    public void Application_ReferencesDomainAndContractsOnly()
    {
        var solutionReferences = References(Application)
            .Where(name => name.StartsWith("Simulab.", StringComparison.Ordinal))
            .Order()
            .ToList();

        solutionReferences.Should().Equal("Simulab.Identity.Contracts", "Simulab.Identity.Domain", "Simulab.SharedKernel");
    }

    [Fact]
    public void User_IsTheDeclaredExceptionToTheTenantEntityRule()
    {
        var user = typeof(Simulab.Identity.Domain.Entities.User);

        user.IsAssignableTo(typeof(Simulab.SharedKernel.Entities.TenantEntity)).Should().BeFalse(
            "it inherits IdentityUser<Guid> instead");
        user.IsAssignableTo(typeof(Simulab.SharedKernel.Entities.IAuditableEntity)).Should().BeTrue();
        user.IsAssignableTo(typeof(Simulab.SharedKernel.Entities.ISoftDeletableEntity)).Should().BeTrue();
        user.GetProperty("TenantId").Should().NotBeNull("it repeats the tenant field by hand");
    }

    [Fact]
    public void EveryIdentityProject_IsListedInTheSolution()
    {
        var solution = XDocument.Load(Path.Combine(SolutionAssemblies.RepositoryRoot(), "Simulab.slnx"));
        var listed = solution.Descendants("Project").Select(project => project.Attribute("Path")!.Value.Replace('\\', '/')).ToList();

        foreach (var layer in new[] { "Api", "Application", "Contracts", "Domain", "Infrastructure" })
        {
            listed.Should().Contain($"src/Modules/Identity/Simulab.Identity.{layer}/Simulab.Identity.{layer}.csproj");
        }

        listed.Should().Contain("tests/Simulab.Identity.Tests/Simulab.Identity.Tests.csproj");
    }
}
