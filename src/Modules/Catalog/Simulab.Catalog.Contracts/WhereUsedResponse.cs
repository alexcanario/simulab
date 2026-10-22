namespace Simulab.Catalog.Contracts;

/// <summary>
/// What makes a subject or a topic "in use" (F-90, BR1 to BR3): the live notice subjects that map it, in the
/// BR5 order (empty when none does, never null), and, for a topic, how many live rows of other modules point at
/// it (questions today, BR9); the count is 0 for a subject. Read-only.
/// </summary>
public sealed record WhereUsedResponse(IReadOnlyList<WhereUsedMappingResponse> Mappings, int QuestionCount);
