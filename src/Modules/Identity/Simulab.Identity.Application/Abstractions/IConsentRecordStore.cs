using Simulab.Identity.Domain.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.Identity.Application.Abstractions;

/// <summary>
/// Writes and reads the consent evidence. There is no update and no delete on purpose (F-4 BR6): a new
/// acceptance is a new row, and the newest one decides what a user still owes (F-85 BR1).
/// </summary>
public interface IConsentRecordStore
{
    Task AddAsync(ConsentRecord record, CancellationToken cancellationToken = default);

    /// <summary>The user's most recent acceptance (by <c>AcceptedAt</c>, then <c>Id</c>), or null when there is none.</summary>
    Task<ConsentRecord?> FindLatestAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs the read-compare-write of an acceptance under a lock of this one user, so two requests at once
    /// write one row (F-85 BR4). A failed result rolls everything back.
    /// </summary>
    Task<Result<T>> RunExclusiveForUserAsync<T>(Guid userId, Func<Task<Result<T>>> work, CancellationToken cancellationToken = default);
}
