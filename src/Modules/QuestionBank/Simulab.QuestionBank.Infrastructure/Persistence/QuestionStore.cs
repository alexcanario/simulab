using Microsoft.EntityFrameworkCore;
using Simulab.Catalog.Contracts;
using Simulab.QuestionBank.Application.Questions;
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
            .FirstOrDefaultAsync(question => question.Id == id, cancellationToken);

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

    public void Add(Question question) => context.Questions.Add(question);

    // The audit and soft-delete interceptor turns this into a flag, never a DELETE; the options loaded with
    // the question are removed with it.
    public void Remove(Question question) => context.Questions.Remove(question);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);

    public async Task<Error?> TrySaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);

            return null;
        }
        catch (DbUpdateException exception) when (QuestionUniqueViolations.Translate(exception) is not null)
        {
            // The refused row is still tracked; drop it so a later save on this context does not retry it.
            context.ChangeTracker.Clear();

            return QuestionUniqueViolations.Translate(exception);
        }
    }
}
