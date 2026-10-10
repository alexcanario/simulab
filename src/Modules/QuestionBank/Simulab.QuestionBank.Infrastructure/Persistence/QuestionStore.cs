using Microsoft.EntityFrameworkCore;
using Simulab.Catalog.Contracts;
using Simulab.QuestionBank.Application.Questions;
using Simulab.QuestionBank.Domain.Entities;

namespace Simulab.QuestionBank.Infrastructure.Persistence;

/// <summary>
/// The question port over the module's context (F-95). The topic is asked of the catalog through its
/// contracts: the module never reads the catalog's tables.
/// </summary>
public sealed class QuestionStore(QuestionBankModuleDbContext context, ITopicDirectory topics) : IQuestionStore
{
    public Task<Question?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        context.Questions
            .Include(question => question.Options)
            .FirstOrDefaultAsync(question => question.Id == id, cancellationToken);

    public async Task<bool> TopicExistsAsync(Guid topicId, CancellationToken cancellationToken) =>
        await topics.FindAsync(topicId, cancellationToken) is not null;

    public void Add(Question question) => context.Questions.Add(question);

    // The audit and soft-delete interceptor turns this into a flag, never a DELETE; the options loaded with
    // the question are removed with it.
    public void Remove(Question question) => context.Questions.Remove(question);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);
}
