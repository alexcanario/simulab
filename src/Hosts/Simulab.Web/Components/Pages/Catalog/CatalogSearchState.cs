using Simulab.Catalog.Contracts;

namespace Simulab.Web.Components.Pages.Catalog;

/// <summary>
/// What the address of <c>/catalog</c> holds (F-36, BR13): the search text, the four filters, the page and the page size.
/// <paramref name="Page"/> is 0-based here; the address counts from 1.
/// </summary>
public sealed record CatalogSearchState(
    string? Search = null,
    AssessmentType? AssessmentType = null,
    ExamScope? Scope = null,
    Guid? OrganizerId = null,
    int? NoticeYear = null,
    int Page = 0,
    int PageSize = CatalogSearchState.DefaultPageSize)
{
    /// <summary>The page size the kit's table starts with; a key at its default is left out of the address.</summary>
    public const int DefaultPageSize = 25;
}
