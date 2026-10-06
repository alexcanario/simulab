using Aspire.Hosting.Azure;

namespace Simulab.AppHost;

/// <summary>
/// F-62 (ADR-0002): what the app host adds when it publishes to Azure Container Apps. It lives apart from
/// <c>AppHost.cs</c> because that file describes the local run, and the architecture overview is checked
/// against it; the cloud resources are a deployment concern, not containers of the system.
/// </summary>
internal static class AzureDeployment
{
    /// <summary>The Container Apps environment, Key Vault, PostgreSQL and the Redis of the named environment.</summary>
    internal static (IResourceBuilder<IResourceWithConnectionString> Database, IResourceBuilder<IResourceWithConnectionString> Redis) AddResources(
        IDistributedApplicationBuilder builder, string environmentName)
    {
        // The images go to the Azure Container Registry this environment creates; Aspire 13.6.0 accepts no other
        // registry here (change note v2 of F-62).
        builder.AddAzureContainerAppEnvironment("cae");

        // Secrets the deployed hosts read from Key Vault; the PostgreSQL password is generated and stored there.
        builder.AddAzureKeyVault("keyvault");

        IResourceBuilder<IResourceWithConnectionString> database =
            builder.AddAzurePostgresFlexibleServer("postgres").AddDatabase("simulab");

        // Azure Cache for Redis cannot be stopped, only deleted. Staging is parked outside test windows and its Redis
        // holds only sessions, so it runs as a container in the environment; production uses the managed cache.
        IResourceBuilder<IResourceWithConnectionString> redis = environmentName == "Production"
            ? builder.AddAzureManagedRedis("redis")
            : builder.AddRedis("redis");

        return (database, redis);
    }

    /// <summary>
    /// F-66: the Email Communication Service, its Azure-managed domain, the Communication Service and the role that lets
    /// the Api, and only the Api, send (<c>Bicep/email.bicep</c>), then the settings the Api reads. No secret and no
    /// parameter: the Api signs in with its managed identity (BR1). The Web gets nothing (BR9).
    /// </summary>
    private static void AddEmail(
        IDistributedApplicationBuilder builder,
        IResourceBuilder<ProjectResource> api,
        IResourceBuilder<AzureUserAssignedIdentityResource> apiIdentity)
    {
        var email = builder.AddBicepTemplate("email", "Bicep/email.bicep")
            .WithParameter("apiPrincipalId", apiIdentity.GetOutput("principalId"));

        api.WithEnvironment("Email__Provider", "AzureCommunicationServices")
            .WithEnvironment("Email__AzureCommunicationServices__Endpoint", email.GetOutput("endpoint"))
            .WithEnvironment("Email__FromAddress", email.GetOutput("senderAddress"));
    }

    /// <summary>
    /// The settings of the two hosts in the cloud. The cloud sets the environment name on them (else a staging
    /// deploy logs "Production"). BR5: the Api runs the job worker, so it never goes to zero (at zero replicas no
    /// email leaves); the Web keeps its sign-in tickets in memory, so it runs as one instance and may sleep at zero.
    /// </summary>
    internal static void ConfigureHosts(
        IDistributedApplicationBuilder builder,
        IResourceBuilder<ProjectResource> api,
        IResourceBuilder<ProjectResource> web,
        string environmentName)
    {
        var openIddictSecret = builder.AddParameter("openiddict-client-secret", secret: true);

        // F-66: one managed identity for the Api, attached explicitly because the email template needs its principal
        // id for the role assignment. It also serves PostgreSQL (Entra ID sign-in) and, in Production, the managed Redis.
        var apiIdentity = builder.AddAzureUserAssignedIdentity("api-identity");
        api.WithAzureUserAssignedIdentity(apiIdentity);
        AddEmail(builder, api, apiIdentity);

        api.WithEnvironment("ASPNETCORE_ENVIRONMENT", environmentName)
            .WithEnvironment("Authentication__OpenIddict__ClientSecret", openIddictSecret)
            .PublishAsAzureContainerApp((_, app) =>
            {
                app.Template.Scale.MinReplicas = 1;

                // F-54 BR7: a second replica would reject the first one's tokens (OpenIddict development
                // certificates per machine, F-73); F-73 lifts this cap.
                app.Template.Scale.MaxReplicas = 1;
            });

        web.WithExternalHttpEndpoints()
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", environmentName)
            .WithEnvironment("Authentication__OpenIddict__ClientSecret", openIddictSecret)
            .PublishAsAzureContainerApp((_, app) =>
            {
                app.Template.Scale.MinReplicas = 0;
                app.Template.Scale.MaxReplicas = 1;
            });
    }
}
