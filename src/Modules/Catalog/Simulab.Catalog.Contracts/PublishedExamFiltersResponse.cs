namespace Simulab.Catalog.Contracts;

/// <summary>
/// The board, notice-year and state options that have something published: boards by acronym, years newest
/// first (F-36, BR7), and the acronyms of the states with a published <c>State</c> exam, in the order of
/// <see cref="BrazilianStates"/> (F-57 BR4).
/// </summary>
public sealed record PublishedExamFiltersResponse(
    IReadOnlyList<PublishedExamOrganizerResponse> Organizers,
    IReadOnlyList<int> NoticeYears,
    IReadOnlyList<string> States);
