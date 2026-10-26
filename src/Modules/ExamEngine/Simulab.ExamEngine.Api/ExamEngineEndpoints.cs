using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Simulab.ExamEngine.Api;

/// <summary>
/// The ExamEngine module's routes (F-166). The host calls this once, under <c>/api/v1</c>.
/// </summary>
public static class ExamEngineEndpoints
{
    public static IEndpointRouteBuilder MapExamEngineEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        _ = endpoints.MapGroup("/exam-engine").WithTags("ExamEngine");

        return endpoints;
    }
}
