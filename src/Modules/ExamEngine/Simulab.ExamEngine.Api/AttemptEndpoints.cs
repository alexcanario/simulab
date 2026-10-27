using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Simulab.ExamEngine.Application.Attempts;
using Simulab.ExamEngine.Contracts;
using Simulab.SharedKernel.Security;
using Simulab.SharedKernel.Serialization;
using static Simulab.ApiResults.ApiProblem;

namespace Simulab.ExamEngine.Api;

/// <summary>
/// The five attempt routes (F-166): start, read, save or replace one answer, clear one answer and submit. The caller is
/// always the student of the attempt; the id comes from the signed-in user, never from the body.
/// </summary>
internal static class AttemptEndpoints
{
    internal static RouteGroupBuilder MapAttemptEndpoints(this RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        var attempts = group.MapGroup("/attempts");

        attempts.MapPost(string.Empty, StartAsync)
            .RequireAuthorization(ExamEngineEndpoints.TakePolicy)
            .WithName("StartAttempt");
        attempts.MapGet("/{id:guid}", ReadAsync)
            .RequireAuthorization()
            .WithName("ReadAttempt");
        attempts.MapPut("/{id:guid}/answers/{questionId:guid}", SaveAsync)
            .RequireAuthorization(ExamEngineEndpoints.TakePolicy)
            .Accepts<AnswerPayload>("application/json")
            .Produces<AttemptQuestionResponse>()
            .WithName("SaveAnswer");
        attempts.MapDelete("/{id:guid}/answers/{questionId:guid}", ClearAsync)
            .RequireAuthorization(ExamEngineEndpoints.TakePolicy)
            .WithName("ClearAnswer");
        attempts.MapPost("/{id:guid}/submit", SubmitAsync)
            .RequireAuthorization(ExamEngineEndpoints.TakePolicy)
            .WithName("SubmitAttempt");

        return group;
    }

    private static async Task<IResult> StartAsync(
        StartAttemptRequest request,
        ICurrentUser currentUser,
        StartAttemptHandler handler,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } studentId)
        {
            return Results.Unauthorized();
        }

        var outcome = await handler.HandleAsync(studentId, request, cancellationToken);
        if (!outcome.IsSuccess)
        {
            return Problem(outcome.Error!, extensions: ErrorsOf(outcome.Fields));
        }

        var started = outcome.Attempt!;

        return started.Created
            ? Results.Created($"/api/v1/exam-engine/attempts/{started.Attempt.Id}", started.Attempt)
            : Results.Ok(started.Attempt);
    }

    private static async Task<IResult> ReadAsync(
        Guid id,
        ICurrentUser currentUser,
        GetAttemptHandler handler,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Results.Unauthorized();
        }

        var outcome = await handler.HandleAsync(userId, id, cancellationToken);

        return outcome.IsSuccess ? Results.Ok(outcome.Value) : Problem(outcome.Error!);
    }

    // The body is read here, up to a limit written in the contract, and not bound: a binder reads it all before the handler
    // runs, and answers a body that names no known type with a 400 that carries no code.
    private static async Task<IResult> SaveAsync(
        Guid id,
        Guid questionId,
        HttpRequest request,
        ICurrentUser currentUser,
        SaveAnswerHandler handler,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } studentId)
        {
            return Results.Unauthorized();
        }

        var body = await ReadBodyAsync(request, cancellationToken);
        if (body is null)
        {
            return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
        }

        var outcome = await handler.HandleAsync(studentId, id, questionId, ParseAnswer(body), cancellationToken);
        if (outcome.IsSuccess)
        {
            return Results.Ok(outcome.Question);
        }

        return Problem(outcome.Error!, extensions: ErrorsOf(outcome.Fields));
    }

    /// <summary>The <c>errors</c> member of a 400, one entry per field, or no member when there is no field entry.</summary>
    private static (string Name, object Value)[] ErrorsOf(IReadOnlyList<AnswerFieldError> fields) =>
        fields.Count == 0
            ? []
            : [("errors", fields.Select(field => new { field = field.Field, code = field.Code }).ToList())];

    private static async Task<IResult> ClearAsync(
        Guid id,
        Guid questionId,
        ICurrentUser currentUser,
        ClearAnswerHandler handler,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } studentId)
        {
            return Results.Unauthorized();
        }

        var outcome = await handler.HandleAsync(studentId, id, questionId, cancellationToken);

        return outcome.IsSuccess ? Results.NoContent() : Problem(outcome.Error!);
    }

    // Nothing is read from the body or the headers: the time is the server's (T8).
    private static async Task<IResult> SubmitAsync(
        Guid id,
        ICurrentUser currentUser,
        SubmitAttemptHandler handler,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } studentId)
        {
            return Results.Unauthorized();
        }

        var outcome = await handler.HandleAsync(studentId, id, cancellationToken);

        return outcome.IsSuccess ? Results.Ok(outcome.Value) : Problem(outcome.Error!);
    }

    /// <summary>The body as text, or null when it is longer than the limit of the route.</summary>
    private static async Task<byte[]?> ReadBodyAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        if (request.ContentLength > AttemptLimits.SaveBodyMaxBytes)
        {
            return null;
        }

        using var buffer = new MemoryStream();
        var chunk = new byte[16 * 1024];
        int read;
        while ((read = await request.Body.ReadAsync(chunk, cancellationToken)) > 0)
        {
            if (buffer.Length + read > AttemptLimits.SaveBodyMaxBytes)
            {
                return null;
            }

            buffer.Write(chunk, 0, read);
        }

        return buffer.ToArray();
    }

    // A body that is not JSON, names no type or an unknown one, or has members of another type is no answer: null.
    private static AnswerPayload? ParseAnswer(byte[] body)
    {
        try
        {
            return JsonSerializer.Deserialize<AnswerPayload>(body, AppJson.Options);
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            return null;
        }
    }
}
