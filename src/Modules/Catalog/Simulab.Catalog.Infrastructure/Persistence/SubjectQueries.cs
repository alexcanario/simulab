using Microsoft.EntityFrameworkCore;
using Simulab.Catalog.Application.Subjects;
using Simulab.Catalog.Contracts;
using Simulab.Catalog.Domain;
using Simulab.Catalog.Domain.Entities;

namespace Simulab.Catalog.Infrastructure.Persistence;

/// <summary>
/// The subject reads (F-79): the seeded areas, and the subject list searched over the normalized name so it
/// ignores case and accents without a PostgreSQL extension. Each row carries its area code and the count of
/// topics that were not deleted, which is what the screen reads to block a delete.
/// </summary>
public sealed class SubjectQueries(CatalogModuleDbContext context) : ISubjectQueries
{
    public async Task<IReadOnlyList<AreaResponse>> ListAreasAsync(CancellationToken cancellationToken) =>
        await context.Areas
            .AsNoTracking()
            .OrderBy(area => area.DisplayOrder)
            .Select(area => new AreaResponse(area.Id, area.Code, area.DisplayOrder))
            .ToListAsync(cancellationToken);

    public async Task<SubjectPageResponse> ListAsync(SubjectListQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var sanitized = query.Sanitized();
        var subjects = context.Subjects.AsNoTracking();

        var search = CatalogText.Normalize(sanitized.Search);
        if (search.Length > 0)
        {
            subjects = subjects.Where(subject => subject.NormalizedName.Contains(search));
        }

        // "No area" wins over a given area: a list is a read, so two filters that contradict are not an error.
        if (sanitized.WithoutArea)
        {
            subjects = subjects.Where(subject => subject.AreaId == null);
        }
        else if (sanitized.AreaId is { } areaId)
        {
            subjects = subjects.Where(subject => subject.AreaId == areaId);
        }

        var total = await subjects.CountAsync(cancellationToken);

        var items = await Project(subjects.OrderBy(subject => subject.NormalizedName))
            .Skip(sanitized.Page * sanitized.PageSize)
            .Take(sanitized.PageSize)
            .ToListAsync(cancellationToken);

        return new SubjectPageResponse(items, total);
    }

    public Task<SubjectResponse?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        Project(context.Subjects.AsNoTracking().Where(subject => subject.Id == id))
            .FirstOrDefaultAsync(cancellationToken);

    private IQueryable<SubjectResponse> Project(IQueryable<Subject> subjects) =>
        subjects.Select(subject => new SubjectResponse(
            subject.Id,
            subject.Name,
            subject.AreaId,
            context.Areas.Where(area => area.Id == subject.AreaId).Select(area => area.Code).FirstOrDefault(),
            context.Topics.Count(topic => topic.SubjectId == subject.Id)));
}
