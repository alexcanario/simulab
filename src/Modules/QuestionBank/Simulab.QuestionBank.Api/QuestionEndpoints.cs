using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
        questions.MapPost("/{id:guid}/annul", AnnulAsync).WithName("AnnulQuestion");
        questions.MapPost("/{id:guid}/reinstate", ReinstateAsync).WithName("ReinstateQuestion");
        questions.MapDelete("/{id:guid}", DeleteAsync).WithName("DeleteQuestion");
        questions.MapGet("/{id:guid}/versions", ListVersionsAsync).WithName("ListQuestionVersions");
        questions.MapGet("/{id:guid}/versions/{number:int}", FindVersionAsync).WithName("FindQuestionVersion");

        return group;
    }

    // A list is a read: a value that is not understood is ignored, not an error (F-95 BR15, F-100 BR6). Every
    // filter list is bound as strings and parsed here, because a typed array answers 400 on one bad value. The
    // cap and the distinct are the query's own (QuestionListQuery.Sanitized).
    private static async Task<IResult> ListAsync(
        IQuestionQueries queries,
        CancellationToken cancellationToken,
        int page = 0,
        int pageSize = QuestionListQuery.DefaultPageSize,
        string? search = null,
        [FromQuery(Name = "status")] string[]? statuses = null,
        [FromQuery(Name = "type")] string[]? types = null,
        [FromQuery(Name = "difficulty")] string[]? difficulties = null,
        [FromQuery(Name = "subject")] string[]? subjects = null,
        [FromQuery(Name = "topic")] string[]? topics = null,
        [FromQuery(Name = "organizer")] string[]? organizers = null,
        string? edition = null)
    {
        var numbers = (difficulties ?? [])
            .Select(value => int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var number) ? number : (int?)null)
            .OfType<int>()
            .ToList();
        var notSet = (difficulties ?? []).Any(value => string.Equals(value?.Trim(), "none", StringComparison.OrdinalIgnoreCase));

        var query = new QuestionListQuery(
            page,
            pageSize,
            search,
            Enums<QuestionStatus>(statuses),
            Enums<QuestionType>(types),
            numbers,
            notSet,
            Guids(subjects),
            Guids(topics),
            Guids(organizers),
            Guid.TryParse(edition, out var editionId) ? editionId : null);

        return Results.Ok(await queries.ListAsync(query, cancellationToken));
    }

    // A name in any case; a number that is a defined value still reads (status=1 has always meant Published).
    private static List<TEnum> Enums<TEnum>(string[]? values)
        where TEnum : struct, Enum =>
        [
            .. (values ?? [])
                .Select(value => Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed)
                    ? parsed
                    : (TEnum?)null)
                .OfType<TEnum>()
        ];

    private static List<Guid> Guids(string[]? values) =>
        [.. (values ?? []).Select(value => Guid.TryParse(value, out var id) ? id : (Guid?)null).OfType<Guid>()];

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
        CancellationToken cancellationToken,
        PublishQuestionRequest? request = null)
    {
        // F-98 BR5: the body is optional; a publish with none is the F-95 publish.
        var result = await handler.HandleAsync(id, request?.ChangeReason, cancellationToken);

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

    // F-101 BR3: the body is optional on the wire, so a missing one answers with the code of a missing reason.
    private static async Task<IResult> AnnulAsync(
        Guid id,
        AnnulQuestionHandler handler,
        CancellationToken cancellationToken,
        AnnulQuestionRequest? request = null)
    {
        var result = await handler.HandleAsync(id, request?.Reason, cancellationToken);

        return result.IsSuccess ? Results.Ok(result.Value) : Problem(result.Error!);
    }

    private static async Task<IResult> ReinstateAsync(
        Guid id,
        ReinstateQuestionHandler handler,
        CancellationToken cancellationToken,
        ReinstateQuestionRequest? request = null)
    {
        var result = await handler.HandleAsync(id, request?.Reason, cancellationToken);

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

    // F-98 UC5, BR7: the history of a question, newest first; a read, so no paging (a question has few).
    private static async Task<IResult> ListVersionsAsync(
        Guid id,
        IQuestionQueries queries,
        CancellationToken cancellationToken)
    {
        var versions = await queries.ListVersionsAsync(id, cancellationToken);

        return versions is null
            ? Problem(new Error(QuestionBankErrorCodes.QuestionNotFound, ErrorKind.NotFound))
            : Results.Ok(versions);
    }

    private static async Task<IResult> FindVersionAsync(
        Guid id,
        int number,
        IQuestionQueries queries,
        CancellationToken cancellationToken)
    {
        var result = await queries.FindVersionAsync(id, number, cancellationToken);

        return result.IsSuccess ? Results.Ok(result.Value) : Problem(result.Error!);
    }

    // The option a refusal is about travels as the problem's `position`, so the form marks the right row.
    private static IResult Refusal(SaveQuestionOutcome outcome) =>
        outcome.OptionPosition is { } position
            ? Problem(outcome.Error!, null, ("position", position))
            : Problem(outcome.Error!);
}
