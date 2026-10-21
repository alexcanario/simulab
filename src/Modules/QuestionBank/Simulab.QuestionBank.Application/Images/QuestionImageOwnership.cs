using Simulab.QuestionBank.Domain;
using Simulab.SharedKernel.Results;

namespace Simulab.QuestionBank.Application.Images;

/// <summary>What a save did with the images of a text: the error that refuses it, or the files to delete once it is committed.</summary>
public sealed record QuestionImageSync(Error? Refused, IReadOnlyList<string> ReleasedBlobs)
{
    public static QuestionImageSync Accepted(IReadOnlyList<string> releasedBlobs) => new(null, releasedBlobs);
}

/// <summary>
/// The rule both saves share (F-103, BR6, BR9, BR10): the images named in the sanitized HTML become the owner's, and the
/// images the owner held that the HTML no longer names are released. A save that names an image that does not exist, or
/// that another owner holds, is refused with the owner's own not-found code. An image leaves only when no stored version
/// holds it; its file is deleted after the save committed, never before.
/// </summary>
public sealed class QuestionImageOwnership(IQuestionImageStore store, IQuestionImageFiles files)
{
    /// <param name="kind">Whether the owner is a question or a base text.</param>
    /// <param name="ownerId">The owner's id; an entity makes it before it is saved.</param>
    /// <param name="imageIds">The ids the sanitized HTML of the owner names, repeats included.</param>
    /// <param name="notFoundCode">The code the owner's refusal carries: <c>question.image_not_found</c> or <c>base_text.image_not_found</c>.</param>
    /// <param name="cancellationToken">Cancels the reads.</param>
    public async Task<QuestionImageSync> SyncAsync(
        QuestionImageOwnerKind kind,
        Guid ownerId,
        IReadOnlyCollection<Guid> imageIds,
        string notFoundCode,
        CancellationToken cancellationToken)
    {
        var named = imageIds.Distinct().ToList();
        if (named.Count > 0)
        {
            var found = (await store.FindForUpdateAsync(named, cancellationToken)).ToDictionary(image => image.Id);
            foreach (var id in named)
            {
                if (!found.TryGetValue(id, out var image) || !image.TryClaim(kind, ownerId))
                {
                    return new QuestionImageSync(new Error(notFoundCode, ErrorKind.NotFound), []);
                }
            }
        }

        var released = new List<string>();
        foreach (var image in await store.ListOwnedAsync(kind, ownerId, cancellationToken))
        {
            if (named.Contains(image.Id) || await store.IsHeldByAVersionAsync(kind, ownerId, image.Id, cancellationToken))
            {
                continue;
            }

            store.Remove(image);
            released.Add(image.BlobName);
        }

        return QuestionImageSync.Accepted(released);
    }

    /// <summary>Deletes the files of the released images, after the save committed (BR10). The adapter logs a failure and leaves an orphan.</summary>
    public async Task DeleteFilesAsync(QuestionImageSync sync, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sync);

        foreach (var blobName in sync.ReleasedBlobs)
        {
            await files.DeleteAsync(blobName, cancellationToken);
        }
    }
}
