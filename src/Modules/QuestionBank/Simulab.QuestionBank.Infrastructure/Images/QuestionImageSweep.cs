using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Simulab.QuestionBank.Application.Images;
using Simulab.QuestionBank.Infrastructure.Persistence;

namespace Simulab.QuestionBank.Infrastructure.Images;

/// <summary>
/// Deletes the images nobody saved a text with (F-103, BR11): a pending image older than 24 hours, its file first and then
/// its row, so a row never points at a missing file. A file that cannot be deleted keeps its row and is tried again on the
/// next pass. The hosted worker calls it every hour; a test calls it directly.
/// </summary>
public sealed partial class QuestionImageSweep(IServiceScopeFactory scopes, TimeProvider clock, ILogger<QuestionImageSweep> logger)
{
    /// <summary>How long a pending image waits for a save to claim it.</summary>
    public static readonly TimeSpan PendingLifetime = TimeSpan.FromHours(24);

    // A pass handles a bounded batch, so one run never holds the context for long; the next hour takes the rest.
    private const int BatchSize = 200;

    /// <summary>One pass; the number of images removed.</summary>
    public async Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = scopes.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<QuestionBankModuleDbContext>();
        var files = scope.ServiceProvider.GetRequiredService<IQuestionImageFiles>();

        var cutoff = clock.GetUtcNow() - PendingLifetime;
        var expired = await context.QuestionImages
            .Where(image => image.OwnerId == null && image.CreatedAt < cutoff)
            .OrderBy(image => image.CreatedAt)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        var removed = 0;
        foreach (var image in expired)
        {
            await files.DeleteAsync(image.BlobName, cancellationToken);

            // The adapter logs a failed delete and returns; the file still being there keeps the row for the next pass.
            await using (var remaining = await files.OpenReadAsync(image.BlobName, cancellationToken))
            {
                if (remaining is not null)
                {
                    LogKept(logger, image.Id);

                    continue;
                }
            }

            context.QuestionImages.Remove(image);
            removed++;
        }

        if (removed > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
            LogRemoved(logger, removed);
        }

        return removed;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Question image sweep removed {Removed} pending image(s) older than 24 hours.")]
    private static partial void LogRemoved(ILogger logger, int removed);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The pending question image {ImageId} could not be deleted; the next pass tries again.")]
    private static partial void LogKept(ILogger logger, Guid imageId);
}
