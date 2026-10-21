using Simulab.SharedKernel.Results;

namespace Simulab.QuestionBank.Application.Images;

/// <summary>
/// Where the files of question images live (F-103, ADR-0001 #23). A port of this module over the storage block:
/// the Application project cannot reference the block, so the adapter sits in Infrastructure. Files are addressed
/// by a name the module makes up, never by the user's file name.
/// </summary>
public interface IQuestionImageFiles
{
    /// <summary>Stores the file under <paramref name="blobName"/>; a failure carries the storage code.</summary>
    Task<Result> SaveAsync(string blobName, Stream content, string contentType, CancellationToken cancellationToken = default);

    /// <summary>The file's content, or null when there is none. The caller disposes the stream.</summary>
    Task<Stream?> OpenReadAsync(string blobName, CancellationToken cancellationToken = default);

    /// <summary>Deletes the file; nothing happens when there is none.</summary>
    Task DeleteAsync(string blobName, CancellationToken cancellationToken = default);
}
