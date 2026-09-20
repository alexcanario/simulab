namespace Simulab.Jobs;

/// <summary>The numbers of F-13 BR5, BR9 and BR10, in one place so a test states them the way the code does.</summary>
public static class JobPolicy
{
    /// <summary>BR5: after this many attempts the job is <see cref="JobStatus.Failed"/> and is not taken again.</summary>
    public const int MaxAttempts = 5;

    /// <summary>BR9: how often the worker looks for work.</summary>
    public static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    /// <summary>BR10: a job left <see cref="JobStatus.Running"/> longer than this is taken again.</summary>
    public static readonly TimeSpan StaleAfter = TimeSpan.FromMinutes(5);

    /// <summary>BR5: 1, 2, 4, 8 and 16 minutes after the attempt that failed.</summary>
    public static TimeSpan BackoffAfter(int attempts) =>
        TimeSpan.FromMinutes(Math.Pow(2, Math.Clamp(attempts, 1, MaxAttempts) - 1));
}
