using Simulab.QuestionBank.Contracts;
using Simulab.Web.Services.Auth;

namespace Simulab.Web.Services.QuestionBank;

/// <summary>
/// F-103 UC3: the browser shows a stored picture from the Web, because the tokens live on the server (B-3) and a plain
/// address to the Api could not sign in. The route asks the Api with the signed-in user's token and streams the answer on,
/// so no storage address ever reaches the browser (BR12). Either <c>questions.manage</c> or <c>questions.review</c> reads;
/// the Api checks it again. The student route arrives with the simulator (E-5).
/// </summary>
internal static class QuestionImageEndpoints
{
    /// <summary>The address the text of a question holds for a stored picture; the sanitizer admits no other.</summary>
    internal const string Route = "/question-images/{id:guid}";

    internal static IEndpointRouteBuilder MapQuestionImageEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet(Route, ShowAsync)
            .RequireAuthorization(policy => policy.RequireClaim(
                WebAuthClaims.Permission, QuestionBankPermissions.Manage, QuestionBankPermissions.Review));

        return app;
    }

    private static async Task<IResult> ShowAsync(
        Guid id,
        HttpContext context,
        WebSessionTokenAccessor tokens,
        QuestionBankApiClient api)
    {
        var accessToken = await tokens.GetAccessTokenAsync(context.User, context.RequestAborted);
        if (accessToken is null)
        {
            return Results.Challenge();
        }

        var response = await api.OpenImageAsync(accessToken, id, context.RequestAborted);

        return await ImageProxy.StreamAsync(response, context);
    }
}
