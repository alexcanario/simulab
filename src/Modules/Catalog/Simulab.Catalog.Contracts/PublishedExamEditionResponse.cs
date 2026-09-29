namespace Simulab.Catalog.Contracts;

/// <summary>One published edition on the exam page (F-36, BR8). It is always published, so it carries no status.</summary>
public sealed record PublishedExamEditionResponse(
    Guid Id,
    int NoticeYear,
    string? Position,
    string OrganizerName,
    string OrganizerAcronym,
    string? NoticeReference,
    string? NoticeUrl,
    DateOnly? AppliedOn);
