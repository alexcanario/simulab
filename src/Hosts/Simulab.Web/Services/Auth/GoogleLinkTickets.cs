namespace Simulab.Web.Services.Auth;

/// <summary>
/// F-29 BR2: a link attempt is valid for 15 minutes — the bound of the round trip itself, which is the OIDC
/// correlation cookie, not the 5-minute external cookie that only starts once Google has answered.
/// </summary>
public sealed class GoogleLinkTickets(TimeProvider timeProvider)
    : SingleUseTickets<GoogleLinkTicket>(timeProvider, Lifetime)
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(15);
}
