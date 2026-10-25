using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Simulab.QuestionBank.Application.Images;
using Simulab.QuestionBank.Infrastructure.Persistence;

namespace Simulab.QuestionBank.Infrastructure.Images;

/// <summary>
/// F-110 (BR8): writes the files of the images the <c>SeedManausGuardQuestions</c> migration seeded. A migration cannot
/// store a blob, so the rows come with the migration and the bytes, embedded in this assembly and named by the image id,
/// are written by the Api right after the migrations. A file is written only for a live row whose blob is missing: an
/// existing blob is never overwritten, a deleted row gets nothing, and a failed write is logged and tried again at the
/// next start.
/// </summary>
public sealed partial class SeedQuestionImageFiles(IServiceScopeFactory scopes, ILogger<SeedQuestionImageFiles> logger)
{
    private const string ResourcePrefix = "SeedImages.";
    private const string ResourceExtension = ".png";
    private const string Exists = "exists";

    /// <summary>Writes each missing file of a live seeded image row; the number of files written.</summary>
    public async Task<int> WriteMissingAsync(CancellationToken cancellationToken = default)
    {
        var assembly = typeof(SeedQuestionImageFiles).Assembly;
        var embedded = assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(ResourcePrefix, StringComparison.Ordinal) && name.EndsWith(ResourceExtension, StringComparison.Ordinal))
            .ToDictionary(name => Guid.Parse(name[ResourcePrefix.Length..^ResourceExtension.Length]));

        await using var scope = scopes.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<QuestionBankModuleDbContext>();
        var files = scope.ServiceProvider.GetRequiredService<IQuestionImageFiles>();

        var ids = embedded.Keys.ToList();
        var rows = await context.QuestionImages
            .AsNoTracking()
            .Where(image => ids.Contains(image.Id))
            .Select(image => new { image.Id, image.BlobName, image.ContentType })
            .ToListAsync(cancellationToken);

        var written = 0;
        foreach (var row in rows)
        {
            // A storage that cannot be reached must not stop the Api from starting: the file waits for the next start.
            try
            {
                var refused = await WriteIfMissingAsync(files, row.BlobName, row.ContentType, embedded[row.Id], cancellationToken);
                if (refused == Exists)
                {
                    continue;
                }

                if (refused is not null)
                {
                    LogNotWritten(logger, row.Id, refused);

                    continue;
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                LogFailed(logger, exception, row.Id);

                continue;
            }

            written++;
        }

        return written;
    }

    // Null when the file was written; the reason otherwise ("exists" for a blob that is already there).
    private static async Task<string?> WriteIfMissingAsync(
        IQuestionImageFiles files, string blobName, string contentType, string resource, CancellationToken cancellationToken)
    {
        await using (var existing = await files.OpenReadAsync(blobName, cancellationToken))
        {
            if (existing is not null)
            {
                return Exists;
            }
        }

        await using var content = typeof(SeedQuestionImageFiles).Assembly.GetManifestResourceStream(resource)!;
        var saved = await files.SaveAsync(blobName, content, contentType, cancellationToken);

        return saved.IsFailure ? saved.Error!.Code : null;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "The file of the seeded question image {ImageId} could not be written ({Code}); the next start tries again.")]
    private static partial void LogNotWritten(ILogger logger, Guid imageId, string code);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The file of the seeded question image {ImageId} could not be written; the next start tries again.")]
    private static partial void LogFailed(ILogger logger, Exception exception, Guid imageId);
}
