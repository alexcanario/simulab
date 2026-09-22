using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Simulab.Jobs.Persistence;

namespace Simulab.Jobs;

/// <summary>
/// Takes one job at a time and runs it (F-13 BR5-BR10). The worker calls it in a loop; a test calls
/// <see cref="RunPendingAsync"/> directly, which is what keeps the tests free of waiting (BR13).
/// </summary>
public sealed class JobRunner(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<JobRunner> logger)
{
    /// <summary>
    /// BR8, BR9, BR10: one statement claims the oldest due row and marks it <see cref="JobStatus.Running"/>.
    /// <c>SKIP LOCKED</c> is what lets a second Api instance poll the same table without ever taking the
    /// row this one is taking. A row left <c>Running</c> by a stopped worker becomes due again.
    /// F-18: the statuses are constants, not parameters, so the planner can prove the query only wants the rows
    /// of the partial index <c>ix_jobs_active_created_at</c>, whatever plan it keeps.
    /// </summary>
    internal static readonly string ClaimSql = $$"""
        UPDATE {{JobsDbContext.QualifiedTableName}} SET status = {{(int)JobStatus.Running}}, started_at = {0}, attempts = attempts + 1
        WHERE id = (
            SELECT id FROM {{JobsDbContext.QualifiedTableName}}
            WHERE (status = {{(int)JobStatus.Pending}} AND run_after <= {0}) OR (status = {{(int)JobStatus.Running}} AND started_at <= {1})
            ORDER BY created_at
            FOR UPDATE SKIP LOCKED
            LIMIT 1
        )
        RETURNING *
        """;

    /// <summary>
    /// Runs the jobs that are due, oldest first, and returns how many ran. It stops at
    /// <see cref="JobPolicy.MaxJobsPerPoll"/> so one poll of a long backlog cannot hold a scope and a
    /// database connection indefinitely, nor ignore the worker's stopping token for that long.
    /// </summary>
    public async Task<int> RunPendingAsync(CancellationToken cancellationToken = default)
    {
        var ran = 0;
        while (ran < JobPolicy.MaxJobsPerPoll
               && !cancellationToken.IsCancellationRequested
               && await RunNextAsync(cancellationToken))
        {
            ran++;
        }

        return ran;
    }

    /// <summary>Runs the next due job. False when there was none.</summary>
    public async Task<bool> RunNextAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<JobsDbContext>();

        var job = await ClaimAsync(context, cancellationToken);
        if (job is null)
        {
            return false;
        }

        try
        {
            var handler = scope.ServiceProvider.GetServices<IJobHandler>()
                .FirstOrDefault(candidate => candidate.Type == job.Type)
                ?? throw new InvalidOperationException($"No handler is registered for job type '{job.Type}'.");

            await handler.HandleAsync(job.Payload, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await FailAsync(context, job, exception, cancellationToken);
            return true;
        }

        // BR6, outside the handler's own try on purpose: once the message is out, a delete that fails is
        // a database problem, not a failed send. Marking the job failed here would leave the entry in the
        // Deleted state, so the next save would repeat the DELETE and lose the error instead of writing it.
        try
        {
            context.Jobs.Remove(job);
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // BR10: another worker had taken the stale row and finished it first. The message went out.
            logger.LogWarning("Job {JobId} of type {JobType} was already gone when it was removed.", job.Id, job.Type);
        }

        return true;
    }

    private async Task<Job?> ClaimAsync(JobsDbContext context, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var stale = now - JobPolicy.StaleAfter;

        var claimed = await context.Jobs
            .FromSqlRaw(ClaimSql, now, stale)
            .ToListAsync(cancellationToken);

        return claimed.Count == 0 ? null : claimed[0];
    }

    /// <summary>BR5, BR7: back off and try again, or give up and keep the row as the evidence.</summary>
    private async Task FailAsync(JobsDbContext context, Job job, Exception exception, CancellationToken cancellationToken)
    {
        if (job.Attempts >= JobPolicy.MaxAttempts)
        {
            job.Status = JobStatus.Failed;

            // BR7 keeps the evidence, and BR6 keeps nothing sensitive at rest. The message itself is not
            // evidence: it is a live link and a recipient address, and a mail server error often echoes
            // the address back. What is lost stays in the log, with the whole exception; the row keeps
            // what it is, how often it was tried and what kind of failure stopped it.
            job.Payload = string.Empty;
            job.LastError = Truncate(exception.GetType().FullName ?? exception.GetType().Name);

            logger.LogError(
                exception,
                "Job {JobId} of type {JobType} failed {Attempts} times and was given up on; its payload was cleared.",
                job.Id, job.Type, job.Attempts);
        }
        else
        {
            job.Status = JobStatus.Pending;
            job.LastError = Truncate(exception.Message);
            job.RunAfter = timeProvider.GetUtcNow().Add(JobPolicy.BackoffAfter(job.Attempts));
            logger.LogWarning(
                exception,
                "Job {JobId} of type {JobType} failed on attempt {Attempts}; it runs again after {RunAfter}.",
                job.Id, job.Type, job.Attempts, job.RunAfter);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static string Truncate(string message) =>
        message.Length <= Persistence.Configurations.JobConfiguration.LastErrorMaxLength
            ? message
            : message[..Persistence.Configurations.JobConfiguration.LastErrorMaxLength];
}
