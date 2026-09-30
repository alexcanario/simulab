using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Simulab.Catalog.Contracts;
using Simulab.Identity.Contracts;
using Simulab.Identity.Domain.Entities;
using Simulab.Identity.Infrastructure;
using Simulab.Identity.Infrastructure.Persistence;
using Simulab.Testing;

namespace Simulab.Identity.Tests;

/// <summary>F-52: the first administrator account, seeded from <c>Identity:SeedAdmin:Password</c> on every start.</summary>
public sealed class SeedAdminTests
{
    private const string Password = Accounts.ValidPassword;

    [Fact]
    public async Task Start_ValidPasswordAndEmptyDatabase_CreatesTheActiveAdmin()
    {
        await using var factory = await StartedAsync(Password);

        var (user, roles) = await ReadAsync(factory, async (context, users) =>
        {
            var found = await users.FindByEmailAsync(SeedAdmin.Email);
            return (found, found is null ? [] : await users.GetRolesAsync(found));
        });

        user.Should().NotBeNull();
        user!.Status.Should().Be(AccountStatus.Active);
        user.EmailConfirmed.Should().BeTrue();
        user.EmailVerifiedAt.Should().NotBeNull();
        user.TwoFactorEnabled.Should().BeFalse();
        user.FullName.Should().Be(SeedAdmin.DisplayName);
        roles.Should().Equal(IdentityRoles.Admin);
    }

    [Fact]
    public async Task SignIn_SeededAccount_ReceivesTokens()
    {
        await using var factory = await StartedAsync(Password);

        var token = await TokenClient.SignInAsync(factory.CreateClient(), SeedAdmin.Email, Password);

        token.AccessToken.Should().NotBeNullOrEmpty(token.ErrorDescription);
    }

    [Fact]
    public async Task ListRoles_SeededAccount_IsAllowedByThePermission()
    {
        await using var factory = await StartedAsync(Password);
        var admin = await Accounts.SignedInAsync(factory.CreateClient(), SeedAdmin.Email);

        var response = await admin.GetAsync("/api/v1/identity/roles");

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Start_SeededAccount_HoldsEveryDeclaredPermissionThroughAdmin()
    {
        await using var factory = await StartedAsync(Password);

        var granted = await ReadAsync(factory, async (context, users) =>
        {
            var adminRole = await context.Roles.SingleAsync(role => role.Name == IdentityRoles.Admin);
            return await context.RolePermissions
                .Where(grant => grant.RoleId == adminRole.Id)
                .Select(grant => grant.PermissionName)
                .ToListAsync();
        });

        granted.Should().BeEquivalentTo([.. IdentityPermissions.All, .. CatalogPermissions.All]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Start_NoPassword_CreatesNoAccountAndStarts(string? password)
    {
        await using var factory = await StartedAsync(password);

        // The host answered a request, so it started; the seed ran or not before that.
        using var response = await factory.CreateClient().GetAsync("/openapi/v1.json");
        var count = await ReadAsync(factory, (context, users) => context.Users.CountAsync());

        response.IsSuccessStatusCode.Should().BeTrue();
        count.Should().Be(0);
    }

    [Fact]
    public async Task Ensure_ExistingAccountWithOtherPasswordAndNoRole_KeepsPasswordAndGainsAdmin()
    {
        await using var factory = await StartedAsync(null);
        await Accounts.CreateAsync(factory.Services, SeedAdmin.Email);

        await factory.Services.EnsureSeedAdminAsync(Configure("Another#Password9"));

        var (roles, keptPassword, count) = await ReadAsync(factory, async (context, users) =>
        {
            var found = (await users.FindByEmailAsync(SeedAdmin.Email))!;
            return (
                await users.GetRolesAsync(found),
                await users.CheckPasswordAsync(found, Accounts.ValidPassword),
                await context.Users.CountAsync(user => user.NormalizedEmail == SeedAdmin.Email.ToUpperInvariant()));
        });

        roles.Should().Equal(IdentityRoles.Admin);
        keptPassword.Should().BeTrue("an existing account keeps its own password (BR5)");
        count.Should().Be(1);
    }

    [Fact]
    public async Task Start_TwiceOnTheSameDatabase_LeavesOneUntouchedAccount()
    {
        await using var factory = await StartedAsync(Password);
        var before = await ReadAsync(factory, async (context, users) => (await users.FindByEmailAsync(SeedAdmin.Email))!);

        await factory.Services.EnsureSeedAdminAsync(Configure(Password));

        var (after, count) = await ReadAsync(factory, async (context, users) =>
            ((await users.FindByEmailAsync(SeedAdmin.Email))!, await context.Users.CountAsync()));
        count.Should().Be(1);
        after.Id.Should().Be(before.Id);
        after.PasswordHash.Should().Be(before.PasswordHash);
        after.SecurityStamp.Should().Be(before.SecurityStamp);
        after.UpdatedAt.Should().Be(before.UpdatedAt);
    }

    [Fact]
    public async Task Start_PasswordBreaksThePolicy_FailsWithoutRevealingIt()
    {
        const string weak = "weakpass";
        using var factory = NewFactory(weak);
        await factory.PrepareAsync($"seed_admin_weak_{Guid.NewGuid():N}");

        var start = () => factory.CreateClient();

        var thrown = start.Should().Throw<InvalidOperationException>().Which;
        thrown.Message.Should().Contain(SeedAdmin.PasswordKey).And.NotContain(weak);
    }

    [Fact]
    public async Task Start_ReleaseWithoutMigrationsOnStart_SeedsRolesAndAdmin()
    {
        string connectionString;
        await using (var first = await StartedAsync(null))
        {
            connectionString = first.ConnectionString;

            // A release finds the schema from the pipeline but no roles: they are the seed's to create.
            await ReadAsync(first, async (context, users) =>
            {
                context.RolePermissions.RemoveRange(context.RolePermissions);
                await context.SaveChangesAsync();
                context.Roles.RemoveRange(context.Roles);
                await context.SaveChangesAsync();
                return 0;
            });
        }

        using var release = NewFactory(Password, migrateOnStart: false);
        await release.PrepareExistingAsync(connectionString);
        _ = release.CreateClient();

        var (roleNames, roles) = await ReadAsync(release, async (context, users) =>
            (await context.Roles.Select(role => role.Name!).ToListAsync(), await users.GetRolesAsync((await users.FindByEmailAsync(SeedAdmin.Email))!)));
        roleNames.Should().BeEquivalentTo(IdentityRoles.All);
        roles.Should().Equal(IdentityRoles.Admin);
    }

    [Fact]
    public async Task Start_DevelopmentSettings_SeedTheAdminWithTheirOwnPassword()
    {
        using var factory = new IdentityApiFactory { KeepDevelopmentSeedAdmin = true };
        await factory.PrepareAsync($"seed_admin_dev_{Guid.NewGuid():N}");

        var token = await TokenClient.SignInAsync(factory.CreateClient(), SeedAdmin.Email, DevelopmentPassword());

        token.AccessToken.Should().NotBeNullOrEmpty(token.ErrorDescription);
    }

    [Fact]
    public async Task Start_WithPassword_NeverLogsIt()
    {
        var logs = new RecordingLoggerProvider();
        using var factory = NewFactory(Password);
        factory.ConfigureHost += builder => builder.ConfigureLogging(logging => logging.AddProvider(logs));
        await factory.PrepareAsync($"seed_admin_log_{Guid.NewGuid():N}");

        _ = factory.CreateClient();

        logs.Entries.Should().Contain(entry => entry.Message.Contains(SeedAdmin.Email), "the seed ran and said so");
        logs.Entries.Should().NotContain(entry => entry.Message.Contains(Password));
    }

    private static IdentityApiFactory NewFactory(string? password, bool migrateOnStart = true)
    {
        var factory = new IdentityApiFactory();
        factory.ConfigureHost = builder =>
        {
            builder.ConfigureAppConfiguration(configuration =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    [SeedAdmin.PasswordKey] = password,
                    ["Database:ApplyMigrationsOnStart"] = migrateOnStart ? "true" : "false",
                }));
        };
        return factory;
    }

    private static async Task<IdentityApiFactory> StartedAsync(string? password)
    {
        var factory = NewFactory(password);
        await factory.PrepareAsync($"seed_admin_{Guid.NewGuid():N}");
        _ = factory.CreateClient();
        return factory;
    }

    private static IConfiguration Configure(string? password) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [SeedAdmin.PasswordKey] = password })
            .Build();

    /// <summary>The password <c>appsettings.Development.json</c> of the Api carries, read from the file the host reads.</summary>
    private static string DevelopmentPassword()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "Simulab.slnx")))
        {
            root = root.Parent;
        }

        var path = Path.Combine(root!.FullName, "src", "Hosts", "Simulab.Api", "appsettings.Development.json");
        var configuration = new ConfigurationBuilder().AddJsonFile(path, optional: false).Build();
        return configuration[SeedAdmin.PasswordKey]!;
    }

    private static async Task<T> ReadAsync<T>(IdentityApiFactory factory, Func<IdentityModuleDbContext, UserManager<User>, Task<T>> read)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        return await read(
            scope.ServiceProvider.GetRequiredService<IdentityModuleDbContext>(),
            scope.ServiceProvider.GetRequiredService<UserManager<User>>());
    }
}
