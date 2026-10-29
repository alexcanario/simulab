using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Simulab.Identity.Contracts;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Identity.Tests;

/// <summary>
/// F-183 AC9 and AC10: what the new e-mail and one-time code fields send still reaches the right account, and a partial or
/// malformed code is still refused and counted. The Api stays the authority (BR9); the page only trims and lower-cases.
/// </summary>
public sealed class TextKindApiReachTests : IdentityApiTests
{
    private static readonly TimeSpan Step = TimeSpan.FromSeconds(30);

    protected override void ConfigureHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Identity:TotpEnabled", "true");
        builder.UseSetting("Identity:TotpEncryptionKey", TotpApi.TestKey);
    }

    private DateTimeOffset Now => Factory.Clock.GetUtcNow();

    // AC10: the account was created as typed with capitals; the field now sends it in lower case.
    [Fact]
    public async Task SignIn_LowerCasedEmail_ReachesTheAccountCreatedWithCapitals()
    {
        var client = Client();
        var created = await ActiveUser.CreateAsync(client, Factory, $"Ana.{Guid.CreateVersion7():N}@Exemplo.com");

        var session = await TokenClient.SignInAsync(client, created.ToLowerInvariant(), SignUpForm.ValidPassword);

        session.AccessToken.Should().NotBeNullOrEmpty($"{session.Error}: {session.ErrorDescription}");
    }

    [Fact]
    public async Task ResetRequest_LowerCasedEmail_SendsTheLinkOfTheAccountCreatedWithCapitals()
    {
        var client = Client();
        var created = await ActiveUser.CreateAsync(client, Factory, $"Ana.{Guid.CreateVersion7():N}@Exemplo.com");
        await RunJobsAsync();
        Emails.Clear();

        using var response = await client.PostAsJsonAsync(
            "/api/v1/identity/password-reset-requests",
            new RequestPasswordResetRequest(created.ToLowerInvariant()),
            AppJson.Options);
        await RunJobsAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        Emails.Count.Should().Be(1);
        Emails.Last!.To.Should().Be(created);
    }

    // AC9: AppOtpField lets a partial code through; the Api refuses it as it refuses a wrong one.
    [Theory]
    [InlineData("123")]
    [InlineData("12345")]
    [InlineData("12a456")]
    [InlineData("1234567")]
    public async Task CodeStep_PartialOrMalformedCode_IsRefusedAndCountedAsOneFailure(string code)
    {
        var client = Client();
        var email = await ActiveUser.CreateAsync(client, Factory);
        var session = await TokenClient.SignInAsync(client, email, SignUpForm.ValidPassword);
        var enrolment = await TotpApi.StartAsync(client, session.AccessToken!);
        await TotpApi.ReadAsync<RecoveryCodesResponse>(await TotpApi.ConfirmAsync(client, session.AccessToken!, TotpApi.CodeAt(enrolment.Secret, Now)));
        Factory.Clock.Advance(Step);

        var step = await TotpApi.PasswordStepAsync(client, email, SignUpForm.ValidPassword);
        var refused = await TotpApi.CodeStepAsync(client, step.Challenge, code);

        refused.Error.Should().Be(IdentityErrorCodes.TotpCodeInvalid);
        refused.AccessToken.Should().BeNull();
        var user = await QueryAsync(context => context.Users.AsNoTracking().SingleAsync(candidate => candidate.Email == email));
        user.AccessFailedCount.Should().Be(1);
    }
}
