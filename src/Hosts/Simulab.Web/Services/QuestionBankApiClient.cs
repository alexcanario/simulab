using System.Globalization;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Serialization;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Services;

/// <summary>
/// What a question bank call gave back: the value, or the stable error code the UI turns into text. A refusal
/// about one option also carries that option's position (F-95: the problem's <c>position</c> extension), so the
/// form marks the right row.
/// </summary>
public sealed record QuestionBankResult<T>(T? Value, string? ErrorCode, int? OptionPosition = null)
{
    public bool IsSuccess => ErrorCode is null;
}

/// <summary>Builds a <see cref="QuestionBankResult{T}"/>. Not on the generic type itself (CA1000).</summary>
public static class QuestionBankResult
{
    public static QuestionBankResult<T> Ok<T>(T? value) => new(value, null);

    public static QuestionBankResult<T> Failed<T>(string code, int? optionPosition = null) => new(default, code, optionPosition);
}

/// <summary>
/// The Web's typed client for the question bank endpoints (F-95). Same shape as <see cref="CatalogApiClient"/>: the
/// base address comes from service discovery (rule: api-contracts), every call carries the visitor's language, and
/// the result carries the stable error code, never a message.
/// </summary>
public sealed class QuestionBankApiClient(HttpClient http)
{
    private const string Base = "/api/v1/question-bank/questions";
    private const string EditionsBase = "/api/v1/question-bank/editions";

    /// <summary>F-95 UC5: one page of questions, newest first, filtered by status and by a word of the statement.</summary>
    public Task<QuestionBankResult<QuestionPageResponse>> ListQuestionsAsync(
        string accessToken,
        QuestionListQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var route = $"{Base}?page={query.Page.ToString(CultureInfo.InvariantCulture)}&pageSize={query.PageSize.ToString(CultureInfo.InvariantCulture)}";
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            route += $"&search={Uri.EscapeDataString(query.Search)}";
        }

        if (query.Status is { } status)
        {
            route += $"&status={status}";
        }

        return SendAsync<QuestionPageResponse>(() => Authorized(new HttpRequestMessage(HttpMethod.Get, route), accessToken), cancellationToken);
    }

    /// <summary>F-95 UC2: the question the form page edits, with its options and key.</summary>
    public Task<QuestionBankResult<QuestionResponse>> FindQuestionAsync(
        string accessToken,
        Guid questionId,
        CancellationToken cancellationToken = default) =>
        SendAsync<QuestionResponse>(() => Authorized(new HttpRequestMessage(HttpMethod.Get, $"{Base}/{questionId}"), accessToken), cancellationToken);

    /// <summary>F-95 UC1: creates a draft.</summary>
    public Task<QuestionBankResult<QuestionResponse>> CreateQuestionAsync(
        string accessToken,
        SaveQuestionRequest body,
        CancellationToken cancellationToken = default) =>
        SendAsync<QuestionResponse>(() => Authorized(WithJson(HttpMethod.Post, Base, body), accessToken), cancellationToken);

    /// <summary>F-95 UC2: replaces the content of a draft or a published question.</summary>
    public Task<QuestionBankResult<QuestionResponse>> UpdateQuestionAsync(
        string accessToken,
        Guid questionId,
        SaveQuestionRequest body,
        CancellationToken cancellationToken = default) =>
        SendAsync<QuestionResponse>(() => Authorized(WithJson(HttpMethod.Put, $"{Base}/{questionId}", body), accessToken), cancellationToken);

    /// <summary>F-95 UC3: changes the status only; refused with 400 when the question is incomplete (BR4).</summary>
    public Task<QuestionBankResult<QuestionResponse>> PublishQuestionAsync(
        string accessToken,
        Guid questionId,
        CancellationToken cancellationToken = default) =>
        SendAsync<QuestionResponse>(
            () => Authorized(new HttpRequestMessage(HttpMethod.Post, $"{Base}/{questionId}/publish"), accessToken),
            cancellationToken);

    /// <summary>F-95 UC4: back to a draft with the same content.</summary>
    public Task<QuestionBankResult<QuestionResponse>> UnpublishQuestionAsync(
        string accessToken,
        Guid questionId,
        CancellationToken cancellationToken = default) =>
        SendAsync<QuestionResponse>(
            () => Authorized(new HttpRequestMessage(HttpMethod.Post, $"{Base}/{questionId}/unpublish"), accessToken),
            cancellationToken);

    /// <summary>F-95 UC7: a soft delete on the server, refused with a 409 while the question is published.</summary>
    public Task<QuestionBankResult<bool>> DeleteQuestionAsync(
        string accessToken,
        Guid questionId,
        CancellationToken cancellationToken = default) =>
        SendAsync<bool>(() => Authorized(new HttpRequestMessage(HttpMethod.Delete, $"{Base}/{questionId}"), accessToken), cancellationToken);

    /// <summary>F-96 BR9: editions of any status the origin lookup offers, at most 20, by exam then year.</summary>
    public Task<QuestionBankResult<IReadOnlyList<ExamEditionLookupResponse>>> SearchEditionsAsync(
        string accessToken,
        string? search,
        CancellationToken cancellationToken = default)
    {
        var route = EditionsBase;
        if (!string.IsNullOrWhiteSpace(search))
        {
            route += $"?search={Uri.EscapeDataString(search)}";
        }

        return SendAsync<IReadOnlyList<ExamEditionLookupResponse>>(
            () => Authorized(new HttpRequestMessage(HttpMethod.Get, route), accessToken), cancellationToken);
    }

    /// <summary>B-28 BR1: subjects whose name contains the text, at most 20, by name.</summary>
    public Task<QuestionBankResult<IReadOnlyList<SubjectLookupResponse>>> SearchSubjectsAsync(
        string accessToken,
        string? search,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    /// <summary>B-28 BR2: the live topics of a subject by name.</summary>
    public Task<QuestionBankResult<IReadOnlyList<TopicLookupResponse>>> ListTopicsAsync(
        string accessToken,
        Guid subjectId,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    /// <summary>F-96 BR9: one edition, to prefill the form when it opens from the edition page.</summary>
    public Task<QuestionBankResult<ExamEditionLookupResponse>> FindEditionAsync(
        string accessToken,
        Guid examEditionId,
        CancellationToken cancellationToken = default) =>
        SendAsync<ExamEditionLookupResponse>(
            () => Authorized(new HttpRequestMessage(HttpMethod.Get, $"{EditionsBase}/{examEditionId}"), accessToken),
            cancellationToken);

    /// <summary>F-96 BR9: the notice subjects of an edition in display order.</summary>
    public Task<QuestionBankResult<IReadOnlyList<NoticeSubjectLookupResponse>>> ListNoticeSubjectsAsync(
        string accessToken,
        Guid examEditionId,
        CancellationToken cancellationToken = default) =>
        SendAsync<IReadOnlyList<NoticeSubjectLookupResponse>>(
            () => Authorized(new HttpRequestMessage(HttpMethod.Get, $"{EditionsBase}/{examEditionId}/notice-subjects"), accessToken),
            cancellationToken);

    /// <summary>F-96 BR10: one page of an edition's questions in paper order; the page is zero-based.</summary>
    public Task<QuestionBankResult<EditionQuestionPageResponse>> ListEditionQuestionsAsync(
        string accessToken,
        Guid examEditionId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var route = $"{EditionsBase}/{examEditionId}/questions"
            + $"?page={page.ToString(CultureInfo.InvariantCulture)}&pageSize={pageSize.ToString(CultureInfo.InvariantCulture)}";

        return SendAsync<EditionQuestionPageResponse>(
            () => Authorized(new HttpRequestMessage(HttpMethod.Get, route), accessToken), cancellationToken);
    }

    private static HttpRequestMessage WithJson<T>(HttpMethod method, string route, T body) =>
        new(method, route) { Content = JsonContent.Create(body, options: AppJson.Options) };

    private static HttpRequestMessage Authorized(HttpRequestMessage request, string accessToken)
    {
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        return request;
    }

    private async Task<QuestionBankResult<T>> SendAsync<T>(Func<HttpRequestMessage> create, CancellationToken cancellationToken)
    {
        try
        {
            using var request = create();
            request.Headers.AcceptLanguage.ParseAdd(CultureInfo.CurrentUICulture.Name);

            using var response = await http.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return response.StatusCode == HttpStatusCode.NoContent || response.Content.Headers.ContentLength is null or 0
                    ? QuestionBankResult.Ok<T>(default)
                    : QuestionBankResult.Ok(await response.Content.ReadFromJsonAsync<T>(AppJson.Options, cancellationToken));
            }

            return await ReadFailureAsync<T>(response, cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or TaskCanceledException)
        {
            // A broken call is not a business answer: the page shows the generic message.
            return QuestionBankResult.Failed<T>(ErrorText.UnexpectedCode);
        }
    }

    private static async Task<QuestionBankResult<T>> ReadFailureAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(AppJson.Options, cancellationToken);
            if (problem?.Extensions.TryGetValue("code", out var code) == true
                && code is JsonElement { ValueKind: JsonValueKind.String } element)
            {
                int? position = problem.Extensions.TryGetValue("position", out var raw)
                    && raw is JsonElement { ValueKind: JsonValueKind.Number } number
                    && number.TryGetInt32(out var value)
                        ? value
                        : null;

                return QuestionBankResult.Failed<T>(element.GetString() ?? ErrorText.UnexpectedCode, position);
            }
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            // Not a problem details body; fall through to the generic message.
        }

        return QuestionBankResult.Failed<T>(ErrorText.UnexpectedCode);
    }
}
