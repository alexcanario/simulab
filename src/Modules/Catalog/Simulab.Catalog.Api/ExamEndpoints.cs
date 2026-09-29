using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Simulab.Catalog.Application.ExamEditions;
using Simulab.Catalog.Application.Exams;
using Simulab.Catalog.Contracts;
using Simulab.Identity.Contracts;
using static Simulab.ApiResults.ApiProblem;

namespace Simulab.Catalog.Api;

/// <summary>
/// The exams back office (F-34). Every route requires <c>catalog.manage</c> (F-33 BR4); a caller without it
/// gets 403 <c>identity.forbidden</c> from the shared permission pipeline (F-6).
/// </summary>
public static class ExamEndpoints
{
    internal static RouteGroupBuilder MapExamEndpoints(this RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        var exams = group
            .MapGroup("/exams")
            .RequireAuthorization(PermissionPolicy.NameFor(CatalogPermissions.Manage));

        exams.MapGet(string.Empty, ListAsync).WithName("ListExams");
        exams.MapGet("/{id:guid}", FindAsync).WithName("FindExam");
        exams.MapPost(string.Empty, CreateAsync).WithName("CreateExam");
        exams.MapPut("/{id:guid}", UpdateAsync).WithName("UpdateExam");
        exams.MapDelete("/{id:guid}", DeleteAsync).WithName("DeleteExam");

        // F-35: the editions live under their exam and share its policy, so a caller without
        // catalog.manage gets the same 403 on every one of them (AC16).
        exams.MapGet("/{examId:guid}/editions", ListEditionsAsync).WithName("ListExamEditions");
        exams.MapGet("/{examId:guid}/editions/{id:guid}", FindEditionAsync).WithName("FindExamEdition");
        exams.MapPost("/{examId:guid}/editions", CreateEditionAsync).WithName("CreateExamEdition");
        exams.MapPut("/{examId:guid}/editions/{id:guid}", UpdateEditionAsync).WithName("UpdateExamEdition");
        exams.MapDelete("/{examId:guid}/editions/{id:guid}", DeleteEditionAsync).WithName("DeleteExamEdition");

        return group;
    }

    private static async Task<IResult> ListEditionsAsync(
        Guid examId,
        IExamQueries exams,
        IExamEditionQueries editions,
        CancellationToken cancellationToken)
    {
        // An exam that does not exist has no editions to list: the caller is told, not given an empty list.
        if (await exams.FindAsync(examId, cancellationToken) is null)
        {
            return Problem(new SharedKernel.Results.Error(CatalogErrorCodes.ExamNotFound, SharedKernel.Results.ErrorKind.NotFound));
        }

        return Results.Ok(await editions.ListAsync(examId, cancellationToken));
    }

    private static async Task<IResult> FindEditionAsync(
        Guid examId,
        Guid id,
        IExamEditionQueries editions,
        CancellationToken cancellationToken)
    {
        var edition = await editions.FindAsync(examId, id, cancellationToken);

        return edition is null
            ? Problem(new SharedKernel.Results.Error(CatalogErrorCodes.ExamEditionNotFound, SharedKernel.Results.ErrorKind.NotFound))
            : Results.Ok(edition);
    }

    private static async Task<IResult> CreateEditionAsync(
        Guid examId,
        SaveExamEditionRequest request,
        SaveExamEditionHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(examId, null, request, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/v1/catalog/exams/{examId}/editions/{result.Value.Id}", result.Value)
            : Problem(result.Error!);
    }

    private static async Task<IResult> UpdateEditionAsync(
        Guid examId,
        Guid id,
        SaveExamEditionRequest request,
        SaveExamEditionHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(examId, id, request, cancellationToken);

        return result.IsSuccess ? Results.Ok(result.Value) : Problem(result.Error!);
    }

    private static async Task<IResult> DeleteEditionAsync(
        Guid examId,
        Guid id,
        DeleteExamEditionHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(examId, id, cancellationToken);

        return result.IsSuccess ? Results.NoContent() : Problem(result.Error!);
    }

    // The three enum-shaped filters arrive as text and an unreadable one is simply no filter: a list is a
    // read, and refusing it would turn a stale bookmark into an error page (BR14).
    private static async Task<IResult> ListAsync(
        IExamQueries queries,
        CancellationToken cancellationToken,
        int page = 0,
        int pageSize = ExamListQuery.DefaultPageSize,
        string? search = null,
        Guid? issuingAuthorityId = null,
        string? assessmentType = null,
        string? scope = null,
        string? sortBy = null,
        bool descending = false,
        string? assessmentTypeOrder = null,
        string? scopeOrder = null) =>
        Results.Ok(await queries.ListAsync(
            new ExamListQuery(
                page,
                pageSize,
                search,
                issuingAuthorityId,
                ParseFilter<AssessmentType>(assessmentType),
                ParseFilter<ExamScope>(scope),
                sortBy,
                descending,
                EnumOrder.Parse<AssessmentType>(assessmentTypeOrder),
                EnumOrder.Parse<ExamScope>(scopeOrder)),
            cancellationToken));

    private static async Task<IResult> FindAsync(Guid id, IExamQueries queries, CancellationToken cancellationToken)
    {
        var exam = await queries.FindAsync(id, cancellationToken);

        return exam is null
            ? Problem(new SharedKernel.Results.Error(CatalogErrorCodes.ExamNotFound, SharedKernel.Results.ErrorKind.NotFound))
            : Results.Ok(exam);
    }

    private static async Task<IResult> CreateAsync(
        SaveExamRequest request,
        SaveExamHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(null, request, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/v1/catalog/exams/{result.Value.Id}", result.Value)
            : Problem(result.Error!);
    }

    private static async Task<IResult> UpdateAsync(
        Guid id,
        SaveExamRequest request,
        SaveExamHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);

        return result.IsSuccess ? Results.Ok(result.Value) : Problem(result.Error!);
    }

    private static async Task<IResult> DeleteAsync(Guid id, DeleteExamHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);

        return result.IsSuccess ? Results.NoContent() : Problem(result.Error!);
    }

    private static TEnum? ParseFilter<TEnum>(string? value)
        where TEnum : struct, Enum =>
        Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed) ? parsed : null;
}
