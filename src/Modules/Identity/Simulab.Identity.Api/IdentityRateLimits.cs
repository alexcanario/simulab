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

    /// <summary>F-38 BR2: distinct account names that failed to sign in from one client address in <see cref="SignInWindow"/>.</summary>
    public const int SignInFailedAccountsPer15Minutes = 30;

    public static readonly TimeSpan Window = TimeSpan.FromHours(1);

    public static readonly TimeSpan SignInWindow = TimeSpan.FromMinutes(15);
}
