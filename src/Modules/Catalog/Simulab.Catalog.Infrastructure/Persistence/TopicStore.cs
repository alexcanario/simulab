using Microsoft.EntityFrameworkCore;
using Simulab.Catalog.Application.Topics;
using Simulab.Catalog.Domain.Entities;
using Simulab.Persistence;
using Simulab.SharedKernel.Results;

namespace Simulab.Catalog.Infrastructure.Persistence;

/// <summary>
/// The topic port over the module's context (F-79). The "is taken" query ignores the soft-delete filter on
/// purpose; the "has topics" guard keeps it, so a deleted topic no longer holds its subject back.
/// </summary>
public sealed class TopicStore(CatalogModuleDbContext context) : ITopicStore
{
    public Task<Topic?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        context.Topics.FirstOrDefaultAsync(topic => topic.Id == id, cancellationToken);

    public Task<bool> NameIsTakenAsync(
        Guid subjectId,
        string normalizedName,
        Guid? exceptId,
        CancellationToken cancellationToken) =>
        Others(exceptId).AnyAsync(
            topic => topic.SubjectId == subjectId && topic.NormalizedName == normalizedName,
            cancellationToken);

    public Task<bool> SubjectHasTopicsAsync(Guid subjectId, CancellationToken cancellationToken) =>
        context.Topics.AnyAsync(topic => topic.SubjectId == subjectId, cancellationToken);

    public void Add(Topic topic) => context.Topics.Add(topic);

    // The audit and soft-delete interceptor turns this into a flag, never a DELETE.
    public void Remove(Topic topic) => context.Topics.Remove(topic);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);

    public async Task<Error?> TrySaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);

            return null;
        }
        catch (DbUpdateException exception) when (TopicUniqueViolations.Translate(exception) is not null)
        {
            // The refused row is still tracked; drop it so a later save does not retry it.
            context.ChangeTracker.Clear();

            return TopicUniqueViolations.Translate(exception);
        }
    }

    // Every topic but the one being edited, deleted ones included (the F-33 lesson on null comparisons).
    private IQueryable<Topic> Others(Guid? exceptId)
    {
        var query = context.Topics.IgnoreQueryFilters([ModuleDbContext.SoftDeleteFilter]);

        return exceptId is { } id ? query.Where(topic => topic.Id != id) : query;
    }
}
