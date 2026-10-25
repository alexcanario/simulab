using Simulab.Identity.Domain.Entities;

namespace Simulab.Identity.Application.Abstractions;

/// <summary>
/// Finds an account before anyone is signed in, whatever its tenant: sign-up and resend run with no tenant,
/// and the email index is not filtered either, so a lookup by tenant would miss an existing account and let
/// the insert fail on the unique index instead (BR3, Simulae bug #269). Since F-150 the user entity has no
/// tenant filter at all; the names keep saying so for the callers that depend on it.
/// </summary>
public interface IUserDirectory
{
    Task<User?> FindByEmailIgnoringTenantAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>The account an external login is linked to (F-20 BR3: the Google subject), with the same tenant rule.</summary>
    Task<User?> FindByLoginIgnoringTenantAsync(string loginProvider, string providerKey, CancellationToken cancellationToken = default);

    /// <summary>The key of the account's link to <paramref name="loginProvider"/>, or null when it has none (F-20 BR3).</summary>
    Task<string?> FindLoginKeyAsync(Guid userId, string loginProvider, CancellationToken cancellationToken = default);
}
