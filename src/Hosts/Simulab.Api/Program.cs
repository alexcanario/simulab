using System.Reflection;
using OpenIddict.Validation.AspNetCore;
using Simulab.Api;
using Simulab.Api.Features.System;
using Simulab.Email;
using Simulab.Identity.Api;
using Simulab.Identity.Api.Authorization;
using Simulab.Identity.Infrastructure;
using Simulab.Persistence;
using Simulab.SharedKernel.Messaging;
using Simulab.SharedKernel.Security;
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

// The signed-in caller (F-5), read from the access token; registered before AddModulePersistence's
// fallback so the audit interceptor sees the real user instead of AnonymousUser.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, ClaimsPrincipalCurrentUser>();

// Refresh-token sessions and the access-token revocation set (F-5). The Aspire client integration (not
// a plain ConnectionMultiplexer.Connect) is what trusts the local Redis container's TLS certificate.
builder.AddRedisClient("redis");

// Modules (F-4: Identity; F-5: sign-in, sign-out, OpenIddict).
builder.Services.AddIdentityModule(
    builder.Configuration,
    builder.Configuration.GetConnectionString("simulab") ?? throw new InvalidOperationException("The connection string 'simulab' is missing."),
    builder.Environment.IsDevelopment());
builder.Services.AddSingleton<ClientRateLimiter>();
builder.Services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
builder.Services.AddAuthorization();
builder.Services.AddIdentityAuthorization();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<RevocationCheckMiddleware>();

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

// OpenIddict's own protocol path (F-5, decision 1): outside /api/v1, its own error shape.
app.MapTokenEndpoints();

// Development applies the module migrations on start; a release applies them from the pipeline.
// A test host that does not need a database turns it off with Database:ApplyMigrationsOnStart.
if (app.Configuration.GetValue("Database:ApplyMigrationsOnStart", app.Environment.IsDevelopment()))
{
    await app.Services.MigrateIdentityModuleAsync();
    await app.Services.EnsureIdentityClientAsync(app.Configuration);
    await app.Services.EnsureRolesAndPermissionsAsync();
}

app.Run();
