using Microsoft.EntityFrameworkCore;
using Simulab.QuestionBank.Application.BaseTexts;
using Simulab.QuestionBank.Contracts;
using Simulab.QuestionBank.Domain.Entities;

namespace Simulab.QuestionBank.Infrastructure.Persistence;

/// <summary>
/// The base text port over the module's context (F-102). The row lock is taken with raw SQL because EF Core has
/// no locking hint; the transaction it needs is opened here and committed by the question store's save.
/// </summary>
public sealed class BaseTextStore(QuestionBankModuleDbContext context) : IBaseTextStore
{
    public async Task<BaseText?> FindForUpdateAsync(Guid id, CancellationToken cancellationToken)
    {
        await context.Database.BeginTransactionIfNoneAsync(cancellationToken);

        return await context.BaseTexts
            .FromSql($"SELECT * FROM question_bank.base_texts WHERE id = {id} AND is_deleted = false FOR UPDATE")
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<bool> HasLinkedQuestionsAsync(Guid baseTextId, CancellationToken cancellationToken) =>
        context.Questions.AsNoTracking().AnyAsync(question => question.BaseTextId == baseTextId, cancellationToken);

    public async Task<IReadOnlyList<Question>> ListPublishedLinkedAsync(
        Guid baseTextId,
        CancellationToken cancellationToken) =>
        await context.Questions
            .Include(question => question.Options)
            .Where(question => question.BaseTextId == baseTextId && question.Status == QuestionStatus.Published)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, QuestionVersion>> FindLatestVersionsAsync(
        IReadOnlyCollection<Guid> questionIds,
        CancellationToken cancellationToken)
    {
        var ids = questionIds.ToList();
        var versions = await context.QuestionVersions
            .AsNoTracking()
            .Where(version => ids.Contains(version.QuestionId)
                && version.Number == context.QuestionVersions
                    .Where(other => other.QuestionId == version.QuestionId)
                    .Max(other => other.Number))
            .ToListAsync(cancellationToken);

        return versions.ToDictionary(version => version.QuestionId);
    }

    // The original value of the status goes into the UPDATE's WHERE (it is the concurrency token), so a question
    // unpublished or annulled since it was read makes the save a lost race.
    public void GuardStatus(Question question) =>
        context.Entry(question).Property(row => row.Status).IsModified = true;

    public void Add(BaseText baseText) => context.BaseTexts.Add(baseText);

    public void Remove(BaseText baseText) => context.BaseTexts.Remove(baseText);
}
