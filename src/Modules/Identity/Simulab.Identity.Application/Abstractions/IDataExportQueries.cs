using Simulab.Identity.Contracts;

namespace Simulab.Identity.Application.Abstractions;

/// <summary>Reads what Identity holds about one user for the data export (F-16 BR4).</summary>
public interface IDataExportQueries
{
    /// <summary>
    /// The Identity section for <paramref name="userId"/>, or null when the account is gone. The session count
    /// comes from the session store, not from the database, so the caller passes it in.
    /// </summary>
    Task<IdentityDataResponse?> GetIdentityDataAsync(Guid userId, int activeSessions, CancellationToken cancellationToken = default);
}
