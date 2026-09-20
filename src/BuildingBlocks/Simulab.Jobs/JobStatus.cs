namespace Simulab.Jobs;

/// <summary>
/// What a job row is doing (F-13 BR1). A job that succeeds leaves no row at all (BR6), so there is
/// no <c>Completed</c>: the three states below are the only ones a reader can ever see.
/// </summary>
public enum JobStatus
{
    /// <summary>Waiting for its turn. The worker takes it once <see cref="Job.RunAfter"/> has passed.</summary>
    Pending = 0,

    /// <summary>A worker is running it. Left behind by a worker that stopped mid-send (BR10).</summary>
    Running = 1,

    /// <summary>Every attempt failed (BR5). The row is kept as the evidence of what was lost (BR7).</summary>
    Failed = 2
}
