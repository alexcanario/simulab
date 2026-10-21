using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Results;

namespace Simulab.QuestionBank.Application.Images;

/// <summary>The file of a stored image and its type, for the Api to stream; the caller disposes <see cref="Content"/>.</summary>
public sealed record QuestionImageContent(Stream Content, string ContentType);

/// <summary>
/// Opens the file of an image (F-103, BR12). Pending and owned images read the same: the editor shows a picture right
/// after the upload. An unknown or deleted image, or one whose file is gone, is the same not found.
/// </summary>
public sealed class OpenQuestionImageHandler(IQuestionImageStore store, IQuestionImageFiles files)
{
    public async Task<Result<QuestionImageContent>> HandleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var image = await store.FindAsync(id, cancellationToken);
        var content = image is null ? null : await files.OpenReadAsync(image.BlobName, cancellationToken);

        return image is null || content is null
            ? Result.Failure<QuestionImageContent>(new Error(QuestionBankErrorCodes.QuestionImageNotFound, ErrorKind.NotFound))
            : Result.Success(new QuestionImageContent(content, image.ContentType));
    }
}
