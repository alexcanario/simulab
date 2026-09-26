using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Simulab.Jobs;

/// <summary>
/// The hosted worker of ADR-0001 #20, in the Api host (F-13 BR13). It does nothing but call
/// <see cref="JobRunner"/> every <see cref="JobPolicy.PollInterval"/>; a test host switches it off and
/// drives the runner itself, so no test ever waits for a timer.
/// </summary>
public sealed class JobWorker(
    JobRunner runner,
    JobCleanup cleanup,
    IOptions<JobOptions> options,
    TimeProvider timeProvider,
    ILogger<JobWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.WorkerEnabled)
        {
            logger.LogInformation("The job worker is switched off by configuration.");
            return;
        }

        using var timer = new PeriodicTimer(JobPolicy.PollInterval, timeProvider);
        while (!stoppingToken.IsCancellationRequested)
        {
            // F-27 BR5: the cleanup goes first and in its own try, so a cleanup that throws never keeps the
            // jobs of this poll from running. It decides for itself whether an hour has passed.
            try
            {
                await cleanup.RunIfDueAsync(stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "The failed-job cleanup failed.");
            }

            try
            {
                await runner.RunPendingAsync(stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // A poll that throws is the database being unreachable: log it and try again next tick,
                // never let it end the worker for the life of the process.
                logger.LogError(exception, "A job poll failed.");
            }

            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }
}
