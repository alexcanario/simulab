using System.Globalization;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Simulab.Identity.Contracts;
using Simulab.SharedKernel.Serialization;
using Simulab.Web.Services.Auth;

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
/// the verification email, in it. B-4: every call also carries the visitor's address, proven by the Web's
/// client secret, so the Api's per-client limits count each visitor, not the Web server.
/// </summary>
public sealed class IdentityApiClient(HttpClient http, VisitorContext visitor, IOptions<OpenIddictClientOptions> client)
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

    /// <summary>F-7 UC1: always a success within the client's limit; the answer never says whether the account exists.</summary>
    public Task<ApiResult<bool>> RequestPasswordResetAsync(string email, CancellationToken cancellationToken = default) =>
        PostAsync<RequestPasswordResetRequest, bool>($"{Base}/password-reset-requests", new RequestPasswordResetRequest(email), cancellationToken);

    /// <summary>F-7: whether a reset link still works, without using it.</summary>
    public Task<ApiResult<bool>> CheckPasswordResetTokenAsync(string token, CancellationToken cancellationToken = default) =>
        PostAsync<PasswordResetTokenCheckRequest, bool>($"{Base}/password-reset-token-checks", new PasswordResetTokenCheckRequest(token), cancellationToken);

    public Task<ApiResult<bool>> ResetPasswordAsync(string token, string newPassword, CancellationToken cancellationToken = default) =>
        PostAsync<ResetPasswordRequest, bool>($"{Base}/password-resets", new ResetPasswordRequest(token, newPassword), cancellationToken);

    /// <summary>
    /// F-7 UC6, the caller's own password. A lockout answer carries the seconds left, which the page counts
    /// down (BR8); every other failure is a code, like any other call.
    /// </summary>
    public async Task<PasswordChangeResult> ChangePasswordAsync(string accessToken, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{Base}/password-changes")
            {
                Content = JsonContent.Create(new ChangePasswordRequest(currentPassword, newPassword), options: AppJson.Options)
            };
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.AcceptLanguage.ParseAdd(CultureInfo.CurrentUICulture.Name);
            AddVisitor(request);

            using var response = await http.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return new PasswordChangeResult(null, null);
            }

            if (response.StatusCode == HttpStatusCode.Locked)
            {
                var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(AppJson.Options, cancellationToken);
                var seconds = problem?.Extensions.TryGetValue("retryAfterSeconds", out var value) == true && value is JsonElement { ValueKind: JsonValueKind.Number } element
                    ? element.GetInt32()
                    : 0;
                return new PasswordChangeResult(IdentityErrorCodes.AccountLocked, seconds);
            }

            return new PasswordChangeResult(await ReadCodeAsync(response, cancellationToken), null);
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or TaskCanceledException or NotSupportedException)
        {
            return new PasswordChangeResult(Components.Ui.ErrorText.UnexpectedCode, null);
        }
    }

    /// <summary>
    /// F-10 UC2: the caller erases their own account. The answer has the same two shapes as a password
    /// change — a code, or a lockout with the seconds left — because it is the same password check (BR2).
    /// </summary>
    public async Task<AccountErasureResult> EraseAccountAsync(string accessToken, string currentPassword, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{Base}/account-erasures")
            {
                Content = JsonContent.Create(new EraseAccountRequest(currentPassword), options: AppJson.Options)
            };
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.AcceptLanguage.ParseAdd(CultureInfo.CurrentUICulture.Name);
            AddVisitor(request);

            using var response = await http.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return new AccountErasureResult(null, null);
            }

            if (response.StatusCode == HttpStatusCode.Locked)
            {
                var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(AppJson.Options, cancellationToken);
                var seconds = problem?.Extensions.TryGetValue("retryAfterSeconds", out var value) == true && value is JsonElement { ValueKind: JsonValueKind.Number } element
                    ? element.GetInt32()
                    : 0;
                return new AccountErasureResult(IdentityErrorCodes.AccountLocked, seconds);
            }

            return new AccountErasureResult(await ReadCodeAsync(response, cancellationToken), null);
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or TaskCanceledException or NotSupportedException)
        {
            return new AccountErasureResult(Components.Ui.ErrorText.UnexpectedCode, null);
        }
    }

    /// <summary>F-9, UC1: every role with its permissions and user count. The Api checks the permission itself.</summary>
    public Task<ApiResult<List<RoleResponse>>> ListRolesAsync(string accessToken, CancellationToken cancellationToken = default) =>
        SendAsync<List<RoleResponse>>(() => Authorized(new HttpRequestMessage(HttpMethod.Get, $"{Base}/roles"), accessToken), cancellationToken);

    /// <summary>F-9: the permission catalog the role dialog offers.</summary>
    public Task<ApiResult<List<PermissionResponse>>> ListPermissionsAsync(string accessToken, CancellationToken cancellationToken = default) =>
        SendAsync<List<PermissionResponse>>(() => Authorized(new HttpRequestMessage(HttpMethod.Get, $"{Base}/permissions"), accessToken), cancellationToken);

    /// <summary>F-9, UC2.</summary>
    public Task<ApiResult<RoleResponse>> CreateRoleAsync(string accessToken, SaveRoleRequest body, CancellationToken cancellationToken = default) =>
        SendAsync<RoleResponse>(() => Authorized(WithJson(HttpMethod.Post, $"{Base}/roles", body), accessToken), cancellationToken);

    /// <summary>F-9, UC3: the name and the whole permission set.</summary>
    public Task<ApiResult<RoleResponse>> UpdateRoleAsync(string accessToken, Guid roleId, SaveRoleRequest body, CancellationToken cancellationToken = default) =>
        SendAsync<RoleResponse>(() => Authorized(WithJson(HttpMethod.Put, $"{Base}/roles/{roleId}", body), accessToken), cancellationToken);

    /// <summary>F-9, UC4.</summary>
    public Task<ApiResult<bool>> DeleteRoleAsync(string accessToken, Guid roleId, CancellationToken cancellationToken = default) =>
        SendAsync<bool>(() => Authorized(new HttpRequestMessage(HttpMethod.Delete, $"{Base}/roles/{roleId}"), accessToken), cancellationToken);

    /// <summary>F-9, UC5: one page of users, searched and filtered on the server.</summary>
    public Task<ApiResult<UserPageResponse>> ListUsersAsync(string accessToken, UserListQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var route = $"{Base}/users?page={query.Page}&pageSize={query.PageSize}&descending={(query.Descending ? "true" : "false")}";
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            route += $"&search={Uri.EscapeDataString(query.Search)}";
        }

        if (query.RoleId is { } roleId)
        {
            route += $"&roleId={roleId}";
        }

        if (!string.IsNullOrWhiteSpace(query.SortBy))
        {
            route += $"&sortBy={Uri.EscapeDataString(query.SortBy)}";
        }

        return SendAsync<UserPageResponse>(() => Authorized(new HttpRequestMessage(HttpMethod.Get, route), accessToken), cancellationToken);
    }

    /// <summary>F-9, UC6: the whole set of roles the user holds afterwards.</summary>
    public Task<ApiResult<UserSummaryResponse>> SetUserRolesAsync(string accessToken, Guid userId, IReadOnlyList<Guid> roleIds, CancellationToken cancellationToken = default) =>
        SendAsync<UserSummaryResponse>(
            () => Authorized(WithJson(HttpMethod.Put, $"{Base}/users/{userId}/roles", new SetUserRolesRequest(roleIds)), accessToken),
            cancellationToken);

    private static HttpRequestMessage WithJson<T>(HttpMethod method, string route, T body) =>
        new(method, route) { Content = JsonContent.Create(body, options: AppJson.Options) };

    /// <summary>F-8 UC1: the caller's own profile.</summary>
    public Task<ApiResult<ProfileResponse>> GetProfileAsync(string accessToken, CancellationToken cancellationToken = default) =>
        SendAsync<ProfileResponse>(() => Authorized(new HttpRequestMessage(HttpMethod.Get, $"{Base}/profile"), accessToken), cancellationToken);

    /// <summary>F-8 UC2: name and language together.</summary>
    public Task<ApiResult<bool>> UpdateProfileAsync(string accessToken, string? fullName, string preferredLanguage, CancellationToken cancellationToken = default) =>
        SendAsync<bool>(
            () => Authorized(
                new HttpRequestMessage(HttpMethod.Put, $"{Base}/profile")
                {
                    Content = JsonContent.Create(new UpdateProfileRequest(fullName, preferredLanguage), options: AppJson.Options)
                },
                accessToken),
            cancellationToken);

    /// <summary>F-8 UC3: the header switch saves the language alone (BR6).</summary>
    public Task<ApiResult<bool>> UpdatePreferredLanguageAsync(string accessToken, string preferredLanguage, CancellationToken cancellationToken = default) =>
        SendAsync<bool>(
            () => Authorized(
                new HttpRequestMessage(HttpMethod.Put, $"{Base}/profile/preferred-language")
                {
                    Content = JsonContent.Create(new UpdatePreferredLanguageRequest(preferredLanguage), options: AppJson.Options)
                },
                accessToken),
            cancellationToken);

    /// <summary>
    /// The Api answers the two-factor routes with 404 while <c>Identity:TotpEnabled</c> is false (F-11 BR12): the Web
    /// reads that as "the feature is off" and shows nothing of it. A marker, never shown to anyone.
    /// </summary>
    public const string TotpSwitchedOffCode = "totp.switched_off";

    /// <summary>F-11: whether two-factor is on for the caller, or <see cref="TotpSwitchedOffCode"/>.</summary>
    public async Task<ApiResult<TotpStatusResponse>> GetTotpStatusAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = Authorized(new HttpRequestMessage(HttpMethod.Get, $"{Base}/totp"), accessToken);
            request.Headers.AcceptLanguage.ParseAdd(CultureInfo.CurrentUICulture.Name);
            AddVisitor(request);

            using var response = await http.SendAsync(request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return ApiResult.Failed<TotpStatusResponse>(TotpSwitchedOffCode);
            }

            return response.IsSuccessStatusCode
                ? ApiResult.Ok(await response.Content.ReadFromJsonAsync<TotpStatusResponse>(AppJson.Options, cancellationToken))
                : ApiResult.Failed<TotpStatusResponse>(await ReadCodeAsync(response, cancellationToken));
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or TaskCanceledException)
        {
            return ApiResult.Failed<TotpStatusResponse>(Components.Ui.ErrorText.UnexpectedCode);
        }
    }

    /// <summary>F-11 UC1: a new secret and its QR code; two-factor stays off until confirmed.</summary>
    public Task<ApiResult<TotpEnrolmentResponse>> StartTotpEnrolmentAsync(string accessToken, CancellationToken cancellationToken = default) =>
        SendAsync<TotpEnrolmentResponse>(() => Authorized(new HttpRequestMessage(HttpMethod.Post, $"{Base}/totp/enrolments"), accessToken), cancellationToken);

    /// <summary>F-11 UC1, UC2: the first code turns two-factor on and brings the ten recovery codes.</summary>
    public Task<LockableResult<RecoveryCodesResponse>> ConfirmTotpAsync(string accessToken, string code, CancellationToken cancellationToken = default) =>
        SendLockableAsync<RecoveryCodesResponse>(HttpMethod.Post, $"{Base}/totp/enrolments/confirmations", new ConfirmTotpRequest(code), accessToken, cancellationToken);

    /// <summary>F-11 UC5.</summary>
    public Task<LockableResult<RecoveryCodesResponse>> RegenerateRecoveryCodesAsync(string accessToken, string code, CancellationToken cancellationToken = default) =>
        SendLockableAsync<RecoveryCodesResponse>(HttpMethod.Post, $"{Base}/totp/recovery-codes", new RegenerateRecoveryCodesRequest(code), accessToken, cancellationToken);

    /// <summary>F-11 UC6: the password and a code, both.</summary>
    public Task<LockableResult<bool>> DisableTotpAsync(string accessToken, string currentPassword, string code, CancellationToken cancellationToken = default) =>
        SendLockableAsync<bool>(HttpMethod.Delete, $"{Base}/totp", new DisableTotpRequest(currentPassword, code), accessToken, cancellationToken);

    /// <summary>A call whose failure may be the sign-in lockout: 423 carries the seconds left (F-7 BR8, F-11 BR10).</summary>
    private async Task<LockableResult<T>> SendLockableAsync<T>(HttpMethod method, string route, object body, string accessToken, CancellationToken cancellationToken)
    {
        try
        {
            using var request = Authorized(new HttpRequestMessage(method, route) { Content = JsonContent.Create(body, body.GetType(), options: AppJson.Options) }, accessToken);
            request.Headers.AcceptLanguage.ParseAdd(CultureInfo.CurrentUICulture.Name);
            AddVisitor(request);

            using var response = await http.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var value = response.StatusCode == HttpStatusCode.NoContent || response.Content.Headers.ContentLength is 0
                    ? default
                    : await response.Content.ReadFromJsonAsync<T>(AppJson.Options, cancellationToken);
                return new LockableResult<T>(value, null, null);
            }

            if (response.StatusCode == HttpStatusCode.Locked)
            {
                var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(AppJson.Options, cancellationToken);
                var seconds = problem?.Extensions.TryGetValue("retryAfterSeconds", out var raw) == true && raw is JsonElement { ValueKind: JsonValueKind.Number } element
                    ? element.GetInt32()
                    : 0;
                return new LockableResult<T>(default, IdentityErrorCodes.AccountLocked, seconds);
            }

            return new LockableResult<T>(default, await ReadCodeAsync(response, cancellationToken), null);
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or TaskCanceledException or NotSupportedException)
        {
            return new LockableResult<T>(default, Components.Ui.ErrorText.UnexpectedCode, null);
        }
    }

    private static HttpRequestMessage Authorized(HttpRequestMessage request, string accessToken)
    {
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        return request;
    }

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
            AddVisitor(request);

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

    /// <summary>B-4: the visitor's address and the proof that it comes from the Web. Nothing when the address is unknown.</summary>
    private void AddVisitor(HttpRequestMessage request)
    {
        if (string.IsNullOrEmpty(visitor.Address) || string.IsNullOrEmpty(client.Value.ClientSecret))
        {
            return;
        }

        request.Headers.Add(ClientAddressHeaders.Address, visitor.Address);
        request.Headers.Add(ClientAddressHeaders.Secret, client.Value.ClientSecret);
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
