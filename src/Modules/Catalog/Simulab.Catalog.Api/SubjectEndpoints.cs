using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Simulab.Catalog.Application.Subjects;
using Simulab.Catalog.Application.Topics;
using Simulab.Catalog.Contracts;
using Simulab.Identity.Contracts;
using Simulab.SharedKernel.Results;
using static Simulab.ApiResults.ApiProblem;

namespace Simulab.Catalog.Api;

/// <summary>
/// The subjects and topics back office (F-79, BR15): the seeded areas, the subjects and, under each subject,
/// its topics. Every route requires <c>catalog.manage</c> (F-33 BR4); a caller without it gets 403
/// <c>identity.forbidden</c> from the shared permission pipeline. A topic is updated and deleted by its own
/// id, so a move to another subject does not depend on the old parent in the route (BR10).
/// </summary>
public static class SubjectEndpoints
{
    internal static RouteGroupBuilder MapSubjectEndpoints(this RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        var policy = PermissionPolicy.NameFor(CatalogPermissions.Manage);

        group.MapGroup("/areas")
            .RequireAuthorization(policy)
            .MapGet(string.Empty, ListAreasAsync)
            .WithName("ListAreas");

        group.MapGroup("/taxonomy")
            .RequireAuthorization(policy)
            .MapGet(string.Empty, GetTaxonomyAsync)
            .WithName("GetTaxonomy");

        var subjects = group.MapGroup("/subjects").RequireAuthorization(policy);
        subjects.MapGet(string.Empty, ListSubjectsAsync).WithName("ListSubjects");
        subjects.MapGet("/{id:guid}", FindSubjectAsync).WithName("FindSubject");
        subjects.MapPost(string.Empty, CreateSubjectAsync).WithName("CreateSubject");
        subjects.MapPut("/{id:guid}", UpdateSubjectAsync).WithName("UpdateSubject");
        subjects.MapDelete("/{id:guid}", DeleteSubjectAsync).WithName("DeleteSubject");
        subjects.MapGet("/{subjectId:guid}/topics", ListTopicsAsync).WithName("ListTopics");
        subjects.MapPost("/{subjectId:guid}/topics", CreateTopicAsync).WithName("CreateTopic");

        var topics = group.MapGroup("/topics").RequireAuthorization(policy);
        topics.MapPut("/{id:guid}", UpdateTopicAsync).WithName("UpdateTopic");
        topics.MapDelete("/{id:guid}", DeleteTopicAsync).WithName("DeleteTopic");

        return group;
    }

    private static async Task<IResult> ListAreasAsync(ISubjectQueries queries, CancellationToken cancellationToken) =>
        Results.Ok(await queries.ListAreasAsync(cancellationToken));

    private static async Task<IResult> GetTaxonomyAsync(ISubjectQueries queries, CancellationToken cancellationToken) =>
        Results.Ok(await queries.ListTaxonomyAsync(cancellationToken));

    private static async Task<IResult> ListSubjectsAsync(
        ISubjectQueries queries,
        CancellationToken cancellationToken,
        int page = 0,
        int pageSize = SubjectListQuery.DefaultPageSize,
        string? search = null,
        Guid? areaId = null,
        bool withoutArea = false) =>
        Results.Ok(await queries.ListAsync(
            new SubjectListQuery(page, pageSize, search, areaId, withoutArea),
            cancellationToken));

    private static async Task<IResult> FindSubjectAsync(
        Guid id,
        ISubjectQueries queries,
        CancellationToken cancellationToken)
    {
        var subject = await queries.FindAsync(id, cancellationToken);

        return subject is null
            ? Problem(new Error(CatalogErrorCodes.SubjectNotFound, ErrorKind.NotFound))
            : Results.Ok(subject);
    }

    private static async Task<IResult> CreateSubjectAsync(
        SaveSubjectRequest request,
        SaveSubjectHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(null, request, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/v1/catalog/subjects/{result.Value.Id}", result.Value)
            : Problem(result.Error!);
    }

    private static async Task<IResult> UpdateSubjectAsync(
        Guid id,
        SaveSubjectRequest request,
        SaveSubjectHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);

        return result.IsSuccess ? Results.Ok(result.Value) : Problem(result.Error!);
    }

    private static async Task<IResult> DeleteSubjectAsync(
        Guid id,
        DeleteSubjectHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);

        return result.IsSuccess ? Results.NoContent() : Problem(result.Error!);
    }

    private static async Task<IResult> ListTopicsAsync(
        Guid subjectId,
        ISubjectQueries subjects,
        ITopicQueries topics,
        CancellationToken cancellationToken)
    {
        // A subject that does not exist has no topics to list: the caller is told, not given an empty list.
        if (await subjects.FindAsync(subjectId, cancellationToken) is null)
        {
            return Problem(new Error(CatalogErrorCodes.SubjectNotFound, ErrorKind.NotFound));
        }

        return Results.Ok(await topics.ListAsync(subjectId, cancellationToken));
    }

    private static async Task<IResult> CreateTopicAsync(
        Guid subjectId,
        SaveTopicRequest request,
        SaveTopicHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(subjectId, null, request, cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/v1/catalog/subjects/{subjectId}/topics", result.Value)
            : Problem(result.Error!);
    }

    private static async Task<IResult> UpdateTopicAsync(
        Guid id,
        SaveTopicRequest request,
        SaveTopicHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(null, id, request, cancellationToken);

        return result.IsSuccess ? Results.Ok(result.Value) : Problem(result.Error!);
    }

    private static async Task<IResult> DeleteTopicAsync(
        Guid id,
        DeleteTopicHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);

        return result.IsSuccess ? Results.NoContent() : Problem(result.Error!);
    }
}
