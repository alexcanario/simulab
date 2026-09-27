using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Simulab.Identity.Contracts;
using Simulab.Web.Services;
using Simulab.Web.Services.Auth;

namespace Simulab.Web.Tests.Auth;

/// <summary>
/// F-20 on the Web host: the two plain endpoints of a Google sign-in. Google itself is never called: the start is read
/// as the redirect it answers, and Google's answer is put in the external scheme by a stand-in handler (AC1, AC14).
/// </summary>
public sealed class GoogleAccountEndpointTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private const string ClientId = "simulab-test.apps.googleusercontent.com";

    private const string AuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";

    private readonly FakeAuthApi _api = new();

    // AC1.
    [Fact]
    public async Task Start_SwitchedOff_IsNotFound()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var start = await client.GetAsync(GoogleAccountEndpoints.StartPath);
        using var complete = await client.GetAsync(GoogleAccountEndpoints.CompletePath);

        start.StatusCode.Should().Be(HttpStatusCode.NotFound);
        complete.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // AC14.
    [Fact]
    public async Task Start_SwitchedOn_RedirectsToGoogleWithTheClientAndTheCallback()
    {
        await using var host = GoogleOn(idToken: null);
        var client = host.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var response = await client.GetAsync(GoogleAccountEndpoints.StartPath);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        var location = Uri.UnescapeDataString(response.Headers.Location!.ToString());
        location.Should().StartWith(AuthorizationEndpoint);
        location.Should().Contain($"client_id={ClientId}");
        location.Should().Contain($"redirect_uri=http://localhost{GoogleSignInSettings.CallbackPath}");
        location.Should().Contain("scope=openid email profile");
        location.Should().Contain("code_challenge_method=S256", "the code flow uses PKCE");
    }

    // AC14: tokens → the existing cookie hand-off, with the ID token Google gave.
    [Fact]
    public async Task Complete_AccountSignsIn_GoesToTheCookieHandOff()
    {
        await using var host = GoogleOn(idToken: "google-id-token");

        var location = await CompleteAsync(host);

        location.Should().StartWith("/account/sign-in-complete?ticket=");
        _api.GoogleForms.Should().ContainSingle().Which.Should().Contain("id_token=google-id-token");
    }

    // AC14: two-factor → the code step of /sign-in, with the challenge kept on the server.
    [Fact]
    public async Task Complete_TwoFactorOn_GoesToTheCodeStepWithTheChallengeOnTheServer()
    {
        _api.GoogleError = new { error = IdentityErrorCodes.TotpRequired, challenge = "challenge-g", expires_in = 300 };
        await using var host = GoogleOn(idToken: "google-id-token");

        var location = await CompleteAsync(host);

        location.Should().StartWith($"/sign-in?{GoogleAccountEndpoints.CodeStepQuery}=");
        location.Should().NotContain("challenge-g");
        var ticket = location[(location.IndexOf('=', StringComparison.Ordinal) + 1)..];
        host.Services.GetRequiredService<CodeStepTickets>().TryPeek(ticket, out var codeStep).Should().BeTrue();
        codeStep.Challenge.Should().Be("challenge-g");
    }

    // AC14: no account → the confirmation page, with the ID token kept on the server (BR8).
    [Fact]
    public async Task Complete_NoAccount_GoesToTheConfirmationWithTheTokenOnTheServer()
    {
        _api.GoogleError = new { error = IdentityErrorCodes.GoogleSignUpRequired, email = "ana@gmail.com", name = "Ana Google" };
        await using var host = GoogleOn(idToken: "google-id-token");
        var client = host.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var response = await client.GetAsync(GoogleAccountEndpoints.CompletePath);

        var location = response.Headers.Location!.ToString();
        location.Should().StartWith($"{GoogleAccountEndpoints.SignUpPath}?ticket=");
        location.Should().NotContain("google-id-token").And.NotContain("ana%40gmail.com");
        var ticket = location[(location.IndexOf('=', StringComparison.Ordinal) + 1)..];
        host.Services.GetRequiredService<GoogleSignUpTickets>().TryPeek(ticket, out var waiting).Should().BeTrue();
        waiting.IdToken.Should().Be("google-id-token");
        waiting.Email.Should().Be("ana@gmail.com");
        waiting.Name.Should().Be("Ana Google");

        // The review's finding 1: the ticket belongs to this browser, which keeps the secret in an HttpOnly cookie.
        var cookie = response.Headers.GetValues("Set-Cookie").Single(value => value.StartsWith(GoogleSignUpTicket.BindingCookie + "=", StringComparison.Ordinal));
        cookie.Should().Contain("httponly").And.Contain("secure").And.Contain($"path={GoogleAccountEndpoints.SignUpPath}");
        var secret = cookie[(GoogleSignUpTicket.BindingCookie.Length + 1)..cookie.IndexOf(';', StringComparison.Ordinal)];
        waiting.IsHeldBy(secret).Should().BeTrue();
        waiting.IsHeldBy(ticket).Should().BeFalse("the id in the URL is not the secret");
        waiting.BindingHash.Should().NotBe(secret, "only the hash stays on the server");
    }

    // The review's finding 9: Google's answer is read once; the external cookie is cleared.
    [Fact]
    public async Task Complete_ClearsTheExternalCookie()
    {
        await using var host = GoogleOn(idToken: "google-id-token");

        await CompleteAsync(host);

        host.Services.GetRequiredService<StandInGoogleAnswer>().SignOuts.Should().Be(1);
    }

    // AC14: any refusal → sign-in with the reason.
    [Fact]
    public async Task Complete_Refused_GoesToSignInWithTheCode()
    {
        _api.GoogleError = new { error = IdentityErrorCodes.GoogleTokenInvalid };
        await using var host = GoogleOn(idToken: "google-id-token");

        var location = await CompleteAsync(host);

        location.Should().Be($"/sign-in?error={Uri.EscapeDataString(IdentityErrorCodes.GoogleTokenInvalid)}");
    }

    // BR8: nothing from Google (a direct visit, an expired external cookie) → sign-in, and the Api is never called.
    [Fact]
    public async Task Complete_NoGoogleAnswer_GoesToSignInAsExpired()
    {
        await using var host = GoogleOn(idToken: null);

        var location = await CompleteAsync(host);

        location.Should().Be($"/sign-in?error={Uri.EscapeDataString(IdentityErrorCodes.GoogleSignInExpired)}");
        _api.GoogleForms.Should().BeEmpty();
    }

    private static async Task<string> CompleteAsync(WebApplicationFactory<Program> host)
    {
        var client = host.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var response = await client.GetAsync(GoogleAccountEndpoints.CompletePath);
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        return response.Headers.Location!.ToString();
    }

    /// <summary>
    /// The host with the feature on. Google's discovery document is given in advance, so the start never calls Google;
    /// the external scheme answers with <paramref name="idToken"/> as Google's saved ID token, or nothing when null.
    /// </summary>
    private WebApplicationFactory<Program> GoogleOn(string? idToken) =>
        factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Identity:GoogleSignInEnabled", "true");
            builder.UseSetting("Authentication:Google:ClientId", ClientId);
            builder.UseSetting("Authentication:Google:ClientSecret", "google-test-secret");
            builder.ConfigureTestServices(services =>
            {
                services.AddHttpClient<AuthClient>().ConfigurePrimaryHttpMessageHandler(() => _api);
                services.AddHttpClient<IdentityApiClient>().ConfigurePrimaryHttpMessageHandler(() => _api);
                services.PostConfigure<OpenIdConnectOptions>(GoogleSignInSettings.Scheme, options =>
                    options.Configuration = new OpenIdConnectConfiguration
                    {
                        Issuer = GoogleSignInSettings.Authority,
                        AuthorizationEndpoint = AuthorizationEndpoint,
                        TokenEndpoint = "https://oauth2.googleapis.com/token",
                    });
                services.AddSingleton(new StandInGoogleAnswer(idToken));
                services.PostConfigure<AuthenticationOptions>(options =>
                    options.SchemeMap[GoogleSignInSettings.ExternalScheme].HandlerType = typeof(StandInExternalHandler));
            });
        });

    public void Dispose() => _api.Dispose();

}
