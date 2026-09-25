using Microsoft.EntityFrameworkCore;
using Simulab.Catalog.Application.IssuingAuthorities;
using Simulab.Catalog.Contracts;
using Simulab.Catalog.Domain;
using Simulab.Catalog.Domain.Entities;

namespace Simulab.Catalog.Infrastructure.Persistence;

/// <summary>
/// The issuing-authority list (F-34 BR18, v2): one page, its total, searched over the normalized columns so
/// the search ignores case and accents without a PostgreSQL extension, and sorted by name or acronym with
/// the name as the default. It is also what the exam form's picker calls.
/// </summary>
public sealed class IssuingAuthorityQueries(CatalogModuleDbContext context) : IIssuingAuthorityQueries
{
    public async Task<IssuingAuthorityPageResponse> ListAsync(
        IssuingAuthorityListQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var sanitized = query.Sanitized();
        var authorities = context.IssuingAuthorities.AsNoTracking();

        var search = CatalogText.Normalize(sanitized.Search);
        if (search.Length > 0)
        {
            authorities = authorities.Where(authority =>
                authority.NormalizedName.Contains(search) || authority.NormalizedAcronym.Contains(search));
        }

        var total = await authorities.CountAsync(cancellationToken);

        var items = await Sort(authorities, sanitized)
            .Skip(sanitized.Page * sanitized.PageSize)
            .Take(sanitized.PageSize)
            .Select(authority => new IssuingAuthorityResponse(
                authority.Id,
                authority.Name,
                authority.Acronym,
                authority.Description,
                authority.Website))
            .ToListAsync(cancellationToken);

        return new IssuingAuthorityPageResponse(items, total);
    }

    // The sort runs over the normalized name so it matches what the reader sees: "Abaco" sorts with the
    // A's, not after Z as a raw byte comparison would put it (the F-33 lesson).
    private static IQueryable<IssuingAuthority> Sort(IQueryable<IssuingAuthority> authorities, IssuingAuthorityListQuery query) =>
        (query.SortBy, query.Descending) switch
        {
            (IssuingAuthoritySort.Acronym, false) => authorities.OrderBy(authority => authority.NormalizedAcronym),
            (IssuingAuthoritySort.Acronym, true) => authorities.OrderByDescending(authority => authority.NormalizedAcronym),
            (_, true) => authorities.OrderByDescending(authority => authority.NormalizedName),
            _ => authorities.OrderBy(authority => authority.NormalizedName)
        };
}
