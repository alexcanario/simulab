using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Simulab.Identity.Contracts;
using Simulab.QuestionBank.Application.Questions;
using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Results;
using static Simulab.ApiResults.ApiProblem;

namespace Simulab.QuestionBank.Api;

/// <summary>
/// The question back office (F-95): list, read, create, update, publish, unpublish and delete. Every route
/// requires <c>questions.manage</c> (BR14); a caller without it gets 403 <c>identity.forbidden</c> from the
/// shared permission pipeline.
/// </summary>
public static class QuestionEndpoints
{
    internal static RouteGroupBuilder MapQuestionEndpoints(this RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        var questions = group.MapGroup("/questions")
            .RequireAuthorization(PermissionPolicy.NameFor(QuestionBankPermissions.Manage));

        questions.MapGet(string.Empty, ListAsync).WithName("ListQuestions");
        questions.MapGet("/{id:guid}", FindAsync).WithName("FindQuestion");
        questions.MapPost(string.Empty, CreateAsync).WithName("CreateQuestion");
        questions.MapPut("/{id:guid}", UpdateAsync).WithName("UpdateQuestion");
        questions.MapPost("/{id:guid}/publish", PublishAsync).WithName("PublishQuestion");
        questions.MapPost("/{id:guid}/unpublish", UnpublishAsync).WithName("UnpublishQuestion");
        questions.MapDelete("/{id:guid}", DeleteAsync).WithName("DeleteQuestion");

        return group;
    }

    // A list is a read: a status that is not one of the two values is ignored, not an error (BR15).
    private static async Task<IResult> ListAsync(
        IQuestionQueries queries,
        CancellationToken cancellationToken,
        int page = 0,
        int pageSize = QuestionListQuery.DefaultPageSize,
        string? search = null,
        string? status = null)
    {
        QuestionStatus? parsed = Enum.TryParse<QuestionStatus>(status, ignoreCase: true, out var value) && Enum.IsDefined(value)
            ? value
            : null;

        return Results.Ok(await queries.ListAsync(new QuestionListQuery(page, pageSize, search, parsed), cancellationToken));
    }

    private static async Task<IResult> FindAsync(Guid id, IQuestionQueries queries, CancellationToken cancellationToken)
    {
        var question = await queries.FindAsync(id, cancellationToken);

        return question is null
            ? Problem(new Error(QuestionBankErrorCodes.QuestionNotFound, ErrorKind.NotFound))
            : Results.Ok(question);
    }

    private static async Task<IResult> CreateAsync(
        SaveQuestionRequest request,
        SaveQuestionHandler handler,
        CancellationToken cancellationToken)
    {
        var outcome = await handler.HandleAsync(null, request, cancellationToken);

        return outcome.IsSuccess
            ? Results.Created($"/api/v1/question-bank/questions/{outcome.Question!.Id}", outcome.Question)
            : Refusal(outcome);
    }

    private static async Task<IResult> UpdateAsync(
        Guid id,
        SaveQuestionRequest request,
        SaveQuestionHandler handler,
        CancellationToken cancellationToken)
    {
        var outcome = await handler.HandleAsync(id, request, cancellationToken);

        return outcome.IsSuccess ? Results.Ok(outcome.Question) : Refusal(outcome);
    }

    private static async Task<IResult> PublishAsync(
        Guid id,
        PublishQuestionHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, null, cancellationToken);

        return result.IsSuccess ? Results.Ok(result.Value) : Problem(result.Error!);
    }

    private static async Task<IResult> UnpublishAsync(
        Guid id,
        UnpublishQuestionHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);

        return result.IsSuccess ? Results.Ok(result.Value) : Problem(result.Error!);
    }

    private static async Task<IResult> DeleteAsync(
        Guid id,
        DeleteQuestionHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);

        return result.IsSuccess ? Results.NoContent() : Problem(result.Error!);
    }

    // The option a refusal is about travels as the problem's `position`, so the form marks the right row.
    private static IResult Refusal(SaveQuestionOutcome outcome) =>
        outcome.OptionPosition is { } position
            ? Problem(outcome.Error!, null, ("position", position))
            : Problem(outcome.Error!);
}
