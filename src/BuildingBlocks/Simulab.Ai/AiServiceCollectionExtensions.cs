using Anthropic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Simulab.Ai.Contracts;
using Simulab.Ai.Persistence;
using Simulab.Persistence;
using Simulab.Plans.Contracts;

namespace Simulab.Ai;

public static class AiServiceCollectionExtensions
{
    /// <summary>
    /// The gateway and its table, registered once by the host (F-41). The key is not required: without
    /// it the host still starts and every call fails with <see cref="AiErrorCodes.NotConfigured"/> (BR4).
    /// </summary>
    public static IServiceCollection AddAiGateway(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<AiOptions>()
            .Bind(configuration.GetSection(AiOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<EntitlementOptions>()
            .Bind(configuration.GetSection(EntitlementOptions.SectionName));

        services.AddDbContext<AiDbContext>((provider, options) =>
            ((DbContextOptionsBuilder<AiDbContext>)options)
                .UseNpgsql(connectionString, npgsql => npgsql.UseModuleHistoryTable(AiDbContext.SchemaName))
                .UseModuleConventions(provider));

        services.AddSingleton(provider =>
        {
            var ai = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<AiOptions>>().Value;
            // BR4 is decided before the client is ever used, from the configured key alone, so an ambient
            // credential on the machine cannot change what "not configured" means.
            return new AnthropicClient { ApiKey = ai.ApiKey ?? string.Empty };
        });

        services.TryAddSingleton(TimeProvider.System);
        // BR10: the counters need a meter factory. A host that already called AddMetrics keeps its own.
        services.AddMetrics();
        services.AddSingleton<AiMetrics>();
        services.TryAddScoped<IEntitlementService, ConfigurationEntitlementService>();
        services.AddScoped<IAiGateway, AnthropicAiGateway>();
        return services;
    }

    /// <summary>Applies the gateway's migration. Development only; a release applies it from the pipeline.</summary>
    public static Task MigrateAiAsync(this IServiceProvider services, CancellationToken cancellationToken = default) =>
        services.MigrateModuleAsync<AiDbContext>(cancellationToken);
}
