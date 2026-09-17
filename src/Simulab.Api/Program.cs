using System.Reflection;
using Simulab.Api.Features.System;
using Simulab.SharedKernel.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.ConfigureHttpJsonOptions(options => AppJson.Configure(options.SerializerOptions));

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
