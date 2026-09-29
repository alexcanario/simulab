namespace Simulab.Catalog.Contracts;

/// <summary>
/// The board and notice-year options that have something published: boards by acronym, years newest first
/// (F-36, BR7).
/// </summary>
public sealed record PublishedExamFiltersResponse(
    IReadOnlyList<PublishedExamOrganizerResponse> Organizers,
    IReadOnlyList<int> NoticeYears);
