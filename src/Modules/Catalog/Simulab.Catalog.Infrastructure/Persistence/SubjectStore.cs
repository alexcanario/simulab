using Microsoft.EntityFrameworkCore;
using Simulab.Catalog.Application.Subjects;
using Simulab.Catalog.Domain.Entities;
using Simulab.Persistence;
using Simulab.SharedKernel.Results;

namespace Simulab.Catalog.Infrastructure.Persistence;

/// <summary>
/// The subject port over the module's context (F-79). The "is taken" query ignores the soft-delete filter on
/// purpose: a deleted subject keeps its name, exactly as the unique index sees it.
/// </summary>
public sealed class SubjectStore(CatalogModuleDbContext context) : ISubjectStore
{
    public Task<Subject?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        context.Subjects.FirstOrDefaultAsync(subject => subject.Id == id, cancellationToken);

    public Task<bool> AreaExistsAsync(Guid areaId, CancellationToken cancellationToken) =>
        context.Areas.AnyAsync(area => area.Id == areaId, cancellationToken);

    public Task<bool> NameIsTakenAsync(string normalizedName, Guid? exceptId, CancellationToken cancellationToken) =>
        Others(exceptId).AnyAsync(subject => subject.NormalizedName == normalizedName, cancellationToken);

    public void Add(Subject subject) => context.Subjects.Add(subject);

    // The audit and soft-delete interceptor turns this into a flag, never a DELETE.
    public void Remove(Subject subject) => context.Subjects.Remove(subject);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);

    public async Task<Error?> TrySaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);

            return null;
        }
        catch (DbUpdateException exception) when (SubjectUniqueViolations.Translate(exception) is not null)
        {
            // The refused row is still tracked as added; drop it so a later save does not retry it.
            context.ChangeTracker.Clear();

            return SubjectUniqueViolations.Translate(exception);
        }
    }

    // Every subject but the one being edited, deleted ones included. The exclusion is its own Where instead of
    // `Id != exceptId`: a null parameter in a comparison is SQL's three-valued logic (the F-33 lesson).
    private IQueryable<Subject> Others(Guid? exceptId)
    {
        var query = context.Subjects.IgnoreQueryFilters([ModuleDbContext.SoftDeleteFilter]);

        return exceptId is { } id ? query.Where(subject => subject.Id != id) : query;
    }
}
