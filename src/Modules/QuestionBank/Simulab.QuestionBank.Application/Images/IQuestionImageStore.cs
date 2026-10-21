using Simulab.QuestionBank.Domain;
using Simulab.QuestionBank.Domain.Entities;

namespace Simulab.QuestionBank.Application.Images;

/// <summary>
/// What the image handlers need from the database (F-103). It reads and stages changes; the save of a text goes through
/// <see cref="Questions.IQuestionStore.TrySaveChangesAsync"/> so the image rows and the content commit together.
/// </summary>
public interface IQuestionImageStore
{
    /// <summary>The image, not tracked; null when it does not exist or was deleted.</summary>
    Task<QuestionImage?> FindAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// The live images with these ids, tracked, each row locked until the save (BR6): two saves that name one pending
    /// image cannot both claim it. An id with no live image is simply missing from the list.
    /// </summary>
    Task<IReadOnlyList<QuestionImage>> FindForUpdateAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);

    /// <summary>The live images this owner holds, tracked.</summary>
    Task<IReadOnlyList<QuestionImage>> ListOwnedAsync(QuestionImageOwnerKind kind, Guid ownerId, CancellationToken cancellationToken);

    /// <summary>
    /// True when a stored version still holds the image (BR9): a version of the question for a question image, a
    /// version of any question for a base text image, since a question unlinked later keeps the passage it was published with.
    /// </summary>
    Task<bool> IsHeldByAVersionAsync(QuestionImageOwnerKind kind, Guid ownerId, Guid imageId, CancellationToken cancellationToken);

    void Add(QuestionImage image);

    /// <summary>Soft-deletes the row: the interceptor turns the removal into a flag (BR10).</summary>
    void Remove(QuestionImage image);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
