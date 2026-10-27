using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Simulab.ExamEngine.Application.Attempts;
using Simulab.ExamEngine.Contracts;
using Simulab.ExamEngine.Infrastructure.Persistence;
using Simulab.Identity.Contracts;
using Simulab.Persistence;
using Simulab.SharedKernel.Messaging;
using Simulab.SharedKernel.Security;

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

        // The names this module defines. Identity owns the table and seeds the union it finds here; a student takes
        // attempts from the start, and only Admin (which gets everything) reads another student's.
        services.AddSingleton(new PermissionCatalog(
            ExamEnginePermissions.ModuleName,
            ExamEnginePermissions.All,
            new Dictionary<string, IReadOnlyList<string>>
            {
                [ExamEnginePermissions.Take] = [IdentityRoles.Student]
            }));

        services.AddScoped<IAttemptStore, AttemptStore>();
        services.AddScoped<IAttemptSource, AttemptSource>();
        services.AddScoped<IAttemptAccess, AttemptAccess>();
        services.AddScoped<StartAttemptHandler>();
        services.AddScoped<GetAttemptHandler>();
        services.AddScoped<SaveAnswerHandler>();
        services.AddScoped<ClearAnswerHandler>();
        services.AddScoped<SubmitAttemptHandler>();

        // F-167: the module's part of an account erasure and of the file a user downloads.
        services.AddScoped<EraseStudentTextsHandler>();
        services.AddIntegrationEventConsumer<UserErased, UserErasedConsumer>();
        services.AddScoped<IDataExportSection, ExamEngineDataExportSection>();

        return services;
    }

    /// <summary>Applies this module's migrations. Development does it at start; a release does it from the pipeline.</summary>
    public static Task MigrateExamEngineModuleAsync(this IServiceProvider services, CancellationToken cancellationToken = default) =>
        services.MigrateModuleAsync<ExamEngineModuleDbContext>(cancellationToken);
}
