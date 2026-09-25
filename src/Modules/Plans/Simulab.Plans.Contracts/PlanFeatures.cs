namespace Simulab.Plans.Contracts;

/// <summary>
/// The feature names a plan can include or limit. One name, spelled once: a caller never passes a
/// literal to <see cref="IEntitlementService"/>.
/// </summary>
public static class PlanFeatures
{
    /// <summary>How many model calls a user may make in one calendar month (F-41, BR3).</summary>
    public const string AiCallsPerMonth = "ai.calls_per_month";
}
