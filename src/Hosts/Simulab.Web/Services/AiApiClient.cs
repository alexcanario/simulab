using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Simulab.SharedKernel.Serialization;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Services;

/// <summary>
/// The Web's typed client for the Development-only diagnostics route (F-41, v2). The request and the
/// answer are declared here rather than in a contracts project: the route exists only in Development
/// and no module owns it, so two records beside the client are the whole contract.
/// </summary>
public sealed class AiApiClient(HttpClient http)
{
    private const string Route = "/api/v1/ai/diagnostics";

    /// <summary>F-41 UC2: one call to the model, and what it cost.</summary>
    public async Task<ApiResult<AiDiagnosticsAnswer>> SendAsync(
        string accessToken,
        string purpose,
        string prompt,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, Route)
            {
                Content = JsonContent.Create(new AiDiagnosticsBody(purpose, prompt), options: AppJson.Options)
            };
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.AcceptLanguage.ParseAdd(CultureInfo.CurrentUICulture.Name);

            using var response = await http.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return ApiResult.Ok(await response.Content.ReadFromJsonAsync<AiDiagnosticsAnswer>(AppJson.Options, cancellationToken));
            }

            return ApiResult.Failed<AiDiagnosticsAnswer>(await ReadCodeAsync(response, cancellationToken));
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or TaskCanceledException)
        {
            // A broken call is not a business answer: the page shows the generic message.
            return ApiResult.Failed<AiDiagnosticsAnswer>(ErrorText.UnexpectedCode);
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

    private sealed record AiDiagnosticsBody(string Purpose, string Prompt);
}

/// <summary>
/// What the Web needs to know about the gateway's vocabulary. The names are repeated here rather than
/// referenced: the Web never references a building block that carries EF Core and the Claude SDK.
/// The codes are also the resource keys the page shows (rule: ui).
/// </summary>
public static class AiCodes
{
    /// <summary>The purpose the diagnostics page sends.</summary>
    public const string DiagnosticsPurpose = "diagnostics";

    /// <summary>Nobody is signed in; the page stops before the call.</summary>
    public const string NoUser = "ai.no_user";
}

/// <summary>What the model answered, and what the call cost (F-41, v2).</summary>
public sealed record AiDiagnosticsAnswer(
    string Text,
    string Model,
    int InputTokens,
    int OutputTokens,
    decimal CostUsd,
    int DurationMs);
