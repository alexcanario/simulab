using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Simulab.Web.Tests.Auth;

/// <summary>
/// What the external cookie holds when the callback reads it: Google's saved ID token, the properties the
/// challenge protected into <c>state</c>, and how many times the endpoint cleared the cookie. Shared by the
/// F-20 sign-in tests and the F-29 link tests, which differ only in those properties.
/// </summary>
public sealed class StandInGoogleAnswer(string? idToken)
{
    private int _signOuts;

    public string? IdToken => idToken;

    /// <summary>F-29 BR2: the link marker and the ticket id, as <c>StartLink</c> would have stored them.</summary>
    public Dictionary<string, string?> Items { get; } = new(StringComparer.Ordinal);

    public int SignOuts => _signOuts;

    public void SignedOut() => Interlocked.Increment(ref _signOuts);
}

/// <summary>Stands in for Google's external cookie handler: no call ever leaves for Google.</summary>
public sealed class StandInExternalHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    StandInGoogleAnswer answer)
    : SignOutAuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        ArgumentNullException.ThrowIfNull(answer);

        // F-29: a link attempt that Google answered nothing to still carries its marker, so the properties
        // are restored even when there is no token — that is exactly the AC7b/AC8 shape.
        if (answer.IdToken is null && answer.Items.Count == 0)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var properties = new AuthenticationProperties();
        if (answer.IdToken is not null)
        {
            properties.StoreTokens([new AuthenticationToken { Name = "id_token", Value = answer.IdToken }]);
        }

        foreach (var (key, value) in answer.Items)
        {
            properties.Items[key] = value;
        }

        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "google-subject")], Scheme.Name));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, properties, Scheme.Name)));
    }

    protected override Task HandleSignOutAsync(AuthenticationProperties? properties)
    {
        answer.SignedOut();
        return Task.CompletedTask;
    }
}
