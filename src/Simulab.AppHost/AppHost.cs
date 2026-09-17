var builder = DistributedApplication.CreateBuilder(args);

// One PostgreSQL server with the app database. The named volume keeps local data across restarts.
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("simulab-postgres-data")
    .WithPgAdmin();

var database = postgres.AddDatabase("simulab");

// Local SMTP capture: nothing leaves the machine, and the web UI shows every message.
var mailpit = builder.AddMailPit("mailpit");

var api = builder.AddProject<Projects.Simulab_Api>("api")
    .WithReference(database)
    .WaitFor(database)
    .WithReference(mailpit)
    .WaitFor(mailpit);

builder.AddProject<Projects.Simulab_Web>("web")
    .WithExternalHttpEndpoints()
    .WithReference(api)
    .WaitFor(api);

builder.Build().Run();
