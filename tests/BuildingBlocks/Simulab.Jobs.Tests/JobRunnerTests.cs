using Simulab.Email;
using Simulab.Jobs.Email;

namespace Simulab.Jobs.Tests;

/// <summary>F-13 AC5-AC9: what the runner does with a job that works, one that fails, and one it shares.</summary>
public sealed class JobRunnerTests : IAsyncLifetime
{
    private JobTestHost _host = null!;

    public async Task InitializeAsync() => _host = await JobTestHost.StartAsync(nameof(JobRunnerTests));

    public async Task DisposeAsync() => await _host.DisposeAsync();

    [Fact]
    public async Task RunNext_JobSucceeds_LeavesNoRow()
    {
        var id = await _host.EnqueueAsync(RecordingJobHandler.JobType, "work");

        (await _host.Runner.RunNextAsync()).Should().BeTrue();

        _host.Handler.Handled.Should().Equal("work");
        (await _host.FindAsync(id)).Should().BeNull("a job that succeeds is deleted (BR6)");
        (await _host.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task RunNext_NothingDue_DoesNothing()
    {
        (await _host.Runner.RunNextAsync()).Should().BeFalse();
        _host.Handler.Handled.Should().BeEmpty();
    }

    [Fact]
    public async Task RunNext_HandlerThrows_KeepsTheJobPendingWithABackoff()
    {
        var id = await _host.EnqueueAsync(RecordingJobHandler.JobType, "work");
        _host.Handler.Fails = true;

        await _host.Runner.RunNextAsync();

        var job = await _host.FindAsync(id);
        job.Should().NotBeNull();
        job!.Attempts.Should().Be(1);
        job.Status.Should().Be(JobStatus.Pending);
        job.LastError.Should().Be(_host.Handler.FailureMessage);
        job.RunAfter.Should().Be(_host.Clock.GetUtcNow().AddMinutes(1), "BR5: the first retry waits one minute");
    }

    [Fact]
    public async Task RunNext_BeforeTheBackoffHasPassed_DoesNotTakeTheJobAgain()
    {
        await _host.EnqueueAsync(RecordingJobHandler.JobType, "work");
        _host.Handler.Fails = true;
        await _host.Runner.RunNextAsync();

        _host.Clock.Advance(TimeSpan.FromSeconds(59));

        (await _host.Runner.RunNextAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task RunNext_FifthAttemptFails_MarksTheJobFailedAndStopsTakingIt()
    {
        var id = await _host.EnqueueAsync(RecordingJobHandler.JobType, "work");
        _host.Handler.Fails = true;

        for (var attempt = 1; attempt <= JobPolicy.MaxAttempts; attempt++)
        {
            (await _host.Runner.RunNextAsync()).Should().BeTrue($"attempt {attempt} is due");
            _host.Clock.Advance(JobPolicy.BackoffAfter(attempt));
        }

        var job = await _host.FindAsync(id);
        job.Should().NotBeNull();
        job!.Attempts.Should().Be(JobPolicy.MaxAttempts);
        job.Status.Should().Be(JobStatus.Failed);
        job.LastError.Should().Be(_host.Handler.FailureMessage, "BR7: the row keeps the evidence");

        _host.Clock.Advance(TimeSpan.FromDays(1));
        (await _host.Runner.RunNextAsync()).Should().BeFalse("a failed job is never taken again");
    }

    [Fact]
    public async Task RunNext_RetryAfterTheBackoff_Succeeds()
    {
        var id = await _host.EnqueueAsync(RecordingJobHandler.JobType, "work");
        _host.Handler.Fails = true;
        await _host.Runner.RunNextAsync();

        _host.Handler.Fails = false;
        _host.Clock.Advance(TimeSpan.FromMinutes(1));

        (await _host.Runner.RunNextAsync()).Should().BeTrue();
        _host.Handler.Handled.Should().Equal("work");
        (await _host.FindAsync(id)).Should().BeNull();
    }

    [Fact]
    public async Task RunNext_JobLeftRunningTooLong_IsClaimedAgain()
    {
        var id = await _host.EnqueueAsync(RecordingJobHandler.JobType, "work");
        await _host.SetRunningAsync(id, _host.Clock.GetUtcNow());

        // BR10: before the timeout the row belongs to the worker that took it.
        (await _host.Runner.RunNextAsync()).Should().BeFalse();

        _host.Clock.Advance(JobPolicy.StaleAfter);

        (await _host.Runner.RunNextAsync()).Should().BeTrue("BR10: a worker that stopped mid-send leaves it behind");
        _host.Handler.Handled.Should().Equal("work");
    }

    [Fact]
    public async Task RunPending_SeveralWorkersAtOnce_RunsEachJobExactlyOnce()
    {
        var payloads = Enumerable.Range(1, 20).Select(number => $"work-{number}").ToList();
        foreach (var payload in payloads)
        {
            await _host.EnqueueAsync(RecordingJobHandler.JobType, payload);
        }

        var workers = Enumerable.Range(0, 4).Select(_ => _host.Runner.RunPendingAsync()).ToList();
        var ran = await Task.WhenAll(workers);

        // BR8: SKIP LOCKED plus the Running state means no row is ever taken twice.
        ran.Sum().Should().Be(payloads.Count);
        _host.Handler.Handled.Should().BeEquivalentTo(payloads);
        (await _host.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task RunNext_NoHandlerForTheType_FailsTheJobWithoutLosingIt()
    {
        var id = await _host.EnqueueAsync("nobody.handles.this", "work");

        await _host.Runner.RunNextAsync();

        var job = await _host.FindAsync(id);
        job.Should().NotBeNull();
        job!.Attempts.Should().Be(1);
        job.LastError.Should().Contain("nobody.handles.this");
    }

    [Fact]
    public async Task RunNext_EmailJob_SendsTheMessageThatWasStored()
    {
        var message = new EmailMessage("student@example.com", "Confirme o seu e-mail", "<p>link</p>", "link");
        await _host.EnqueueAsync(EmailJob.Type, EmailJob.Serialize(message));

        await _host.Runner.RunNextAsync();

        _host.Emails.Count.Should().Be(1);
        _host.Emails.Last.Should().Be(message);
        (await _host.CountAsync()).Should().Be(0);
    }
}
