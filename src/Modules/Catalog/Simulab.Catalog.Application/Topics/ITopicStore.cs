using Simulab.Catalog.Domain.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.Catalog.Application.Topics;

/// <summary>What the topic handlers, and the subject delete guard, need from the database (F-79).</summary>
public interface ITopicStore
{
    /// <summary>The topic with this id, or null when it does not exist or was deleted.</summary>
    Task<Topic?> FindAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// True when another topic of this subject already holds this normalized name. Deleted rows count,
    /// as the unique index sees them (BR7).
    /// </summary>
    Task<bool> NameIsTakenAsync(Guid subjectId, string normalizedName, Guid? exceptId, CancellationToken cancellationToken);

    /// <summary>True when the subject has at least one topic that was not deleted (BR8).</summary>
    Task<bool> SubjectHasTopicsAsync(Guid subjectId, CancellationToken cancellationToken);

    /// <summary>True when a live notice subject maps this topic (F-75, BR9).</summary>
    Task<bool> IsMappedAsync(Guid topicId, CancellationToken cancellationToken);

    void Add(Topic topic);

    /// <summary>Soft-deletes the topic: the interceptor turns the removal into a flag.</summary>
    void Remove(Topic topic);

    Task SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>Saves, and answers with the 409 for a duplicate name when the unique index refuses the row.</summary>
    Task<Error?> TrySaveChangesAsync(CancellationToken cancellationToken);
}
