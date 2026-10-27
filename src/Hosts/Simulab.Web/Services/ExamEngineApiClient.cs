using System.Globalization;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Simulab.ExamEngine.Contracts;
using Simulab.SharedKernel.Serialization;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Services;

/// <summary>
/// The Web's typed client for the ExamEngine endpoints (F-169). The base address comes from service discovery (rule:
/// api-contracts) and every call carries the visitor's language. The result carries the stable error code, never a
/// message: the page localizes by code.
/// </summary>
public sealed class ExamEngineApiClient(HttpClient http)
{
    private const string Base = "/api/v1/exam-engine/attempts";

    /// <summary>F-169 BR1: starts an attempt on the edition, or answers the one in progress (F-166 BR3).</summary>
    public Task<ApiResult<AttemptResponse>> StartAsync(
        string accessToken,
        Guid examEditionId,
        CancellationToken cancellationToken = default) =>
        SendAsync<AttemptResponse>(
            () => Authorized(WithJson(HttpMethod.Post, Base, new StartAttemptRequest(examEditionId)), accessToken), cancellationToken);

    /// <summary>F-169 BR2, BR3: the questions of the attempt as the student reads them; the owner only (BR11).</summary>
    public Task<ApiResult<AttemptContentResponse>> ReadContentAsync(
        string accessToken,
        Guid attemptId,
        CancellationToken cancellationToken = default) =>
        SendAsync<AttemptContentResponse>(
            () => Authorized(new HttpRequestMessage(HttpMethod.Get, $"{Base}/{attemptId}/content"), accessToken), cancellationToken);

    /// <summary>F-169 BR6: saves or replaces the answer to one question; answers the question as stored.</summary>
    public Task<ApiResult<AttemptQuestionResponse>> SaveAnswerAsync(
        string accessToken,
        Guid attemptId,
        Guid questionId,
        AnswerPayload answer,
        CancellationToken cancellationToken = default) =>
        SendAsync<AttemptQuestionResponse>(
            () => Authorized(WithJson(HttpMethod.Put, $"{Base}/{attemptId}/answers/{questionId}", answer), accessToken), cancellationToken);

    /// <summary>F-169 BR6: clears the answer to one question.</summary>
    public Task<ApiResult<bool>> ClearAnswerAsync(
        string accessToken,
        Guid attemptId,
        Guid questionId,
        CancellationToken cancellationToken = default) =>
        SendAsync<bool>(
            () => Authorized(new HttpRequestMessage(HttpMethod.Delete, $"{Base}/{attemptId}/answers/{questionId}"), accessToken), cancellationToken);

    /// <summary>F-169 BR9: submits the attempt.</summary>
    public Task<ApiResult<AttemptResponse>> SubmitAsync(
        string accessToken,
        Guid attemptId,
        CancellationToken cancellationToken = default) =>
        SendAsync<AttemptResponse>(
            () => Authorized(new HttpRequestMessage(HttpMethod.Post, $"{Base}/{attemptId}/submit"), accessToken), cancellationToken);

    /// <summary>
    /// F-169 BR12: one picture of an attempt, streamed. The caller disposes the response; it reads the status, the type and
    /// the body itself, because the body is a file and not JSON.
    /// </summary>
    public async Task<HttpResponseMessage> OpenImageAsync(
        string accessToken,
        Guid attemptId,
        Guid imageId,
        CancellationToken cancellationToken = default)
    {
        using var request = Authorized(new HttpRequestMessage(HttpMethod.Get, $"{Base}/{attemptId}/images/{imageId}"), accessToken);
        request.Headers.AcceptLanguage.ParseAdd(CultureInfo.CurrentUICulture.Name);

        return await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
    }

    // The answer is the base type, so the "type" member that names its shape is written (F-166 BR5).
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
                return response.StatusCode == HttpStatusCode.NoContent || response.Content.Headers.ContentLength is 0
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
