using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Simulab.Catalog.Application.ExamEditions;
using Simulab.Catalog.Application.NoticeSubjects;
using Simulab.Catalog.Contracts;
using Simulab.Identity.Contracts;
using Simulab.SharedKernel.Results;
using static Simulab.ApiResults.ApiProblem;

namespace Simulab.Catalog.Api;

/// <summary>
/// The notice subjects of an edition (F-74): what the notice says the edition covers, grouped and ordered.
/// They live under their edition and every route requires <c>catalog.manage</c>, so a caller without it gets
/// 403 <c>identity.forbidden</c> from the shared permission pipeline on all five (BR11). An edition that is
/// not under the exam of the route answers 404 <c>exam_edition.not_found</c>, a missing exam included.
/// </summary>
public static class NoticeSubjectEndpoints
{
    internal static RouteGroupBuilder MapNoticeSubjectEndpoints(this RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        var subjects = group
            .MapGroup("/exams/{examId:guid}/editions/{editionId:guid}/notice-subjects")
            .RequireAuthorization(PermissionPolicy.NameFor(CatalogPermissions.Manage));

        subjects.MapGet(string.Empty, ListAsync).WithName("ListNoticeSubjects");
        subjects.MapPost(string.Empty, CreateAsync).WithName("CreateNoticeSubject");
        subjects.MapPut("/{id:guid}", UpdateAsync).WithName("UpdateNoticeSubject");
        subjects.MapPost("/{id:guid}/move", MoveAsync).WithName("MoveNoticeSubject");
        subjects.MapDelete("/{id:guid}", DeleteAsync).WithName("DeleteNoticeSubject");

        return group;
    }

    private static async Task<IResult> ListAsync(
        Guid examId,
        Guid editionId,
        IExamEditionQueries editions,
        INoticeSubjectQueries queries,
        CancellationToken cancellationToken)
    {
        if (await editions.FindAsync(examId, editionId, cancellationToken) is null)
        {
            return Problem(new Error(CatalogErrorCodes.ExamEditionNotFound, ErrorKind.NotFound));
        }

        return Results.Ok(await queries.ListAsync(editionId, cancellationToken));
    }

    private static async Task<IResult> CreateAsync(
        Guid examId,
        Guid editionId,
        SaveNoticeSubjectRequest request,
        SaveNoticeSubjectHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(examId, editionId, null, request, cancellationToken);

        return result.IsSuccess
            ? Results.Created(
                $"/api/v1/catalog/exams/{examId}/editions/{editionId}/notice-subjects/{result.Value.Id}",
                result.Value)
            : Problem(result.Error!);
    }

    private static async Task<IResult> UpdateAsync(
        Guid examId,
        Guid editionId,
        Guid id,
        SaveNoticeSubjectRequest request,
        SaveNoticeSubjectHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(examId, editionId, id, request, cancellationToken);

        return result.IsSuccess ? Results.Ok(result.Value) : Problem(result.Error!);
    }

    private static async Task<IResult> MoveAsync(
        Guid examId,
        Guid editionId,
        Guid id,
        MoveNoticeSubjectRequest request,
        MoveNoticeSubjectHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(examId, editionId, id, request, cancellationToken);

        return result.IsSuccess ? Results.NoContent() : Problem(result.Error!);
    }

    private static async Task<IResult> DeleteAsync(
        Guid examId,
        Guid editionId,
        Guid id,
        DeleteNoticeSubjectHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(examId, editionId, id, cancellationToken);

        return result.IsSuccess ? Results.NoContent() : Problem(result.Error!);
    }
}
