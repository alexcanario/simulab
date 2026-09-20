using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Simulab.Identity.Application.Abstractions;
using Simulab.Identity.Contracts;
using Simulab.Identity.Domain.Entities;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Identity.Tests;

/// <summary>Verifying and resending through the real HTTP pipeline: AC6, AC7, AC8 and the per-address half of AC9.</summary>
public sealed class VerificationEndpointTests : IdentityApiTests
{
    private const string RegisterRoute = "/api/v1/identity/registrations";
    private const string VerifyRoute = "/api/v1/identity/email-verifications";
    private const string ResendRoute = "/api/v1/identity/email-verifications/resend";

    /// <summary>Signs a visitor up and returns the raw token that reached the inbox.</summary>
    private async Task<(string Email, string Token)> SignUpAsync(string email)
    {
        await RunJobsAsync();
        Emails.Clear();
        await PostAsync(Client(), RegisterRoute, SignUpForm.Valid(email), HttpStatusCode.Accepted);
        await RunJobsAsync();
        return (email, VerificationLink.TokenOf(Emails.Last!.HtmlBody));
    }

    private async Task<HttpResponseMessage> VerifyAsync(string token) =>
        await Client().PostAsJsonAsync(VerifyRoute, new VerifyEmailRequest(token), AppJson.Options);

    [Fact]
    public async Task Verify_ValidToken_ActivatesTheAccountAndConsumesTheToken()
    {
        var (email, token) = await SignUpAsync("verificar@exemplo.com");

        var response = await VerifyAsync(token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<VerifyEmailResponse>(AppJson.Options);
        body!.Outcome.Should().Be(VerificationOutcome.Verified);

        var user = await QueryAsync(context => context.Users.SingleAsync(u => u.Email == email));
        user.Status.Should().Be(AccountStatus.Active);
        user.EmailConfirmed.Should().BeTrue();
        user.EmailVerifiedAt.Should().Be(Factory.Clock.GetUtcNow());

        var stored = await QueryAsync(context => context.EmailVerificationTokens.SingleAsync(t => t.UserId == user.Id));
        stored.IsConsumed.Should().BeTrue();
    }

    [Fact]
    public async Task Verify_TokenOfAnAccountThatIsAlreadyActive_SucceedsAndChangesNothing()
    {
        var (email, token) = await SignUpAsync("duas.vezes@exemplo.com");
        await VerifyAsync(token);
        var verifiedAt = (await QueryAsync(context => context.Users.SingleAsync(u => u.Email == email))).EmailVerifiedAt;

        Factory.Clock.Advance(TimeSpan.FromMinutes(5));
        var response = await VerifyAsync(token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = await QueryAsync(context => context.Users.SingleAsync(u => u.Email == email));
        user.EmailVerifiedAt.Should().Be(verifiedAt, "a second click changes nothing");
    }

    [Fact]
    public async Task Verify_UnknownToken_IsInvalid()
    {
        var response = await VerifyAsync("nao-existe-este-token");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(IdentityErrorCodes.VerificationInvalid);
    }

    [Fact]
    public async Task Verify_TokenReplacedByAResend_IsInvalid()
    {
        var (email, firstToken) = await SignUpAsync("token.substituido@exemplo.com");
        Factory.Clock.Advance(VerificationTokenPolicy.ResendCooldown);
        await PostAsync(Client(), ResendRoute, new ResendVerificationRequest(email), HttpStatusCode.Accepted);

        var response = await VerifyAsync(firstToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(IdentityErrorCodes.VerificationInvalid);

        // The link from the newest email still works.
        await RunJobsAsync();
        var newToken = VerificationLink.TokenOf(Emails.Last!.HtmlBody);
        (await VerifyAsync(newToken)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Verify_TokenOlderThanItsLifetime_IsExpired()
    {
        var (_, token) = await SignUpAsync("expirado@exemplo.com");

        Factory.Clock.Advance(VerificationTokenPolicy.Lifetime + TimeSpan.FromMinutes(1));
        var response = await VerifyAsync(token);

        response.StatusCode.Should().Be(HttpStatusCode.Gone);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(IdentityErrorCodes.VerificationExpired);
    }

    [Fact]
    public async Task Resend_UnknownAddress_AnswersTheSameAndSendsNothing()
    {
        await RunJobsAsync();
        Emails.Clear();

        await PostAsync(Client(), ResendRoute, new ResendVerificationRequest("ninguem@exemplo.com"), HttpStatusCode.Accepted);

        await RunJobsAsync();
        Emails.Count.Should().Be(0);
    }

    [Fact]
    public async Task Resend_AccountAlreadyActive_AnswersTheSameAndSendsNothing()
    {
        var (email, token) = await SignUpAsync("ja.ativa@exemplo.com");
        await VerifyAsync(token);
        await RunJobsAsync();
        Emails.Clear();
        Factory.Clock.Advance(VerificationTokenPolicy.ResendCooldown);

        await PostAsync(Client(), ResendRoute, new ResendVerificationRequest(email), HttpStatusCode.Accepted);

        await RunJobsAsync();
        Emails.Count.Should().Be(0);
    }

    [Fact]
    public async Task Resend_InsideTheCooldown_SendsNothingAndStillAnswersTheSame()
    {
        var (email, _) = await SignUpAsync("no.cooldown@exemplo.com");
        await RunJobsAsync();
        Emails.Clear();

        // Less than a minute after the sign-up email: refused, and the answer gives nothing away (BR11).
        Factory.Clock.Advance(VerificationTokenPolicy.ResendCooldown - TimeSpan.FromSeconds(1));
        await PostAsync(Client(), ResendRoute, new ResendVerificationRequest(email), HttpStatusCode.Accepted);
        await RunJobsAsync();
        Emails.Count.Should().Be(0);

        Factory.Clock.Advance(TimeSpan.FromSeconds(2));
        await PostAsync(Client(), ResendRoute, new ResendVerificationRequest(email), HttpStatusCode.Accepted);
        await RunJobsAsync();
        Emails.Count.Should().Be(1);
    }

    [Fact]
    public async Task Resend_AboveTheHourlyCapForOneAddress_SendsNothing()
    {
        var (email, _) = await SignUpAsync("cap.por.endereco@exemplo.com");
        await RunJobsAsync();
        Emails.Clear();

        // The sign-up email counts as the first of the window; four resends fill the cap of five.
        for (var i = 0; i < VerificationTokenPolicy.MaxResendsPerWindow - 1; i++)
        {
            Factory.Clock.Advance(VerificationTokenPolicy.ResendCooldown + TimeSpan.FromSeconds(1));
            await PostAsync(Client(), ResendRoute, new ResendVerificationRequest(email), HttpStatusCode.Accepted);
        }

        await RunJobsAsync();
        Emails.Count.Should().Be(VerificationTokenPolicy.MaxResendsPerWindow - 1);

        Factory.Clock.Advance(VerificationTokenPolicy.ResendCooldown + TimeSpan.FromSeconds(1));
        await PostAsync(Client(), ResendRoute, new ResendVerificationRequest(email), HttpStatusCode.Accepted);
        await RunJobsAsync();
        Emails.Count.Should().Be(VerificationTokenPolicy.MaxResendsPerWindow - 1, "the cap for this address is full");

        // Once the window has passed, the address can ask again.
        Factory.Clock.Advance(VerificationTokenPolicy.ResendWindow);
        await PostAsync(Client(), ResendRoute, new ResendVerificationRequest(email), HttpStatusCode.Accepted);
        await RunJobsAsync();
        Emails.Count.Should().Be(VerificationTokenPolicy.MaxResendsPerWindow);
    }
}
