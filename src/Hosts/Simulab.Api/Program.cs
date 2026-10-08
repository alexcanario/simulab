using System.Reflection;
using OpenIddict.Validation.AspNetCore;
using Simulab.Ai;
using Simulab.Api;
using Simulab.Api.Features.Ai;
using Simulab.Api.Features.System;
using Simulab.Catalog.Api;
using Simulab.Catalog.Infrastructure;
using Simulab.Email;
using Simulab.Identity.Api;
using Simulab.Identity.Api.Authorization;
using Simulab.Identity.Infrastructure;
using Simulab.Jobs;
using Simulab.Persistence;
using Simulab.ServiceDefaults;
using Simulab.SharedKernel.Messaging;
using Simulab.SharedKernel.Security;
using Simulab.SharedKernel.Serialization;

var builder = WebApplication.CreateBuilder(args);

// F-64 BR7: in the cloud the secrets (the seeded admin password, the OpenIddict certificates) are read from Key Vault
// as configuration, before anything below reads it. A local run has no vault and changes nothing.
if (!string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("keyvault")))
{
    builder.Configuration.AddAzureKeyVaultSecrets("keyvault");
}

// F-64: the cloud database accepts only encrypted connections; its connection string says nothing about SSL.
builder.Configuration.RequireDatabaseTls();

builder.AddServiceDefaults();
builder.AddAppDataProtection("simulab-api");
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.ConfigureHttpJsonOptions(options => AppJson.Configure(options.SerializerOptions));

// Foundation (F-3): database and its health check, module persistence services, email and in-process events.
builder.Services.AddAppDatabase(builder.Configuration.GetConnectionString("simulab")
    ?? throw new InvalidOperationException("The connection string 'simulab' is missing."));
builder.Services.AddModulePersistence();
builder.Services.AddEmailSender(
    builder.Configuration,
    builder.Configuration.GetConnectionString("mailpit"),
    cloudEnvironment: !builder.Environment.IsDevelopment());
builder.Services.AddIntegrationEvents();

// The one door to a model (F-41, BR1). Without a key the host still starts and every call fails with
// ai.not_configured (BR4), so nobody needs a key to work on the rest of the app.
builder.Services.AddAiGateway(
    builder.Configuration,
    builder.Configuration.GetConnectionString("simulab")
        ?? throw new InvalidOperationException("The connection string 'simulab' is missing."));

// The job table and its worker (F-13, ADR-0001 #20). Every identity email leaves through it, so no
// request ever waits for the mail server.
builder.Services.AddJobs(
    builder.Configuration,
    builder.Configuration.GetConnectionString("simulab")
        ?? throw new InvalidOperationException("The connection string 'simulab' is missing."));

// The signed-in caller (F-5), read from the access token; registered before AddModulePersistence's
// fallback so the audit interceptor sees the real user instead of AnonymousUser.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, ClaimsPrincipalCurrentUser>();

// F-21 BR2: the address the account event trail records, read exactly as the rate limiter reads it.
// Registered before AddIdentityModule's fallback, as the caller above is.
builder.Services.AddScoped<ICallerAddress, HttpContextCallerAddress>();

// Refresh-token sessions and the access-token revocation set (F-5). The Aspire client integration (not
// a plain ConnectionMultiplexer.Connect) is what trusts the local Redis container's TLS certificate.
builder.AddRedisClient("redis");

// Modules (F-4: Identity; F-5: sign-in, sign-out, OpenIddict).
builder.Services.AddIdentityModule(
    builder.Configuration,
    builder.Configuration.GetConnectionString("simulab") ?? throw new InvalidOperationException("The connection string 'simulab' is missing."),
    builder.Environment.IsDevelopment());
// F-33: the Catalog module. Registered after Identity so the permission catalogs are both in the
// container before EnsureRolesAndPermissionsAsync reads them (F-33, BR2).
builder.Services.AddCatalogModule(
    builder.Configuration.GetConnectionString("simulab") ?? throw new InvalidOperationException("The connection string 'simulab' is missing."));

builder.Services.AddSingleton<ClientRateLimiter>();
builder.Services.AddSingleton<ClientAddress>();
builder.Services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
builder.Services.AddAuthorization();
builder.Services.AddIdentityAuthorization();

// F-64 BR6: X-Forwarded-For and X-Forwarded-Proto only from the proxies the configuration lists (none in dev).
// Behind the cloud ingress the request arrives as http; OpenIddict refuses it unless the scheme is believed.
var forwarded = new ForwardedHeadersOptions();
var behindTrustedProxy = TrustedProxies.Configure(forwarded, builder.Configuration);

var app = builder.Build();

if (behindTrustedProxy)
{
    app.UseUnlistedSenderLog(forwarded);
    app.UseForwardedHeaders(forwarded);
}
else if (!app.Environment.IsDevelopment())
{
    app.UseUnlistedProxyLog();
}

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
v1.MapCatalogEndpoints();

// F-41 (v2), AC9b: the diagnostics route exists only in Development. Outside it the route is never
// mapped, so it answers 404 rather than being merely hidden.
if (app.Environment.IsDevelopment())
{
    v1.MapAiDiagnosticsEndpoints();
}

// OpenIddict's own protocol path (F-5, decision 1): outside /api/v1, its own error shape.
app.MapTokenEndpoints();

// Development applies the module migrations on start; a release applies them from the pipeline.
// A test host that does not need a database turns it off with Database:ApplyMigrationsOnStart.
if (app.Configuration.GetValue("Database:ApplyMigrationsOnStart", app.Environment.IsDevelopment()))
{
    await app.Services.MigrateAiAsync();
    await app.Services.MigrateJobsAsync();
    await app.Services.MigrateIdentityModuleAsync();
    await app.Services.MigrateCatalogModuleAsync();
    await app.Services.EnsureIdentityClientAsync(app.Configuration);
    await app.Services.EnsureRolesAndPermissionsAsync();
}

// F-52: the first administrator, in every environment and independent of the migrations switch above.
// Without Identity:SeedAdmin:Password nothing happens.
await app.Services.EnsureSeedAdminAsync(app.Configuration);

app.Run();
