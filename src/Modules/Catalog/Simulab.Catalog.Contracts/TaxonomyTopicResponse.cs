namespace Simulab.Catalog.Contracts;

/// <summary>A live topic inside the taxonomy the mapping picker reads (F-75).</summary>
public sealed record TaxonomyTopicResponse(Guid Id, string Name);
