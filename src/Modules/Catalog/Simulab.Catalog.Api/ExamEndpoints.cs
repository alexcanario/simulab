using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Simulab.Catalog.Application.Exams;
using Simulab.Catalog.Contracts;
using Simulab.Identity.Contracts;

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

        return group;
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
            ? CatalogEndpoints.Problem(new SharedKernel.Results.Error(CatalogErrorCodes.ExamNotFound, SharedKernel.Results.ErrorKind.NotFound))
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
            : CatalogEndpoints.Problem(result.Error!);
    }

    private static async Task<IResult> UpdateAsync(
        Guid id,
        SaveExamRequest request,
        SaveExamHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);

        return result.IsSuccess ? Results.Ok(result.Value) : CatalogEndpoints.Problem(result.Error!);
    }

    private static async Task<IResult> DeleteAsync(Guid id, DeleteExamHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);

        return result.IsSuccess ? Results.NoContent() : CatalogEndpoints.Problem(result.Error!);
    }

    private static TEnum? ParseFilter<TEnum>(string? value)
        where TEnum : struct, Enum =>
        Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed) ? parsed : null;
}
