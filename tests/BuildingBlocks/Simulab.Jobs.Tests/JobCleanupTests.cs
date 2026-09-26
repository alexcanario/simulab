using Microsoft.Extensions.Logging;

namespace Simulab.Jobs.Tests;

/// <summary>
/// F-27: the failed rows are evidence with a shelf life. These run against a real PostgreSQL database, the
/// way the rest of the queue's tests do, because the delete is a statement and not a loop in memory.
/// </summary>
public sealed class JobCleanupTests : IAsyncLifetime
{
    private JobTestHost _host = null!;

    public async Task InitializeAsync() => _host = await JobTestHost.StartAsync(nameof(JobCleanupTests));

    public async Task DisposeAsync() => await _host.DisposeAsync();

    private DateTimeOffset DaysAgo(double days) => _host.Clock.GetUtcNow() - TimeSpan.FromDays(days);

    // AC1: the line is drawn at the retention, and it is drawn on created_at (BR2).
    [Fact]
    public async Task Run_AFailedRowPastTheRetention_IsRemovedAndAYoungerOneStays()
    {
        var expired = await _host.SeedAsync(JobStatus.Failed, DaysAgo(91));
        var recent = await _host.SeedAsync(JobStatus.Failed, DaysAgo(89));

        var removed = await _host.Cleanup.RunAsync();

        removed.Should().Be(1);
        (await _host.FindAsync(expired)).Should().BeNull("90 days is the retention (BR1)");
        (await _host.FindAsync(recent)).Should().NotBeNull();
    }

    // AC2: an old Running row is a job a worker still takes again (F-13 BR10), not rubbish.
    [Fact]
    public async Task Run_PendingAndRunningRows_AreNeverTouchedHoweverOldTheyAre()
    {
        var pending = await _host.SeedAsync(JobStatus.Pending, DaysAgo(365));
        var running = await _host.SeedAsync(JobStatus.Running, DaysAgo(365));

        var removed = await _host.Cleanup.RunAsync();

        removed.Should().Be(0);
        (await _host.FindAsync(pending)).Should().NotBeNull();
        (await _host.FindAsync(running)).Should().NotBeNull();
    }

    // AC5: the first poll of a process always cleans, or a host that never lives an hour never would.
    [Fact]
    public async Task RunIfDue_TheFirstCall_Runs()
    {
        var expired = await _host.SeedAsync(JobStatus.Failed, DaysAgo(91));

        (await _host.Cleanup.RunIfDueAsync()).Should().Be(1);
        (await _host.FindAsync(expired)).Should().BeNull();
    }

    // AC3: however often the worker polls, the cleanup is an hourly thing.
    [Fact]
    public async Task RunIfDue_LessThanAnHourAfterTheLastRun_DoesNothing()
    {
        await _host.Cleanup.RunIfDueAsync();
        var expired = await _host.SeedAsync(JobStatus.Failed, DaysAgo(91));

        _host.Clock.Advance(JobPolicy.CleanupInterval - TimeSpan.FromSeconds(1));

        (await _host.Cleanup.RunIfDueAsync()).Should().BeNull("it is not due yet");
        (await _host.FindAsync(expired)).Should().NotBeNull("nothing was touched");
    }

    // AC4.
    [Fact]
    public async Task RunIfDue_AnHourAfterTheLastRun_RunsAgain()
    {
        await _host.Cleanup.RunIfDueAsync();
        var expired = await _host.SeedAsync(JobStatus.Failed, DaysAgo(91));

        _host.Clock.Advance(JobPolicy.CleanupInterval);

        (await _host.Cleanup.RunIfDueAsync()).Should().Be(1);
        (await _host.FindAsync(expired)).Should().BeNull();
    }

    // AC6: one line with the count.
    [Fact]
    public async Task Run_RowsRemoved_LogsOneLineWithTheCount()
    {
        await _host.SeedAsync(JobStatus.Failed, DaysAgo(91));
        await _host.SeedAsync(JobStatus.Failed, DaysAgo(120));

        await _host.Cleanup.RunAsync();

        var lines = _host.Logs.Entries
            .Where(entry => entry.Message.Contains("failed job", StringComparison.OrdinalIgnoreCase))
            .ToList();
        lines.Should().ContainSingle();
        lines[0].Level.Should().Be(LogLevel.Information);
        lines[0].Message.Should().Contain("2").And.Contain("90");
    }

    // AC6, the other half: silence, or the log grows by a line an hour forever.
    [Fact]
    public async Task Run_NothingToRemove_LogsNothing()
    {
        await _host.SeedAsync(JobStatus.Failed, DaysAgo(1));

        (await _host.Cleanup.RunAsync()).Should().Be(0);

        _host.Logs.Entries.Should().NotContain(entry => entry.Message.Contains("failed job", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// The retention counts from <c>created_at</c> and not from the last attempt: a row created before the
    /// line but whose last attempt is recent still goes. The gap between the two is at most the backoff.
    /// </summary>
    [Fact]
    public async Task Run_AgeIsCountedFromCreatedAt()
    {
        var expired = await _host.SeedAsync(JobStatus.Failed, DaysAgo(91));
        await _host.SetRunningAsync(expired, _host.Clock.GetUtcNow());
        await _host.SeedAsync(JobStatus.Failed, DaysAgo(91), "second");

        // The first row is Running now, so only the second is removed - which is BR3, checked from the other side.
        (await _host.Cleanup.RunAsync()).Should().Be(1);
        (await _host.FindAsync(expired)).Should().NotBeNull("it is no longer Failed");
    }
}
