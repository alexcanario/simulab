using Simulab.QuestionBank.Contracts;
using Simulab.QuestionBank.Domain.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.QuestionBank.Application.Images;

/// <summary>
/// Stores an image a curator chose in the editor (F-103, UC1). The size is judged first, then the first bytes decide
/// whether it is an image at all (BR2); the file goes to the storage under a name the module makes up (BR13) and the row
/// is written after it, as a pending image (BR3). A row that cannot be written takes its file back.
/// </summary>
public sealed class UploadQuestionImageHandler(IQuestionImageStore store, IQuestionImageFiles files)
{
    public const string UrlPrefix = "/question-images/";

    public async Task<Result<QuestionImageResponse>> HandleAsync(
        Stream content,
        long length,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (length > QuestionLimits.ImageMaxBytes)
        {
            return Failure(QuestionBankErrorCodes.QuestionImageTooLarge);
        }

        var header = new byte[QuestionImageFormat.SignatureLength];
        content.Position = 0;
        var read = await content.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, cancellationToken);
        content.Position = 0;
        var contentType = QuestionImageFormat.Detect(header.AsSpan(0, read));
        if (contentType is null)
        {
            return Failure(QuestionBankErrorCodes.QuestionImageNotAnImage);
        }

        var blobName = $"question-images/{Guid.CreateVersion7()}";
        var stored = await files.SaveAsync(blobName, content, contentType, cancellationToken);
        if (stored.IsFailure)
        {
            return Result.Failure<QuestionImageResponse>(stored.Error!);
        }

        var image = new QuestionImage(blobName, contentType, length);
        store.Add(image);
        try
        {
            await store.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await files.DeleteAsync(blobName, CancellationToken.None);
            throw;
        }

        return Result.Success(new QuestionImageResponse(image.Id, UrlPrefix + image.Id, contentType, length));
    }

    private static Result<QuestionImageResponse> Failure(string code) =>
        Result.Failure<QuestionImageResponse>(new Error(code, ErrorKind.Validation));
}
