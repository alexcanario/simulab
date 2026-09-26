using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Simulab.Jobs.Email;
using Simulab.Jobs.Persistence;
using Simulab.Persistence;

namespace Simulab.Jobs;

public static class JobsServiceCollectionExtensions
{
    /// <summary>
    /// The queue and its worker, registered once by the host (F-13 BR13). A module that enqueues also
    /// calls <c>AddJobQueueFor&lt;TContext&gt;</c> so its own context is the unit of work (BR2).
    /// </summary>
    public static IServiceCollection AddJobs(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<JobOptions>().Bind(configuration.GetSection(JobOptions.SectionName));

        services.AddDbContext<JobsDbContext>((provider, options) =>
            ((DbContextOptionsBuilder<JobsDbContext>)options)
                .UseNpgsql(connectionString, npgsql => npgsql.UseModuleHistoryTable(JobsDbContext.SchemaName))
                .UseModuleConventions(provider));

        services.AddScoped<IJobHandler, SendEmailJobHandler>();
        services.AddSingleton<JobRunner>();
        // F-27: a singleton, because it is the one that remembers when the cleanup last ran.
        services.AddSingleton<JobCleanup>();
        services.AddHostedService<JobWorker>();
        return services;
    }

    /// <summary>
    /// Makes <typeparamref name="TContext"/> the unit of work a handler enqueues onto (F-13 BR2). The
    /// context must map the table with <c>AddJobQueue()</c> in its <c>OnModelCreating</c>.
    /// </summary>
    public static IServiceCollection AddJobQueueFor<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<IJobQueue, DbContextJobQueue<TContext>>();
        return services;
    }

    /// <summary>Applies the queue's migration. Development only; a release applies it from the pipeline.</summary>
    public static Task MigrateJobsAsync(this IServiceProvider services, CancellationToken cancellationToken = default) =>
        services.MigrateModuleAsync<JobsDbContext>(cancellationToken);
}
