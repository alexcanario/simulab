namespace Simulab.Jobs.Tests;

/// <summary>
/// F-18: the claim reads only the rows it can take. BR7 keeps every job given up on, so a claim that reads
/// them grows for good; the planner picks by statistics, so only a plan over a filled table proves it.
/// </summary>
public sealed class JobClaimPlanTests : IAsyncLifetime
{
    private const int KeptFailedRows = 10_000;

    private JobTestHost _host = null!;

    public async Task InitializeAsync() => _host = await JobTestHost.StartAsync(nameof(JobClaimPlanTests));

    public async Task DisposeAsync() => await _host.DisposeAsync();

    /// <summary>AC1: with ten thousand failed rows kept, the claim goes through the active-rows index.</summary>
    [Fact]
    public async Task Claim_ManyFailedRowsKept_ReadsOnlyTheActiveRowsIndex()
    {
        await _host.EnqueueAsync(RecordingJobHandler.JobType, "due");
        var stale = await _host.EnqueueAsync(RecordingJobHandler.JobType, "stale");
        await _host.SetRunningAsync(stale, _host.Clock.GetUtcNow() - JobPolicy.StaleAfter - TimeSpan.FromMinutes(1));
        await _host.SeedFailedAsync(KeptFailedRows);

        var plan = await _host.ExplainClaimAsync();

        plan.Should().Contain("ix_jobs_active_created_at", "the claim must read the partial index of active rows");
        plan.Should().NotContain("Seq Scan on jobs", "a sequential scan reads every failed row kept by BR7");
    }

    /// <summary>AC2: the migration leaves one partial index over the active rows, and the old one is gone.</summary>
    [Fact]
    public async Task Migrations_JobTable_HasThePartialActiveIndexOnly()
    {
        var definitions = await _host.IndexDefinitionsAsync();

        definitions.Should().NotContain(definition => definition.Contains("ix_jobs_status_run_after_created_at", StringComparison.Ordinal));
        definitions.Should().ContainSingle(definition => definition.Contains("ix_jobs_active_created_at", StringComparison.Ordinal))
            .Which.Should().Contain("(created_at)").And.Contain("WHERE (status = ANY (ARRAY[0, 1]))");
    }

    /// <summary>BR2: the claim still takes the due row and the stale one, and never a failed row, with the table full.</summary>
    [Fact]
    public async Task RunPending_ManyFailedRowsKept_RunsTheDueAndTheStaleJobs()
    {
        await _host.EnqueueAsync(RecordingJobHandler.JobType, "due");
        var stale = await _host.EnqueueAsync(RecordingJobHandler.JobType, "stale");
        await _host.SetRunningAsync(stale, _host.Clock.GetUtcNow() - JobPolicy.StaleAfter - TimeSpan.FromMinutes(1));
        await _host.SeedFailedAsync(KeptFailedRows);

        (await _host.Runner.RunPendingAsync()).Should().Be(2);

        _host.Handler.Handled.Should().BeEquivalentTo(["due", "stale"]);
        (await _host.CountAsync()).Should().Be(KeptFailedRows, "only the failed rows remain");
    }
}
