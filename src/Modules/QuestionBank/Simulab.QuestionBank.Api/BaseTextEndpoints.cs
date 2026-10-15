using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Simulab.Identity.Contracts;
using Simulab.QuestionBank.Application.BaseTexts;
using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Results;
using static Simulab.ApiResults.ApiProblem;

namespace Simulab.QuestionBank.Api;

/// <summary>
/// The base text back office (F-102): list, read, create, update, delete and the questions a text links. Every
/// route requires <c>questions.manage</c> (BR12); a caller without it gets 403 <c>identity.forbidden</c> from the
/// shared permission pipeline.
/// </summary>
public static class BaseTextEndpoints
{
    internal static RouteGroupBuilder MapBaseTextEndpoints(this RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        var baseTexts = group.MapGroup("/base-texts")
            .RequireAuthorization(PermissionPolicy.NameFor(QuestionBankPermissions.Manage));

        baseTexts.MapGet(string.Empty, ListAsync).WithName("ListBaseTexts");
        baseTexts.MapGet("/{id:guid}", FindAsync).WithName("FindBaseText");
        baseTexts.MapPost(string.Empty, CreateAsync).WithName("CreateBaseText");
        baseTexts.MapPut("/{id:guid}", UpdateAsync).WithName("UpdateBaseText");
        baseTexts.MapDelete("/{id:guid}", DeleteAsync).WithName("DeleteBaseText");
        baseTexts.MapGet("/{id:guid}/questions", ListQuestionsAsync).WithName("ListBaseTextQuestions");

        return group;
    }

    // A list is a read: a value that is not understood is ignored, not an error (F-95 BR15).
    private static async Task<IResult> ListAsync(
        IBaseTextQueries queries,
        CancellationToken cancellationToken,
        int page = 0,
        int pageSize = BaseTextListQuery.DefaultPageSize,
        string? search = null,
        string? language = null) =>
        Results.Ok(await queries.ListAsync(new BaseTextListQuery(page, pageSize, search, language), cancellationToken));

    private static async Task<IResult> FindAsync(Guid id, IBaseTextQueries queries, CancellationToken cancellationToken)
    {
        var baseText = await queries.FindAsync(id, cancellationToken);

        return baseText is null
            ? Problem(new Error(QuestionBankErrorCodes.BaseTextNotFound, ErrorKind.NotFound))
            : Results.Ok(baseText);
    }

    private static async Task<IResult> CreateAsync(
        SaveBaseTextRequest request,
        SaveBaseTextHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(null, request, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/v1/question-bank/base-texts/{result.Value!.Id}", result.Value)
            : Problem(result.Error!);
    }

    private static async Task<IResult> UpdateAsync(
        Guid id,
        SaveBaseTextRequest request,
        SaveBaseTextHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);

        return result.IsSuccess ? Results.Ok(result.Value) : Problem(result.Error!);
    }

    private static async Task<IResult> DeleteAsync(
        Guid id,
        DeleteBaseTextHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);

        return result.IsSuccess ? Results.NoContent() : Problem(result.Error!);
    }

    private static async Task<IResult> ListQuestionsAsync(
        Guid id,
        IBaseTextQueries queries,
        CancellationToken cancellationToken,
        int page = 0,
        int pageSize = QuestionListQuery.DefaultPageSize)
    {
        var questions = await queries.ListQuestionsAsync(id, Math.Max(page, 0), pageSize, cancellationToken);

        return questions is null
            ? Problem(new Error(QuestionBankErrorCodes.BaseTextNotFound, ErrorKind.NotFound))
            : Results.Ok(questions);
    }
}
