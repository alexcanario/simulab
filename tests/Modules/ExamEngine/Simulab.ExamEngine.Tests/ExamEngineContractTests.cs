using System.Net;
using System.Text.Json;

namespace Simulab.ExamEngine.Tests;

/// <summary>
/// F-166 AC21 and F-169 AC18: the OpenAPI document lists the seven routes and answers 200 (a struct default in a contract
/// makes it answer 500).
/// </summary>
public sealed class ExamEngineContractTests : ExamEngineApiTests
{
    // F-166 AC21, F-169 AC18
    [Fact]
    public async Task TheOpenApiDocument_Answers200_AndListsTheSevenAttemptRoutes()
    {
        var response = await Client().GetAsync("/openapi/v1.json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var operations = document.RootElement.GetProperty("paths").EnumerateObject()
            .SelectMany(path => path.Value.EnumerateObject().Select(operation => $"{operation.Name.ToUpperInvariant()} {path.Name}"))
            .Where(operation => operation.Contains("/exam-engine/", StringComparison.Ordinal))
            .ToList();
        operations.Should().BeEquivalentTo(
        [
            "POST /api/v1/exam-engine/attempts",
            "GET /api/v1/exam-engine/attempts/{id}",
            "PUT /api/v1/exam-engine/attempts/{id}/answers/{questionId}",
            "DELETE /api/v1/exam-engine/attempts/{id}/answers/{questionId}",
            "POST /api/v1/exam-engine/attempts/{id}/submit",
            "GET /api/v1/exam-engine/attempts/{id}/content",
            "GET /api/v1/exam-engine/attempts/{id}/images/{imageId}"
        ]);
    }
}
