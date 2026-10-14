using Simulab.Catalog.Application.Topics;
using Simulab.Catalog.Contracts;
using Simulab.SharedKernel.Results;

namespace Simulab.Catalog.Application.Subjects;

/// <summary>
/// Removes a subject from the taxonomy (F-79, UC4, BR8). The removal is a soft delete: the row stays, so its
/// name stays taken. A subject that still has topics does not leave at all — the topics would be orphaned.
/// </summary>
public sealed class DeleteSubjectHandler(ISubjectStore store, ITopicStore topics)
{
    public async Task<Result> HandleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var subject = await store.FindAsync(id, cancellationToken);
        if (subject is null)
        {
            return Result.Failure(new Error(CatalogErrorCodes.SubjectNotFound, ErrorKind.NotFound));
        }

        // Deleted topics do not count: they are gone from the taxonomy, so nothing is orphaned.
        if (await topics.SubjectHasTopicsAsync(id, cancellationToken))
        {
            return Result.Failure(new Error(CatalogErrorCodes.SubjectHasTopics, ErrorKind.Conflict));
        }

        // F-75 BR9: a live notice subject maps it as a whole subject. The topics check above runs first.
        if (await store.IsMappedWholeAsync(id, cancellationToken))
        {
            return Result.Failure(new Error(CatalogErrorCodes.SubjectInUse, ErrorKind.Conflict));
        }

        store.Remove(subject);
        await store.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
