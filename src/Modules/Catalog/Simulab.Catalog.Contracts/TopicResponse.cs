namespace Simulab.Catalog.Contracts;

/// <summary>A topic as the subject page shows it (F-79).</summary>
/// <param name="Id">The topic.</param>
/// <param name="SubjectId">The subject it sits under.</param>
/// <param name="Name">The name as typed.</param>
/// <param name="InUse">True when a live notice subject maps this topic (F-75, BR10); the screen blocks the delete on it.</param>
public sealed record TopicResponse(Guid Id, Guid SubjectId, string Name, bool InUse = false);
