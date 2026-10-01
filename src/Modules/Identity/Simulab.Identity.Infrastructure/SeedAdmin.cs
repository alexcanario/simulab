using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Simulab.Identity.Contracts;
using Simulab.Identity.Domain.Entities;

namespace Simulab.Identity.Infrastructure;

/// <summary>F-52: the first administrator account, seeded from configuration on every start.</summary>
public static class SeedAdmin
{
    /// <summary>BR1: fixed. The <c>.local</c> domain cannot receive the verification email, so the account is created verified.</summary>
    public const string Email = "admin@simulab.local";

    public const string DisplayName = "Administrator";

    /// <summary>BR3: the only setting; a missing or empty value means nothing is seeded.</summary>
    public const string PasswordKey = "Identity:SeedAdmin:Password";

    /// <summary>
    /// Creates <see cref="Email"/> with the Admin role when <see cref="PasswordKey"/> is configured (BR3).
    /// The roles and permissions are ensured first (BR6): a release does not run them with the migrations.
    /// An existing account keeps its password, status and two-factor state and only gains the role (BR5).
    /// The password is never logged and never part of an exception message (BR3, BR4).
    /// </summary>
    public static async Task EnsureSeedAdminAsync(
        this IServiceProvider services,
        IConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var password = configuration[PasswordKey];
        if (string.IsNullOrEmpty(password))
        {
            return;
        }

        await services.EnsureRolesAndPermissionsAsync(cancellationToken);

        await using var scope = services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(SeedAdmin));
        var time = scope.ServiceProvider.GetRequiredService<TimeProvider>();

        var user = await users.FindByEmailAsync(Email);
        if (user is null)
        {
            user = new User { Id = Guid.CreateVersion7(), UserName = Email, Email = Email, FullName = DisplayName };
            user.VerifyEmail(time.GetUtcNow());

            var created = await users.CreateAsync(user, password);
            if (!created.Succeeded)
            {
                // Error codes only: an identity error description can echo the rejected value.
                throw new InvalidOperationException(
                    $"The configuration '{PasswordKey}' cannot be used for the seed administrator: "
                    + string.Join(", ", created.Errors.Select(error => error.Code)) + ".");
            }

            logger.LogInformation("Seed administrator {Email} created.", Email);
        }

        if (!await users.IsInRoleAsync(user, IdentityRoles.Admin))
        {
            var added = await users.AddToRoleAsync(user, IdentityRoles.Admin);
            if (!added.Succeeded)
            {
                throw new InvalidOperationException(
                    $"The seed administrator could not be given the {IdentityRoles.Admin} role: "
                    + string.Join(", ", added.Errors.Select(error => error.Code)) + ".");
            }

            logger.LogInformation("Seed administrator {Email} holds the {Role} role.", Email, IdentityRoles.Admin);
        }
    }
}
