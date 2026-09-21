using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Simulab.Identity.Domain.Entities;

namespace Simulab.Identity.Tests;

/// <summary>
/// F-11 with <c>Identity:TotpEnabled</c> false (AC13, BR12). Set here on purpose: the test host runs as
/// Development, whose settings file turns the feature on.
/// </summary>
public sealed class TotpSwitchedOffTests : IdentityApiTests
{
    protected override void ConfigureHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Identity:TotpEnabled", "false");
        builder.UseSetting("Identity:TotpEncryptionKey", string.Empty);
    }

    // AC13.
    [Theory]
    [InlineData("GET", TotpApi.Route)]
    [InlineData("DELETE", TotpApi.Route)]
    [InlineData("POST", TotpApi.Route + "/enrolments")]
    [InlineData("POST", TotpApi.Route + "/enrolments/confirmations")]
    [InlineData("POST", TotpApi.Route + "/recovery-codes")]
    public async Task Routes_FeatureOff_Answer404(string method, string route)
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Factory);
        var session = await TokenClient.SignInAsync(client, email, SignUpForm.ValidPassword);

        using var response = await TotpApi.SendAsync(client, new HttpMethod(method), route, session.AccessToken!);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // AC13.
    [Fact]
    public async Task TotpGrant_FeatureOff_IsRefused()
    {
        var tokens = await TotpApi.CodeStepAsync(Client(), new string('a', 64), "123456");

        tokens.Error.Should().Be("unsupported_grant_type");
        tokens.AccessToken.Should().BeNull();
    }

    // AC13: nobody is locked out by the switch.
    [Fact]
    public async Task SignIn_FeatureOffAndAccountWithTwoFactorOn_ThePasswordAloneIssuesTokens()
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Factory);
        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var user = await users.FindByEmailAsync(email);
            user!.StartTotpEnrolment("c2VjcmV0LXdyaXR0ZW4td2hpbGUtb24=");
            user.EnableTotp(DateTimeOffset.UtcNow);
            (await users.UpdateAsync(user)).Succeeded.Should().BeTrue();
        }

        var tokens = await TokenClient.SignInAsync(client, email, SignUpForm.ValidPassword);

        tokens.Error.Should().BeNull(tokens.ErrorDescription);
        tokens.AccessToken.Should().NotBeNullOrEmpty();
    }
}
