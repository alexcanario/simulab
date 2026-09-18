using Simulab.Identity.Domain.Entities;

namespace Simulab.Identity.Application.Abstractions;

/// <summary>Writes the consent evidence. There is no update and no delete on purpose (BR6).</summary>
public interface IConsentRecordStore
{
    Task AddAsync(ConsentRecord record, CancellationToken cancellationToken = default);
}
