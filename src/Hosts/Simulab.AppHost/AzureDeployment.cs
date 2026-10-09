using Aspire.Hosting.Azure;
using Azure.Provisioning.KeyVault;
using Azure.Provisioning.Storage;

namespace Simulab.AppHost;

/// <summary>
/// F-62 (ADR-0002): what the app host adds when it publishes to Azure Container Apps. It lives apart from
/// <c>AppHost.cs</c> because that file describes the local run, and the architecture overview is checked
/// against it; the cloud resources are a deployment concern, not containers of the system.
/// </summary>
internal static class AzureDeployment
{
    /// <summary>The cloud resources the hosts are wired to, returned by <see cref="AddResources"/>.</summary>
    internal sealed record CloudResources(
        IResourceBuilder<IResourceWithConnectionString> Database,
        IResourceBuilder<IResourceWithConnectionString> Redis,
        IResourceBuilder<AzureKeyVaultResource> KeyVault,
        IResourceBuilder<AzureBlobStorageContainerResource> ApiKeys,
        IResourceBuilder<AzureBlobStorageContainerResource> WebKeys);

    /// <summary>
    /// The Container Apps environment, Key Vault, the storage account of the Data Protection keys, PostgreSQL and the Redis
    /// of the named environment.
    /// </summary>
    internal static CloudResources AddResources(IDistributedApplicationBuilder builder, string environmentName)
    {
        // The images go to the Azure Container Registry this environment creates; Aspire 13.6.0 accepts no other
        // registry here (change note v2 of F-62).
        builder.AddAzureContainerAppEnvironment("cae");

        // Secrets the deployed hosts read from Key Vault; the PostgreSQL connection string is stored there.
        var keyVault = builder.AddAzureKeyVault("keyvault");

        // F-64 D20 (2026-10-08): the server's administrator login and password are deploy-time parameters, the same value
        // on every deploy. Left to Aspire they are generated again by every `aspire deploy --clear-cache`, while a server
        // never changes its login: the Api then signed in with a login the server did not know (28P01). The owner keeps
        // both in the vault as `deploy--PostgresAdminUser` / `deploy--PostgresAdminPassword` (docs/infra.md).
        var databaseUser = builder.AddParameter("postgres-admin-user");
        var databasePassword = builder.AddParameter("postgres-admin-password", secret: true);

        // F-64 D9 (change note v2): the Data Protection key ring of each host is a blob, so a restart, a deploy or a park does
        // not lose it. One storage account per host (a few cents): Aspire gives a referencing host its roles on the whole
        // account, so a shared one would let the Web read and overwrite the Api's key ring (review of F-64).
        var apiKeys = AddKeyStorage(builder, "storage-api");
        var webKeys = AddKeyStorage(builder, "storage-web");

        IResourceBuilder<IResourceWithConnectionString> database =
            builder.AddAzurePostgresFlexibleServer("postgres")
                .WithPasswordAuthentication(keyVault, databaseUser, databasePassword)
                .AddDatabase("simulab");

        // Azure Cache for Redis cannot be stopped, only deleted. Staging is parked outside test windows and its Redis
        // holds only sessions, so it runs as a container in the environment; production uses the managed cache.
        // F-64 D21 (2026-10-08): the container's password is a deploy-time parameter too. Generated, it changed with every
        // `--clear-cache` deploy, the platform updated the secret but did not restart the running Redis, and the hosts failed
        // with NOAUTH until it was restarted by hand. The owner keeps it in the vault as `deploy--RedisPassword`.
        IResourceBuilder<IResourceWithConnectionString> redis = environmentName == "Production"
            ? builder.AddAzureManagedRedis("redis")
            : builder.AddRedis("redis", password: builder.AddParameter("redis-password", secret: true));

        return new CloudResources(database, redis, keyVault, apiKeys, webKeys);
    }

    /// <summary>
    /// A locally redundant storage account (the default, geo-redundant, costs more and the keys are cheap to lose in staging:
    /// everybody signs in again) with the blob container <c>keys</c>.
    /// </summary>
    private static IResourceBuilder<AzureBlobStorageContainerResource> AddKeyStorage(IDistributedApplicationBuilder builder, string name) =>
        builder.AddAzureStorage(name)
            .ConfigureInfrastructure(infrastructure =>
            {
                foreach (var account in infrastructure.GetProvisionableResources().OfType<StorageAccount>())
                {
                    account.Sku = new StorageSku { Name = StorageSkuName.StandardLrs };
                }
            })
            .AddBlobContainer($"keys-{name["storage-".Length..]}", "keys");

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
        string environmentName,
        CloudResources cloud)
    {
        var openIddictSecret = builder.AddParameter("openiddict-client-secret", secret: true);

        // F-66: one managed identity for the Api, attached explicitly because the email template needs its principal
        // id for the role assignment. It also reads the vault and, in Production, the managed Redis; the database uses a password (F-64 D15).
        var apiIdentity = builder.AddAzureUserAssignedIdentity("api-identity");
        api.WithAzureUserAssignedIdentity(apiIdentity);
        AddEmail(builder, api, apiIdentity);

        // F-64 BR4, D9: both hosts keep their Data Protection keys in the blob container and encrypt them with this Key Vault
        // key (the owner creates it, docs/infra.md). The Api also reads its secrets from the vault as configuration
        // (the seeded admin password, the OpenIddict certificates; BR7), which `WithReference` lets it do.
        var keyId = ReferenceExpression.Create($"{cloud.KeyVault.Resource.VaultUri}keys/dataprotection");
        api.WithReference(cloud.ApiKeys, connectionName: "keys");
        web.WithReference(cloud.WebKeys, connectionName: "keys");
        foreach (var host in new[] { api, web })
        {
            host.WithEnvironment("DataProtection__KeyVaultKeyId", keyId);
        }

        // An explicit role assignment replaces the default one of `WithReference`, so each host names all it needs: the Web
        // only unwraps its keys; the Api also reads the secrets.
        web.WithRoleAssignments(cloud.KeyVault, KeyVaultBuiltInRole.KeyVaultCryptoServiceEncryptionUser);
        api.WithReference(cloud.KeyVault)
            .WithRoleAssignments(cloud.KeyVault, KeyVaultBuiltInRole.KeyVaultSecretsUser, KeyVaultBuiltInRole.KeyVaultCryptoServiceEncryptionUser);

        // F-64 BR6, D12: the ingress addresses the hosts believe `X-Forwarded-*` from. The deploy knows no fixed range of its
        // own, so they are measured on the first staging and written to the app host's `appsettings.<Environment>.json`;
        // with none listed nothing is believed (docs/infra.md).
        foreach (var host in new[] { api, web })
        {
            AddForwardedHeaders(builder, host);
        }

        // F-64 BR5, D11: Staging applies the module migrations, roles and the OpenIddict client on start; production keeps
        // the default (off) until the release pipeline decides how a release migrates (F-65).
        if (environmentName == "Staging")
        {
            api.WithEnvironment("Database__ApplyMigrationsOnStart", "true");
        }

        api.WithEnvironment("ASPNETCORE_ENVIRONMENT", environmentName)
            .WithEnvironment("Authentication__OpenIddict__ClientSecret", openIddictSecret)
            .PublishAsAzureContainerApp((_, app) =>
            {
                DropBlanketForwardedHeaders(app);
                app.Template.Scale.MinReplicas = 1;

                // F-54 BR7: a second replica would reject the first one's tokens (OpenIddict development
                // certificates per machine, F-73); F-73 lifts this cap.
                app.Template.Scale.MaxReplicas = 1;
            });

        // The cloud Web has no appsettings.Development.json, so the client id it sends to the token endpoint comes from
        // here; it is the id the Api seeds (IdentityModule.WebClientId) and is not a secret.
        web.WithExternalHttpEndpoints()
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", environmentName)
            .WithEnvironment("Authentication__OpenIddict__ClientId", "simulab-web")
            .WithEnvironment("Authentication__OpenIddict__ClientSecret", openIddictSecret)
            .PublishAsAzureContainerApp((_, app) =>
            {
                DropBlanketForwardedHeaders(app);
                app.Template.Scale.MinReplicas = 0;
                app.Template.Scale.MaxReplicas = 1;
            });
    }

    /// <summary>
    /// F-64 BR6, D17: the publish sets <c>ASPNETCORE_FORWARDEDHEADERS_ENABLED=true</c> on every project, which makes a host
    /// believe the forwarded headers of any sender. Removed: the hosts believe only the proxies they are told (BR6).
    /// </summary>
    private static void DropBlanketForwardedHeaders(Azure.Provisioning.AppContainers.ContainerApp app)
    {
        foreach (var container in app.Template.Containers)
        {
            var environment = container.Value!.Env;
            for (var index = environment.Count - 1; index >= 0; index--)
            {
                if (environment[index].Value?.Name.Value == "ASPNETCORE_FORWARDEDHEADERS_ENABLED")
                {
                    environment.RemoveAt(index);
                }
            }
        }
    }

    /// <summary>Passes <c>ForwardedHeaders:KnownProxies</c> and <c>KnownNetworks</c> of the app host's configuration to a host.</summary>
    private static void AddForwardedHeaders(IDistributedApplicationBuilder builder, IResourceBuilder<ProjectResource> host)
    {
        foreach (var key in new[] { "KnownProxies", "KnownNetworks" })
        {
            var entries = builder.Configuration.GetSection($"ForwardedHeaders:{key}").GetChildren()
                .Select(entry => entry.Value)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToArray();
            for (var index = 0; index < entries.Length; index++)
            {
                host.WithEnvironment($"ForwardedHeaders__{key}__{index}", entries[index]);
            }
        }
    }
}
