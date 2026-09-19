using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Simulab.Identity.Domain.Entities;

namespace Simulab.Identity.Tests;

/// <summary>
/// Creates accounts straight through <see cref="UserManager{TUser}"/>, for tests that need many of them:
/// sign-up through HTTP is limited to 10 per client per hour (F-4), and these tests are not about sign-up.
/// </summary>
public static class Accounts
{
    public static async Task<User> CreateAsync(
        IServiceProvider services,
        string? email = null,
        bool active = true,
        string? fullName = "Ana Ribeiro",
        params string[] roles)
    {
        ArgumentNullException.ThrowIfNull(services);

        await using var scope = services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var address = email ?? $"user.{Guid.CreateVersion7():N}@exemplo.com";

        var user = new User { Id = Guid.CreateVersion7(), UserName = address, Email = address, FullName = fullName };
        if (active)
        {
            user.VerifyEmail(DateTimeOffset.UtcNow);
        }

        var created = await users.CreateAsync(user, SignUpForm.ValidPassword);
        created.Succeeded.Should().BeTrue(string.Join(", ", created.Errors.Select(error => error.Code)));

        foreach (var role in roles)
        {
            (await users.AddToRoleAsync(user, role)).Succeeded.Should().BeTrue();
        }

        return user;
    }

    /// <summary>An HTTP client signed in as <paramref name="email"/> with a bearer access token.</summary>
    public static async Task<HttpClient> SignedInAsync(HttpClient client, string email)
    {
        ArgumentNullException.ThrowIfNull(client);

        var token = await TokenClient.SignInAsync(client, email, SignUpForm.ValidPassword);
        token.AccessToken.Should().NotBeNull(token.ErrorDescription);
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.AccessToken);
        return client;
    }
}
