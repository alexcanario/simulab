namespace Simulab.QuestionBank.Contracts;

/// <summary>
/// A live subject with its live topics, both by name, for the filters of the question list (F-100). The same
/// shape the catalog's taxonomy has, held by the question bank so that a curator who cannot manage the catalog
/// can pick subjects and topics.
/// </summary>
/// <param name="Id">The subject.</param>
/// <param name="Name">The subject's name as typed.</param>
/// <param name="Topics">Its live topics by name; empty when it has none.</param>
public sealed record QuestionTaxonomySubjectResponse(Guid Id, string Name, IReadOnlyList<TopicLookupResponse> Topics);
