using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Simulab.Identity.Contracts;
using Simulab.QuestionBank.Application.Questions;
using Simulab.QuestionBank.Contracts;

namespace Simulab.QuestionBank.Api;

/// <summary>
/// What the filters of the question list offer (F-100): the whole taxonomy and the organizers. They live in the
/// question bank and require <c>questions.manage</c> only, so a curator needs no <c>catalog.manage</c> to pick
/// a subject, a topic or an organizer (the precedent is <see cref="SubjectLookupEndpoints"/>, B-28). The names
/// differ from the catalog's routes: operation ids are unique across the Api.
/// </summary>
public static class QuestionFilterEndpoints
{
    internal static RouteGroupBuilder MapQuestionFilterEndpoints(this RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        var filters = group.MapGroup(string.Empty)
            .RequireAuthorization(PermissionPolicy.NameFor(QuestionBankPermissions.Manage));

        filters.MapGet("/taxonomy", TaxonomyAsync).WithName("GetQuestionTaxonomy");
        filters.MapGet("/organizers", OrganizersAsync).WithName("ListQuestionOrganizers");

        return group;
    }

    private static async Task<IResult> TaxonomyAsync(IQuestionQueries queries, CancellationToken cancellationToken) =>
        Results.Ok(await queries.GetTaxonomyAsync(cancellationToken));

    private static async Task<IResult> OrganizersAsync(IQuestionQueries queries, CancellationToken cancellationToken) =>
        Results.Ok(await queries.ListOrganizersAsync(cancellationToken));
}
