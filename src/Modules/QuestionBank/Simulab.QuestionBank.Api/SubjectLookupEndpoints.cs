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
/// The classification lookups of the question form (B-28, BR1 and BR2): the subjects to search and the topics of
/// one subject. They live in the question bank and require <c>questions.manage</c> only, so a curator needs no
/// <c>catalog.manage</c> to pick a topic. The catalog routes and their permission are not changed.
/// </summary>
public static class SubjectLookupEndpoints
{
    /// <summary>The most subjects one lookup returns.</summary>
    private const int LookupMaxTake = 20;

    internal static RouteGroupBuilder MapSubjectLookupEndpoints(this RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        var subjects = group.MapGroup("/subjects")
            .RequireAuthorization(PermissionPolicy.NameFor(QuestionBankPermissions.Manage));

        subjects.MapGet(string.Empty, SearchAsync).WithName("SearchQuestionSubjects");
        subjects.MapGet("/{subjectId:guid}/topics", TopicsAsync).WithName("ListQuestionSubjectTopics");

        return group;
    }

    private static async Task<IResult> SearchAsync(
        IQuestionQueries queries,
        CancellationToken cancellationToken,
        string? search = null,
        int take = LookupMaxTake) =>
        Results.Ok(await queries.SearchSubjectsAsync(search, take, cancellationToken));

    private static async Task<IResult> TopicsAsync(
        Guid subjectId,
        IQuestionQueries queries,
        CancellationToken cancellationToken)
    {
        var topics = await queries.ListSubjectTopicsAsync(subjectId, cancellationToken);

        return topics is null
            ? Problem(new Error(QuestionBankErrorCodes.QuestionSubjectNotFound, ErrorKind.NotFound))
            : Results.Ok(topics);
    }
}
