using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Simulab.SharedKernel.Results;

namespace Simulab.ApiResults;

/// <summary>
/// The one place that turns an <see cref="Error"/> into an HTTP answer (F-39, BR1): RFC 9457 problem
/// details plus the stable <c>code</c> the UI turns into text (rule: api-contracts). Every
/// <c>/api/v1/</c> endpoint of every module and of the Api host calls it, so the answer a caller gets
/// does not depend on which module wrote the endpoint.
/// </summary>
/// <remarks>
/// The type is not named <c>ApiResults</c> like its namespace (CA1724) and not <c>Results</c>, which
/// would collide with <see cref="Results"/> at every call site. Callers add
/// <c>using static Simulab.ApiResults.ApiProblem;</c> and keep writing <c>Problem(error)</c>.
/// </remarks>
public static class ApiProblem
{
    /// <summary>
    /// The answer for <paramref name="error"/>. <paramref name="status"/> overrides the status its
    /// <see cref="ErrorKind"/> would give — a locked sign-in answers 423, a rate-limited one 429 — and
    /// <paramref name="extensions"/> adds members the client reads, such as <c>retryAfterSeconds</c>.
    /// </summary>
    public static IResult Problem(Error error, int? status = null, params (string Name, object Value)[] extensions)
    {
        ArgumentNullException.ThrowIfNull(error);

        var problem = new ProblemDetails
        {
            Status = status ?? StatusFor(error.Kind),
            Title = error.Code,
            Detail = error.Detail,
            Extensions = { ["code"] = error.Code }
        };

        foreach (var (name, value) in extensions)
        {
            problem.Extensions[name] = value;
        }

        return Results.Problem(problem);
    }

    /// <summary>
    /// The status each kind of failure answers with (BR2). An endpoint that needs another one for a
    /// single kind passes <c>status</c> instead of writing a second table of its own (BR6).
    /// </summary>
    public static int StatusFor(ErrorKind kind) => kind switch
    {
        ErrorKind.NotFound => StatusCodes.Status404NotFound,
        ErrorKind.Conflict => StatusCodes.Status409Conflict,
        ErrorKind.BusinessRule => StatusCodes.Status422UnprocessableEntity,
        ErrorKind.Forbidden => StatusCodes.Status403Forbidden,
        _ => StatusCodes.Status400BadRequest
    };
}
