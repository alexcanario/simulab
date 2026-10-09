using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Simulab.Catalog.Contracts;
using Simulab.Identity.Contracts;
using static Simulab.ApiResults.ApiProblem;

namespace Simulab.Catalog.Api;

/// <summary>
/// The student catalog (F-36): the published exams, one exam with its published editions, and the filter
/// options. Every route requires <c>catalog.browse</c> (BR10); a caller without it gets 403
/// <c>identity.forbidden</c> from the shared permission pipeline (F-6). <c>catalog.manage</c> alone does not
/// open it.
/// </summary>
public static class PublishedExamEndpoints
{
    internal static RouteGroupBuilder MapPublishedExamEndpoints(this RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        var published = group
            .MapGroup(string.Empty)
            .RequireAuthorization(PermissionPolicy.NameFor(CatalogPermissions.Browse));

        published.MapGet("/published-exams", ListAsync).WithName("ListPublishedExams");
        published.MapGet("/published-exams/{id:guid}", FindAsync).WithName("FindPublishedExam");
        published.MapGet("/published-exam-filters", FiltersAsync).WithName("GetPublishedExamFilters");

        return group;
    }

    // Every filter arrives as text and an unreadable one is no filter (BR6): a stale bookmark shows results.
    // The board and the year are parsed here too, because binding them as Guid? and int? would answer 400.
    private static async Task<IResult> ListAsync(
        IPublishedExamQueries queries,
        CancellationToken cancellationToken,
        int page = 0,
        int pageSize = PublishedExamListQuery.DefaultPageSize,
        string? search = null,
        string? assessmentType = null,
        string? scope = null,
        string? organizerId = null,
        string? noticeYear = null,
        string? state = null)
    {
        // F-57 BR6: unlike the other filters, an unknown state is refused. Ignoring it would silently list every exam.
        if (!StateFilter.TryRead(state, out var acronym))
        {
            return Problem(StateFilter.UnknownState);
        }

        return Results.Ok(await queries.ListAsync(
            new PublishedExamListQuery(
                page,
                pageSize,
                search,
                ParseEnum<AssessmentType>(assessmentType),
                ParseEnum<ExamScope>(scope),
                Guid.TryParse(organizerId, out var organizer) ? organizer : null,
                int.TryParse(noticeYear, out var year) ? year : null,
                acronym),
            cancellationToken));
    }

    private static async Task<IResult> FindAsync(Guid id, IPublishedExamQueries queries, CancellationToken cancellationToken)
    {
        var exam = await queries.FindAsync(id, cancellationToken);

        return exam is null
            ? Problem(new SharedKernel.Results.Error(CatalogErrorCodes.ExamNotFound, SharedKernel.Results.ErrorKind.NotFound))
            : Results.Ok(exam);
    }

    private static async Task<IResult> FiltersAsync(IPublishedExamQueries queries, CancellationToken cancellationToken) =>
        Results.Ok(await queries.FiltersAsync(cancellationToken));

    private static TEnum? ParseEnum<TEnum>(string? value)
        where TEnum : struct, Enum =>
        Enum.GetNames<TEnum>().FirstOrDefault(name => string.Equals(name, value, StringComparison.OrdinalIgnoreCase)) is { } match
            ? Enum.Parse<TEnum>(match)
            : null; // by name only, as the Web reads the same keys: "scope=1" is no filter on either side
}
