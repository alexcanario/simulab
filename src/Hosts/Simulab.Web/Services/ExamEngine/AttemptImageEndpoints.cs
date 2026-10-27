using Simulab.ExamEngine.Contracts;
using Simulab.Web.Services.Auth;

namespace Simulab.Web.Services.ExamEngine;

/// <summary>
/// F-169 BR12: the student's pictures. The content read rewrites a question's picture addresses to this route, which asks
/// the Api with the signed-in user's token and streams the answer on, so no storage address reaches the browser. Only a
/// holder of <c>attempts.take</c> reaches it; the Api checks the owner and the picture again.
/// </summary>
internal static class AttemptImageEndpoints
{
    internal const string Route = "/attempts/{id:guid}/images/{imageId:guid}";

    internal static IEndpointRouteBuilder MapAttemptImageEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet(Route, ShowAsync)
            .RequireAuthorization(policy => policy.RequireClaim(WebAuthClaims.Permission, ExamEnginePermissions.Take));

        return app;
    }

    private static async Task<IResult> ShowAsync(
        Guid id,
        Guid imageId,
        HttpContext context,
        WebSessionTokenAccessor tokens,
        ExamEngineApiClient api)
    {
        var accessToken = await tokens.GetAccessTokenAsync(context.User, context.RequestAborted);
        if (accessToken is null)
        {
            return Results.Challenge();
        }

        var response = await api.OpenImageAsync(accessToken, id, imageId, context.RequestAborted);
        context.Response.RegisterForDispose(response);

        if (!response.IsSuccessStatusCode)
        {
            // The Api's answer, unchanged: a 404 stays a 404. A 401 means the session ended.
            return response.StatusCode == System.Net.HttpStatusCode.Unauthorized
                ? Results.Challenge()
                : Results.StatusCode((int)response.StatusCode);
        }

        // F-103 BR13: the type the Api stored, never sniffed, and a stored file that cannot act as a page of this origin.
        context.Response.Headers.XContentTypeOptions = "nosniff";
        context.Response.Headers.ContentSecurityPolicy = "sandbox";
        context.Response.Headers.CacheControl = "private";

        return Results.Stream(
            await response.Content.ReadAsStreamAsync(context.RequestAborted),
            response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream");
    }
}
