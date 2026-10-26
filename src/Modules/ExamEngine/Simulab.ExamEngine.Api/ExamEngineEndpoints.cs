using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Simulab.ExamEngine.Contracts;
using Simulab.Identity.Contracts;

namespace Simulab.ExamEngine.Api;

/// <summary>
/// The ExamEngine module's routes (F-166). The host calls this once, under <c>/api/v1</c>. Starting, answering and
/// submitting need <c>attempts.take</c>; reading needs a signed-in caller and answers 404 to anyone who is neither the
/// owner nor a holder of <c>attempts.read</c> (BR7).
/// </summary>
public static class ExamEngineEndpoints
{
    public static IEndpointRouteBuilder MapExamEngineEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapGroup("/exam-engine").WithTags("ExamEngine");

        group.MapAttemptEndpoints();

        return endpoints;
    }

    internal static string TakePolicy => PermissionPolicy.NameFor(ExamEnginePermissions.Take);
}
