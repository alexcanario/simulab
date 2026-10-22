namespace Simulab.Catalog.Contracts;

/// <summary>
/// What makes a subject or a topic "in use" (F-90, BR1 to BR3): the live notice subjects that map it, in the
/// BR5 order, and, for a topic, how many live rows of other modules point at it (questions today). Read-only.
/// </summary>
/// <param name="Mappings">The live notice subjects that map it; empty when none does. Never null.</param>
/// <param name="QuestionCount">The rows of other modules that use the topic (BR3, BR9); 0 for a subject.</param>
public sealed record WhereUsedResponse(IReadOnlyList<WhereUsedMappingResponse> Mappings, int QuestionCount);
