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
/// The review queue and the decision on a question in review (F-105). Every route requires
/// <c>questions.review</c> and nothing else (BR10): a reviewer without <c>questions.manage</c> does the whole review.
/// </summary>
public static class ReviewQueueEndpoints
{
    internal static RouteGroupBuilder MapReviewQueueEndpoints(this RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        var queue = group.MapGroup("/review-queue")
            .RequireAuthorization(PermissionPolicy.NameFor(QuestionBankPermissions.Review));

        queue.MapGet(string.Empty, ListAsync).WithName("ListReviewQueue");
        queue.MapGet("/{id:guid}", FindAsync).WithName("FindQuestionInReview");
        queue.MapPost("/{id:guid}/approve", ApproveAsync).WithName("ApproveQuestion");
        queue.MapPost("/{id:guid}/reject", RejectAsync).WithName("RejectQuestion");

        return group;
    }

    // BR8: zero-based pages, the size capped by the query itself.
    private static async Task<IResult> ListAsync(
        IQuestionQueries queries,
        CancellationToken cancellationToken,
        int page = 0,
        int pageSize = QuestionListQuery.DefaultPageSize) =>
        Results.Ok(await queries.ListReviewQueueAsync(page, pageSize, cancellationToken));

    // BR9: the question as the reviewer sees it, with the key; one that is not in review is a 409 so a stale link
    // is told why (AC10).
    private static async Task<IResult> FindAsync(Guid id, IQuestionQueries queries, CancellationToken cancellationToken)
    {
        var question = await queries.FindAsync(id, cancellationToken);
        if (question is null)
        {
            return Problem(new Error(QuestionBankErrorCodes.QuestionNotFound, ErrorKind.NotFound));
        }

        return question.Status == QuestionStatus.InReview
            ? Results.Ok(question)
            : Problem(new Error(QuestionBankErrorCodes.QuestionNotInReview, ErrorKind.Conflict));
    }

    // BR5: the body is optional on the wire; a missing one is an approval with no note.
    private static async Task<IResult> ApproveAsync(
        Guid id,
        ApproveQuestionHandler handler,
        CancellationToken cancellationToken,
        ApproveQuestionRequest? request = null)
    {
        var result = await handler.HandleAsync(id, request?.Note, request?.SubmittedAt, cancellationToken);

        return result.IsSuccess ? Results.Ok(result.Value) : Problem(result.Error!);
    }

    // BR6: the body is optional on the wire, so a missing one answers with the code of a missing reason.
    private static async Task<IResult> RejectAsync(
        Guid id,
        RejectQuestionHandler handler,
        CancellationToken cancellationToken,
        RejectQuestionRequest? request = null)
    {
        var result = await handler.HandleAsync(id, request?.Reason, request?.SubmittedAt, cancellationToken);

        return result.IsSuccess ? Results.Ok(result.Value) : Problem(result.Error!);
    }
}
