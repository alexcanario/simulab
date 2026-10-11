using Simulab.QuestionBank.Contracts;

namespace Simulab.QuestionBank.Application.Questions;

/// <summary>The read side of the question list and the question form (F-95).</summary>
public interface IQuestionQueries
{
    /// <summary>One page, newest first, deleted questions excluded, filtered by status and statement text (BR15).</summary>
    Task<QuestionPageResponse> ListAsync(QuestionListQuery query, CancellationToken cancellationToken);

    /// <summary>The question with its options and key, or null when it does not exist or was deleted.</summary>
    Task<QuestionResponse?> FindAsync(Guid id, CancellationToken cancellationToken);
}
