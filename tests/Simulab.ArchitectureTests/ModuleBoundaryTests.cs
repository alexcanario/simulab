using System.Reflection;
using System.Xml.Linq;

namespace Simulab.ArchitectureTests;

/// <summary>
/// AC14 (F-4), AC1 (F-33): the five projects of a module keep the profile's direction of dependency.
/// Written for Identity and generalized when Catalog became the second module — the rules belong to the
/// profile, not to one module.
/// </summary>
public class ModuleBoundaryTests
{
    /// <summary>The five assemblies of a module, by the profile's layer names.</summary>
    public sealed record Module(string Name, Assembly Domain, Assembly Application, Assembly Contracts, Assembly Infrastructure, Assembly Api)
    {
        public override string ToString() => Name;
    }

    private static readonly Module Identity = new(
        "Identity",
        typeof(Simulab.Identity.Domain.Entities.User).Assembly,
        typeof(Simulab.Identity.Application.Registration.RegisterUserHandler).Assembly,
        typeof(Simulab.Identity.Contracts.IdentityErrorCodes).Assembly,
        typeof(Simulab.Identity.Infrastructure.IdentityModule).Assembly,
        typeof(Simulab.Identity.Api.IdentityEndpoints).Assembly);

    private static readonly Module Catalog = new(
        "Catalog",
        typeof(Simulab.Catalog.Domain.Entities.Organizer).Assembly,
        typeof(Simulab.Catalog.Application.Organizers.SaveOrganizerHandler).Assembly,
        typeof(Simulab.Catalog.Contracts.CatalogErrorCodes).Assembly,
        typeof(Simulab.Catalog.Infrastructure.CatalogModule).Assembly,
        typeof(Simulab.Catalog.Api.CatalogEndpoints).Assembly);

    public static TheoryData<Module> Modules => [Identity, Catalog];

    private static IReadOnlyList<Module> AllModules => [Identity, Catalog];

    private static Assembly Web => typeof(Simulab.Web.Resources.SharedResources).Assembly;

    private static IReadOnlyList<string> References(Assembly assembly) =>
        [.. assembly.GetReferencedAssemblies().Select(reference => reference.Name!)];

    private static bool IsEfCoreOrAspNetCore(string name) =>
        name.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal)
        || name.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal)
        || name.StartsWith("Npgsql", StringComparison.Ordinal)
        // F-5, AC8: OpenIddict and the Redis client are Infrastructure/Api concerns; the session store is
        // only an interface (Simulab.Identity.Application.Sessions.IRefreshSessionStore) in Application.
        || name.StartsWith("OpenIddict", StringComparison.Ordinal)
        || name.StartsWith("StackExchange.Redis", StringComparison.Ordinal)
        // F-39: the first building block that carries ASP.NET. Without this line a Domain or an
        // Application project could reference it and bring ASP.NET in past the rule above.
        || name.Equals("Simulab.ApiResults", StringComparison.Ordinal);

    [Theory]
    [MemberData(nameof(Modules))]
    public void Domain_DoesNotDependOnEfCoreOrAspNetCore(Module module)
    {
        ArgumentNullException.ThrowIfNull(module);

        // Microsoft.Extensions.Identity.* is neither: it carries IdentityUser and UserManager, which the
        // profile puts in the domain and the use cases (ADR-0001, decision 12).
        var references = References(module.Domain);

        references.Should().NotBeEmpty("the rule must have looked at something");
        references.Should().NotContain(name => IsEfCoreOrAspNetCore(name));
    }

    [Theory]
    [MemberData(nameof(Modules))]
    public void Application_DoesNotDependOnEfCoreOrAspNetCore(Module module)
    {
        ArgumentNullException.ThrowIfNull(module);

        var references = References(module.Application);

        references.Should().NotBeEmpty("the rule must have looked at something");
        references.Should().NotContain(name => IsEfCoreOrAspNetCore(name));
    }

    [Theory]
    [MemberData(nameof(Modules))]
    public void Contracts_ReferencesNoSolutionProjectBesidesSharedKernel(Module module)
    {
        ArgumentNullException.ThrowIfNull(module);

        // The compiler drops a reference no type uses, so the assertion is about what may appear, not
        // about what must: contracts are records, and today they need nothing from SharedKernel.
        var solutionReferences = References(module.Contracts)
            .Where(name => name.StartsWith("Simulab.", StringComparison.Ordinal))
            .ToList();

        solutionReferences.Should().OnlyContain(name => name == "Simulab.SharedKernel");
    }

    [Theory]
    [MemberData(nameof(Modules))]
    public void Application_ReferencesItsOwnDomainAndContractsOnly(Module module)
    {
        ArgumentNullException.ThrowIfNull(module);

        var solutionReferences = References(module.Application)
            .Where(name => name.StartsWith("Simulab.", StringComparison.Ordinal))
            .Order()
            .ToList();

        solutionReferences.Should().Equal($"Simulab.{module.Name}.Contracts", $"Simulab.{module.Name}.Domain", "Simulab.SharedKernel");
    }

    /// <summary>A module reaches another module only through its Contracts project (profile).</summary>
    [Theory]
    [MemberData(nameof(Modules))]
    public void EveryLayer_ReachesAnotherModuleOnlyThroughItsContracts(Module module)
    {
        ArgumentNullException.ThrowIfNull(module);

        var others = AllModules.Where(other => other.Name != module.Name).Select(other => other.Name).ToList();
        others.Should().NotBeEmpty("the rule must have looked at something");

        foreach (var layer in new[] { module.Domain, module.Application, module.Contracts, module.Infrastructure, module.Api })
        {
            var crossing = References(layer)
                .Where(name => others.Exists(other => name.StartsWith($"Simulab.{other}.", StringComparison.Ordinal)))
                .ToList();

            crossing.Should().OnlyContain(
                name => name.EndsWith(".Contracts", StringComparison.Ordinal),
                "{0} may only reach another module through its contracts",
                layer.GetName().Name!);
        }
    }

    [Fact]
    public void Web_ReferencesContractsAndNoModuleProject()
    {
        foreach (var module in AllModules)
        {
            var moduleReferences = References(Web)
                .Where(name => name.StartsWith($"Simulab.{module.Name}", StringComparison.Ordinal))
                .ToList();

            // The Web talks to a module through its contracts only.
            moduleReferences.Should().Equal($"Simulab.{module.Name}.Contracts");
        }
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

    [Theory]
    [MemberData(nameof(Modules))]
    public void EveryProjectOfTheModule_IsListedInTheSolution(Module module)
    {
        ArgumentNullException.ThrowIfNull(module);

        var solution = XDocument.Load(Path.Combine(SolutionAssemblies.RepositoryRoot(), "Simulab.slnx"));
        var listed = solution.Descendants("Project").Select(project => project.Attribute("Path")!.Value.Replace('\\', '/')).ToList();

        foreach (var layer in new[] { "Api", "Application", "Contracts", "Domain", "Infrastructure" })
        {
            listed.Should().Contain($"src/Modules/{module.Name}/Simulab.{module.Name}.{layer}/Simulab.{module.Name}.{layer}.csproj");
        }

        listed.Should().Contain($"tests/Modules/{module.Name}/Simulab.{module.Name}.Tests/Simulab.{module.Name}.Tests.csproj");
    }
}
