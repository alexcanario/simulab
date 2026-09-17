namespace Simulab.Identity.Api;

/// <summary>The numbers of BR12 for the per-client limits, in one place.</summary>
public static class IdentityRateLimits
{
    public const int RegistrationsPerHour = 10;

    public const int ResendsPerHour = 5;

    public static readonly TimeSpan Window = TimeSpan.FromHours(1);
}
