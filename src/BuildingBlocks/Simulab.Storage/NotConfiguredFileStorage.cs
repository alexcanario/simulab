using Simulab.SharedKernel.Results;
using Simulab.Storage.Contracts;

namespace Simulab.Storage;

/// <summary>
/// What a Development host without a storage connection gets (open point 1 of F-89): the app starts, a save
/// answers <see cref="StorageErrorCodes.NotConfigured"/>, and a read finds nothing.
/// </summary>
public sealed class NotConfiguredFileStorage : IFileStorage
{
    public Task<Result> SaveAsync(string name, Stream content, string contentType, CancellationToken cancellationToken = default) =>
        Task.FromResult(Result.Failure(new Error(StorageErrorCodes.NotConfigured, ErrorKind.BusinessRule)));

    public Task<Stream?> OpenReadAsync(string name, CancellationToken cancellationToken = default) =>
        Task.FromResult<Stream?>(null);

    public Task<bool> DeleteAsync(string name, CancellationToken cancellationToken = default) =>
        Task.FromResult(false);

    public Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default) =>
        Task.FromResult(false);
}
