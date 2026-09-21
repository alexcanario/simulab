using System.Net;
using System.Net.Http.Json;
using Simulab.Api.Features.System;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Api.Tests;

/// <summary>Through the real HTTP pipeline, with the shared JSON options.</summary>
public class ApiHostTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task OpenApi_document_is_served()
    {
        var response = await _client.GetAsync("/openapi/v1.json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Contain("/api/v1/system/info");
    }

    /// <summary>
    /// F-15 (v2): the served document is the source of docs/api/Simulab.Api.json, which DocGen reads for the route
    /// map. The test rewrites the file when it differs, so a suite run before DocGen leaves it current.
    /// </summary>
    [Fact]
    public async Task OpenApi_document_is_written_to_docs_api()
    {
        var served = (await _client.GetStringAsync("/openapi/v1.json")).ReplaceLineEndings("\n");
        var file = Path.Combine(RepositoryRoot(), "docs", "api", "Simulab.Api.json");

        var current = File.Exists(file) ? File.ReadAllText(file).ReplaceLineEndings("\n") : null;
        if (current != served)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            await File.WriteAllTextAsync(file, served);
        }

        File.ReadAllText(file).ReplaceLineEndings("\n").Should().Be(served);
        served.Should().Contain("/api/v1/identity/").And.Contain("/api/v1/system/info");
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Simulab.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Simulab.slnx was not found above the test output folder.");
    }

    [Fact]
    public async Task System_info_returns_the_app_name()
    {
        var info = await _client.GetFromJsonAsync<SystemInfoResponse>("/api/v1/system/info", AppJson.Options);

        info.Should().NotBeNull();
        info!.Name.Should().Be("Simulab");
        info.Version.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Unknown_route_returns_problem_details()
    {
        var response = await _client.GetAsync("/api/v1/does-not-exist");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }
}
