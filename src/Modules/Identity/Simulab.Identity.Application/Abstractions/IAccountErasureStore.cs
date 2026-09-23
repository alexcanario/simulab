using Simulab.Identity.Domain.Entities;

namespace Simulab.Identity.Application.Abstractions;

/// <summary>
/// The writes that erasing an account needs beyond the account row itself (F-10, BR8, BR9). They run
/// inside the role-administration transaction (<see cref="IRoleAdministrationStore.RunExclusiveAsync{T}"/>),
/// which is also what makes the last-manager check (BR11) safe against a role change happening at the
/// same time: both take the same lock.
/// </summary>
public interface IAccountErasureStore
{
    /// <summary>The account to erase, tracked for changes. Null when it is already gone.</summary>
    Task<User?> FindAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// BR4: writes the anonymized row, with the normalized columns recomputed from
    /// <paramref name="tombstoneAddress"/>, and soft deletes it. The interceptor fills the delete fields.
    /// </summary>
    void ApplyErasure(User user, string tombstoneAddress);

    /// <summary>BR8: drops the rows that exist only to serve this account, in the same transaction.</summary>
    Task RemoveAccountDataAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>BR9: keeps the consent records and clears their IP address.</summary>
    Task ClearConsentAddressesAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>F-21 BR9: keeps this account's events and clears their client address, the same way.</summary>
    Task ClearAccountEventAddressesAsync(Guid userId, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
