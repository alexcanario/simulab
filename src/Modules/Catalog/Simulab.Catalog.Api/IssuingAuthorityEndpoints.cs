using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Simulab.Catalog.Application.IssuingAuthorities;
using Simulab.Catalog.Contracts;
using Simulab.Identity.Contracts;
using static Simulab.ApiResults.ApiProblem;

namespace Simulab.Catalog.Api;

/// <summary>
/// The issuing authorities back office (F-34 BR18, v2). Every route requires <c>catalog.manage</c>
/// (F-33 BR4); a caller without it gets 403 <c>identity.forbidden</c> from the shared permission pipeline.
/// </summary>
public static class IssuingAuthorityEndpoints
{
    internal static RouteGroupBuilder MapIssuingAuthorityEndpoints(this RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        var authorities = group
            .MapGroup("/issuing-authorities")
            .RequireAuthorization(PermissionPolicy.NameFor(CatalogPermissions.Manage));

        authorities.MapGet(string.Empty, ListAsync).WithName("ListIssuingAuthorities");
        authorities.MapPost(string.Empty, CreateAsync).WithName("CreateIssuingAuthority");
        authorities.MapPut("/{id:guid}", UpdateAsync).WithName("UpdateIssuingAuthority");
        authorities.MapDelete("/{id:guid}", DeleteAsync).WithName("DeleteIssuingAuthority");

        return group;
    }

    private static async Task<IResult> ListAsync(
        IIssuingAuthorityQueries queries,
        CancellationToken cancellationToken,
        int page = 0,
        int pageSize = IssuingAuthorityListQuery.DefaultPageSize,
        string? search = null,
        string? sortBy = null,
        bool descending = false) =>
        Results.Ok(await queries.ListAsync(
            new IssuingAuthorityListQuery(page, pageSize, search, sortBy, descending),
            cancellationToken));

    private static async Task<IResult> CreateAsync(
        SaveIssuingAuthorityRequest request,
        SaveIssuingAuthorityHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(null, request, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/v1/catalog/issuing-authorities/{result.Value.Id}", result.Value)
            : Problem(result.Error!);
    }

    private static async Task<IResult> UpdateAsync(
        Guid id,
        SaveIssuingAuthorityRequest request,
        SaveIssuingAuthorityHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);

        return result.IsSuccess ? Results.Ok(result.Value) : Problem(result.Error!);
    }

    private static async Task<IResult> DeleteAsync(
        Guid id,
        DeleteIssuingAuthorityHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);

        return result.IsSuccess ? Results.NoContent() : Problem(result.Error!);
    }
}
