var builder = DistributedApplication.CreateBuilder(args);

// Fixed local-only password (owner decision): easier to reach the container by hand (psql, DataGrip)
// without checking the dashboard every run. Never used outside local development.
var postgresPassword = builder.AddParameter("postgres-password", "postgres", secret: true);

// One PostgreSQL server with the app database. The named volume keeps local data across restarts.
var postgres = builder.AddPostgres("postgres", password: postgresPassword)
    .WithDataVolume("simulab-postgres-data")
    .WithHostPort(5432);

var database = postgres.AddDatabase("simulab");

// Local SMTP capture: nothing leaves the machine, and the web UI shows every message.
var mailpit = builder.AddMailPit("mailpit");

// Refresh-token sessions and the access-token revocation set (F-5).
var redis = builder.AddRedis("redis")
    .WithDataVolume("simulab-redis-data");

var api = builder.AddProject<Projects.Simulab_Api>("api")
    .WithReference(database)
    .WaitFor(database)
    .WithReference(mailpit)
    .WaitFor(mailpit)
    .WithReference(redis)
    .WaitFor(redis)
    // The MailPit connection string carries the container's own SMTP port (1025), which is not the port
    // the Api reaches from the host. Without this the Api talks to localhost:1025 and every email is
    // refused (found on screen, F-4). The endpoint reference resolves to the mapped host and port.
    .WithEnvironment("ConnectionStrings__mailpit", ReferenceExpression.Create(
        $"smtp://{mailpit.GetEndpoint("smtp").Property(EndpointProperty.HostAndPort)}"));

var web = builder.AddProject<Projects.Simulab_Web>("web")
    .WithExternalHttpEndpoints()
    .WithReference(api)
    .WaitFor(api)
    // B-3: each signed-in browser's tokens live here, not in its cookie.
    .WithReference(redis)
    .WaitFor(redis);

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

builder.Build().Run();
