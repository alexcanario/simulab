using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using Simulab.Identity.Application.Abstractions;
using Simulab.Identity.Application.Registration;
using Simulab.Identity.Application.Sessions;
using Simulab.Identity.Application.Verification;
using Simulab.Identity.Contracts;
using Simulab.Identity.Domain.Entities;
using Simulab.Identity.Infrastructure.Content;
using Simulab.Identity.Infrastructure.Email;
using Simulab.Identity.Infrastructure.Persistence;
using Simulab.Identity.Infrastructure.Sessions;
using Simulab.Persistence;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Simulab.Identity.Infrastructure;

/// <summary>Everything the Identity module needs, registered by the host in one call.</summary>
public static class IdentityModule
{
    /// <summary>Client id of the single first-party confidential client (F-5, decision 2).</summary>
    public const string WebClientId = "simulab-web";

    /// <summary>
    /// <paramref name="services"/> already has an <c>IConnectionMultiplexer</c> registered by the host
    /// (<c>builder.AddRedisClient("redis")</c>): that Aspire client integration is what trusts the local
    /// Redis container's TLS certificate, which a plain <c>ConnectionMultiplexer.Connect</c> call cannot.
    /// </summary>
    public static IServiceCollection AddIdentityModule(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionString,
        bool isDevelopment = false)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDbContext<IdentityModuleDbContext>((provider, options) =>
            ((DbContextOptionsBuilder<IdentityModuleDbContext>)options)
                .UseNpgsql(connectionString, npgsql => npgsql.UseModuleHistoryTable(IdentityModuleDbContext.SchemaName))
                .UseModuleConventions(provider));

        // Identity core plus the sign-in pieces OpenIddict needs (lockout, password checks) - F-5.
        services.AddIdentityCore<User>(identity =>
            {
                // BR2: Simulae's policy, already in use there.
                identity.Password.RequiredLength = 12;
                identity.Password.RequireUppercase = true;
                identity.Password.RequireDigit = true;
                identity.Password.RequireNonAlphanumeric = true;
                identity.User.RequireUniqueEmail = true;
                identity.SignIn.RequireConfirmedEmail = true;
                identity.Lockout.MaxFailedAccessAttempts = 5;
                identity.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddUserStore<UserOnlyStore<User, IdentityModuleDbContext, Guid>>();

        services.AddScoped<IEmailVerificationTokenStore, EmailVerificationTokenStore>();
        services.AddScoped<IConsentRecordStore, ConsentRecordStore>();
        services.AddScoped<IUserDirectory, UserDirectory>();
        services.AddScoped<IVerificationMailer, VerificationMailer>();
        services.AddSingleton<ILegalDocumentProvider, LegalDocumentProvider>();

        // The verification email is written from this module's own resources.
        services.AddLocalization();

        services.AddScoped<RegisterUserHandler>();
        services.AddScoped<VerifyEmailHandler>();
        services.AddScoped<ResendVerificationHandler>();

        services.AddOptions<LegalContentOptions>().Bind(configuration.GetSection(LegalContentOptions.SectionName));
        services.AddOptions<VerificationEmailOptions>().Bind(configuration.GetSection(VerificationEmailOptions.SectionName));

        // Refresh-token sessions and the access-token revocation set (F-5, BR4-BR7).
        services.AddScoped<IRefreshSessionStore, RedisRefreshSessionStore>();

        services.AddOpenIddict()
            .AddCore(options => options.UseEntityFrameworkCore().UseDbContext<IdentityModuleDbContext>())
            .AddServer(options =>
            {
                options.SetTokenEndpointUris("connect/token");

                // Password flow is first-party only (ADR-0001 #12); refresh keeps a session alive silently.
                options.AllowPasswordFlow();
                options.AllowRefreshTokenFlow();

                options.SetAccessTokenLifetime(TokenLifetimes.AccessToken);
                options.SetRefreshTokenLifetime(TokenLifetimes.RefreshToken);

                // Development-only certificates (F-5, decision: staging/production stay `planned`, docs/infra.md).
                options.AddDevelopmentEncryptionCertificate()
                    .AddDevelopmentSigningCertificate();

                // Custom endpoint below (TokenEndpoints.cs) does the actual credential check; OpenIddict
                // only validates the protocol shape and the client before passing the request through.
                var aspNetCore = options.UseAspNetCore()
                    .EnableTokenEndpointPassthrough();

                // Development and the test hosts (WebApplicationFactory) talk plain HTTP; a real
                // environment always sits behind TLS (docs/infra.md), so this never applies there.
                if (isDevelopment)
                {
                    aspNetCore.DisableTransportSecurityRequirement();
                }
            })
            .AddValidation(options =>
            {
                options.UseLocalServer();
                options.UseAspNetCore();
            });

        return services;
    }

    /// <summary>Applies the module's migrations. Development only; a release applies them from the pipeline.</summary>
    public static async Task MigrateIdentityModuleAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);

        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityModuleDbContext>();
        await context.Database.MigrateAsync(cancellationToken);
    }

    /// <summary>
    /// Registers the one first-party client (F-5, decision 2) if it does not exist yet. Its secret is
    /// configuration on both hosts (Web calls the token endpoint, Api validates it), never the browser.
    /// </summary>
    public static async Task EnsureIdentityClientAsync(
        this IServiceProvider services,
        IConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        await using var scope = services.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        if (await manager.FindByClientIdAsync(WebClientId, cancellationToken) is not null)
        {
            return;
        }

        var secret = configuration["Authentication:OpenIddict:ClientSecret"]
            ?? throw new InvalidOperationException("The configuration 'Authentication:OpenIddict:ClientSecret' is missing.");

        await manager.CreateAsync(new OpenIddictApplicationDescriptor
        {
            ClientId = WebClientId,
            ClientSecret = secret,
            ClientType = ClientTypes.Confidential,
            Permissions =
            {
                Permissions.Endpoints.Token,
                Permissions.GrantTypes.Password,
                Permissions.GrantTypes.RefreshToken
            }
        }, cancellationToken);
    }
}
