using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Simulab.SharedKernel.Results;

namespace Simulab.Storage;

/// <summary>
/// <see cref="IFileStorage"/> over Azure Blob Storage (Azurite locally): one container, created private on first
/// use, so a file is never reachable by an address (F-89, BR8). Registered as a singleton.
/// </summary>
public sealed class BlobFileStorage(BlobServiceClient service) : IFileStorage
{
    /// <summary>The one container every file lives in.</summary>
    public const string ContainerName = "documents";

    private volatile BlobContainerClient? _container;

    public async Task<Result> SaveAsync(string name, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        var blob = (await ContainerAsync(cancellationToken)).GetBlobClient(name);
        await blob.UploadAsync(
            content,
            new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = contentType } },
            cancellationToken);

        return Result.Success();
    }

    public async Task<Stream?> OpenReadAsync(string name, CancellationToken cancellationToken = default)
    {
        var blob = (await ContainerAsync(cancellationToken)).GetBlobClient(name);
        try
        {
            return await blob.OpenReadAsync(cancellationToken: cancellationToken);
        }
        catch (RequestFailedException missing) when (missing.Status == 404)
        {
            return null;
        }
    }

    public async Task<bool> DeleteAsync(string name, CancellationToken cancellationToken = default) =>
        (await (await ContainerAsync(cancellationToken)).GetBlobClient(name).DeleteIfExistsAsync(cancellationToken: cancellationToken)).Value;

    public async Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default) =>
        (await (await ContainerAsync(cancellationToken)).GetBlobClient(name).ExistsAsync(cancellationToken)).Value;

    // Created on first use, with anonymous access off. Two requests racing here both ask for the same container, which is harmless.
    private async Task<BlobContainerClient> ContainerAsync(CancellationToken cancellationToken)
    {
        if (_container is { } ready)
        {
            return ready;
        }

        var container = service.GetBlobContainerClient(ContainerName);
        await container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);
        _container = container;

        return container;
    }
}
