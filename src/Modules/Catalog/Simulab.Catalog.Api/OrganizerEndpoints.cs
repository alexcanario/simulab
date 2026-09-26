using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Simulab.Catalog.Application.Organizers;
using Simulab.Catalog.Contracts;
using Simulab.Identity.Contracts;
using static Simulab.ApiResults.ApiProblem;

namespace Simulab.Catalog.Api;

/// <summary>
/// The organizers back office (F-33). Every route requires <c>catalog.manage</c> (BR4); a caller
/// without it gets 403 <c>identity.forbidden</c> from the shared permission pipeline (F-6).
/// </summary>
public static class OrganizerEndpoints
{
    internal static RouteGroupBuilder MapOrganizerEndpoints(this RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        var organizers = group
            .MapGroup("/organizers")
            .RequireAuthorization(PermissionPolicy.NameFor(CatalogPermissions.Manage));

        organizers.MapGet(string.Empty, ListAsync).WithName("ListOrganizers");
        organizers.MapPost(string.Empty, CreateAsync).WithName("CreateOrganizer");
        organizers.MapPut("/{id:guid}", UpdateAsync).WithName("UpdateOrganizer");
        organizers.MapDelete("/{id:guid}", DeleteAsync).WithName("DeleteOrganizer");

        return group;
    }

    private static async Task<IResult> ListAsync(
        IOrganizerQueries queries,
        CancellationToken cancellationToken,
        int page = 0,
        int pageSize = OrganizerListQuery.DefaultPageSize,
        string? search = null,
        string? sortBy = null,
        bool descending = false,
        string? kindOrder = null) =>
        Results.Ok(await queries.ListAsync(
            new OrganizerListQuery(page, pageSize, search, sortBy, descending, OrganizerKindOrder.Parse(kindOrder)),
            cancellationToken));

    private static async Task<IResult> CreateAsync(
        SaveOrganizerRequest request,
        SaveOrganizerHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(null, request, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/v1/catalog/organizers/{result.Value.Id}", result.Value)
            : Problem(result.Error!);
    }

    private static async Task<IResult> UpdateAsync(
        Guid id,
        SaveOrganizerRequest request,
        SaveOrganizerHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);

        return result.IsSuccess ? Results.Ok(result.Value) : Problem(result.Error!);
    }

    private static async Task<IResult> DeleteAsync(Guid id, DeleteOrganizerHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);

        return result.IsSuccess ? Results.NoContent() : Problem(result.Error!);
    }
}
