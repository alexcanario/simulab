using Simulab.SharedKernel.Results;

namespace Simulab.Storage;

/// <summary>
/// What a Development host without a storage connection gets (open point 1 of F-89): the app starts, a save
/// answers <see cref="StorageErrorCodes.NotConfigured"/>, and a read finds nothing.
/// </summary>
public sealed class NotConfiguredFileStorage : IFileStorage
{
    public Task<Result> SaveAsync(string name, Stream content, string contentType, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<Stream?> OpenReadAsync(string name, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<bool> DeleteAsync(string name, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();
}
