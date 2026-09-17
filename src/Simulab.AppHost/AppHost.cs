var builder = DistributedApplication.CreateBuilder(args);

// Containers (PostgreSQL, Redis, Azurite, Mailpit) are added by the first feature that needs each one.
var api = builder.AddProject<Projects.Simulab_Api>("api");

builder.AddProject<Projects.Simulab_Web>("web")
    .WithExternalHttpEndpoints()
    .WithReference(api)
    .WaitFor(api);

builder.Build().Run();
