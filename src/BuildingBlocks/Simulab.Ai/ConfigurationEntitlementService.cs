using Microsoft.Extensions.Options;
using Simulab.Plans.Contracts;

namespace Simulab.Ai;

/// <summary>
/// A stand-in for the <c>Plans</c> module while it does not exist (F-41, decisions of 2026-09-24):
/// every user gets the same limits, read from the <c>Entitlements</c> section. It lives here because
/// the gateway is its only caller; the real module replaces the registration, and no caller changes.
/// </summary>
public sealed class ConfigurationEntitlementService(IOptions<EntitlementOptions> options) : IEntitlementService
{
    private readonly EntitlementOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

    public Task<bool> HasFeatureAsync(Guid userId, string feature, CancellationToken cancellationToken = default) =>
        Task.FromResult(_options.Features.Contains(feature, StringComparer.Ordinal));

    public Task<int?> GetLimitAsync(Guid userId, string feature, CancellationToken cancellationToken = default) =>
        Task.FromResult(_options.Limits.TryGetValue(feature, out var limit) ? limit : (int?)null);
}

/// <summary>
/// Settings of the stand-in, section <c>Entitlements</c>. A feature absent from <see cref="Limits"/>
/// has no ceiling, which is what an empty configuration means.
/// </summary>
public sealed class EntitlementOptions
{
    public const string SectionName = "Entitlements";

    /// <summary>The features every user has.</summary>
    public List<string> Features { get; } = [];

    /// <summary>The ceiling per feature name (<see cref="PlanFeatures"/>).</summary>
    public Dictionary<string, int> Limits { get; } = [];
}
