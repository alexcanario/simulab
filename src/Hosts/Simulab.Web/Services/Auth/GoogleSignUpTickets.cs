namespace Simulab.Web.Services.Auth;

/// <summary>F-20 BR8: the confirmation waits at most 10 minutes; after that, the visitor starts again at sign-in.</summary>
public sealed class GoogleSignUpTickets(TimeProvider timeProvider)
    : SingleUseTickets<GoogleSignUpTicket>(timeProvider, Lifetime)
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);
}
