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
/// The reads of the origin (F-96, BR9 and BR10): the edition lookup and the notice subjects the question form
/// fills its "Origin" section from, and the questions of one edition for the edition page. They live in the
/// question bank and require <c>questions.manage</c> only, so a curator needs no <c>catalog.manage</c> (BR12).
/// </summary>
public static class ExamEditionLookupEndpoints
{
    /// <summary>The most editions one lookup returns.</summary>
    private const int LookupMaxTake = 20;

    internal static RouteGroupBuilder MapExamEditionLookupEndpoints(this RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        var editions = group.MapGroup("/editions")
            .RequireAuthorization(PermissionPolicy.NameFor(QuestionBankPermissions.Manage));

        editions.MapGet(string.Empty, SearchAsync).WithName("SearchQuestionEditions");
        editions.MapGet("/{editionId:guid}", FindAsync).WithName("FindQuestionEdition");
        editions.MapGet("/{editionId:guid}/notice-subjects", NoticeSubjectsAsync).WithName("ListQuestionEditionNoticeSubjects");
        editions.MapGet("/{editionId:guid}/questions", QuestionsAsync).WithName("ListEditionQuestions");
        editions.MapGet("/{editionId:guid}/next-paper-number", NextPaperNumberAsync).WithName("GetNextEditionPaperNumber");

        return group;
    }

    private static async Task<IResult> SearchAsync(
        IQuestionQueries queries,
        CancellationToken cancellationToken,
        string? search = null,
        int take = LookupMaxTake) =>
        Results.Ok(await queries.SearchEditionsAsync(search, take, cancellationToken));

    private static async Task<IResult> FindAsync(
        Guid editionId,
        IQuestionQueries queries,
        CancellationToken cancellationToken)
    {
        var edition = await queries.FindEditionAsync(editionId, cancellationToken);

        return edition is null ? EditionNotFound() : Results.Ok(edition);
    }

    private static async Task<IResult> NoticeSubjectsAsync(
        Guid editionId,
        IQuestionQueries queries,
        CancellationToken cancellationToken)
    {
        var subjects = await queries.ListNoticeSubjectsAsync(editionId, cancellationToken);

        return subjects is null ? EditionNotFound() : Results.Ok(subjects);
    }

    private static async Task<IResult> NextPaperNumberAsync(
        Guid editionId,
        IQuestionQueries queries,
        CancellationToken cancellationToken)
    {
        var number = await queries.GetNextPaperNumberAsync(editionId, cancellationToken);

        return number is null ? EditionNotFound() : Results.Ok(new NextPaperNumberResponse(number.Value));
    }

    private static async Task<IResult> QuestionsAsync(
        Guid editionId,
        IQuestionQueries queries,
        CancellationToken cancellationToken,
        int page = 0,
        int pageSize = QuestionListQuery.DefaultPageSize)
    {
        var questions = await queries.ListEditionQuestionsAsync(editionId, page, pageSize, cancellationToken);

        return questions is null ? EditionNotFound() : Results.Ok(questions);
    }

    private static IResult EditionNotFound() =>
        Problem(new Error(QuestionBankErrorCodes.QuestionExamEditionNotFound, ErrorKind.NotFound));
}
