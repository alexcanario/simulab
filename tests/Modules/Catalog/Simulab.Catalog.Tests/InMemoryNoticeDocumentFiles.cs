using System.Collections.Concurrent;
using Simulab.Catalog.Application.ExamEditions;
using Simulab.SharedKernel.Results;

namespace Simulab.Catalog.Tests;

/// <summary>
/// The notice PDFs of F-89 in memory: the Catalog tests prove the handlers and the endpoints against it, and the
/// real blob emulator is proved once, in the storage block's own tests (design trade-off 6).
/// </summary>
public sealed class InMemoryNoticeDocumentFiles : INoticeDocumentFiles
{
    private readonly ConcurrentDictionary<string, byte[]> _files = new();

    /// <summary>The blob names stored right now.</summary>
    public IReadOnlyCollection<string> Names => [.. _files.Keys];

    public async Task<Result> SaveAsync(string blobName, Stream content, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        using var copy = new MemoryStream();
        await content.CopyToAsync(copy, cancellationToken);
        _files[blobName] = copy.ToArray();

        return Result.Success();
    }

    public Task<Stream?> OpenReadAsync(string blobName, CancellationToken cancellationToken = default) =>
        Task.FromResult<Stream?>(_files.TryGetValue(blobName, out var bytes) ? new MemoryStream(bytes) : null);

    public Task DeleteAsync(string blobName, CancellationToken cancellationToken = default)
    {
        _files.TryRemove(blobName, out _);
        return Task.CompletedTask;
    }
}
