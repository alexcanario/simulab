using Simulab.Catalog.Domain.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.Catalog.Application.NoticeSubjects;

/// <summary>What the notice subject handlers, and the edition delete, need from the database (F-74).</summary>
public interface INoticeSubjectStore
{
    /// <summary>The notice subject with this id inside this edition, or null when it does not exist, was deleted or belongs to another edition.</summary>
    Task<NoticeSubject?> FindAsync(Guid examEditionId, Guid id, CancellationToken cancellationToken);

    /// <summary>The edition's live notice subjects in display order, tracked so the order can be rewritten.</summary>
    Task<IReadOnlyList<NoticeSubject>> ListForEditionAsync(Guid examEditionId, CancellationToken cancellationToken);

    /// <summary>
    /// True when another live notice subject of this edition holds this normalized label inside this normalized
    /// group (BR5). Deleted rows do not count: the unique index is filtered the same way.
    /// </summary>
    Task<bool> IsDuplicateAsync(
        Guid examEditionId,
        string normalizedGroup,
        string normalizedLabel,
        Guid? exceptId,
        CancellationToken cancellationToken);

    /// <summary>The notice subject's live mapping entries, tracked so a save can drop some and keep the rest (F-75, BR7).</summary>
    Task<IReadOnlyList<NoticeSubjectMapping>> ListMappingsAsync(Guid noticeSubjectId, CancellationToken cancellationToken);

    /// <summary>
    /// Which of these subjects and topics exist and are not deleted, and each live topic's current subject
    /// (F-75, BR3, BR4). An id that is not in the answer does not exist.
    /// </summary>
    Task<MappingTargets> FindLiveTargetsAsync(
        IReadOnlyCollection<Guid> subjectIds,
        IReadOnlyCollection<Guid> topicIds,
        CancellationToken cancellationToken);

    void AddMapping(NoticeSubjectMapping mapping);

    /// <summary>Soft-deletes the entry: the interceptor turns the removal into a flag.</summary>
    void RemoveMapping(NoticeSubjectMapping mapping);

    void Add(NoticeSubject subject);

    /// <summary>Soft-deletes the notice subject: the interceptor turns the removal into a flag (BR10).</summary>
    void Remove(NoticeSubject subject);

    /// <summary>Saves, and answers with the 409 for a duplicate when the unique index refuses a row after the check passed. Null when saved.</summary>
    Task<Error?> TrySaveChangesAsync(CancellationToken cancellationToken);
}
