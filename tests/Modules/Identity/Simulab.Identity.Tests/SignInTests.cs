using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Simulab.Identity.Contracts;

namespace Simulab.Identity.Tests;

/// <summary>Sign-in, refresh and sign-out through the real HTTP pipeline: AC1 to AC7.</summary>
public sealed class SignInTests : IdentityApiTests
{
    [Fact]
    public async Task SignIn_ActiveAccountWithRightPassword_IssuesTokens()
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Factory);

        var token = await TokenClient.SignInAsync(client, email, SignUpForm.ValidPassword);

        token.Error.Should().BeNull(token.ErrorDescription);
        token.AccessToken.Should().NotBeNullOrWhiteSpace();
        token.RefreshToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task SignIn_PendingAccount_ReturnsEmailNotVerifiedAndNoToken()
    {
        var client = Client();
        var request = SignUpForm.Valid("pendente.login@exemplo.com");
        await client.PostAsJsonAsync("/api/v1/identity/registrations", request, Simulab.SharedKernel.Serialization.AppJson.Options);

        var token = await TokenClient.SignInAsync(client, request.Email, SignUpForm.ValidPassword);

        token.Error.Should().Be(IdentityErrorCodes.EmailNotVerified);
        token.AccessToken.Should().BeNull();
    }

    [Fact]
    public async Task SignIn_WrongPasswordOrUnknownEmail_ReturnsTheSameGenericCode()
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Factory);

        var wrongPassword = await TokenClient.SignInAsync(client, email, "not-the-password");
        var unknownEmail = await TokenClient.SignInAsync(client, "nao.existe@exemplo.com", SignUpForm.ValidPassword);

        wrongPassword.Error.Should().Be(IdentityErrorCodes.InvalidCredentials);
        unknownEmail.Error.Should().Be(IdentityErrorCodes.InvalidCredentials);
    }

    [Fact]
    public async Task SignIn_AfterFiveWrongPasswords_LocksTheAccountForFifteenMinutes()
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Factory);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            await TokenClient.SignInAsync(client, email, "not-the-password");
        }

        // BR3: a correct password during lockout is still refused.
        var duringLockout = await TokenClient.SignInAsync(client, email, SignUpForm.ValidPassword);
        duringLockout.Error.Should().Be(IdentityErrorCodes.AccountLocked);

        // The 15 minutes passed: same as production once the wall clock moves past LockoutEnd.
        await QueryAsync(async context =>
        {
            var user = await context.Users.SingleAsync(u => u.Email == email);
            user.LockoutEnd = Factory.Clock.GetUtcNow().AddMinutes(-1);
            return await context.SaveChangesAsync();
        });

        var afterLockout = await TokenClient.SignInAsync(client, email, SignUpForm.ValidPassword);
        afterLockout.Error.Should().BeNull(afterLockout.ErrorDescription);
        afterLockout.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Refresh_ValidToken_IssuesNewPairAndInvalidatesTheOldOne()
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Factory);
        var first = await TokenClient.SignInAsync(client, email, SignUpForm.ValidPassword);

        var refreshed = await TokenClient.RefreshAsync(client, first.RefreshToken!);
        refreshed.Error.Should().BeNull();
        refreshed.AccessToken.Should().NotBeNullOrWhiteSpace();
        refreshed.RefreshToken.Should().NotBe(first.RefreshToken);

        var reused = await TokenClient.RefreshAsync(client, first.RefreshToken!);
        reused.Error.Should().Be(IdentityErrorCodes.RefreshTokenInvalid);
    }

    [Fact]
    public async Task SignOut_RevokesTheSessionSoTheSameAccessTokenStopsWorking()
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Factory);
        var token = await TokenClient.SignInAsync(client, email, SignUpForm.ValidPassword);

        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.AccessToken);

        // AC7: the session works normally before sign-out.
        var firstSignOut = await client.PostAsync("/api/v1/identity/sign-out", null);
        firstSignOut.StatusCode.Should().Be(System.Net.HttpStatusCode.NoContent);

        // AC6: the same access token is now revoked.
        var secondCall = await client.PostAsync("/api/v1/identity/sign-out", null);
        secondCall.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);

        var body = await secondCall.Content.ReadAsStringAsync();
        CodeOf(body).Should().Be(IdentityErrorCodes.TokenRevoked);
    }
}
