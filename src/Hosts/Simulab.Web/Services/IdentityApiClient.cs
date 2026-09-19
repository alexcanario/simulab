using System.Globalization;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Simulab.Identity.Contracts;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Web.Services;

/// <summary>
/// What an API call gave back: the value, or the stable error code the UI turns into text. The Web
/// never branches on a message or on a status number (rule: ui).
/// </summary>
public sealed record ApiResult<T>(T? Value, string? ErrorCode)
{
    public bool IsSuccess => ErrorCode is null;
}

/// <summary>Builds an <see cref="ApiResult{T}"/>. Not on the generic type itself (CA1000).</summary>
public static class ApiResult
{
    public static ApiResult<T> Ok<T>(T? value) => new(value, null);

    public static ApiResult<T> Failed<T>(string code) => new(default, code);
}

/// <summary>
/// The Web's typed client for the Identity endpoints. The base address comes from service discovery
/// (rule: api-contracts), and every call carries the visitor's language so the API answers, and writes
/// the verification email, in it.
/// </summary>
public sealed class IdentityApiClient(HttpClient http)
{
    public const string ClientName = "api";

    private const string Base = "/api/v1/identity";

    public async Task<ApiResult<LegalDocumentResponse>> GetLegalDocumentAsync(LegalTopic topic, CancellationToken cancellationToken = default)
    {
        var slug = topic == LegalTopic.Terms ? "terms" : "privacy";
        return await SendAsync<LegalDocumentResponse>(
            () => new HttpRequestMessage(HttpMethod.Get, $"{Base}/legal-documents/{slug}"),
            cancellationToken);
    }

    public Task<ApiResult<bool>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default) =>
        PostAsync<RegisterRequest, bool>($"{Base}/registrations", request, cancellationToken);

    public Task<ApiResult<VerifyEmailResponse>> VerifyEmailAsync(string token, CancellationToken cancellationToken = default) =>
        PostAsync<VerifyEmailRequest, VerifyEmailResponse>($"{Base}/email-verifications", new VerifyEmailRequest(token), cancellationToken);

    public Task<ApiResult<bool>> ResendVerificationAsync(string email, CancellationToken cancellationToken = default) =>
        PostAsync<ResendVerificationRequest, bool>($"{Base}/email-verifications/resend", new ResendVerificationRequest(email), cancellationToken);

    /// <summary>F-6, BR9: the placeholder /admin/roles screen. The Api still checks the permission itself.</summary>
    public Task<ApiResult<RoleNamesResponse>> GetRoleNamesAsync(string accessToken, CancellationToken cancellationToken = default) =>
        SendAsync<RoleNamesResponse>(
            () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"{Base}/roles");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                return request;
            },
            cancellationToken);

    private Task<ApiResult<TResponse>> PostAsync<TRequest, TResponse>(string route, TRequest body, CancellationToken cancellationToken) =>
        SendAsync<TResponse>(
            () => new HttpRequestMessage(HttpMethod.Post, route)
            {
                Content = JsonContent.Create(body, options: AppJson.Options)
            },
            cancellationToken);

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
            return ApiResult.Failed<T>(Components.Ui.ErrorText.UnexpectedCode);
        }
    }

    /// <summary>The <c>code</c> of the problem details. Without one, the page shows the generic message.</summary>
    private static async Task<string> ReadCodeAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(AppJson.Options, cancellationToken);
            if (problem?.Extensions.TryGetValue("code", out var code) == true && code is JsonElement element
                && element.ValueKind == JsonValueKind.String)
            {
                return element.GetString() ?? Components.Ui.ErrorText.UnexpectedCode;
            }
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            // Not a problem details body; fall through to the generic message.
        }

        return Components.Ui.ErrorText.UnexpectedCode;
    }
}
