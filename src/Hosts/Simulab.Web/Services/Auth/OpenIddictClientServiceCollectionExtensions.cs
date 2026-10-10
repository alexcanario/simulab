namespace Simulab.Web.Services.Auth;

internal static class OpenIddictClientServiceCollectionExtensions
{
    /// <summary>
    /// Binds <see cref="OpenIddictClientOptions"/> and checks it when the host starts (F-94 BR6): a host without its client
    /// id or secret refuses to start and names the key. A method of its own so a test can run the same registration without
    /// starting a host, whose failed start races inside <c>WebApplicationFactory</c>.
    /// </summary>
    internal static IServiceCollection AddOpenIddictClientOptions(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<OpenIddictClientOptions>()
            .Bind(configuration.GetSection(OpenIddictClientOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        return services;
    }
}
