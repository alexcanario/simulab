var builder = DistributedApplication.CreateBuilder(args);

// One PostgreSQL server with the app database. The named volume keeps local data across restarts.
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("simulab-postgres-data");

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
    .WaitFor(api);

// The verification link in the email points at the Web page that consumes the token (F-4). The Api
// cannot know that address on its own, and the ports change on every run.
api.WithEnvironment(context =>
    context.EnvironmentVariables["Identity__VerificationUrl"] =
        ReferenceExpression.Create($"{web.GetEndpoint("https")}/verify-email"));

builder.Build().Run();
