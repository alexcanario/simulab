using Microsoft.EntityFrameworkCore;
using Simulab.Catalog.Application.Exams;
using Simulab.Catalog.Domain.Entities;
using Simulab.Persistence;
using Simulab.SharedKernel.Results;

namespace Simulab.Catalog.Infrastructure.Persistence;

/// <summary>
/// The exam port over the module's context (F-34). The "is taken" query ignores the soft-delete filter on
/// purpose: a deleted exam keeps its name inside its issuing authority, exactly as the unique index sees it
/// (BR10). Everything else reads the catalog as a reader sees it, deleted rows excluded.
/// </summary>
public sealed class ExamStore(CatalogModuleDbContext context) : IExamStore
{
    public Task<Exam?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        context.Exams.FirstOrDefaultAsync(exam => exam.Id == id, cancellationToken);

    public Task<bool> IssuingAuthorityExistsAsync(Guid issuingAuthorityId, CancellationToken cancellationToken) =>
        context.Organizers.AnyAsync(organizer => organizer.Id == issuingAuthorityId, cancellationToken);

    public Task<bool> NameIsTakenAsync(
        Guid issuingAuthorityId,
        string normalizedName,
        Guid? exceptId,
        CancellationToken cancellationToken) =>
        Others(exceptId).AnyAsync(
            exam => exam.IssuingAuthorityId == issuingAuthorityId && exam.NormalizedName == normalizedName,
            cancellationToken);

    /// <summary>BR12: only exams that are still in the catalog hold their organizer back.</summary>
    public Task<bool> OrganizerHasExamsAsync(Guid issuingAuthorityId, CancellationToken cancellationToken) =>
        context.Exams.AnyAsync(exam => exam.IssuingAuthorityId == issuingAuthorityId, cancellationToken);

    public void Add(Exam exam) => context.Exams.Add(exam);

    // The audit and soft-delete interceptor turns this into a flag, never a DELETE (BR1).
    public void Remove(Exam exam) => context.Exams.Remove(exam);

    public async Task<Error?> TrySaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);

            return null;
        }
        catch (DbUpdateException exception) when (ExamUniqueViolations.Translate(exception) is not null)
        {
            // The refused row is still tracked as added; drop it so a later save on this context does not retry it.
            context.ChangeTracker.Clear();

            return ExamUniqueViolations.Translate(exception);
        }
    }

    // Every exam but the one being edited, deleted ones included. The exclusion is added as its own Where
    // instead of `Id != exceptId`: a null parameter in a comparison is SQL's three-valued logic, and the row
    // the caller is editing would be the one it wrongly keeps or drops (the F-33 lesson).
    private IQueryable<Exam> Others(Guid? exceptId)
    {
        var query = context.Exams.IgnoreQueryFilters([ModuleDbContext.SoftDeleteFilter]);

        return exceptId is { } id ? query.Where(exam => exam.Id != id) : query;
    }
}
