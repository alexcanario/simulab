var builder = DistributedApplication.CreateBuilder(args);

// F-62 (ADR-0002): the Azure resources exist only when the host publishes (`aspire publish` / `aspire deploy`).
// A local run keeps today's containers and needs no Azure sign-in.
var publishing = builder.ExecutionContext.IsPublishMode;
var environmentName = builder.Environment.EnvironmentName;

IResourceBuilder<IResourceWithConnectionString> database;
IResourceBuilder<IResourceWithConnectionString> redis;
IResourceBuilder<MailPitContainerResource>? mailpit = null;

if (publishing)
{
    builder.AddAzureContainerAppEnvironment("cae");

    // Secrets the deployed hosts read from Key Vault; the PostgreSQL password is generated and stored there.
    builder.AddAzureKeyVault("keyvault");

    database = builder.AddAzurePostgresFlexibleServer("postgres").AddDatabase("simulab");

    // Azure Cache for Redis cannot be stopped, only deleted. Staging is parked outside test windows and its Redis
    // holds only sessions, so it runs as a container in the environment; production uses the managed cache.
    redis = environmentName == "Production"
        ? builder.AddAzureManagedRedis("redis")
        : builder.AddRedis("redis");
}
else
{
    // Fixed local-only password (owner decision): easier to reach the container by hand (psql, DataGrip)
    // without checking the dashboard every run. Never used outside local development.
    var postgresPassword = builder.AddParameter("postgres-password", "postgres", secret: true);

    // One PostgreSQL server with the app database. The named volume keeps local data across restarts.
    var postgres = builder.AddPostgres("postgres", password: postgresPassword)
        .WithDataVolume("simulab-postgres-data")
        .WithHostPort(5432);

    // The resource keeps its name, so every connection string stays "simulab"; only the physical database
    // changes. A worktree sets Database:Name (or Database__Name) so an item's migration never lands in the
    // shared local database while it is still being built (rule: worktrees).
    database = postgres.AddDatabase("simulab", builder.Configuration["Database:Name"] ?? "simulab");

    // Local SMTP capture: nothing leaves the machine, and the web UI shows every message.
    mailpit = builder.AddMailPit("mailpit");

    // Refresh-token sessions and the access-token revocation set (F-5).
    redis = builder.AddRedis("redis")
        .WithDataVolume("simulab-redis-data");
}

var api = builder.AddProject<Projects.Simulab_Api>("api")
    .WithReference(database)
    .WaitFor(database)
    .WithReference(redis)
    .WaitFor(redis);

var web = builder.AddProject<Projects.Simulab_Web>("web")
    .WithReference(api)
    .WaitFor(api)
    // B-3: each signed-in browser's tokens live here, not in its cookie.
    .WithReference(redis)
    .WaitFor(redis);

if (mailpit is not null)
{
    api.WithReference(mailpit)
        .WaitFor(mailpit)
        // The MailPit connection string carries the container's own SMTP port (1025), which is not the port
        // the Api reaches from the host. Without this the Api talks to localhost:1025 and every email is
        // refused (found on screen, F-4). The endpoint reference resolves to the mapped host and port.
        .WithEnvironment("ConnectionStrings__mailpit", ReferenceExpression.Create(
            $"smtp://{mailpit.GetEndpoint("smtp").Property(EndpointProperty.HostAndPort)}"));
}

if (publishing)
{
    // The cloud sets the environment name on the hosts (else a staging deploy logs "Production").
    // BR5: the Api runs the job worker, so it never goes to zero (at zero replicas no email leaves);
    // the Web keeps its sign-in tickets in memory, so it runs as one instance and may sleep at zero.
    var openIddictSecret = builder.AddParameter("openiddict-client-secret", secret: true);
    api.WithEnvironment("ASPNETCORE_ENVIRONMENT", environmentName)
        .WithEnvironment("Authentication__OpenIddict__ClientSecret", openIddictSecret)
        .PublishAsAzureContainerApp((_, app) => app.Template.Scale.MinReplicas = 1);
    web.WithExternalHttpEndpoints()
        .WithEnvironment("ASPNETCORE_ENVIRONMENT", environmentName)
        .WithEnvironment("Authentication__OpenIddict__ClientSecret", openIddictSecret)
        .PublishAsAzureContainerApp((_, app) =>
        {
            app.Template.Scale.MinReplicas = 0;
            app.Template.Scale.MaxReplicas = 1;
        });
}
else
{
    web.WithExternalHttpEndpoints();
}

// The verification link in the email points at the Web page that consumes the token (F-4). The Api
// cannot know that address on its own, and the ports change on every run.
api.WithEnvironment(context =>
{
    context.EnvironmentVariables["Identity__VerificationUrl"] =
        ReferenceExpression.Create($"{web.GetEndpoint("https")}/verify-email");

    // F-7: the reset link, and the "was it not you?" link in the password-changed email.
    context.EnvironmentVariables["Identity__PasswordResetUrl"] =
        ReferenceExpression.Create($"{web.GetEndpoint("https")}/reset-password");
    context.EnvironmentVariables["Identity__ForgotPasswordUrl"] =
        ReferenceExpression.Create($"{web.GetEndpoint("https")}/forgot-password");

    // F-10: the farewell email says the address is free again and points back at sign-up.
    context.EnvironmentVariables["Identity__SignUpUrl"] =
        ReferenceExpression.Create($"{web.GetEndpoint("https")}/sign-up");
});

// F-20: Google sign-in is on locally only when the OAuth client is in this host's user secrets (Google:ClientId,
// Google:ClientSecret; docs/infra.md). Both hosts read the same switch; only the Web gets the secret.
// F-62 BR7: never when publishing, or the value of a local secret would become the default of a parameter
// and be written into the deployment files. In the cloud these come from Key Vault (docs/infra.md).
var googleClientId = publishing ? null : builder.Configuration["Google:ClientId"];
var googleClientSecret = publishing ? null : builder.Configuration["Google:ClientSecret"];
if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
    // A secret parameter, so the dashboard masks it like the database password.
    var googleSecret = builder.AddParameter("google-client-secret", googleClientSecret, secret: true);
    api.WithEnvironment("Identity__GoogleSignInEnabled", "true")
        .WithEnvironment("Authentication__Google__ClientId", googleClientId);
    web.WithEnvironment("Identity__GoogleSignInEnabled", "true")
        .WithEnvironment("Authentication__Google__ClientId", googleClientId)
        .WithEnvironment("Authentication__Google__ClientSecret", googleSecret);
}

// F-41: the gateway's key lives in this host's user secrets (Ai:ApiKey; docs/infra.md) and reaches only
// the Api, as a secret parameter so the dashboard masks it. Without it the app starts normally and every
// call fails with ai.not_configured (BR4), which is what a session that does not need the model wants.
// F-62 BR7: not when publishing, for the same reason as the Google secret above.
var aiApiKey = publishing ? null : builder.Configuration["Ai:ApiKey"];
if (!string.IsNullOrWhiteSpace(aiApiKey))
{
    api.WithEnvironment("Ai__ApiKey", builder.AddParameter("ai-api-key", aiApiKey, secret: true));
}

builder.Build().Run();
