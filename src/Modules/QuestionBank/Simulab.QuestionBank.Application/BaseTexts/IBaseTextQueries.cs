using Simulab.QuestionBank.Contracts;

namespace Simulab.QuestionBank.Application.BaseTexts;

/// <summary>The read side of the base text list, the picker and the base text form (F-102).</summary>
public interface IBaseTextQueries
{
    /// <summary>One page sorted by title, deleted texts excluded, filtered by language and title text (BR11).</summary>
    Task<BaseTextPageResponse> ListAsync(BaseTextListQuery query, CancellationToken cancellationToken);

    /// <summary>The base text with the number of live questions that link it, or null when it does not exist or was deleted.</summary>
    Task<BaseTextResponse?> FindAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// One page of the live questions that link the base text, most recently changed first (BR11), or null when
    /// the base text does not exist or was deleted. <paramref name="page"/> is zero-based and the size is capped.
    /// </summary>
    Task<QuestionPageResponse?> ListQuestionsAsync(
        Guid baseTextId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
