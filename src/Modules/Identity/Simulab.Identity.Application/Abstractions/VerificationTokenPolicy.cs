namespace Simulab.Identity.Application.Abstractions;

/// <summary>The numbers of BR8 and BR12, in one place so the handlers and their tests agree.</summary>
public static class VerificationTokenPolicy
{
    /// <summary>How long a verification link works (BR8).</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(24);

    /// <summary>Shortest interval between two emails for the same address (BR12).</summary>
    public static readonly TimeSpan ResendCooldown = TimeSpan.FromSeconds(60);

    /// <summary>The window the hourly cap is counted in (BR12).</summary>
    public static readonly TimeSpan ResendWindow = TimeSpan.FromHours(1);

    /// <summary>How many emails one address may get inside <see cref="ResendWindow"/> (BR12).</summary>
    public const int MaxResendsPerWindow = 5;
}
