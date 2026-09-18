namespace Simulab.Identity.Contracts;

/// <summary>The numbers of BR4, in one place: the OpenIddict server config and the token endpoint both read them.</summary>
public static class TokenLifetimes
{
    public static readonly TimeSpan AccessToken = TimeSpan.FromMinutes(15);

    public static readonly TimeSpan RefreshToken = TimeSpan.FromDays(30);
}
