using System.Xml.Linq;

namespace Simulab.ArchitectureTests;

/// <summary>F-46: the production assemblies are derived from Simulab.slnx, and a project the rules cannot see fails the run.</summary>
public class ProductionProjectsTests
{
    private static XDocument Solution(params string[] paths) =>
        new(new XElement("Solution", paths.Select(path => new XElement("Folder", new XElement("Project", new XAttribute("Path", path))))));

    // AC1: today's solution gives every project of src/ and tools/ except the AppHost.
    [Fact]
    public void All_OnTheRealSolution_HoldsTheProductionProjectsExceptTheAppHost()
    {
        var names = SolutionAssemblies.All.Select(assembly => assembly.GetName().Name).ToList();

        names.Should().HaveCountGreaterThan(20, "the rules must see the whole solution");
        names.Should().Contain(["Simulab.SharedKernel", "Simulab.Web", "Simulab.Plans.Contracts", "Simulab.DocGen"]);
        names.Should().NotContain("Simulab.AppHost");
        names.Should().NotContain(name => name!.EndsWith(".Tests", StringComparison.Ordinal) || name.StartsWith("Simulab.Testing", StringComparison.Ordinal));
    }

    [Fact]
    public void Exempt_OnTheRealSolution_NamesEachProjectWithItsReason()
    {
        SolutionAssemblies.ExemptProjects.Keys.Should().Equal("Simulab.AppHost");
        SolutionAssemblies.ExemptProjects.Values.Should().OnlyContain(reason => !string.IsNullOrWhiteSpace(reason));
    }

    // AC1 (BR1, BR2): the name is the file name without the extension, from src/ and tools/ only.
    [Fact]
    public void Listed_TakesSrcAndToolsAndNamesThemByFile()
    {
        var solution = Solution(
            "src/Hosts/Simulab.Api/Simulab.Api.csproj",
            "tools/Simulab.DocGen/Simulab.DocGen.csproj",
            "src\\Modules\\Plans\\Simulab.Plans.Contracts\\Simulab.Plans.Contracts.csproj");

        ProductionProjects.Listed(solution).Should().Equal("Simulab.Api", "Simulab.DocGen", "Simulab.Plans.Contracts");
    }

    // AC4 (BR1): projects under tests/ are never production.
    [Fact]
    public void Listed_NeverTakesAProjectUnderTests()
    {
        var solution = Solution(
            "src/Hosts/Simulab.Api/Simulab.Api.csproj",
            "tests/Simulab.ArchitectureTests/Simulab.ArchitectureTests.csproj",
            "tests/Hosts/Simulab.Api.Tests/Simulab.Api.Tests.csproj");

        ProductionProjects.Listed(solution).Should().Equal("Simulab.Api");
    }

    [Fact]
    public void Selected_LeavesOutTheExemptProjects() =>
        ProductionProjects.Selected(["Simulab.Api", "Simulab.AppHost"], ["Simulab.AppHost"]).Should().Equal("Simulab.Api");

    // AC2 (BR5): a project the tests cannot load is named with the file to edit.
    [Fact]
    public void Unresolved_NamesTheProjectAndTheProjectReferenceToAdd()
    {
        var unresolved = ProductionProjects.Unresolved(["Simulab.Api", "Simulab.Reports"], name => name == "Simulab.Api");

        unresolved.Should().Equal("Simulab.Reports");
        ProductionProjects.UnresolvedMessage(unresolved)
            .Should().Contain("Simulab.Reports")
            .And.Contain("ProjectReference")
            .And.Contain("tests/Simulab.ArchitectureTests/Simulab.ArchitectureTests.csproj");
    }

    [Fact]
    public void Unresolved_WhenEveryProjectLoads_IsEmpty() =>
        ProductionProjects.Unresolved(["Simulab.Api"], _ => true).Should().BeEmpty();

    // AC2: the real loader answers false for a name no output folder holds.
    [Fact]
    public void Unresolved_WithTheRealLoader_NamesAProjectNoOutputFolderHolds()
    {
        var unresolved = ProductionProjects.Unresolved(["Simulab.DoesNotExist"], TryLoad);

        unresolved.Should().Equal("Simulab.DoesNotExist");
    }

    // AC3 (BR6): an exemption for a project the solution does not list is stale.
    [Fact]
    public void StaleExemptions_NamesTheExemptionAndTheEntryToRemove()
    {
        var stale = ProductionProjects.StaleExemptions(["Simulab.Api"], ["Simulab.AppHost"]);

        stale.Should().Equal("Simulab.AppHost");
        ProductionProjects.StaleExemptionMessage(stale)
            .Should().Contain("Simulab.AppHost")
            .And.Contain("SolutionAssemblies.ExemptProjects");
    }

    [Fact]
    public void StaleExemptions_WhenTheProjectIsListed_IsEmpty() =>
        ProductionProjects.StaleExemptions(["Simulab.Api", "Simulab.AppHost"], ["Simulab.AppHost"]).Should().BeEmpty();

    private static bool TryLoad(string name)
    {
        try
        {
            System.Reflection.Assembly.Load(new System.Reflection.AssemblyName(name));
            return true;
        }
        catch (FileNotFoundException)
        {
            return false;
        }
    }
}
