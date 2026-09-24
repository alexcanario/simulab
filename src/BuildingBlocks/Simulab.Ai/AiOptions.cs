using System.ComponentModel.DataAnnotations;

namespace Simulab.Ai;

/// <summary>
/// Settings of the gateway, section <c>Ai</c>. The key is never committed: user secrets in development,
/// an environment variable or the vault in the cloud. Without it the app still starts and every call
/// fails with <see cref="AiErrorCodes.NotConfigured"/> (F-41, BR4).
/// </summary>
public sealed class AiOptions
{
    public const string SectionName = "Ai";

    /// <summary>The model every purpose uses unless <see cref="Models"/> names another one (BR7).</summary>
    [Required]
    public string DefaultModel { get; set; } = "claude-opus-5";

    /// <summary>The API key. Empty means the gateway is not configured; it is not a validation failure.</summary>
    public string? ApiKey { get; set; }

    /// <summary>The largest answer the gateway asks for. Below the SDK timeout, so no call needs streaming.</summary>
    [Range(1, 128_000)]
    public int MaxTokens { get; set; } = 16_000;

    /// <summary>Model per purpose, for the purposes that do not use <see cref="DefaultModel"/> (BR7).</summary>
    public Dictionary<string, string> Models { get; } = [];

    /// <summary>
    /// Price per model, in US dollars per million tokens (BR6). A model with no price recorded costs
    /// zero on the row: the tokens are still recorded, and the missing price is visible.
    /// </summary>
    public Dictionary<string, AiModelPrice> Prices { get; } = [];

    /// <summary>The model <paramref name="purpose"/> calls (BR7).</summary>
    public string ModelFor(string purpose) =>
        Models.TryGetValue(purpose, out var model) && !string.IsNullOrWhiteSpace(model) ? model : DefaultModel;

    /// <summary>The price of <paramref name="model"/>, or zero when none is configured.</summary>
    public AiModelPrice PriceOf(string model) =>
        Prices.TryGetValue(model, out var price) ? price : new AiModelPrice();
}

/// <summary>What one million input or output tokens of a model cost, in US dollars.</summary>
public sealed class AiModelPrice
{
    [Range(0, 10_000)]
    public decimal InputPerMillion { get; set; }

    [Range(0, 10_000)]
    public decimal OutputPerMillion { get; set; }

    /// <summary>What <paramref name="inputTokens"/> and <paramref name="outputTokens"/> cost at these prices.</summary>
    public decimal CostOf(int inputTokens, int outputTokens) =>
        ((inputTokens * InputPerMillion) + (outputTokens * OutputPerMillion)) / 1_000_000m;
}
