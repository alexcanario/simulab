using Microsoft.EntityFrameworkCore;
using Simulab.Catalog.Contracts;
using Simulab.QuestionBank.Application.Questions;
using Simulab.QuestionBank.Contracts;
using Simulab.Persistence;
using Simulab.QuestionBank.Domain.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.QuestionBank.Infrastructure.Persistence;

/// <summary>
/// The question port over the module's context (F-95). The topic, the edition and the notice subject are
/// asked of the catalog through its contracts: the module never reads the catalog's tables.
/// </summary>
public sealed class QuestionStore(
    QuestionBankModuleDbContext context,
    ITopicDirectory topics,
    IExamEditionDirectory editions) : IQuestionStore
{
    public Task<Question?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        context.Questions
            .Include(question => question.Options)
            .Include(question => question.MatchItems)
            .Include(question => question.Blanks)
            .Include(question => question.Rubric)
            .AsSplitQuery()
            .FirstOrDefaultAsync(question => question.Id == id, cancellationToken);

    public async Task<LinkedBaseText?> FindBaseTextAsync(Guid id, CancellationToken cancellationToken)
    {
        // F-102: the shared row lock lives as long as the transaction, so it is opened here and ended by the save
        // that follows (or by the context, which rolls it back when the handler refuses before saving).
        await context.Database.BeginTransactionIfNoneAsync(cancellationToken);

        return await context.BaseTexts
            .FromSql($"SELECT * FROM question_bank.base_texts WHERE id = {id} AND is_deleted = false FOR SHARE")
            .AsNoTracking()
            .Select(row => new LinkedBaseText(row.Id, row.ContentLanguage, row.TextHtml))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> TopicExistsAsync(Guid topicId, CancellationToken cancellationToken) =>
        await topics.FindAsync(topicId, cancellationToken) is not null;

    public async Task<bool> ExamEditionExistsAsync(Guid examEditionId, CancellationToken cancellationToken) =>
        await editions.FindAsync(examEditionId, cancellationToken) is not null;

    public async Task<bool> NoticeSubjectBelongsToAsync(
        Guid noticeSubjectId,
        Guid examEditionId,
        CancellationToken cancellationToken) =>
        (await editions.FindNoticeSubjectsAsync([noticeSubjectId], cancellationToken))
            .Any(subject => subject.ExamEditionId == examEditionId);

    public Task<bool> IsPaperNumberTakenAsync(
        Guid examEditionId,
        int paperNumber,
        Guid? exceptQuestionId,
        CancellationToken cancellationToken)
    {
        var others = context.Questions.AsNoTracking()
            .Where(question => question.ExamEditionId == examEditionId && question.PaperNumber == paperNumber);

        // The exclusion is its own Where: a null parameter in a comparison is SQL's three-valued logic.
        if (exceptQuestionId is { } except)
        {
            others = others.Where(question => question.Id != except);
        }

        return others.AnyAsync(cancellationToken);
    }

    // Read only: the version is compared with, never changed.
    public Task<QuestionVersion?> FindLatestVersionAsync(Guid questionId, CancellationToken cancellationToken) =>
        context.QuestionVersions
            .AsNoTracking()
            .Where(version => version.QuestionId == questionId)
            .OrderByDescending(version => version.Number)
            .FirstOrDefaultAsync(cancellationToken);

    public void AddVersion(QuestionVersion version) => context.QuestionVersions.Add(version);

    public void AddReviewEntry(QuestionReviewEntry entry) => context.QuestionReviewEntries.Add(entry);

    // The original value of the status goes into the UPDATE's WHERE (it is the concurrency token), so a draft that
    // went into review since it was read makes even a save that changes no column of the question a lost race.
    public void GuardStatus(Question question) =>
        context.Entry(question).Property(row => row.Status).IsModified = true;

    public void Add(Question question) => context.Questions.Add(question);

    // The audit and soft-delete interceptor turns this into a flag, never a DELETE; the options loaded with
    // the question are removed with it.
    public void Remove(Question question) => context.Questions.Remove(question);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await context.SaveChangesAsync(cancellationToken);
        await context.Database.CommitTransactionIfAnyAsync(cancellationToken);
    }

    public async Task<Error?> TrySaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            await context.Database.CommitTransactionIfAnyAsync(cancellationToken);

            return null;
        }
        catch (DbUpdateException exception) when (QuestionUniqueViolations.Translate(exception) is not null)
        {
            // The refused row is still tracked; drop it so a later save on this context does not retry it.
            context.ChangeTracker.Clear();
            await context.Database.RollbackTransactionIfAnyAsync(cancellationToken);

            return QuestionUniqueViolations.Translate(exception);
        }
        catch (DbUpdateConcurrencyException)
        {
            // F-101: the status read is no longer the stored one (the question was annulled, unpublished or
            // published meanwhile). Nothing of this save was written; the curator reloads and tries again.
            context.ChangeTracker.Clear();
            await context.Database.RollbackTransactionIfAnyAsync(cancellationToken);

            return new Error(QuestionBankErrorCodes.QuestionVersionConflict, ErrorKind.Conflict);
        }
    }
}
