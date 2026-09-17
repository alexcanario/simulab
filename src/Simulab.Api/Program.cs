using System.Reflection;
using Simulab.Api.Features.System;
using Simulab.Email;
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

app.Run();
