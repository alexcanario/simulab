using Microsoft.EntityFrameworkCore;
using Simulab.Catalog.Application.ExamEditions;
using Simulab.Catalog.Domain.Entities;
using Simulab.Persistence;
using Simulab.SharedKernel.Results;

namespace Simulab.Catalog.Infrastructure.Persistence;

/// <summary>
/// The edition port over the module's context (F-35). The duplicate query ignores the soft-delete filter on
/// purpose: a deleted edition keeps its year, position and board taken inside the exam, exactly as the unique
/// index sees them (BR10). Everything else reads the catalog as a reader sees it, deleted rows excluded.
/// </summary>
public sealed class ExamEditionStore(CatalogModuleDbContext context) : IExamEditionStore
{
    public Task<ExamEdition?> FindAsync(Guid examId, Guid id, CancellationToken cancellationToken) =>
        context.ExamEditions.FirstOrDefaultAsync(
            edition => edition.Id == id && edition.ExamId == examId,
            cancellationToken);

    public Task<bool> ExamExistsAsync(Guid examId, CancellationToken cancellationToken) =>
        context.Exams.AnyAsync(exam => exam.Id == examId, cancellationToken);

    public Task<bool> OrganizerExistsAsync(Guid organizerId, CancellationToken cancellationToken) =>
        context.Organizers.AnyAsync(organizer => organizer.Id == organizerId, cancellationToken);

    public Task<bool> IsDuplicateAsync(
        Guid examId,
        int noticeYear,
        string normalizedPosition,
        Guid organizerId,
        Guid? exceptId,
        CancellationToken cancellationToken) =>
        Others(exceptId).AnyAsync(
            edition => edition.ExamId == examId
                && edition.NoticeYear == noticeYear
                && edition.NormalizedPosition == normalizedPosition
                && edition.OrganizerId == organizerId,
            cancellationToken);

    /// <summary>BR12: only editions that are still in the catalog hold their exam back.</summary>
    public Task<bool> ExamHasEditionsAsync(Guid examId, CancellationToken cancellationToken) =>
        context.ExamEditions.AnyAsync(edition => edition.ExamId == examId, cancellationToken);

    /// <summary>BR12: only editions that are still in the catalog hold their board back.</summary>
    public Task<bool> OrganizerHasEditionsAsync(Guid organizerId, CancellationToken cancellationToken) =>
        context.ExamEditions.AnyAsync(edition => edition.OrganizerId == organizerId, cancellationToken);

    public void Add(ExamEdition edition) => context.ExamEditions.Add(edition);

    // The audit and soft-delete interceptor turns this into a flag, never a DELETE (BR1).
    public void Remove(ExamEdition edition) => context.ExamEditions.Remove(edition);

    public async Task<Error?> TrySaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);

            return null;
        }
        catch (DbUpdateException exception) when (ExamEditionUniqueViolations.Translate(exception) is not null)
        {
            // The refused row is still tracked as added; drop it so a later save on this context does not retry it.
            context.ChangeTracker.Clear();

            return ExamEditionUniqueViolations.Translate(exception);
        }
    }

    // Every edition but the one being edited, deleted ones included. The exclusion is added as its own Where
    // instead of `Id != exceptId`: a null parameter in a comparison is SQL's three-valued logic, and the row
    // the caller is editing would be the one it wrongly keeps or drops (the F-33 lesson).
    private IQueryable<ExamEdition> Others(Guid? exceptId)
    {
        var query = context.ExamEditions.IgnoreQueryFilters([ModuleDbContext.SoftDeleteFilter]);

        return exceptId is { } id ? query.Where(edition => edition.Id != id) : query;
    }
}
