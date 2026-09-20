using Simulab.SharedKernel.Entities;

namespace Simulab.Jobs;

/// <summary>
/// One unit of background work (F-13 BR1). It is infrastructure, not business data: it carries no tenant,
/// it is never soft deleted, and a successful job is removed for real (BR6) so a live link and the
/// recipient's address do not stay at rest.
/// </summary>
public sealed class Job : Entity
{
    /// <summary>What kind of work this is, for example <see cref="Email.EmailJob.Type"/>.</summary>
    public required string Type { get; init; }

    /// <summary>The whole input of the job, as JSON. The worker needs nothing else to run it.</summary>
    public required string Payload { get; init; }

    public JobStatus Status { get; set; } = JobStatus.Pending;

    /// <summary>How many times a worker has started it. The first attempt makes it 1.</summary>
    public int Attempts { get; set; }

    /// <summary>When it was enqueued. The worker takes the oldest first (BR9).</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Not before this instant. Set at enqueue and pushed forward by the retry backoff (BR5, BR11).</summary>
    public DateTimeOffset RunAfter { get; set; }

    /// <summary>When the current attempt started. A row stuck here is taken again (BR10).</summary>
    public DateTimeOffset? StartedAt { get; set; }

    /// <summary>The message of the last failure, kept with a <see cref="JobStatus.Failed"/> row (BR7).</summary>
    public string? LastError { get; set; }
}
