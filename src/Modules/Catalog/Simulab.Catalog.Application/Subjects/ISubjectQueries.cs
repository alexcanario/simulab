using Simulab.Catalog.Contracts;

namespace Simulab.Catalog.Application.Subjects;

/// <summary>The read side of the subject list, the subject page and the area filter (F-79).</summary>
public interface ISubjectQueries
{
    /// <summary>The seeded areas in display order (BR1).</summary>
    Task<IReadOnlyList<AreaResponse>> ListAreasAsync(CancellationToken cancellationToken);

    /// <summary>One page of subjects by normalized name, with area and topic count (BR11, BR12).</summary>
    Task<SubjectPageResponse> ListAsync(SubjectListQuery query, CancellationToken cancellationToken);

    /// <summary>Every live subject with its live topics, both alphabetical, for the mapping picker (F-75, AC13).</summary>
    Task<IReadOnlyList<TaxonomySubjectResponse>> ListTaxonomyAsync(CancellationToken cancellationToken);

    /// <summary>The subject with this id, or null when it does not exist or was deleted.</summary>
    Task<SubjectResponse?> FindAsync(Guid id, CancellationToken cancellationToken);
}
