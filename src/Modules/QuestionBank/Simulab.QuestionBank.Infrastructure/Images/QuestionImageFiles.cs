using Microsoft.Extensions.Logging;
using Simulab.QuestionBank.Application.Images;
using Simulab.SharedKernel.Results;
using Simulab.Storage;

namespace Simulab.QuestionBank.Infrastructure.Images;

/// <summary>
/// <see cref="IQuestionImageFiles"/> over the storage building block (F-103): the pictures live in the one private
/// container, under <c>question-images/{guid}</c>. A delete that fails is logged and left as an orphan blob — the row has
/// already stopped pointing at it, and a lost file is worse than a stray one (the rule of F-89).
/// </summary>
public sealed class QuestionImageFiles(IFileStorage storage, ILogger<QuestionImageFiles> logger) : IQuestionImageFiles
{
    public Task<Result> SaveAsync(string blobName, Stream content, string contentType, CancellationToken cancellationToken = default) =>
        storage.SaveAsync(blobName, content, contentType, cancellationToken);

    public Task<Stream?> OpenReadAsync(string blobName, CancellationToken cancellationToken = default) =>
        storage.OpenReadAsync(blobName, cancellationToken);

    public async Task DeleteAsync(string blobName, CancellationToken cancellationToken = default)
    {
        try
        {
            await storage.DeleteAsync(blobName, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "The question image {BlobName} could not be deleted and stays in the storage as an orphan.", blobName);
        }
    }
}
