using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Testing.ApiHost;

/// <summary>
/// One Api host and one database per test class, with the recorded email sender and the fake clock.
/// Tests go through HTTP, the way the Web does. Shared by every module's tests since F-33.
/// </summary>
public abstract class ApiHostTests : IAsyncLifetime, IDisposable
{
    protected SimulabApiFactory Factory { get; } = new();

    protected RecordingEmailSender Emails => Factory.Emails;

    public Task InitializeAsync()
    {
        Factory.ConfigureHost = ConfigureHost;
        return Factory.PrepareAsync(GetType().Name);
    }

    /// <summary>A test class overrides it to change the host's configuration.</summary>
    protected virtual void ConfigureHost(IWebHostBuilder builder)
    {
    }

    public Task DisposeAsync() => Task.CompletedTask;

    protected HttpClient Client(string locale = "en") => Factory.CreateClient(locale);

    protected static async Task<string> PostAsync<T>(HttpClient client, string route, T body, HttpStatusCode expected)
    {
        ArgumentNullException.ThrowIfNull(client);

        var response = await client.PostAsJsonAsync(route, body, AppJson.Options);
        response.StatusCode.Should().Be(expected, await response.Content.ReadAsStringAsync());
        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>The stable <c>code</c> of a problem details body: what the UI branches on.</summary>
    protected static string CodeOf(string problemBody) =>
        JsonDocument.Parse(problemBody).RootElement.GetProperty("code").GetString()!;

    public void Dispose()
    {
        Factory.Dispose();
        GC.SuppressFinalize(this);
    }
}
