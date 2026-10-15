using Microsoft.EntityFrameworkCore;
using Simulab.Identity.Contracts;
using Simulab.QuestionBank.Application.BaseTexts;
using Simulab.QuestionBank.Application.Questions;
using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Text;

namespace Simulab.QuestionBank.Infrastructure.Persistence;

/// <summary>
/// The base text reads (F-102): the list sorted by title and searched without case or accents, one text with
/// the number of live questions that link it, and the questions it links (through the question list, so the
/// topic names and the source line come from one place).
/// </summary>
public sealed class BaseTextQueries(QuestionBankModuleDbContext context, IQuestionQueries questionQueries) : IBaseTextQueries
{
    public async Task<BaseTextPageResponse> ListAsync(BaseTextListQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var sanitized = query.Sanitized();
        var baseTexts = context.BaseTexts.AsNoTracking();

        // A list is a read: a language that is not understood is ignored, not "matches nothing" (F-95 BR15).
        if (SupportedLanguages.Canonical(sanitized.Language) is { } language)
        {
            baseTexts = baseTexts.Where(baseText => baseText.ContentLanguage == language);
        }

        var search = ComparableText.Normalize(sanitized.Search);
        if (search.Length > 0)
        {
            baseTexts = baseTexts.Where(baseText => baseText.NormalizedTitle.Contains(search));
        }

        var total = await baseTexts.CountAsync(cancellationToken);

        var items = await baseTexts
            .OrderBy(baseText => baseText.NormalizedTitle)
            .ThenBy(baseText => baseText.Id)
            .Skip(sanitized.Page * sanitized.PageSize)
            .Take(sanitized.PageSize)
            .Select(baseText => new BaseTextListItemResponse(
                baseText.Id,
                baseText.Title,
                baseText.ContentLanguage,
                context.Questions.Count(question => question.BaseTextId == baseText.Id),
                baseText.UpdatedAt ?? baseText.CreatedAt))
            .ToListAsync(cancellationToken);

        return new BaseTextPageResponse(items, total);
    }

    public Task<BaseTextResponse?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        context.BaseTexts
            .AsNoTracking()
            .Where(baseText => baseText.Id == id)
            .Select(baseText => new BaseTextResponse(
                baseText.Id,
                baseText.Title,
                baseText.ContentLanguage,
                baseText.TextHtml,
                context.Questions.Count(question => question.BaseTextId == baseText.Id),
                baseText.UpdatedAt ?? baseText.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<QuestionPageResponse?> ListQuestionsAsync(
        Guid baseTextId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (!await context.BaseTexts.AsNoTracking().AnyAsync(baseText => baseText.Id == baseTextId, cancellationToken))
        {
            return null;
        }

        return await questionQueries.ListAsync(
            new QuestionListQuery(Page: page, PageSize: pageSize, BaseTextId: baseTextId),
            cancellationToken);
    }
}
