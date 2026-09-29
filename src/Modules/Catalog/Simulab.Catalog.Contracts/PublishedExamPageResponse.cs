namespace Simulab.Catalog.Contracts;

/// <summary>One page of the student catalog and how many published exams match in all (F-36, BR2).</summary>
public sealed record PublishedExamPageResponse(IReadOnlyList<PublishedExamResponse> Items, int Total);
