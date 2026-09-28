using System.ComponentModel;
using Microsoft.Extensions.AI;
using Simulab.Ai;
using Simulab.DocGen;

namespace Simulab.ArchitectureTests.DocGen;

/// <summary>F-49: the tools catalogue — what <c>--check</c> refuses and what <c>tools.md</c> says (AC2 to AC10).</summary>
public class ToolCatalogueDocTests
{
    // Types the catalogue may look at: the fixtures, plus the factory that builds every schema.
    private static IReadOnlyCollection<Type> Types(params Type[] fixtures) => [.. fixtures, typeof(AIFunctionFactory)];

    private static IReadOnlyList<string> ProblemsOf(params Type[] fixtures) =>
        Assert.Throws<ToolCatalogueException>(() => ToolCatalogueDoc.Render(Types(fixtures))).Problems;

    // AC1: the attribute lives in Simulab.Ai, where the gateway is.
    [Fact]
    public void ModelToolAttribute_LivesInTheAiBuildingBlock() =>
        typeof(ModelToolAttribute).Assembly.GetName().Name.Should().Be("Simulab.Ai");

    // AC2, AC7: one tool fully declared gives no problem and one row with the five facts.
    [Fact]
    public void Render_OneFullyDeclaredTool_ListsItsNameDescriptionSchemaPermissionsAndReaches()
    {
        var text = ToolCatalogueDoc.Render(Types(typeof(GoodTool)));

        text.Should().Contain("1 tool.");
        text.Should().Contain("| [GetExamAsync](#getexamasync) | no | n/a | `read_exams` | `ExamService` |");
        text.Should().Contain("Gets exam details.");
        text.Should().Contain("| `examId` | string | yes | The exam id. | — |");
        text.Should().Contain("\"examId\"");
        text.Should().Contain("Do not edit");
    }

    // AC3
    [Fact]
    public void Render_ToolWithoutDescription_FailsNamingTheMethod() =>
        ProblemsOf(typeof(NoDescription)).Should().ContainSingle()
            .Which.Should().Contain("NoDescription.Run").And.Contain("no description");

    // AC3 (BR2): every parameter the model fills is described, not only the method.
    [Fact]
    public void Render_ToolWithAnUndescribedParameter_FailsNamingIt() =>
        ProblemsOf(typeof(NoParameterDescription)).Should().ContainSingle()
            .Which.Should().Contain("parameter \"examId\"");

    // AC4
    [Fact]
    public void Render_ToolWithNoPermissions_Fails() =>
        ProblemsOf(typeof(NoPermissions)).Should().ContainSingle().Which.Should().Contain("no Permissions");

    [Fact]
    public void Render_ToolOpenToEverySignedInUser_PassesWhenItSaysSo() =>
        ToolCatalogueDoc.Render(Types(typeof(OpenTool))).Should().Contain("`authenticated`");

    // AC5
    [Fact]
    public void Render_ToolWithNoReaches_Fails() =>
        ProblemsOf(typeof(NoReaches)).Should().ContainSingle().Which.Should().Contain("no Reaches");

    // AC6
    [Fact]
    public void Render_TwoToolsWithTheSameDerivedName_Fails() =>
        ProblemsOf(typeof(GoodTool), typeof(SameNameAsGoodTool)).Should().ContainSingle()
            .Which.Should().Contain("\"GetExamAsync\"").And.Contain("GoodTool").And.Contain("SameNameAsGoodTool");

    [Fact]
    public void Render_ExplicitName_OverridesTheDerivedOne() =>
        ToolCatalogueDoc.Render(Types(typeof(GoodTool), typeof(RenamedTool)))
            .Should().Contain("[GetExamAsync](#getexamasync)").And.Contain("[GetExamDetails](#getexamdetails)").And.Contain("2 tools.");

    // AC10: no tool leaves a header and an empty table, not a missing file.
    [Fact]
    public void Render_WithNoTool_GivesAHeaderAndAnEmptyTable()
    {
        var text = ToolCatalogueDoc.Render(Types());

        text.Should().Contain("# Tools offered to the model").And.Contain("0 tools.")
            .And.Contain("| Tool | Writes | Confirms | Permissions | Reaches |\n|---|---|---|---|---|\n");
        text.Should().NotContain("\n## ");
    }

    // AC8: the same code gives the same text, so a second run is up to date.
    [Fact]
    public void Render_Twice_GivesTheSameTextWithoutCarriageReturns()
    {
        var first = ToolCatalogueDoc.Render(Types(typeof(GoodTool)));

        first.Should().Be(ToolCatalogueDoc.Render(Types(typeof(GoodTool))));
        first.Should().NotContain("\r");
    }

    // AC7, AC8, AC9: through the set --check compares with the files on disk.
    [Fact]
    public void Generate_AddingAToolChangesTheFile_SoTheCheckSeesItStale()
    {
        var root = SolutionAssemblies.RepositoryRoot();
        var options = new DocGenOptions(false, false, false, false, true);
        var before = DocSet.Generate(root, "Simulab", options, [], Types());
        var after = DocSet.Generate(root, "Simulab", options, [], Types(typeof(GoodTool)));

        before.Keys.Should().Equal("README.md", "tools.md");
        after["tools.md"].Should().NotBe(before["tools.md"]);
        after["README.md"].Should().Contain("- [tools.md](tools.md)");
    }

    // AC7: the committed catalogue is what the real code produces today: no tool has been added yet.
    [Fact]
    public void CommittedCatalogue_MatchesTheToolsInTheProductionAssemblies()
    {
        var types = EntityModelsTests.DocGenAssemblies().SelectMany(a => a.GetTypes()).ToList();
        var file = Path.Combine(SolutionAssemblies.RepositoryRoot(), "docs", "architecture", "tools.md");

        File.ReadAllText(file).Replace("\r\n", "\n", StringComparison.Ordinal)
            .Should().Be(ToolCatalogueDoc.Render([.. types, typeof(AIFunctionFactory)]));
    }

    public sealed class GoodTool
    {
        private readonly string _prefix = "";

        [ModelTool(Permissions = ["read_exams"], Reaches = ["ExamService"])]
        [Description("Gets exam details.")]
        public string GetExamAsync([Description("The exam id.")] string examId) => _prefix + examId;
    }

    public sealed class SameNameAsGoodTool
    {
        [ModelTool(Permissions = ["read_exams"], Reaches = ["ExamTable"])]
        [Description("Another way to get an exam.")]
        public static string GetExamAsync([Description("The exam id.")] string examId) => examId;
    }

    public sealed class RenamedTool
    {
        [ModelTool(Name = "GetExamDetails", Permissions = ["read_exams"], Reaches = ["ExamTable"])]
        [Description("Gets the details of an exam.")]
        public static string GetExamAsync([Description("The exam id.")] string examId) => examId;
    }

    public sealed class NoDescription
    {
        [ModelTool(Permissions = ["read_exams"], Reaches = ["ExamService"])]
        public static string Run() => "";
    }

    public sealed class NoParameterDescription
    {
        [ModelTool(Permissions = ["read_exams"], Reaches = ["ExamService"])]
        [Description("Gets exam details.")]
        public static string Run(string examId) => examId;
    }

    public sealed class NoPermissions
    {
        [ModelTool(Reaches = ["ExamService"])]
        [Description("Gets exam details.")]
        public static string Run() => "";
    }

    public sealed class NoReaches
    {
        [ModelTool(Permissions = ["read_exams"])]
        [Description("Gets exam details.")]
        public static string Run() => "";
    }

    public sealed class OpenTool
    {
        [ModelTool(Permissions = ["authenticated"], Reaches = ["ExamService"])]
        [Description("Gets exam details.")]
        public static string Run() => "";
    }
}
