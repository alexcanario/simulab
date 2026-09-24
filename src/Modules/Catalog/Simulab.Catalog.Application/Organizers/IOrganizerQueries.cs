using Simulab.Catalog.Contracts;

namespace Simulab.Catalog.Application.Organizers;

/// <summary>The read side of the organizer list (F-33, UC1 and BR12).</summary>
public interface IOrganizerQueries
{
    Task<OrganizerPageResponse> ListAsync(OrganizerListQuery query, CancellationToken cancellationToken);
}
