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

        return await ImageProxy.StreamAsync(response, context);
    }
}
