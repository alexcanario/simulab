namespace Simulab.Catalog.Contracts;

/// <summary>A live subject with its live topics, both alphabetical, for the mapping picker (F-75).</summary>
public sealed record TaxonomySubjectResponse(Guid Id, string Name, IReadOnlyList<TaxonomyTopicResponse> Topics);
