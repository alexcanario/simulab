using Simulab.SharedKernel.Results;

namespace Simulab.Catalog.Application.ExamEditions;

/// <summary>
/// Where the handlers keep the notice PDFs (F-89). The port exists because Application references only Contracts,
/// Domain and SharedKernel; the adapter in Infrastructure wraps the storage building block.
/// </summary>
public interface INoticeDocumentFiles
{
    /// <summary>Stores the file under this blob name; a failure carries the stable code to answer with.</summary>
    Task<Result> SaveAsync(string blobName, Stream content, CancellationToken cancellationToken = default);

    /// <summary>The file's content, or null when there is none. The caller disposes the stream.</summary>
    Task<Stream?> OpenReadAsync(string blobName, CancellationToken cancellationToken = default);

    /// <summary>Deletes the file; a missing file is not an error.</summary>
    Task DeleteAsync(string blobName, CancellationToken cancellationToken = default);
}
