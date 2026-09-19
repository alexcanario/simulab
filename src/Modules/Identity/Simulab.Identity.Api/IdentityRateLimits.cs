namespace Simulab.Identity.Api;

/// <summary>The per-client limits of F-4 BR12 and F-7 BR3, in one place.</summary>
public static class IdentityRateLimits
{
    public const int RegistrationsPerHour = 10;

    public const int ResendsPerHour = 5;

    /// <summary>F-7 BR3: asking for a reset link.</summary>
    public const int PasswordResetRequestsPerHour = 5;

    /// <summary>F-7 BR3: using or checking a reset token, together.</summary>
    public const int PasswordResetsPerHour = 10;

    public static readonly TimeSpan Window = TimeSpan.FromHours(1);
}
