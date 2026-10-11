using Simulab.QuestionBank.Domain.Entities;

namespace Simulab.QuestionBank.Application.Questions;

/// <summary>
/// What the question handlers need from the database (F-95). The port exists because a rule needs data — the
/// topic exists — and this project never references EF Core.
/// </summary>
public interface IQuestionStore
{
    /// <summary>The question with its options, tracked; null when it does not exist or was deleted.</summary>
    Task<Question?> FindAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>True when a live topic with this id exists in the catalog (BR16).</summary>
    Task<bool> TopicExistsAsync(Guid topicId, CancellationToken cancellationToken);

    void Add(Question question);

    /// <summary>Soft-deletes the question and its options: the interceptor turns the removal into a flag (BR11).</summary>
    void Remove(Question question);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
