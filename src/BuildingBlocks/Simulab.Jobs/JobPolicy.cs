namespace Simulab.Jobs;

/// <summary>The numbers of F-13 BR5, BR9 and BR10, in one place so a test states them the way the code does.</summary>
public static class JobPolicy
{
    /// <summary>BR5: after this many attempts the job is <see cref="JobStatus.Failed"/> and is not taken again.</summary>
    public const int MaxAttempts = 5;

    /// <summary>BR9: how often the worker looks for work.</summary>
    public static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    /// <summary>How many jobs one poll runs before it lets the worker breathe. A backlog waits 5 s more.</summary>
    public const int MaxJobsPerPoll = 100;

    /// <summary>BR10: a job left <see cref="JobStatus.Running"/> longer than this is taken again.</summary>
    public static readonly TimeSpan StaleAfter = TimeSpan.FromMinutes(5);

    /// <summary>
    /// F-27 BR1: how long a <see cref="JobStatus.Failed"/> row is kept as the evidence of what was lost. A
    /// constant and not configuration: the other numbers of the module live here, and this one is not a knob
    /// anybody turns per environment (owner, 2026-09-26).
    /// </summary>
    public static readonly TimeSpan FailedRetention = TimeSpan.FromDays(90);

    /// <summary>F-27 BR5: at most one cleanup an hour, however often the worker polls.</summary>
    public static readonly TimeSpan CleanupInterval = TimeSpan.FromHours(1);

    /// <summary>BR5: 1, 2, 4, 8 and 16 minutes after the attempt that failed.</summary>
    public static TimeSpan BackoffAfter(int attempts) =>
        TimeSpan.FromMinutes(Math.Pow(2, Math.Clamp(attempts, 1, MaxAttempts) - 1));
}
