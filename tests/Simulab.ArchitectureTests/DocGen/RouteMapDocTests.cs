using System.Text.Json;
using Simulab.DocGen;

namespace Simulab.ArchitectureTests.DocGen;

/// <summary>F-15: the route map from the committed OpenAPI document (AC4).</summary>
public class RouteMapDocTests
{
    private static readonly string Root = SolutionAssemblies.RepositoryRoot();

    private static readonly string[] Verbs = ["get", "put", "post", "delete", "patch"];

    [Fact]
    public void Render_ListsEveryVersionedOperationOfTheCommittedDocument_AndNothingElse()
    {
        var file = Path.Combine(Root, "docs", "api", "Simulab.Api.json");
        File.Exists(file).Should().BeTrue("the Api host test writes it (F-15 v2)");
        using var document = JsonDocument.Parse(File.ReadAllText(file));
        var operations = document.RootElement.GetProperty("paths").EnumerateObject()
            .SelectMany(path => path.Value.EnumerateObject().Where(o => Verbs.Contains(o.Name)).Select(o => (Verb: o.Name.ToUpperInvariant(), Route: path.Name)))
            .ToList();
        operations.Should().Contain(o => !o.Route.StartsWith("/api/v1/", StringComparison.Ordinal), "the document also serves /connect/...");

        var maps = RouteMapDoc.Render(Root).ToDictionary(m => m.Area, m => m.Text);

        maps.Keys.Should().Equal("Identity", "System");
        var text = string.Concat(maps.Values);
        foreach (var (verb, route) in operations)
        {
            var row = $"| `{verb}` | `{route}` |";
            if (route.StartsWith("/api/v1/", StringComparison.Ordinal))
            {
                text.Should().Contain(row);
            }
            else
            {
                text.Should().NotContain($"`{route}`");
            }
        }
    }

    [Fact]
    public void Render_AddsTheAuthColumnOnlyWhenTheDocumentDeclaresSecurity()
    {
        const string Open = """{ "paths": { "/api/v1/exams": { "get": { "operationId": "ListExams", "responses": { "200": {} } } } } }""";
        const string Secured = """{ "paths": { "/api/v1/exams": { "get": { "operationId": "ListExams", "responses": { "200": {} }, "security": [ { "bearer": [] } ] } } } }""";

        RouteMapDoc.Render(Open, "a.json").Single().Text.Should().Contain("| `GET` | `/api/v1/exams` | ListExams | 200 |\n").And.NotContain("Auth");
        RouteMapDoc.Render(Secured, "a.json").Single().Text.Should().Contain("| `GET` | `/api/v1/exams` | ListExams | 200 | yes |\n");
    }
}
