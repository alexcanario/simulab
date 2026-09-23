using Simulab.Identity.Contracts;

namespace Simulab.Identity.Application.Abstractions;

/// <summary>Reads the account event trail for the back office (F-21, UC2).</summary>
public interface IAccountEventQueries
{
    /// <summary>One page of the trail. <see cref="AccountEventListQuery.Days"/> and the event are already checked.</summary>
    Task<AccountEventPageResponse> ListAsync(AccountEventListQuery query, CancellationToken cancellationToken = default);
}
