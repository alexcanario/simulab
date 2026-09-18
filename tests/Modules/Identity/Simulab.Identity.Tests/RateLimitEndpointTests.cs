using System.Net;
using Simulab.Identity.Api;
using Simulab.Identity.Contracts;

namespace Simulab.Identity.Tests;

/// <summary>The per-client half of AC9. Every request here comes from one client, so one bucket fills.</summary>
public sealed class RateLimitEndpointTests : IdentityApiTests
{
    private const string RegisterRoute = "/api/v1/identity/registrations";
    private const string ResendRoute = "/api/v1/identity/email-verifications/resend";

    [Fact]
    public async Task Register_AboveTheHourlyCapForOneClient_IsRefused()
    {
        var client = Client();

        for (var i = 0; i < IdentityRateLimits.RegistrationsPerHour; i++)
        {
            await PostAsync(client, RegisterRoute, SignUpForm.Valid($"limite{i}@exemplo.com"), HttpStatusCode.Accepted);
        }

        var body = await PostAsync(client, RegisterRoute, SignUpForm.Valid("acima.do.limite@exemplo.com"), HttpStatusCode.TooManyRequests);

        CodeOf(body).Should().Be(IdentityErrorCodes.RegistrationRateLimited);
        (await QueryAsync(context => Task.FromResult(context.Users.Count()))).Should().Be(IdentityRateLimits.RegistrationsPerHour);

        // Once the window has passed the client can try again.
        Factory.Clock.Advance(IdentityRateLimits.Window);
        await PostAsync(client, RegisterRoute, SignUpForm.Valid("depois.da.janela@exemplo.com"), HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task Resend_AboveTheHourlyCapForOneClient_IsRefused()
    {
        var client = Client();

        for (var i = 0; i < IdentityRateLimits.ResendsPerHour; i++)
        {
            await PostAsync(client, ResendRoute, new ResendVerificationRequest($"quem.quer.que.seja{i}@exemplo.com"), HttpStatusCode.Accepted);
        }

        var body = await PostAsync(client, ResendRoute, new ResendVerificationRequest("mais.um@exemplo.com"), HttpStatusCode.TooManyRequests);

        CodeOf(body).Should().Be(IdentityErrorCodes.VerificationRateLimited);
        Emails.Count.Should().Be(0, "none of those addresses exists, and the refused one was not even looked up");
    }
}
