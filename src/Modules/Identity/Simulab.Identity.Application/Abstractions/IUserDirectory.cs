using Simulab.Identity.Domain.Entities;

namespace Simulab.Identity.Application.Abstractions;

/// <summary>
/// Finds an account before anyone is signed in. It ignores the tenant filter on purpose: sign-up and
/// resend run with no tenant, and the email index is not filtered either, so a filtered lookup would
/// miss an existing account and let the insert fail on the unique index instead (BR3, Simulae bug #269).
/// </summary>
public interface IUserDirectory
{
    Task<User?> FindByEmailIgnoringTenantAsync(string email, CancellationToken cancellationToken = default);
}
