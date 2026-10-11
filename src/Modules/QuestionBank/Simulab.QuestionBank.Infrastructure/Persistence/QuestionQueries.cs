using Microsoft.EntityFrameworkCore;
using Simulab.Catalog.Contracts;
using Simulab.QuestionBank.Application.Questions;
using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Text;

namespace Simulab.QuestionBank.Infrastructure.Persistence;

/// <summary>
/// The question reads (F-95): the list, filtered by status and by the normalized statement so it ignores case
/// and accents, newest first; and one question with its options. Topic and subject names come from the catalog
/// through <see cref="ITopicDirectory"/>, one call per page, never a join across schemas.
/// </summary>
public sealed class QuestionQueries(QuestionBankModuleDbContext context, ITopicDirectory topics) : IQuestionQueries
{
    public async Task<QuestionPageResponse> ListAsync(QuestionListQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var sanitized = query.Sanitized();
        var questions = context.Questions.AsNoTracking();

        if (sanitized.Status is { } status)
        {
            questions = questions.Where(question => question.Status == status);
        }

        var search = ComparableText.Normalize(sanitized.Search);
        if (search.Length > 0)
        {
            questions = questions.Where(question => question.NormalizedStatement.Contains(search));
        }

        var total = await questions.CountAsync(cancellationToken);

        var rows = await questions
            .OrderByDescending(question => question.CreatedAt)
            .ThenByDescending(question => question.Id)
            .Skip(sanitized.Page * sanitized.PageSize)
            .Take(sanitized.PageSize)
            .Select(question => new
            {
                question.Id,
                question.StatementText,
                question.TopicId,
                question.Difficulty,
                question.ContentLanguage,
                question.Status
            })
            .ToListAsync(cancellationToken);

        var names = (await topics.FindManyAsync([.. rows.Select(row => row.TopicId).Distinct()], cancellationToken))
            .ToDictionary(entry => entry.Id);

        var items = rows
            .Select(row =>
            {
                names.TryGetValue(row.TopicId, out var topic);

                return new QuestionListItemResponse(
                    row.Id,
                    Excerpt(row.StatementText),
                    row.TopicId,
                    topic?.Name ?? string.Empty,
                    topic?.SubjectId ?? Guid.Empty,
                    topic?.SubjectName ?? string.Empty,
                    row.Difficulty,
                    row.ContentLanguage,
                    row.Status);
            })
            .ToList();

        return new QuestionPageResponse(items, total);
    }

    public async Task<QuestionResponse?> FindAsync(Guid id, CancellationToken cancellationToken)
    {
        var question = await context.Questions
            .AsNoTracking()
            .Include(row => row.Options)
            .FirstOrDefaultAsync(row => row.Id == id, cancellationToken);
        if (question is null)
        {
            return null;
        }

        var topic = await topics.FindAsync(question.TopicId, cancellationToken);

        return new QuestionResponse(
            question.Id,
            question.Type,
            question.Status,
            question.TopicId,
            topic?.Name ?? string.Empty,
            topic?.SubjectId ?? Guid.Empty,
            topic?.SubjectName ?? string.Empty,
            question.ContentLanguage,
            question.StatementHtml,
            question.BaseTextHtml,
            question.ExplanationHtml,
            question.Difficulty,
            [
                .. question.Options
                    .OrderBy(option => option.Position)
                    .Select(option => new QuestionOptionResponse(option.Position, option.TextHtml, option.IsCorrect))
            ]);
    }

    private static string Excerpt(string text) =>
        text.Length <= QuestionLimits.ExcerptLength ? text : text[..QuestionLimits.ExcerptLength];
}
