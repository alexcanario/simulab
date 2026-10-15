using Simulab.QuestionBank.Domain.Entities;

namespace Simulab.QuestionBank.Application.BaseTexts;

/// <summary>
/// What the base text handlers need from the database (F-102). It reads and stages changes; a save goes through
/// <see cref="Questions.IQuestionStore.TrySaveChangesAsync"/> so the lost-race answers stay in one place.
/// </summary>
public interface IBaseTextStore
{
    /// <summary>
    /// The base text, tracked, with a row lock held until the save (BR4, BR6): a question save that links it, or a
    /// language change racing a link, waits for this one. Null when it does not exist or was deleted.
    /// </summary>
    Task<BaseText?> FindForUpdateAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>True when a live question of any status links the base text (BR3, BR6).</summary>
    Task<bool> HasLinkedQuestionsAsync(Guid baseTextId, CancellationToken cancellationToken);

    /// <summary>The live questions that link the base text and are <c>Published</c>, tracked, with their options (BR4).</summary>
    Task<IReadOnlyList<Question>> ListPublishedLinkedAsync(Guid baseTextId, CancellationToken cancellationToken);

    /// <summary>The latest version of each question, by question id; a question never published has none (BR4).</summary>
    Task<IReadOnlyDictionary<Guid, QuestionVersion>> FindLatestVersionsAsync(
        IReadOnlyCollection<Guid> questionIds,
        CancellationToken cancellationToken);

    /// <summary>
    /// Makes the next save check the status the question was read with, so an unpublish or an annulment that
    /// committed first is a lost race and not a version on a question that is no longer published (BR4).
    /// </summary>
    void GuardStatus(Question question);

    void Add(BaseText baseText);

    /// <summary>Soft-deletes the base text: the interceptor turns the removal into a flag (BR6).</summary>
    void Remove(BaseText baseText);
}
