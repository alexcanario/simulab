using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenIddict.Abstractions;
using Simulab.Identity.Application.Abstractions;
using Simulab.Identity.Application.Passwords;
using Simulab.Identity.Application.Profile;
using Simulab.Identity.Application.Registration;
using Simulab.Identity.Application.Roles;
using Simulab.Identity.Application.Sessions;
using Simulab.Identity.Application.Verification;
using Simulab.Identity.Contracts;
using Simulab.Identity.Domain.Entities;
using Simulab.Identity.Infrastructure.Authorization;
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
            .AddRoles<Role>()
            .AddUserStore<UserStore<User, Role, IdentityModuleDbContext, Guid>>()
            .AddRoleStore<RoleStore<Role, IdentityModuleDbContext, Guid>>()
            // F-7: ResetPasswordAsync asks for Identity's own reset token. Our emailed token is the real proof;
            // this provider only issues the key that API needs, in the same call (ResetPasswordHandler).
            .AddTokenProvider<DataProtectorTokenProvider<User>>(TokenOptions.DefaultProvider);

        // BR3: the 10s cache mirrors Simulae's pattern - a revoked permission takes effect almost
        // immediately without the Api ever trusting a token claim. It reads the same TimeProvider as the
        // rest of the host, so a test can move the clock instead of sleeping 10 real seconds.
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<PermissionCache>();
        services.AddScoped<IPermissionQueryService, PermissionQueryService>();

        services.AddScoped<IEmailVerificationTokenStore, EmailVerificationTokenStore>();
        services.AddScoped<IConsentRecordStore, ConsentRecordStore>();
        services.AddScoped<IUserDirectory, UserDirectory>();
        services.AddScoped<IVerificationMailer, VerificationMailer>();
        services.AddScoped<IPasswordResetTokenStore, PasswordResetTokenStore>();
        services.AddScoped<IPasswordMailer, PasswordMailer>();
        services.AddSingleton<ILegalDocumentProvider, LegalDocumentProvider>();

        // The verification email is written from this module's own resources.
        services.AddLocalization();

        services.AddScoped<RegisterUserHandler>();
        services.AddScoped<VerifyEmailHandler>();
        services.AddScoped<ResendVerificationHandler>();
        services.AddScoped<RequestPasswordResetHandler>();
        services.AddScoped<CheckPasswordResetTokenHandler>();
        services.AddScoped<ResetPasswordHandler>();
        services.AddScoped<ChangePasswordHandler>();
        services.AddScoped<ProfileHandler>();

        // F-9: the role management back office.
        services.AddScoped<IRoleAdministrationStore, RoleAdministrationStore>();
        services.AddScoped<IRoleAdministrationQueries, RoleAdministrationQueries>();
        services.AddScoped<SaveRoleHandler>();
        services.AddScoped<DeleteRoleHandler>();
        services.AddScoped<SetUserRolesHandler>();

        services.AddOptions<LegalContentOptions>().Bind(configuration.GetSection(LegalContentOptions.SectionName));
        services.AddOptions<VerificationEmailOptions>().Bind(configuration.GetSection(VerificationEmailOptions.SectionName));
        services.AddOptions<PasswordEmailOptions>().Bind(configuration.GetSection(PasswordEmailOptions.SectionName));

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

    /// <summary>
    /// Creates the seed roles (F-6, BR1) and the one seed permission (BR8), granted to Admin, if they do
    /// not exist yet. Idempotent, like <see cref="EnsureIdentityClientAsync"/>.
    /// </summary>
    public static async Task EnsureRolesAndPermissionsAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);

        await using var scope = services.CreateAsyncScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
        var context = scope.ServiceProvider.GetRequiredService<IdentityModuleDbContext>();

        // F-9, BR1: the seed roles are system roles; one created before F-9 is marked on the next start.
        foreach (var name in IdentityRoles.All)
        {
            var existing = await roleManager.FindByNameAsync(name);
            if (existing is null)
            {
                await roleManager.CreateAsync(Role.CreateSystem(name));
            }
            else if (!existing.IsSystem)
            {
                existing.MarkAsSystem();
                await roleManager.UpdateAsync(existing);
            }
        }

        if (await context.Permissions.FindAsync([IdentityPermissions.RolesManage], cancellationToken) is null)
        {
            context.Permissions.Add(new Permission { Name = IdentityPermissions.RolesManage });
        }

        var adminRole = await roleManager.FindByNameAsync(IdentityRoles.Admin);
        if (adminRole is not null)
        {
            var granted = await context.RolePermissions.FindAsync([adminRole.Id, IdentityPermissions.RolesManage], cancellationToken);
            if (granted is null)
            {
                context.RolePermissions.Add(new RolePermission { RoleId = adminRole.Id, PermissionName = IdentityPermissions.RolesManage });
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
