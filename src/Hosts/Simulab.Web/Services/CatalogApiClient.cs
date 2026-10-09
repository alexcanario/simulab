using System.Globalization;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Simulab.Catalog.Contracts;
using Simulab.SharedKernel.Serialization;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Services;

/// <summary>
/// The Web's typed client for the Catalog endpoints (F-33). The base address comes from service
/// discovery (rule: api-contracts) and every call carries the visitor's language, so the Api answers
/// in it. The result carries the stable error code, never a message: the page localizes by code.
/// </summary>
public sealed class CatalogApiClient(HttpClient http)
{
    private const string Base = "/api/v1/catalog";

    /// <summary>F-33 UC1: one page of organizers, searched and sorted on the server.</summary>
    public Task<ApiResult<OrganizerPageResponse>> ListOrganizersAsync(
        string accessToken,
        OrganizerListQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var route = $"{Base}/organizers?page={query.Page}&pageSize={query.PageSize}&descending={(query.Descending ? "true" : "false")}";
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            route += $"&search={Uri.EscapeDataString(query.Search)}";
        }

        if (!string.IsNullOrWhiteSpace(query.SortBy))
        {
            route += $"&sortBy={Uri.EscapeDataString(query.SortBy)}";
        }

        if (query.KindOrder is { Count: > 0 } order)
        {
            route += $"&kindOrder={Uri.EscapeDataString(string.Join(',', order))}";
        }

        return SendAsync<OrganizerPageResponse>(() => Authorized(new HttpRequestMessage(HttpMethod.Get, route), accessToken), cancellationToken);
    }

    /// <summary>F-33 UC2.</summary>
    public Task<ApiResult<OrganizerResponse>> CreateOrganizerAsync(
        string accessToken,
        SaveOrganizerRequest body,
        CancellationToken cancellationToken = default) =>
        SendAsync<OrganizerResponse>(() => Authorized(WithJson(HttpMethod.Post, $"{Base}/organizers", body), accessToken), cancellationToken);

    /// <summary>F-33 UC3.</summary>
    public Task<ApiResult<OrganizerResponse>> UpdateOrganizerAsync(
        string accessToken,
        Guid organizerId,
        SaveOrganizerRequest body,
        CancellationToken cancellationToken = default) =>
        SendAsync<OrganizerResponse>(() => Authorized(WithJson(HttpMethod.Put, $"{Base}/organizers/{organizerId}", body), accessToken), cancellationToken);

    /// <summary>F-33 UC4: a soft delete on the server.</summary>
    public Task<ApiResult<bool>> DeleteOrganizerAsync(string accessToken, Guid organizerId, CancellationToken cancellationToken = default) =>
        SendAsync<bool>(() => Authorized(new HttpRequestMessage(HttpMethod.Delete, $"{Base}/organizers/{organizerId}"), accessToken), cancellationToken);

    /// <summary>F-34 v2: one page of issuing authorities. It is also what the exam form's picker calls.</summary>
    public Task<ApiResult<IssuingAuthorityPageResponse>> ListIssuingAuthoritiesAsync(
        string accessToken,
        IssuingAuthorityListQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var route = $"{Base}/issuing-authorities?page={query.Page}&pageSize={query.PageSize}&descending={(query.Descending ? "true" : "false")}";
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            route += $"&search={Uri.EscapeDataString(query.Search)}";
        }

        if (!string.IsNullOrWhiteSpace(query.SortBy))
        {
            route += $"&sortBy={Uri.EscapeDataString(query.SortBy)}";
        }

        return SendAsync<IssuingAuthorityPageResponse>(
            () => Authorized(new HttpRequestMessage(HttpMethod.Get, route), accessToken),
            cancellationToken);
    }

    /// <summary>F-34 v2.</summary>
    public Task<ApiResult<IssuingAuthorityResponse>> CreateIssuingAuthorityAsync(
        string accessToken,
        SaveIssuingAuthorityRequest body,
        CancellationToken cancellationToken = default) =>
        SendAsync<IssuingAuthorityResponse>(
            () => Authorized(WithJson(HttpMethod.Post, $"{Base}/issuing-authorities", body), accessToken),
            cancellationToken);

    /// <summary>F-34 v2.</summary>
    public Task<ApiResult<IssuingAuthorityResponse>> UpdateIssuingAuthorityAsync(
        string accessToken,
        Guid authorityId,
        SaveIssuingAuthorityRequest body,
        CancellationToken cancellationToken = default) =>
        SendAsync<IssuingAuthorityResponse>(
            () => Authorized(WithJson(HttpMethod.Put, $"{Base}/issuing-authorities/{authorityId}", body), accessToken),
            cancellationToken);

    /// <summary>F-34 v2: a soft delete on the server, refused with a 409 when the body still has exams.</summary>
    public Task<ApiResult<bool>> DeleteIssuingAuthorityAsync(
        string accessToken,
        Guid authorityId,
        CancellationToken cancellationToken = default) =>
        SendAsync<bool>(
            () => Authorized(new HttpRequestMessage(HttpMethod.Delete, $"{Base}/issuing-authorities/{authorityId}"), accessToken),
            cancellationToken);

    /// <summary>F-36 UC1 to UC3: one page of published exams, searched and filtered on the server.</summary>
    public Task<ApiResult<PublishedExamPageResponse>> ListPublishedExamsAsync(
        string accessToken,
        PublishedExamListQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var route = $"{Base}/published-exams?page={query.Page}&pageSize={query.PageSize}";
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            route += $"&search={Uri.EscapeDataString(query.Search)}";
        }

        if (query.AssessmentType is { } assessmentType)
        {
            route += $"&assessmentType={assessmentType}";
        }

        if (query.Scope is { } scope)
        {
            route += $"&scope={scope}";
        }

        if (query.OrganizerId is { } organizer)
        {
            route += $"&organizerId={organizer}";
        }

        if (query.NoticeYear is { } year)
        {
            route += $"&noticeYear={year.ToString(CultureInfo.InvariantCulture)}";
        }

        if (!string.IsNullOrWhiteSpace(query.State))
        {
            route += $"&state={Uri.EscapeDataString(query.State)}";
        }

        return SendAsync<PublishedExamPageResponse>(() => Authorized(new HttpRequestMessage(HttpMethod.Get, route), accessToken), cancellationToken);
    }

    /// <summary>F-36 UC4: a published exam with its published editions; 404 <c>exam.not_found</c> when there is none.</summary>
    public Task<ApiResult<PublishedExamDetailResponse>> FindPublishedExamAsync(
        string accessToken,
        Guid examId,
        CancellationToken cancellationToken = default) =>
        SendAsync<PublishedExamDetailResponse>(() => Authorized(new HttpRequestMessage(HttpMethod.Get, $"{Base}/published-exams/{examId}"), accessToken), cancellationToken);

    /// <summary>F-36 BR7: the boards and notice years that have something published.</summary>
    public Task<ApiResult<PublishedExamFiltersResponse>> GetPublishedExamFiltersAsync(
        string accessToken,
        CancellationToken cancellationToken = default) =>
        SendAsync<PublishedExamFiltersResponse>(() => Authorized(new HttpRequestMessage(HttpMethod.Get, $"{Base}/published-exam-filters"), accessToken), cancellationToken);

    /// <summary>F-34 UC1: one page of exams, searched, filtered and sorted on the server.</summary>
    public Task<ApiResult<ExamPageResponse>> ListExamsAsync(
        string accessToken,
        ExamListQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var route = $"{Base}/exams?page={query.Page}&pageSize={query.PageSize}&descending={(query.Descending ? "true" : "false")}";
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            route += $"&search={Uri.EscapeDataString(query.Search)}";
        }

        if (query.IssuingAuthorityId is { } authority)
        {
            route += $"&issuingAuthorityId={authority}";
        }

        if (query.AssessmentType is { } assessmentType)
        {
            route += $"&assessmentType={assessmentType}";
        }

        if (query.Scope is { } scope)
        {
            route += $"&scope={scope}";
        }

        if (!string.IsNullOrWhiteSpace(query.State))
        {
            route += $"&state={Uri.EscapeDataString(query.State)}";
        }

        if (!string.IsNullOrWhiteSpace(query.SortBy))
        {
            route += $"&sortBy={Uri.EscapeDataString(query.SortBy)}";
        }

        if (query.AssessmentTypeOrder is { Count: > 0 } assessmentTypeOrder)
        {
            route += $"&assessmentTypeOrder={Uri.EscapeDataString(string.Join(',', assessmentTypeOrder))}";
        }

        if (query.ScopeOrder is { Count: > 0 } scopeOrder)
        {
            route += $"&scopeOrder={Uri.EscapeDataString(string.Join(',', scopeOrder))}";
        }

        return SendAsync<ExamPageResponse>(() => Authorized(new HttpRequestMessage(HttpMethod.Get, route), accessToken), cancellationToken);
    }

    /// <summary>F-34 UC3: the exam the form page edits.</summary>
    public Task<ApiResult<ExamResponse>> FindExamAsync(string accessToken, Guid examId, CancellationToken cancellationToken = default) =>
        SendAsync<ExamResponse>(() => Authorized(new HttpRequestMessage(HttpMethod.Get, $"{Base}/exams/{examId}"), accessToken), cancellationToken);

    /// <summary>F-34 UC2.</summary>
    public Task<ApiResult<ExamResponse>> CreateExamAsync(
        string accessToken,
        SaveExamRequest body,
        CancellationToken cancellationToken = default) =>
        SendAsync<ExamResponse>(() => Authorized(WithJson(HttpMethod.Post, $"{Base}/exams", body), accessToken), cancellationToken);

    /// <summary>F-34 UC3.</summary>
    public Task<ApiResult<ExamResponse>> UpdateExamAsync(
        string accessToken,
        Guid examId,
        SaveExamRequest body,
        CancellationToken cancellationToken = default) =>
        SendAsync<ExamResponse>(() => Authorized(WithJson(HttpMethod.Put, $"{Base}/exams/{examId}", body), accessToken), cancellationToken);

    /// <summary>F-34 UC4: a soft delete on the server.</summary>
    public Task<ApiResult<bool>> DeleteExamAsync(string accessToken, Guid examId, CancellationToken cancellationToken = default) =>
        SendAsync<bool>(() => Authorized(new HttpRequestMessage(HttpMethod.Delete, $"{Base}/exams/{examId}"), accessToken), cancellationToken);

    /// <summary>F-35 UC1: the exam's editions in one call, newest year first (the Api orders them, BR14).</summary>
    public Task<ApiResult<IReadOnlyList<ExamEditionResponse>>> ListExamEditionsAsync(
        string accessToken,
        Guid examId,
        CancellationToken cancellationToken = default) =>
        SendAsync<IReadOnlyList<ExamEditionResponse>>(
            () => Authorized(new HttpRequestMessage(HttpMethod.Get, $"{Base}/exams/{examId}/editions"), accessToken),
            cancellationToken);

    /// <summary>F-35 UC3: the edition the edit page opens.</summary>
    public Task<ApiResult<ExamEditionResponse>> FindExamEditionAsync(
        string accessToken,
        Guid examId,
        Guid editionId,
        CancellationToken cancellationToken = default) =>
        SendAsync<ExamEditionResponse>(
            () => Authorized(new HttpRequestMessage(HttpMethod.Get, $"{Base}/exams/{examId}/editions/{editionId}"), accessToken),
            cancellationToken);

    /// <summary>F-35 UC2.</summary>
    public Task<ApiResult<ExamEditionResponse>> CreateExamEditionAsync(
        string accessToken,
        Guid examId,
        SaveExamEditionRequest body,
        CancellationToken cancellationToken = default) =>
        SendAsync<ExamEditionResponse>(
            () => Authorized(WithJson(HttpMethod.Post, $"{Base}/exams/{examId}/editions", body), accessToken),
            cancellationToken);

    /// <summary>F-35 UC3. The Status must be sent: a blank one is refused on a PUT (BR9).</summary>
    public Task<ApiResult<ExamEditionResponse>> UpdateExamEditionAsync(
        string accessToken,
        Guid examId,
        Guid editionId,
        SaveExamEditionRequest body,
        CancellationToken cancellationToken = default) =>
        SendAsync<ExamEditionResponse>(
            () => Authorized(WithJson(HttpMethod.Put, $"{Base}/exams/{examId}/editions/{editionId}", body), accessToken),
            cancellationToken);

    /// <summary>F-35 UC4 and UC5: a soft delete on the server, refused with a 409 while the edition is published.</summary>
    public Task<ApiResult<bool>> DeleteExamEditionAsync(
        string accessToken,
        Guid examId,
        Guid editionId,
        CancellationToken cancellationToken = default) =>
        SendAsync<bool>(
            () => Authorized(new HttpRequestMessage(HttpMethod.Delete, $"{Base}/exams/{examId}/editions/{editionId}"), accessToken),
            cancellationToken);

    /// <summary>F-74 UC1: the edition's notice subjects in display order (the Api orders them, BR6).</summary>
    public Task<ApiResult<IReadOnlyList<NoticeSubjectResponse>>> ListNoticeSubjectsAsync(
        string accessToken,
        Guid examId,
        Guid editionId,
        CancellationToken cancellationToken = default) =>
        SendAsync<IReadOnlyList<NoticeSubjectResponse>>(
            () => Authorized(new HttpRequestMessage(HttpMethod.Get, NoticeSubjectsRoute(examId, editionId)), accessToken),
            cancellationToken);

    /// <summary>F-74 UC2.</summary>
    public Task<ApiResult<NoticeSubjectResponse>> CreateNoticeSubjectAsync(
        string accessToken,
        Guid examId,
        Guid editionId,
        SaveNoticeSubjectRequest body,
        CancellationToken cancellationToken = default) =>
        SendAsync<NoticeSubjectResponse>(
            () => Authorized(WithJson(HttpMethod.Post, NoticeSubjectsRoute(examId, editionId), body), accessToken),
            cancellationToken);

    /// <summary>F-74 UC3.</summary>
    public Task<ApiResult<NoticeSubjectResponse>> UpdateNoticeSubjectAsync(
        string accessToken,
        Guid examId,
        Guid editionId,
        Guid noticeSubjectId,
        SaveNoticeSubjectRequest body,
        CancellationToken cancellationToken = default) =>
        SendAsync<NoticeSubjectResponse>(
            () => Authorized(WithJson(HttpMethod.Put, $"{NoticeSubjectsRoute(examId, editionId)}/{noticeSubjectId}", body), accessToken),
            cancellationToken);

    /// <summary>F-74 UC4: swaps the notice subject with its neighbour in the same group; "up" or "down".</summary>
    public Task<ApiResult<bool>> MoveNoticeSubjectAsync(
        string accessToken,
        Guid examId,
        Guid editionId,
        Guid noticeSubjectId,
        string direction,
        CancellationToken cancellationToken = default) =>
        SendAsync<bool>(
            () => Authorized(
                WithJson(HttpMethod.Post, $"{NoticeSubjectsRoute(examId, editionId)}/{noticeSubjectId}/move", new MoveNoticeSubjectRequest(direction)),
                accessToken),
            cancellationToken);

    /// <summary>F-74 UC5: a soft delete on the server.</summary>
    public Task<ApiResult<bool>> DeleteNoticeSubjectAsync(
        string accessToken,
        Guid examId,
        Guid editionId,
        Guid noticeSubjectId,
        CancellationToken cancellationToken = default) =>
        SendAsync<bool>(
            () => Authorized(new HttpRequestMessage(HttpMethod.Delete, $"{NoticeSubjectsRoute(examId, editionId)}/{noticeSubjectId}"), accessToken),
            cancellationToken);

    /// <summary>F-75: every live subject with its live topics, alphabetical, for the mapping picker (one call per dialog opening).</summary>
    public Task<ApiResult<IReadOnlyList<TaxonomySubjectResponse>>> GetTaxonomyAsync(
        string accessToken,
        CancellationToken cancellationToken = default) =>
        SendAsync<IReadOnlyList<TaxonomySubjectResponse>>(
            () => Authorized(new HttpRequestMessage(HttpMethod.Get, $"{Base}/taxonomy"), accessToken),
            cancellationToken);

    private static string NoticeSubjectsRoute(Guid examId, Guid editionId) =>
        $"{Base}/exams/{examId}/editions/{editionId}/notice-subjects";

    /// <summary>F-79 BR1: the seeded areas in display order; the names are the screen's.</summary>
    public Task<ApiResult<IReadOnlyList<AreaResponse>>> ListAreasAsync(
        string accessToken,
        CancellationToken cancellationToken = default) =>
        SendAsync<IReadOnlyList<AreaResponse>>(
            () => Authorized(new HttpRequestMessage(HttpMethod.Get, $"{Base}/areas"), accessToken),
            cancellationToken);

    /// <summary>F-79 UC1: one page of subjects, searched and filtered by area on the server.</summary>
    public Task<ApiResult<SubjectPageResponse>> ListSubjectsAsync(
        string accessToken,
        SubjectListQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var route = $"{Base}/subjects?page={query.Page}&pageSize={query.PageSize}";
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            route += $"&search={Uri.EscapeDataString(query.Search)}";
        }

        if (query.WithoutArea)
        {
            route += "&withoutArea=true";
        }
        else if (query.AreaId is { } areaId)
        {
            route += $"&areaId={areaId}";
        }

        return SendAsync<SubjectPageResponse>(
            () => Authorized(new HttpRequestMessage(HttpMethod.Get, route), accessToken),
            cancellationToken);
    }

    /// <summary>F-79 UC5: the subject the page opens.</summary>
    public Task<ApiResult<SubjectResponse>> FindSubjectAsync(
        string accessToken,
        Guid subjectId,
        CancellationToken cancellationToken = default) =>
        SendAsync<SubjectResponse>(
            () => Authorized(new HttpRequestMessage(HttpMethod.Get, $"{Base}/subjects/{subjectId}"), accessToken),
            cancellationToken);

    /// <summary>F-79 UC2.</summary>
    public Task<ApiResult<SubjectResponse>> CreateSubjectAsync(
        string accessToken,
        SaveSubjectRequest body,
        CancellationToken cancellationToken = default) =>
        SendAsync<SubjectResponse>(
            () => Authorized(WithJson(HttpMethod.Post, $"{Base}/subjects", body), accessToken),
            cancellationToken);

    /// <summary>F-79 UC3.</summary>
    public Task<ApiResult<SubjectResponse>> UpdateSubjectAsync(
        string accessToken,
        Guid subjectId,
        SaveSubjectRequest body,
        CancellationToken cancellationToken = default) =>
        SendAsync<SubjectResponse>(
            () => Authorized(WithJson(HttpMethod.Put, $"{Base}/subjects/{subjectId}", body), accessToken),
            cancellationToken);

    /// <summary>F-79 UC4: a soft delete on the server, refused with a 409 while the subject has topics.</summary>
    public Task<ApiResult<bool>> DeleteSubjectAsync(
        string accessToken,
        Guid subjectId,
        CancellationToken cancellationToken = default) =>
        SendAsync<bool>(
            () => Authorized(new HttpRequestMessage(HttpMethod.Delete, $"{Base}/subjects/{subjectId}"), accessToken),
            cancellationToken);

    /// <summary>F-79 UC5: the subject's topics in one call, alphabetically (the Api orders them, BR11).</summary>
    public Task<ApiResult<IReadOnlyList<TopicResponse>>> ListTopicsAsync(
        string accessToken,
        Guid subjectId,
        CancellationToken cancellationToken = default) =>
        SendAsync<IReadOnlyList<TopicResponse>>(
            () => Authorized(new HttpRequestMessage(HttpMethod.Get, $"{Base}/subjects/{subjectId}/topics"), accessToken),
            cancellationToken);

    /// <summary>F-79 UC6.</summary>
    public Task<ApiResult<TopicResponse>> CreateTopicAsync(
        string accessToken,
        Guid subjectId,
        SaveTopicRequest body,
        CancellationToken cancellationToken = default) =>
        SendAsync<TopicResponse>(
            () => Authorized(WithJson(HttpMethod.Post, $"{Base}/subjects/{subjectId}/topics", body), accessToken),
            cancellationToken);

    /// <summary>F-79 UC7: the body's subject is where the topic moves to (BR10).</summary>
    public Task<ApiResult<TopicResponse>> UpdateTopicAsync(
        string accessToken,
        Guid topicId,
        SaveTopicRequest body,
        CancellationToken cancellationToken = default) =>
        SendAsync<TopicResponse>(
            () => Authorized(WithJson(HttpMethod.Put, $"{Base}/topics/{topicId}", body), accessToken),
            cancellationToken);

    /// <summary>F-79 UC8: a soft delete on the server.</summary>
    public Task<ApiResult<bool>> DeleteTopicAsync(
        string accessToken,
        Guid topicId,
        CancellationToken cancellationToken = default) =>
        SendAsync<bool>(
            () => Authorized(new HttpRequestMessage(HttpMethod.Delete, $"{Base}/topics/{topicId}"), accessToken),
            cancellationToken);

    private static HttpRequestMessage WithJson<T>(HttpMethod method, string route, T body) =>
        new(method, route) { Content = JsonContent.Create(body, options: AppJson.Options) };

    private static HttpRequestMessage Authorized(HttpRequestMessage request, string accessToken)
    {
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        return request;
    }

    private async Task<ApiResult<T>> SendAsync<T>(Func<HttpRequestMessage> create, CancellationToken cancellationToken)
    {
        try
        {
            using var request = create();
            request.Headers.AcceptLanguage.ParseAdd(CultureInfo.CurrentUICulture.Name);

            using var response = await http.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return response.StatusCode == HttpStatusCode.NoContent || response.Content.Headers.ContentLength is null or 0
                    ? ApiResult.Ok<T>(default)
                    : ApiResult.Ok(await response.Content.ReadFromJsonAsync<T>(AppJson.Options, cancellationToken));
            }

            return ApiResult.Failed<T>(await ReadCodeAsync(response, cancellationToken));
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or TaskCanceledException)
        {
            // A broken call is not a business answer: the page shows the generic message.
            return ApiResult.Failed<T>(ErrorText.UnexpectedCode);
        }
    }

    private static async Task<string> ReadCodeAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(AppJson.Options, cancellationToken);
            if (problem?.Extensions.TryGetValue("code", out var code) == true
                && code is JsonElement { ValueKind: JsonValueKind.String } element)
            {
                return element.GetString() ?? ErrorText.UnexpectedCode;
            }
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            // Not a problem details body; fall through to the generic message.
        }

        return ErrorText.UnexpectedCode;
    }
}
