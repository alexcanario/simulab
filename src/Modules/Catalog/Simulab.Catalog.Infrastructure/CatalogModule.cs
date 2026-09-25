using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Simulab.Catalog.Application.Exams;
using Simulab.Catalog.Application.IssuingAuthorities;
using Simulab.Catalog.Application.Organizers;
using Simulab.Catalog.Contracts;
using Simulab.Catalog.Infrastructure.Persistence;
using Simulab.Persistence;
using Simulab.SharedKernel.Security;

namespace Simulab.Catalog.Infrastructure;

/// <summary>Everything the Catalog module needs, registered by the host in one call (F-33, BR1).</summary>
public static class CatalogModule
{
    public static IServiceCollection AddCatalogModule(this IServiceCollection services, string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddDbContext<CatalogModuleDbContext>((provider, options) =>
            ((DbContextOptionsBuilder<CatalogModuleDbContext>)options)
                .UseNpgsql(connectionString, npgsql => npgsql.UseModuleHistoryTable(CatalogModuleDbContext.SchemaName))
                .UseModuleConventions(provider));

        // BR2: the names this module defines. Identity owns the table and seeds the union it finds here.
        services.AddSingleton(new PermissionCatalog(CatalogPermissions.ModuleName, CatalogPermissions.All));

        services.AddScoped<IOrganizerStore, OrganizerStore>();
        services.AddScoped<IOrganizerQueries, OrganizerQueries>();
        services.AddScoped<SaveOrganizerHandler>();
        services.AddScoped<DeleteOrganizerHandler>();

        services.AddScoped<IIssuingAuthorityStore, IssuingAuthorityStore>();
        services.AddScoped<IIssuingAuthorityQueries, IssuingAuthorityQueries>();
        services.AddScoped<SaveIssuingAuthorityHandler>();
        services.AddScoped<DeleteIssuingAuthorityHandler>();

        services.AddScoped<IExamStore, ExamStore>();
        services.AddScoped<IExamQueries, ExamQueries>();
        services.AddScoped<SaveExamHandler>();
        services.AddScoped<DeleteExamHandler>();

        return services;
    }

    /// <summary>Applies this module's migrations. Development does it at start; a release does it from the pipeline.</summary>
    public static Task MigrateCatalogModuleAsync(this IServiceProvider services, CancellationToken cancellationToken = default) =>
        services.MigrateModuleAsync<CatalogModuleDbContext>(cancellationToken);
}
