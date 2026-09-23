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
