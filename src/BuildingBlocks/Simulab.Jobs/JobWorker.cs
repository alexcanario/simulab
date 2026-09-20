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
