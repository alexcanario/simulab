using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Simulab.ExamEngine.Infrastructure.Persistence;
using Simulab.Persistence;

namespace Simulab.ExamEngine.Infrastructure;

/// <summary>Everything the ExamEngine module needs, registered by the host in one call (F-166).</summary>
public static class ExamEngineModule
{
    public static IServiceCollection AddExamEngineModule(this IServiceCollection services, string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddDbContext<ExamEngineModuleDbContext>((provider, options) =>
            ((DbContextOptionsBuilder<ExamEngineModuleDbContext>)options)
                .UseNpgsql(connectionString, npgsql => npgsql.UseModuleHistoryTable(ExamEngineModuleDbContext.SchemaName))
                .UseModuleConventions(provider));

        return services;
    }

    /// <summary>Applies this module's migrations. Development does it at start; a release does it from the pipeline.</summary>
    public static Task MigrateExamEngineModuleAsync(this IServiceProvider services, CancellationToken cancellationToken = default) =>
        services.MigrateModuleAsync<ExamEngineModuleDbContext>(cancellationToken);
}
