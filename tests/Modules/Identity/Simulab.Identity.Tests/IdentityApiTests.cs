using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Simulab.Identity.Infrastructure.Persistence;
using Simulab.Jobs;
using Simulab.Jobs.Persistence;
using Simulab.Testing.ApiHost;

namespace Simulab.Identity.Tests;

/// <summary>
/// The shared Api host (<see cref="ApiHostTests"/>, moved to Simulab.Testing in F-33) plus what only
/// this module's tests need: its own context and the job queue behind its emails.
/// </summary>
public abstract class IdentityApiTests : ApiHostTests
{
    /// <summary>
    /// F-13 AC11: the emails are jobs now. A test that reads <see cref="ApiHostTests.Emails"/> runs the
    /// queue first; the worker is off in the test host, so nothing here ever waits on a timer.
    /// </summary>
    protected Task<int> RunJobsAsync() => Factory.RunJobsAsync();

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
}
