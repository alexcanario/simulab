using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Simulab.Jobs;
using Simulab.Jobs.Persistence;

namespace Simulab.Api.Tests;

/// <summary>
/// F-13: a module stages its emails through <c>AddJobQueueFor</c>, and only the host calls
/// <c>AddJobs</c>. Nothing in the compiler ties the two together, so a host that forgot the second
/// call would enqueue every identity email into a table no worker ever reads. This is the check.
/// </summary>
public class JobCompositionTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public void The_host_wires_the_queue_the_runner_and_the_worker()
    {
        using var scope = factory.Services.CreateScope();
        var services = scope.ServiceProvider;

        services.GetService<IJobQueue>().Should().NotBeNull("a module has nowhere to stage its jobs without it");
        services.GetService<JobsDbContext>().Should().NotBeNull();
        services.GetRequiredService<JobRunner>().Should().NotBeNull();
        factory.Services.GetServices<IHostedService>().Should().ContainItemsAssignableTo<JobWorker>();
    }

    [Fact]
    public void The_email_job_has_its_handler()
    {
        using var scope = factory.Services.CreateScope();

        scope.ServiceProvider.GetServices<IJobHandler>()
            .Should().Contain(handler => handler.Type == Jobs.Email.EmailJob.Type);
    }
}
