using Microsoft.EntityFrameworkCore;
using Simulab.QuestionBank.Application.Images;
using Simulab.QuestionBank.Domain;
using Simulab.QuestionBank.Domain.Entities;

namespace Simulab.QuestionBank.Infrastructure.Persistence;

/// <summary>
/// The image port over the module's context (F-103). The row lock is taken with raw SQL because EF Core has no locking
/// hint; the transaction it needs is opened here and committed by the question store's save, as for a base text.
/// </summary>
public sealed class QuestionImageStore(QuestionBankModuleDbContext context) : IQuestionImageStore
{
    public Task<QuestionImage?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        context.QuestionImages.AsNoTracking().FirstOrDefaultAsync(image => image.Id == id, cancellationToken);

    public async Task<IReadOnlyList<QuestionImage>> FindForUpdateAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken)
    {
        var wanted = ids.ToArray();
        await context.Database.BeginTransactionIfNoneAsync(cancellationToken);

        return await context.QuestionImages
            .FromSql($"SELECT * FROM question_bank.question_images WHERE id = ANY({wanted}) AND is_deleted = false ORDER BY id FOR UPDATE")
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<QuestionImage>> ListOwnedAsync(
        QuestionImageOwnerKind kind,
        Guid ownerId,
        CancellationToken cancellationToken) =>
        await context.QuestionImages
            .Where(image => image.OwnerKind == kind && image.OwnerId == ownerId)
            .ToListAsync(cancellationToken);

    // A uuid is hex digits and hyphens, so it has no LIKE wildcard in it. The options are a JSON column read as text.
    public async Task<bool> IsHeldByAVersionAsync(
        QuestionImageOwnerKind kind,
        Guid ownerId,
        Guid imageId,
        CancellationToken cancellationToken)
    {
        var address = $"%/question-images/{imageId}%";

        return kind == QuestionImageOwnerKind.Question
            ? await context.Database
                .SqlQuery<bool>($"""
                    SELECT EXISTS (
                        SELECT 1 FROM question_bank.question_versions
                        WHERE question_id = {ownerId} AND (statement_html LIKE {address} OR options::text LIKE {address})) AS "Value"
                    """)
                .SingleAsync(cancellationToken)
            : await context.Database
                .SqlQuery<bool>($"""
                    SELECT EXISTS (
                        SELECT 1 FROM question_bank.question_versions WHERE base_text_html LIKE {address}) AS "Value"
                    """)
                .SingleAsync(cancellationToken);
    }

    public void Add(QuestionImage image) => context.QuestionImages.Add(image);

    public void Remove(QuestionImage image) => context.QuestionImages.Remove(image);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);
}
