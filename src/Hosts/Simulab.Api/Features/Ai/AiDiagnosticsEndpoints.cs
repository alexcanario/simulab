using Microsoft.AspNetCore.Mvc;
using Simulab.Ai;
using Simulab.Ai.Contracts;
using Simulab.SharedKernel.Results;

namespace Simulab.Api.Features.Ai;

/// <summary>
/// The one route that exercises <see cref="IAiGateway"/> end to end, for the <c>/dev/ai</c> page
/// (F-41, v2). It is mapped only in Development, so outside it the route does not exist (AC9b).
/// </summary>
public static class AiDiagnosticsEndpoints
{
    public static IEndpointRouteBuilder MapAiDiagnosticsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapPost("/ai/diagnostics", SendAsync)
            .WithName("SendAiDiagnostics")
            .WithTags("Ai")
            // The committed OpenAPI document describes the API production serves; this route exists only
            // in Development, so documenting it would promise callers an endpoint they will never reach.
            .ExcludeFromDescription();

        return endpoints;
    }

    private static async Task<IResult> SendAsync(
        AiDiagnosticsRequest request,
        IAiGateway gateway,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Prompt))
        {
            return Problem(new Error(AiErrorCodes.PromptRequired, ErrorKind.Validation, "The prompt is empty."));
        }

        var purpose = string.IsNullOrWhiteSpace(request.Purpose) ? AiPurposes.Diagnostics : request.Purpose;
        var result = await gateway
            .CompleteAsync(new AiRequest(purpose, request.Prompt), cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            return Problem(result.Error!);
        }

        var completion = result.Value;
        return Results.Ok(new AiDiagnosticsResponse(
            completion.Text,
            completion.Model,
            completion.InputTokens,
            completion.OutputTokens,
            completion.CostUsd,
            (int)completion.Duration.TotalMilliseconds));
    }

    // The third copy of this mapping (Catalog and Identity have their own). F-39 exists to replace all
    // three with one helper; this one is recorded there as debt (owner, 2026-09-24).
    private static IResult Problem(Error error) =>
        Results.Problem(new ProblemDetails
        {
            Status = StatusFor(error.Kind),
            Title = error.Code,
            Detail = error.Detail,
            Extensions = { ["code"] = error.Code }
        });

    private static int StatusFor(ErrorKind kind) => kind switch
    {
        ErrorKind.NotFound => StatusCodes.Status404NotFound,
        ErrorKind.Conflict => StatusCodes.Status409Conflict,
        ErrorKind.BusinessRule => StatusCodes.Status422UnprocessableEntity,
        ErrorKind.Forbidden => StatusCodes.Status403Forbidden,
        _ => StatusCodes.Status400BadRequest
    };
}
