namespace Simulab.Catalog.Contracts;

/// <summary>
/// The read side of the catalog for everyone who is not the back office (F-36, BR1 and BR11): the student
/// pages call it through the Api, and later modules (simulators, target exam, coach) call it directly. It
/// never returns an unpublished exam or a draft edition, whoever asks.
/// </summary>
public interface IPublishedExamQueries
{
    /// <summary>One row per published exam, by name, filtered and paged (BR2, BR3, BR5).</summary>
    Task<PublishedExamPageResponse> ListAsync(PublishedExamListQuery query, CancellationToken cancellationToken);

    /// <summary>A published exam with all its published editions, or null when there is none (BR8).</summary>
    Task<PublishedExamDetailResponse?> FindAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>The boards and notice years that have something published (BR7).</summary>
    Task<PublishedExamFiltersResponse> FiltersAsync(CancellationToken cancellationToken);
}
