using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Simulab.Identity.Infrastructure.Persistence;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Identity.Tests;

/// <summary>
/// One Api host and one database per test class, with the recorded email sender and the fake clock.
/// Tests go through HTTP, the way the Web does.
/// </summary>
public abstract class IdentityApiTests : IAsyncLifetime, IDisposable
{
    protected IdentityApiFactory Factory { get; } = new();

    protected RecordingEmailSender Emails => Factory.Emails;

    public Task InitializeAsync() => Factory.PrepareAsync(GetType().Name);

    public Task DisposeAsync() => Task.CompletedTask;

    protected HttpClient Client(string locale = "en") => Factory.CreateClient(locale);

    /// <summary>Runs a query on the module's own context, as the module itself would see the data.</summary>
    protected async Task<T> QueryAsync<T>(Func<IdentityModuleDbContext, Task<T>> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        await using var scope = Factory.Services.CreateAsyncScope();
        return await query(scope.ServiceProvider.GetRequiredService<IdentityModuleDbContext>());
    }

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
