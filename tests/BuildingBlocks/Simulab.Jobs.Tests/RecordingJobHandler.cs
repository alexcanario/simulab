using System.Collections.Concurrent;

namespace Simulab.Jobs.Tests;

/// <summary>A job type the test owns: it records every payload it ran and fails when the test says so.</summary>
public sealed class RecordingJobHandler : IJobHandler
{
    public const string JobType = "test.work";

    private readonly ConcurrentQueue<string> _handled = new();

    public string Type => JobType;

    public IReadOnlyList<string> Handled => [.. _handled];

    /// <summary>While true every attempt throws, the way an unreachable mail server does.</summary>
    public bool Fails { get; set; }

    /// <summary>The message of the failure, so a test can read it back from <see cref="Job.LastError"/>.</summary>
    public string FailureMessage { get; set; } = "The mail server could not be reached.";

    public Task HandleAsync(string payload, CancellationToken cancellationToken = default)
    {
        if (Fails)
        {
            throw new InvalidOperationException(FailureMessage);
        }

        _handled.Enqueue(payload);
        return Task.CompletedTask;
    }
}
