using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Simulab.Persistence.Interceptors;
using Simulab.SharedKernel.Security;

namespace Simulab.Persistence;

public static class PersistenceServiceCollectionExtensions
{
    /// <summary>
    /// The pieces every module context needs: the audit interceptor, the current user and tenant
    /// (anonymous and no tenant until F-5) and the clock. Called once by the host.
    /// </summary>
    public static IServiceCollection AddModulePersistence(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddScoped<ICurrentUser, AnonymousUser>();
        services.TryAddScoped<ICurrentTenant, NoTenant>();
        services.AddScoped<AuditAndSoftDeleteInterceptor>();
        return services;
    }

    /// <summary>
    /// Applies the conventions every module context shares: snake_case names and the audit
    /// interceptor. The history table comes from <see cref="UseModuleHistoryTable"/>.
    /// </summary>
    public static DbContextOptionsBuilder<TContext> UseModuleConventions<TContext>(
        this DbContextOptionsBuilder<TContext> options,
        IServiceProvider services)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(services);

        options.UseSnakeCaseNamingConvention()
            .AddInterceptors(services.GetRequiredService<AuditAndSoftDeleteInterceptor>());
        return options;
    }

    /// <summary>The Npgsql options every module context shares: its migrations history table in its own schema.</summary>
    public static void UseModuleHistoryTable(
        this Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.NpgsqlDbContextOptionsBuilder npgsql,
        string schema)
    {
        ArgumentNullException.ThrowIfNull(npgsql);
        npgsql.MigrationsHistoryTable(HistoryTableName, schema);
    }

    /// <summary>Migrations history table name, the same in every module schema.</summary>
    public const string HistoryTableName = "__ef_migrations_history";

    /// <summary>Name of the database health check, as it appears in the health endpoint.</summary>
    public const string DatabaseHealthCheckName = "database";

    /// <summary>Registers the Npgsql data source for the app database and its health check.</summary>
    public static IServiceCollection AddAppDatabase(this IServiceCollection services, string connectionString)
    {
        services.AddNpgsqlDataSource(connectionString);
        services.AddHealthChecks().AddCheck<DatabaseHealthCheck>(DatabaseHealthCheckName, tags: ["ready"]);
        return services;
    }
}
