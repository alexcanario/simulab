using Simulab.DocGen;

namespace Simulab.ArchitectureTests.DocGen;

/// <summary>
/// F-45: what DocGen documents and what the tests load are the same set, by construction. F-24 found them four
/// weeks apart — <c>docs/architecture/Ai/</c> was generated since F-41 and every test built on the hand-written
/// list was blind to <c>ai_calls</c> — so the drift is what these tests watch, in both directions.
/// </summary>
public class DocGenModelDriftTests
{
    private static readonly DocGenOptions All = new(true, true, true, true);

    private static string ArchitectureDirectory() =>
        Path.Combine(SolutionAssemblies.RepositoryRoot(), "docs", "architecture");

    // AC1: the list comes from DocGen's own references, and it names exactly today's modules.
    [Fact]
    public void TheDerivedList_NamesEveryModuleDocGenReferences()
    {
        EntityModelsTests.DocGenProjects().Should().Contain("Simulab.Ai")
            .And.Contain("Simulab.Jobs")
            .And.Contain("Simulab.Catalog.Infrastructure")
            .And.Contain("Simulab.Identity.Infrastructure")
            .And.Contain("Simulab.Persistence", "the closure follows a reference through another project");

        EntityModelsTests.RealModels().Select(m => m.Module).Should().Equal("Ai", "Catalog", "Identity", "Jobs");
    }

    // AC1: nothing that only the test project sees comes in with it.
    [Fact]
    public void TheDerivedList_LeavesOutTheContextsDocGenNeverSees()
    {
        EntityModelsTests.DocGenAssemblies().Select(a => a.GetName().Name)
            .Should().NotContain("Simulab.ArchitectureTests", "SharedTableDbContext is a fixture of these tests")
            .And.NotContain("Simulab.Persistence.Tests", "its sample context is no module");

        EntityModelsTests.RealModels().Select(m => m.Module).Should().NotContain("SharedTable");
    }

    // AC2: the two sides agree on today's solution.
    [Fact]
    public void TheModulesOnDisk_AreTheModulesTheTestsLoad()
    {
        var documented = DocumentedModules.In(ArchitectureDirectory(), All);
        var loaded = EntityModelsTests.RealModels().Select(m => m.Module).ToList();

        documented.Should().NotBeEmpty("the rule must have read the generated folders");
        DocumentedModules.Drift(documented, loaded).Should().BeEmpty();
        documented.Should().Equal(loaded);
    }

    // AC2: System/ holds routes only, so it is no module; a module folder is one with a schema or a dictionary.
    [Fact]
    public void TheModulesOnDisk_AreTheFoldersWithASchemaOrADictionary()
    {
        DocumentedModules.In(ArchitectureDirectory(), All)
            .Should().Equal("Ai", "Catalog", "Identity", "Jobs")
            .And.NotContain("System");
    }

    // AC3: the F-24 drift, reproduced — a documented module no test loads.
    [Fact]
    public void Drift_NamesADocumentedModuleNoTestLoads()
    {
        var drift = DocumentedModules.Drift(["Ai", "Identity"], ["Identity"]);

        drift.Should().HaveCount(1);
        drift[0].Should().StartWith("Ai: DocGen documents docs/architecture/Ai/ and no test loads its model")
            .And.Contain("tools/Simulab.DocGen/Simulab.DocGen.csproj");
    }

    // AC3, other direction: a loaded model nobody documented.
    [Fact]
    public void Drift_NamesALoadedModuleWithNoGeneratedFolder()
    {
        var drift = DocumentedModules.Drift(["Identity"], ["Identity", "Catalog"]);

        drift.Should().HaveCount(1);
        drift[0].Should().StartWith("Catalog: the tests load a model for Catalog and docs/architecture/Catalog/ does not exist")
            .And.Contain("dotnet run --project tools/Simulab.DocGen");
    }

    [Fact]
    public void Drift_OnTheSameModules_IsEmpty() =>
        DocumentedModules.Drift(["Ai", "Identity"], ["Identity", "Ai"]).Should().BeEmpty();

    // AC4: a project DocGen references that no loaded assembly answers for is named with the file to edit.
    [Fact]
    public void Unresolved_NamesTheProjectAndTheFileToEdit()
    {
        var unresolved = DocGenProjectReferences.Unresolved(
            ["Simulab.Ai", "Simulab.Reports"],
            ["Simulab.Ai", "Simulab.Jobs"]);

        unresolved.Should().Equal("Simulab.Reports");
        DocGenProjectReferences.UnresolvedMessage(unresolved)
            .Should().Contain("Simulab.Reports")
            .And.Contain("tests/Simulab.ArchitectureTests/Simulab.ArchitectureTests.csproj")
            .And.Contain("SolutionAssemblies.All");
    }

    [Fact]
    public void Unresolved_OnAClosureEveryAssemblyAnswersFor_IsEmpty() =>
        DocGenProjectReferences.Unresolved(["Simulab.Ai"], ["Simulab.Ai", "Simulab.Jobs"]).Should().BeEmpty();

    // AC4: the closure is read from the real project files, transitively and without repeats.
    [Fact]
    public void Closure_FollowsReferencesThroughAnotherProjectWithoutRepeating()
    {
        var projects = EntityModelsTests.DocGenProjects();

        projects.Should().OnlyHaveUniqueItems().And.NotContain("Simulab.DocGen", "a project is not its own reference");
        projects.Should().Contain("Simulab.Email", "Simulab.Jobs references it and DocGen references Simulab.Jobs")
            .And.Contain("Simulab.SharedKernel", "three levels down")
            .And.NotContain("Simulab.Web", "DocGen references no host");
    }

    // AC5: with entities and the data dictionary off, DocGen writes no module folder and none is expected.
    [Fact]
    public void WithEntitiesAndTheDictionaryOff_NoModuleFolderIsExpected()
    {
        var options = new DocGenOptions(Entities: false, DataDictionary: false, Routes: true, Modules: true);

        DocumentedModules.In(ArchitectureDirectory(), options).Should().BeEmpty();
        DocumentedModules.Drift(DocumentedModules.In(ArchitectureDirectory(), options), []).Should().BeEmpty();
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void WithEitherDocumentOn_TheModulesAreRead(bool entities, bool dataDictionary) =>
        DocumentedModules.In(ArchitectureDirectory(), new DocGenOptions(entities, dataDictionary, Routes: true, Modules: true))
            .Should().Equal("Ai", "Catalog", "Identity", "Jobs");

    // AC5: the options the tool really runs with keep the comparison on.
    [Fact]
    public void TheCommittedOptions_KeepTheComparisonOn()
    {
        var options = DocGenOptions.Read(
            Path.Combine(SolutionAssemblies.RepositoryRoot(), "tools", "Simulab.DocGen", "docgen.json"));

        (options.Entities || options.DataDictionary).Should().BeTrue();
    }
}
