using Microsoft.EntityFrameworkCore;
using Simulab.Catalog.Application.Organizers;
using Simulab.Catalog.Contracts;
using Simulab.Catalog.Domain;
using Simulab.Catalog.Domain.Entities;

namespace Simulab.Catalog.Infrastructure.Persistence;

/// <summary>
/// The organizer list (F-33, UC1 and BR12): one page, its total, searched over the normalized columns
/// so the search ignores case and accents without a PostgreSQL extension, and sorted by one of three
/// columns with the name as the default.
/// </summary>
public sealed class OrganizerQueries(CatalogModuleDbContext context) : IOrganizerQueries
{
    public async Task<OrganizerPageResponse> ListAsync(OrganizerListQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var sanitized = query.Sanitized();
        var organizers = context.Organizers.AsNoTracking();

        var search = CatalogText.Normalize(sanitized.Search);
        if (search.Length > 0)
        {
            organizers = organizers.Where(organizer =>
                organizer.NormalizedName.Contains(search) || organizer.NormalizedAcronym.Contains(search));
        }

        var total = await organizers.CountAsync(cancellationToken);

        var items = await Sort(organizers, sanitized)
            .Skip(sanitized.Page * sanitized.PageSize)
            .Take(sanitized.PageSize)
            .Select(organizer => new OrganizerResponse(
                organizer.Id,
                organizer.Name,
                organizer.Acronym,
                organizer.Kind,
                organizer.Description,
                organizer.Website))
            .ToListAsync(cancellationToken);

        return new OrganizerPageResponse(items, total);
    }

    // The sort runs over the normalized name so it matches what the reader sees: "Ábaco" sorts with the
    // A's, not after Z as a raw byte comparison would put it.
    private static IQueryable<Organizer> Sort(IQueryable<Organizer> organizers, OrganizerListQuery query) =>
        (query.SortBy, query.Descending) switch
        {
            (OrganizerSort.Acronym, false) => organizers.OrderBy(organizer => organizer.NormalizedAcronym),
            (OrganizerSort.Acronym, true) => organizers.OrderByDescending(organizer => organizer.NormalizedAcronym),
            (OrganizerSort.Kind, false) => organizers.OrderBy(organizer => organizer.Kind).ThenBy(organizer => organizer.NormalizedName),
            (OrganizerSort.Kind, true) => organizers.OrderByDescending(organizer => organizer.Kind).ThenBy(organizer => organizer.NormalizedName),
            (_, true) => organizers.OrderByDescending(organizer => organizer.NormalizedName),
            _ => organizers.OrderBy(organizer => organizer.NormalizedName)
        };
}
