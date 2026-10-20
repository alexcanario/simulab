using Azure.Storage.Blobs;
using Simulab.SharedKernel.Results;

namespace Simulab.Storage;

/// <summary><see cref="IFileStorage"/> over Azure Blob Storage (Azurite locally): one private container.</summary>
public sealed class BlobFileStorage(BlobServiceClient service) : IFileStorage
{
    /// <summary>The one container every file lives in; created private on first use.</summary>
    public const string ContainerName = "documents";

    public Task<Result> SaveAsync(string name, Stream content, string contentType, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<Stream?> OpenReadAsync(string name, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<bool> DeleteAsync(string name, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();
}
