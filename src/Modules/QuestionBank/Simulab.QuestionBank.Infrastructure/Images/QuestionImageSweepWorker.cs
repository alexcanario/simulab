using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Simulab.QuestionBank.Infrastructure.Images;

/// <summary>
/// Runs <see cref="QuestionImageSweep"/> every hour in the Api host (F-103, BR11). <c>QuestionBank:ImageSweepEnabled</c>
/// (true by default) switches it off, as <c>Jobs:WorkerEnabled</c> does for the job worker: a test drains the sweep itself.
/// </summary>
public sealed class QuestionImageSweepWorker(
    QuestionImageSweep sweep,
    IConfiguration configuration,
    ILogger<QuestionImageSweepWorker> logger) : BackgroundService
{
    public const string EnabledKey = "QuestionBank:ImageSweepEnabled";

    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!configuration.GetValue(EnabledKey, true))
        {
            return;
        }

        using var timer = new PeriodicTimer(Interval);
        do
        {
            try
            {
                await sweep.RunAsync(stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "The question image sweep failed; the next hour tries again.");
            }
        }
        while (await WaitAsync(timer, stoppingToken));
    }

    private static async Task<bool> WaitAsync(PeriodicTimer timer, CancellationToken stoppingToken)
    {
        try
        {
            return await timer.WaitForNextTickAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
