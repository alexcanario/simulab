using Simulab.DocGen;

namespace Simulab.ArchitectureTests.DocGen;

/// <summary>F-15: the whole set the tool writes, from the real models and the repository (AC1, AC7).</summary>
public class DocSetTests
{
    private static readonly string Root = SolutionAssemblies.RepositoryRoot();

    private static readonly DocGenOptions All = new(true, true, true, true);

    [Fact]
    public void Generate_ProducesTheFourDocumentsAndAnIndexLinkingThem()
    {
        var generated = DocSet.Generate(Root, "Simulab", All, EntityModelsTests.RealModels());

        generated.Keys.Should().Equal(
            "Catalog/data-dictionary.md", "Catalog/routes.md", "Catalog/schema.dbml",
            "Identity/data-dictionary.md", "Identity/routes.md", "Identity/schema.dbml",
            "Jobs/data-dictionary.md", "Jobs/schema.dbml",
            "README.md", "System/routes.md", "modules.md");
        foreach (var file in generated.Keys.Where(f => f != "README.md"))
        {
            generated["README.md"].Should().Contain($"- [{file}]({file})");
        }

        generated.Values.Should().OnlyContain(text => text.Contains("Do not edit", StringComparison.Ordinal) && !text.Contains('\r', StringComparison.Ordinal));
    }

    // F-26 AC1, AC2: a DBML schema per module in place of the Mermaid page, linked with the viewer that draws it.
    [Fact]
    public void Generate_WritesASchemaPerModuleLinkedWithItsViewer()
    {
        var generated = DocSet.Generate(Root, "Simulab", All, EntityModelsTests.RealModels());

        generated.Keys.Should().NotContain(k => k.EndsWith("entities.md", StringComparison.Ordinal));
        foreach (var module in new[] { "Identity", "Jobs" })
        {
            var file = $"{module}/schema.dbml";
            generated["README.md"].Should().Contain($"- [{file}]({file}) — {EntityModels.SchemaViewerHint}\n");
        }
    }

    [Fact]
    public void Generate_TwiceWithNoCodeChange_GivesTheSameText() =>
        DocSet.Generate(Root, "Simulab", All, EntityModelsTests.RealModels())
            .Should().Equal(DocSet.Generate(Root, "Simulab", All, EntityModelsTests.RealModels()));

    [Fact]
    public void Generate_HonoursTheOptions() =>
        DocSet.Generate(Root, "Simulab", new DocGenOptions(false, false, false, true), EntityModelsTests.RealModels())
            .Keys.Should().Equal("README.md", "modules.md");
}
