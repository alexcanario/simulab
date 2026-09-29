using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

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
        group.MapPublishedExamEndpoints();

        return endpoints;
    }
}
