namespace Simulab.Web.Services;

/// <summary>
/// Who the visitor of this circuit is, as far as the Api needs it (B-4): the address the first request came
/// from. A Blazor circuit has no HTTP request of its own, so <c>App</c> reads it while the page is first
/// rendered and <c>Routes</c> stores it here; <see cref="IdentityApiClient"/> sends it with every call.
/// </summary>
public sealed class VisitorContext
{
    /// <summary>Null outside a page (plain endpoints), where the Api falls back to the connection's address.</summary>
    public string? Address { get; set; }

    /// <summary>
    /// F-20 BR8: the secret this browser holds for its waiting Google sign-up, from the cookie the first request
    /// carried. Null when there is none.
    /// </summary>
    public string? GoogleSignUpBinding { get; set; }
}
