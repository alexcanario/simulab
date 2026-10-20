using Simulab.Catalog.Contracts;
using Simulab.Identity.Contracts;
using Simulab.Web.Services.Auth;

namespace Simulab.Web.Services.Catalog;

/// <summary>
/// F-89 UC3 and UC4: the browser downloads the notice PDF from the Web, because the tokens live on the server (B-3) and a
/// plain link to the Api could not sign in. Each route asks the Api with the signed-in user's token and streams the answer
/// on as a download, so no storage address ever reaches the browser (BR8). The permission is checked here too, by the
/// policy of its name and not by a role (BR6); the Api checks it again.
/// </summary>
internal static class NoticeDocumentEndpoints
{
    internal const string ManageRoute = "/catalog/exams/{examId:guid}/editions/{id:guid}/notice-document";

    internal const string BrowseRoute = "/catalog/published-exams/{examId:guid}/editions/{id:guid}/notice-document";

    internal static IEndpointRouteBuilder MapNoticeDocumentEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet(ManageRoute, (Guid examId, Guid id, HttpContext context, WebSessionTokenAccessor tokens, CatalogApiClient api) =>
                DownloadAsync(published: false, examId, id, context, tokens, api))
            .RequireAuthorization(PermissionPolicy.Prefix + CatalogPermissions.Manage);

        app.MapGet(BrowseRoute, (Guid examId, Guid id, HttpContext context, WebSessionTokenAccessor tokens, CatalogApiClient api) =>
                DownloadAsync(published: true, examId, id, context, tokens, api))
            .RequireAuthorization(PermissionPolicy.Prefix + CatalogPermissions.Browse);

        return app;
    }

    private static async Task<IResult> DownloadAsync(
        bool published,
        Guid examId,
        Guid id,
        HttpContext context,
        WebSessionTokenAccessor tokens,
        CatalogApiClient api)
    {
        var accessToken = await tokens.GetAccessTokenAsync(context.User, context.RequestAborted);
        if (accessToken is null)
        {
            return Results.Challenge();
        }

        var response = await api.OpenNoticeDocumentAsync(accessToken, examId, id, published, context.RequestAborted);
        context.Response.RegisterForDispose(response);

        if (!response.IsSuccessStatusCode)
        {
            // The Api's answer, unchanged: a 404 stays a 404 and a 403 a 403. A 401 means the session ended.
            return response.StatusCode == System.Net.HttpStatusCode.Unauthorized
                ? Results.Challenge()
                : Results.StatusCode((int)response.StatusCode);
        }

        var disposition = response.Content.Headers.ContentDisposition;
        var fileName = disposition?.FileNameStar ?? disposition?.FileName?.Trim('"') ?? "notice.pdf";
        context.Response.Headers.CacheControl = "private, no-store";

        return Results.File(await response.Content.ReadAsStreamAsync(context.RequestAborted), "application/pdf", fileName);
    }
}
