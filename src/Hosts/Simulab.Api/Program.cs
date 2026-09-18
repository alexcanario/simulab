using System.Reflection;
using Simulab.Api.Features.System;
using Simulab.Email;
using Simulab.Identity.Api;
using Simulab.Identity.Infrastructure;
using Simulab.Persistence;
using Simulab.SharedKernel.Messaging;
using Simulab.SharedKernel.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.ConfigureHttpJsonOptions(options => AppJson.Configure(options.SerializerOptions));

// Foundation (F-3): database and its health check, module persistence services, email and in-process events.
builder.Services.AddAppDatabase(builder.Configuration.GetConnectionString("simulab")
    ?? throw new InvalidOperationException("The connection string 'simulab' is missing."));
builder.Services.AddModulePersistence();
builder.Services.AddEmailSender(builder.Configuration, builder.Configuration.GetConnectionString("mailpit"));
builder.Services.AddIntegrationEvents();

// Modules (F-4: Identity).
builder.Services.AddIdentityModule(builder.Configuration, builder.Configuration.GetConnectionString("simulab")
    ?? throw new InvalidOperationException("The connection string 'simulab' is missing."));
builder.Services.AddSingleton<ClientRateLimiter>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapDefaultEndpoints();

// Host only: composition, auth, middleware, OpenAPI. Modules map their own endpoints here (MapXxxEndpoints).
var v1 = app.MapGroup("/api/v1");

v1.MapGet("/system/info", (IHostEnvironment environment) => new SystemInfoResponse(
        "Simulab",
        Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0",
        environment.EnvironmentName))
    .WithName("GetSystemInfo");

v1.MapIdentityEndpoints();

// Development applies the module migrations on start; a release applies them from the pipeline.
// A test host that does not need a database turns it off with Database:ApplyMigrationsOnStart.
if (app.Configuration.GetValue("Database:ApplyMigrationsOnStart", app.Environment.IsDevelopment()))
{
    await app.Services.MigrateIdentityModuleAsync();
}

app.Run();
