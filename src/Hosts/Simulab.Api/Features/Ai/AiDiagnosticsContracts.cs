namespace Simulab.Api.Features.Ai;

/// <summary>What the diagnostics page asks the gateway for (F-41, v2).</summary>
/// <param name="Purpose">One of the gateway's purposes; the page sends <c>diagnostics</c>.</param>
/// <param name="Prompt">The message to send to the model.</param>
public sealed record AiDiagnosticsRequest(string Purpose, string Prompt);

/// <summary>What the model answered, and what the call cost (F-41, v2).</summary>
public sealed record AiDiagnosticsResponse(
    string Text,
    string Model,
    int InputTokens,
    int OutputTokens,
    decimal CostUsd,
    int DurationMs);
