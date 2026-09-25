using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Simulab.SharedKernel.Results;

namespace Simulab.Catalog.Api;

/// <summary>
/// The Catalog module's routes (F-33, BR1). The host calls this once, under <c>/api/v1</c>.
/// </summary>
public static class CatalogEndpoints
{
    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapGroup("/catalog").WithTags("Catalog");

        group.MapOrganizerEndpoints();
        group.MapIssuingAuthorityEndpoints();
        group.MapExamEndpoints();

        return endpoints;
    }

    /// <summary>
    /// RFC 9457 problem details plus the stable <c>code</c> the UI translates (rule: api-contracts).
    /// Identity has the same shape with its own extensions; the two become one building block when a
    /// third module needs it (F-33, decision of 2026-09-23).
    /// </summary>
    internal static IResult Problem(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return Results.Problem(new ProblemDetails
        {
            Status = StatusFor(error.Kind),
            Title = error.Code,
            Detail = error.Detail,
            Extensions = { ["code"] = error.Code }
        });
    }

    private static int StatusFor(ErrorKind kind) => kind switch
    {
        ErrorKind.NotFound => StatusCodes.Status404NotFound,
        ErrorKind.Conflict => StatusCodes.Status409Conflict,
        ErrorKind.BusinessRule => StatusCodes.Status422UnprocessableEntity,
        ErrorKind.Forbidden => StatusCodes.Status403Forbidden,
        _ => StatusCodes.Status400BadRequest
    };
}
