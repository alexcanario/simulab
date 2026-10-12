using Simulab.Catalog.Contracts;

namespace Simulab.Catalog.Infrastructure.Persistence;

/// <summary>
/// What another module may ask about subjects and their topics (B-28): live rows only, over the catalog's own
/// context. The global soft-delete filters leave out a deleted subject and a deleted topic.
/// </summary>
public sealed class SubjectDirectory(CatalogModuleDbContext context) : ISubjectDirectory
{
    public Task<IReadOnlyList<SubjectDirectoryEntry>> SearchAsync(
        string? search,
        int take,
        CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task<IReadOnlyList<TopicDirectoryEntry>?> ListTopicsAsync(Guid subjectId, CancellationToken cancellationToken) =>
        throw new NotImplementedException();
}
