namespace Simulab.Jobs;

/// <summary>
/// Runs one kind of job. A handler that throws fails the attempt; the runner decides whether it is
/// retried or given up on (F-13 BR5). It is resolved in a scope of its own, per attempt.
/// </summary>
public interface IJobHandler
{
    /// <summary>The value of <see cref="Job.Type"/> this handler answers for.</summary>
    string Type { get; }

    Task HandleAsync(string payload, CancellationToken cancellationToken = default);
}
