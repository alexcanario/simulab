namespace Simulab.Plans.Contracts;

/// <summary>
/// What a user's plan includes (ADR-0001, decision 15e). RBAC decides who may act; the plan decides
/// what is included. No module or page reads a plan column: they ask here.
/// </summary>
/// <remarks>
/// F-41 adds the two members the AI gateway needs. <c>GetRemainingAsync</c> waits for the real
/// <c>Plans</c> module: what is left needs a usage meter per feature, and the meter for AI lives with
/// the <c>ai_calls</c> rows that hold the usage, not here.
/// </remarks>
public interface IEntitlementService
{
    /// <summary>True when the user's effective plan includes <paramref name="feature"/>.</summary>
    Task<bool> HasFeatureAsync(Guid userId, string feature, CancellationToken cancellationToken = default);

    /// <summary>
    /// The ceiling the user's effective plan sets for <paramref name="feature"/>, or null when the
    /// plan sets none. Null is no ceiling, not zero.
    /// </summary>
    Task<int?> GetLimitAsync(Guid userId, string feature, CancellationToken cancellationToken = default);
}
