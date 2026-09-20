using System.Xml.Linq;

namespace Simulab.ArchitectureTests;

/// <summary>ADR-0001: no MassTransit, no RabbitMQ, no FluentAssertions. Checked in the package list and in the compiled references.</summary>
public class ForbiddenReferencesTests
{
    private static readonly string[] Forbidden = ["MassTransit", "RabbitMQ", "FluentAssertions"];

    private static bool IsForbidden(string name) =>
        Forbidden.Any(term => name.StartsWith(term, StringComparison.OrdinalIgnoreCase)
                              || name.Contains("." + term, StringComparison.OrdinalIgnoreCase));

    [Fact]
    public void The_rules_see_every_production_assembly()
    {
        SolutionAssemblies.All.Select(a => a.GetName().Name).Should().BeEquivalentTo(
            "Simulab.SharedKernel", "Simulab.Persistence", "Simulab.Email", "Simulab.Jobs", "Simulab.Api", "Simulab.Web", "Simulab.ServiceDefaults",
            "Simulab.Identity.Domain", "Simulab.Identity.Contracts", "Simulab.Identity.Application",
            "Simulab.Identity.Infrastructure", "Simulab.Identity.Api");
    }

    [Fact]
    public void No_production_assembly_references_a_forbidden_library()
    {
        var references = SolutionAssemblies.All
            .SelectMany(assembly => assembly.GetReferencedAssemblies().Select(r => (Assembly: assembly.GetName().Name, Reference: r.Name!)))
            .ToList();

        references.Should().NotBeEmpty("the rule must have looked at something");
        references.Where(r => IsForbidden(r.Reference)).Should().BeEmpty();
    }

    [Fact]
    public void No_forbidden_package_is_listed_centrally()
    {
        var file = Path.Combine(SolutionAssemblies.RepositoryRoot(), "Directory.Packages.props");
        var packages = XDocument.Load(file).Descendants("PackageVersion").Select(e => e.Attribute("Include")!.Value).ToList();

        packages.Should().Contain("AwesomeAssertions", "the rule must have read the real package list");
        packages.Where(IsForbidden).Should().BeEmpty();
    }

    [Fact]
    public void No_project_declares_a_package_version()
    {
        var root = SolutionAssemblies.RepositoryRoot();
        var projects = Directory.EnumerateFiles(root, "*.csproj", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .ToList();

        projects.Should().NotBeEmpty();
        var offenders = projects.Where(path => XDocument.Load(path).Descendants("PackageReference").Any(e => e.Attribute("Version") is not null));
        offenders.Should().BeEmpty("versions live in Directory.Packages.props");
    }
}
