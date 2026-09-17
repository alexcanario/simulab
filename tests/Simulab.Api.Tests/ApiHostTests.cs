using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Simulab.Api.Features.System;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Api.Tests;

/// <summary>Through the real HTTP pipeline, with the shared JSON options.</summary>
public class ApiHostTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task OpenApi_document_is_served()
    {
        var response = await _client.GetAsync("/openapi/v1.json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Contain("/api/v1/system/info");
    }

    [Fact]
    public async Task Health_endpoint_answers()
    {
        var response = await _client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
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
