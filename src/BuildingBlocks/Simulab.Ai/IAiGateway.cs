using System.Text.Json;
using Simulab.Ai.Contracts;
using Simulab.SharedKernel.Results;

namespace Simulab.Ai;

/// <summary>
/// The one door to a model (ADR-0001, decision 22; F-41, BR1). It knows who is calling, refuses what
/// the plan does not allow, and writes down what every call cost. No other code talks to the Claude API.
/// </summary>
public interface IAiGateway
{
    /// <summary>
    /// Asks the model for one answer. Never throws for a failure of the call: the reasons a caller can
    /// act on come back as an <see cref="Error"/> with one of the <see cref="AiErrorCodes"/>.
    /// </summary>
    Task<Result<AiCompletion>> CompleteAsync(AiRequest request, CancellationToken cancellationToken = default);
}

/// <summary>What to ask the model.</summary>
/// <param name="Purpose">One of <see cref="AiPurposes"/>: what the call is for.</param>
/// <param name="Prompt">The user message.</param>
/// <param name="System">Instructions that frame the answer, or null.</param>
/// <param name="JsonSchema">
/// When set, the answer is constrained to this JSON schema (structured output) and
/// <see cref="AiCompletion.Text"/> holds the JSON document.
/// </param>
public sealed record AiRequest(
    string Purpose,
    string Prompt,
    string? System = null,
    IReadOnlyDictionary<string, JsonElement>? JsonSchema = null);

/// <summary>What the model answered, and what it cost.</summary>
/// <param name="Text">The answer; a JSON document when the request carried a schema.</param>
/// <param name="Model">The model that answered.</param>
/// <param name="InputTokens">Tokens the request spent, as the response reports them.</param>
/// <param name="OutputTokens">Tokens the answer spent, as the response reports them.</param>
/// <param name="CostUsd">What the call cost at the prices configured for <paramref name="Model"/>.</param>
/// <param name="Duration">How long the call took.</param>
public sealed record AiCompletion(
    string Text,
    string Model,
    int InputTokens,
    int OutputTokens,
    decimal CostUsd,
    TimeSpan Duration);
