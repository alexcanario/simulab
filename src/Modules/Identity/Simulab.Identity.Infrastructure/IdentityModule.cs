using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using OpenIddict.Abstractions;
using Simulab.Identity.Application.Abstractions;
using Simulab.Identity.Application.Account;
using Simulab.Identity.Application.GoogleSignIn;
using Simulab.Identity.Application.Passwords;
using Simulab.Identity.Application.Profile;
using Simulab.Identity.Application.Registration;
using Simulab.Identity.Application.Roles;
using Simulab.Identity.Application.Sessions;
using Simulab.Identity.Application.Totp;
using Simulab.Identity.Application.Verification;
using Simulab.Identity.Contracts;
using Simulab.Identity.Domain.Entities;
using Simulab.Identity.Infrastructure.Authorization;
using Simulab.Identity.Infrastructure.Content;
using Simulab.Identity.Infrastructure.Account;
using Simulab.Identity.Infrastructure.Email;
using Simulab.Identity.Infrastructure.GoogleSignIn;
using Simulab.Identity.Infrastructure.Persistence;
using Simulab.Identity.Infrastructure.Sessions;
using Simulab.Identity.Infrastructure.Totp;
using Simulab.Jobs;
using Simulab.Persistence;
using Simulab.SharedKernel.Security;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Simulab.Identity.Infrastructure;

/// <summary>Everything the Identity module needs, registered by the host in one call.</summary>
public static class IdentityModule
{
    /// <summary>Client id of the single first-party confidential client (F-5, decision 2).</summary>
    public const string WebClientId = "simulab-web";

    /// <summary>The custom grant of the code step at sign-in (F-11 BR9): <c>challenge</c> and <c>code</c> in, tokens out.</summary>
    public const string TotpGrantType = "totp";

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

        // F-33 BR2: this module's own permission names, seeded together with every other module's by
        // EnsureRolesAndPermissionsAsync. The `permissions` table is Identity's; the names are not.
        services.AddSingleton(new PermissionCatalog("identity", IdentityPermissions.All));

        // BR3: the 10s cache mirrors Simulae's pattern - a revoked permission takes effect almost
        // immediately without the Api ever trusting a token claim. It reads the same TimeProvider as the
        // rest of the host, so a test can move the clock instead of sleeping 10 real seconds.
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<PermissionCache>();
        services.AddScoped<IPermissionQueryService, PermissionQueryService>();

        // F-13 BR2: the module's own context is the unit of work a mailer stages its job on.
        services.AddJobQueueFor<IdentityModuleDbContext>();
        services.AddScoped<IIdentityUnitOfWork, IdentityUnitOfWork>();

        services.AddScoped<IEmailVerificationTokenStore, EmailVerificationTokenStore>();
        services.AddScoped<IConsentRecordStore, ConsentRecordStore>();
        services.AddScoped<IUserDirectory, UserDirectory>();
        services.AddScoped<IVerificationMailer, VerificationMailer>();
        services.AddScoped<IPasswordResetTokenStore, PasswordResetTokenStore>();
        services.AddScoped<IPasswordMailer, PasswordMailer>();
        services.AddScoped<IErasureMailer, ErasureMailer>();
        services.AddScoped<IErasureFollowUp, ErasureFollowUp>();
        services.AddScoped<IJobHandler, AccountErasedJobHandler>();
        services.AddSingleton<ILegalDocumentProvider, LegalDocumentProvider>();

        // The verification email is written from this module's own resources.
        services.AddLocalization();

        services.AddScoped<RegistrationTerms>();
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

        // F-10: erasing the own account reuses the role-administration transaction (BR11).
        services.AddScoped<IAccountErasureStore, AccountErasureStore>();
        services.AddScoped<EraseAccountHandler>();

        // F-21: the account event trail. The log is used by almost every handler above, and by the token
        // endpoint; the queries serve the back office page. A host that does not know the caller's address
        // (a worker, a test that calls a handler directly) gets the null one.
        services.TryAddScoped<ICallerAddress, NoCallerAddress>();
        services.AddScoped<IAccountEventLog, AccountEventLog>();
        services.AddScoped<IAccountEventQueries, AccountEventQueries>();

        // F-16: the user downloads their own data, confirmed with the password as an erasure is.
        services.AddScoped<IDataExportQueries, DataExportQueries>();
        services.AddScoped<IDataExportMailer, DataExportMailer>();
        services.AddScoped<ExportDataHandler>();

        services.AddOptions<LegalContentOptions>().Bind(configuration.GetSection(LegalContentOptions.SectionName));
        services.AddOptions<VerificationEmailOptions>().Bind(configuration.GetSection(VerificationEmailOptions.SectionName));
        services.AddOptions<PasswordEmailOptions>().Bind(configuration.GetSection(PasswordEmailOptions.SectionName));
        services.AddOptions<ErasureEmailOptions>().Bind(configuration.GetSection(ErasureEmailOptions.SectionName));

        // Refresh-token sessions and the access-token revocation set (F-5, BR4-BR7).
        services.AddScoped<IRefreshSessionStore, RedisRefreshSessionStore>();

        // F-11 BR12: two-factor is registered only while it is on; the key is checked when the host starts (AC14).
        var totp = configuration.GetSection(TotpOptions.SectionName).Get<TotpOptions>() ?? new TotpOptions();
        services.AddOptions<TotpOptions>()
            .Bind(configuration.GetSection(TotpOptions.SectionName))
            .Validate(options => options.Problem() is null, totp.Problem() ?? "Identity:TotpEncryptionKey is not valid.")
            .ValidateOnStart();

        if (totp.TotpEnabled)
        {
            services.AddSingleton<ITotpAuthenticator, TotpAuthenticator>();
            services.AddSingleton<ITotpSecretProtector, AesGcmTotpSecretProtector>();
            services.AddScoped<ITotpChallengeStore, RedisTotpChallengeStore>();
            services.AddScoped<RecoveryCodes>();
            services.AddScoped<SecondFactor>();
            services.AddScoped<TotpAccountHandler>();
            services.AddScoped<TotpSignInHandler>();
        }

        // F-20 BR1: Google sign-in is registered only while it is on; the client id is checked when the host starts.
        var google = configuration.GetSection(GoogleSignInOptions.SectionName).Get<GoogleSignInOptions>() ?? new GoogleSignInOptions();
        var googleClient = configuration.GetSection(GoogleClientOptions.SectionName).Get<GoogleClientOptions>() ?? new GoogleClientOptions();
        services.AddOptions<GoogleSignInOptions>().Bind(configuration.GetSection(GoogleSignInOptions.SectionName));
        services.AddOptions<GoogleClientOptions>()
            .Bind(configuration.GetSection(GoogleClientOptions.SectionName))
            .Validate(options => options.Problem(google.GoogleSignInEnabled) is null, googleClient.Problem(google.GoogleSignInEnabled) ?? "Authentication:Google is not valid.")
            .ValidateOnStart();

        if (google.GoogleSignInEnabled)
        {
            // BR2: Google's keys, from its discovery document, cached and refreshed by the library.
            services.AddSingleton<IConfigurationManager<OpenIdConnectConfiguration>>(_ =>
                new ConfigurationManager<OpenIdConnectConfiguration>(
                    GoogleIdTokenValidator.DiscoveryDocument,
                    new OpenIdConnectConfigurationRetriever(),
                    new HttpDocumentRetriever { RequireHttps = true }));
            services.AddSingleton<IGoogleIdTokenValidator, GoogleIdTokenValidator>();
            services.AddScoped<GoogleSignInHandler>();
            services.AddScoped<RegisterGoogleUserHandler>();
            // F-29: the account's own link, behind the same switch as the rest of the feature.
            services.AddScoped<GoogleLinkHandler>();
        }

        services.AddOpenIddict()
            .AddCore(options => options.UseEntityFrameworkCore().UseDbContext<IdentityModuleDbContext>())
            .AddServer(options =>
            {
                options.SetTokenEndpointUris("connect/token");

                // Password flow is first-party only (ADR-0001 #12); refresh keeps a session alive silently.
                options.AllowPasswordFlow();
                options.AllowRefreshTokenFlow();

                // F-11 BR9: the code step of a two-factor sign-in, only while the feature is on (BR12).
                if (totp.TotpEnabled)
                {
                    options.AllowCustomFlow(TotpGrantType);
                }

                // F-20: the Google step of a sign-in, only while the feature is on (BR1).
                if (google.GoogleSignInEnabled)
                {
                    options.AllowCustomFlow(GoogleSignInProtocol.GrantType);
                }

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
    public static Task MigrateIdentityModuleAsync(this IServiceProvider services, CancellationToken cancellationToken = default) =>
        services.MigrateModuleAsync<IdentityModuleDbContext>(cancellationToken);

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

        var existing = await manager.FindByClientIdAsync(WebClientId, cancellationToken);
        if (existing is not null)
        {
            // F-11, F-20: a client registered before a custom grant existed gains its permission on the next start.
            var permissions = await manager.GetPermissionsAsync(existing, cancellationToken);
            var missing = CustomGrantPermissions.Where(permission => !permissions.Contains(permission)).ToList();
            if (missing.Count > 0)
            {
                var descriptor = new OpenIddictApplicationDescriptor();
                await manager.PopulateAsync(descriptor, existing, cancellationToken);
                descriptor.Permissions.UnionWith(missing);
                await manager.UpdateAsync(existing, descriptor, cancellationToken);
            }

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
                Permissions.GrantTypes.RefreshToken,
                TotpGrantPermission,
                GoogleGrantPermission
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Granted whether the feature is on or off: the permission alone opens nothing, because the server only
    /// accepts the grant while <c>Identity:TotpEnabled</c> is true (BR12), and turning it on needs no data change.
    /// </summary>
    private const string TotpGrantPermission = Permissions.Prefixes.GrantType + TotpGrantType;

    /// <summary>F-20 BR1: granted on or off for the same reason as <see cref="TotpGrantPermission"/>.</summary>
    private const string GoogleGrantPermission = Permissions.Prefixes.GrantType + GoogleSignInProtocol.GrantType;

    private static readonly string[] CustomGrantPermissions = [TotpGrantPermission, GoogleGrantPermission];

    /// <summary>
    /// Creates the seed roles (F-6, BR1) and every permission the registered modules declare (F-33, BR2),
    /// each granted to Admin (BR3), if they do not exist yet. Idempotent, like <see cref="EnsureIdentityClientAsync"/>.
    /// </summary>
    public static async Task EnsureRolesAndPermissionsAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);

        await using var scope = services.CreateAsyncScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
        var context = scope.ServiceProvider.GetRequiredService<IdentityModuleDbContext>();
        var catalogs = scope.ServiceProvider.GetServices<PermissionCatalog>();

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

        // F-33 BR2: the union of what every registered module declares, in a stable order so two starts
        // write the same rows. A module that is not registered contributes nothing and loses nothing.
        var declared = catalogs
            .SelectMany(catalog => catalog.Permissions)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();

        var adminRole = await roleManager.FindByNameAsync(IdentityRoles.Admin);

        // F-36 BR10: who holds a permission from the start, by name. Read only when the row is created below.
        var initialGrants = catalogs
            .Where(catalog => catalog.InitialGrants is not null)
            .SelectMany(catalog => catalog.InitialGrants!)
            .ToLookup(grant => grant.Key, grant => grant.Value, StringComparer.Ordinal);

        foreach (var name in declared)
        {
            if (await context.Permissions.FindAsync([name], cancellationToken) is null)
            {
                context.Permissions.Add(new Permission { Name = name });

                // Only in the start that creates the row: a later start must not give back what the roles
                // back office took away. Admin is left out (it has its own path below).
                var roleNames = initialGrants[name].SelectMany(roles => roles)
                    .Where(role => !string.Equals(role, IdentityRoles.Admin, StringComparison.Ordinal))
                    .Distinct(StringComparer.Ordinal);
                foreach (var roleName in roleNames)
                {
                    var role = await roleManager.FindByNameAsync(roleName);
                    if (role is not null)
                    {
                        context.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionName = name });
                    }
                }
            }

            // BR3: Admin holds every permission there is; the other seed roles get theirs from the back office.
            if (adminRole is not null
                && await context.RolePermissions.FindAsync([adminRole.Id, name], cancellationToken) is null)
            {
                context.RolePermissions.Add(new RolePermission { RoleId = adminRole.Id, PermissionName = name });
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
