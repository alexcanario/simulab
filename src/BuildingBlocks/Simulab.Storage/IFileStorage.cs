using Simulab.SharedKernel.Results;

namespace Simulab.Storage;

/// <summary>
/// Where Simulab keeps the files users upload (ADR-0001 #23, F-89): one private container, files addressed by a
/// name the caller makes up (never the user's file name), nothing handed out as a storage address. Modules do
/// not use it directly: each wraps it in a port of its own, as Identity does with the email sender.
/// </summary>
public interface IFileStorage
{
    /// <summary>
    /// Stores <paramref name="content"/> under <paramref name="name"/>, replacing a file of that name. A failure
    /// is the stable code <see cref="StorageErrorCodes.NotConfigured"/> when the host has no storage.
    /// </summary>
    Task<Result> SaveAsync(string name, Stream content, string contentType, CancellationToken cancellationToken = default);

    /// <summary>The file's content, or null when there is none. The caller disposes the stream.</summary>
    Task<Stream?> OpenReadAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>Deletes the file; true when there was one to delete.</summary>
    Task<bool> DeleteAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>True when a file of this name is stored.</summary>
    Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default);
}
