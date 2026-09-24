using Simulab.SharedKernel.Entities;

namespace Simulab.Ai;

/// <summary>
/// One call that reached the Claude API, whether it answered or failed (F-41, BR5). A call refused
/// before it left the app — no user, no quota, no key — is not a row here: nothing was spent.
/// The prices that produced <see cref="CostUsd"/> are kept on the row, so a later price change never
/// rewrites what the past cost (BR6).
/// </summary>
public sealed class AiCall : TenantEntity
{
    private AiCall()
    {
    }

    /// <summary>Who the call is charged to.</summary>
    public Guid UserId { get; private set; }

    /// <summary>What the call was for: one of <see cref="AiPurposes"/>.</summary>
    public string Purpose { get; private set; } = string.Empty;

    /// <summary>The model that was called.</summary>
    public string Model { get; private set; } = string.Empty;

    public int InputTokens { get; private set; }

    public int OutputTokens { get; private set; }

    /// <summary>US dollars per million input tokens, as configured when the call was made.</summary>
    public decimal InputPricePerMillion { get; private set; }

    /// <summary>US dollars per million output tokens, as configured when the call was made.</summary>
    public decimal OutputPricePerMillion { get; private set; }

    /// <summary>What the call cost, in US dollars.</summary>
    public decimal CostUsd { get; private set; }

    public int DurationMs { get; private set; }

    /// <summary>False when the API answered with an error or could not be reached.</summary>
    public bool Succeeded { get; private set; }

    /// <summary>The <see cref="AiErrorCodes"/> code of the failure, or null when the call succeeded.</summary>
    public string? ErrorCode { get; private set; }

    /// <summary>When the call left the app.</summary>
    public DateTimeOffset StartedAt { get; private set; }

    /// <summary>A call the model answered.</summary>
    public static AiCall ForAnswer(
        Guid userId,
        string purpose,
        string model,
        int inputTokens,
        int outputTokens,
        AiModelPrice price,
        TimeSpan duration,
        DateTimeOffset startedAt) =>
        new()
        {
            UserId = userId,
            Purpose = purpose,
            Model = model,
            InputTokens = inputTokens,
            OutputTokens = outputTokens,
            InputPricePerMillion = price.InputPerMillion,
            OutputPricePerMillion = price.OutputPerMillion,
            CostUsd = price.CostOf(inputTokens, outputTokens),
            DurationMs = (int)duration.TotalMilliseconds,
            Succeeded = true,
            StartedAt = startedAt
        };

    /// <summary>A call that left the app and came back as a failure. No tokens are known, so it cost nothing.</summary>
    public static AiCall ForFailure(
        Guid userId,
        string purpose,
        string model,
        string errorCode,
        TimeSpan duration,
        DateTimeOffset startedAt) =>
        new()
        {
            UserId = userId,
            Purpose = purpose,
            Model = model,
            ErrorCode = errorCode,
            DurationMs = (int)duration.TotalMilliseconds,
            Succeeded = false,
            StartedAt = startedAt
        };
}
