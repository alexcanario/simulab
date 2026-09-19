namespace Simulab.Identity.Application.Abstractions;

/// <summary>The numbers of F-7 BR2 and BR3, in one place so the handlers and their tests agree.</summary>
public static class PasswordResetPolicy
{
    /// <summary>How long a reset link works (BR2).</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(1);

    /// <summary>Shortest interval between two reset emails for the same account (BR3).</summary>
    public static readonly TimeSpan Cooldown = TimeSpan.FromSeconds(60);

    /// <summary>The window the hourly cap is counted in (BR3).</summary>
    public static readonly TimeSpan Window = TimeSpan.FromHours(1);

    /// <summary>How many reset emails one account may get inside <see cref="Window"/> (BR3).</summary>
    public const int MaxEmailsPerWindow = 5;
}
