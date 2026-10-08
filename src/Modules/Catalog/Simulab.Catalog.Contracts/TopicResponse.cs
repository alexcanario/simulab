namespace Simulab.Catalog.Contracts;

/// <summary>A topic as the subject page shows it (F-79).</summary>
public sealed record TopicResponse(Guid Id, Guid SubjectId, string Name);
