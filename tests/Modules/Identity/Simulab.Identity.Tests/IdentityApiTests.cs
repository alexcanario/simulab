using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Simulab.Identity.Infrastructure.Persistence;
using Simulab.Jobs;
using Simulab.Jobs.Persistence;
using Simulab.SharedKernel.Serialization;
using Simulab.Testing;

namespace Simulab.Identity.Tests;

/// <summary>
/// One Api host and one database per test class, with the recorded email sender and the fake clock.
/// Tests go through HTTP, the way the Web does.
/// </summary>
public abstract class IdentityApiTests : IAsyncLifetime, IDisposable
{
    protected IdentityApiFactory Factory { get; } = new();

    protected RecordingEmailSender Emails => Factory.Emails;

    /// <summary>
    /// F-13 AC11: the emails are jobs now. A test that reads <see cref="Emails"/> runs the queue
    /// first; the worker is off in the test host, so nothing here ever waits on a timer.
    /// </summary>
    protected Task<int> RunJobsAsync() => Factory.RunJobsAsync();

    public Task InitializeAsync()
    {
        Factory.ConfigureHost = ConfigureHost;
        return Factory.PrepareAsync(GetType().Name);
    }

    /// <summary>A test class overrides it to change the host's configuration.</summary>
    protected virtual void ConfigureHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
    }

    public Task DisposeAsync() => Task.CompletedTask;

    protected HttpClient Client(string locale = "en") => Factory.CreateClient(locale);

    /// <summary>The jobs still in the queue, as the worker would see them (F-13 AC2, AC4, AC7).</summary>
    protected async Task<List<Job>> JobsAsync()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<JobsDbContext>();
        return await context.Jobs.AsNoTracking().ToListAsync();
    }

    /// <summary>How many jobs are waiting to run.</summary>
    protected async Task<int> PendingJobCountAsync() =>
        (await JobsAsync()).Count(job => job.Status == JobStatus.Pending);

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
