namespace Simulab.Catalog.Contracts;

/// <summary>A live topic with the subject it sits under, as another module sees it (F-95).</summary>
/// <param name="Id">The topic.</param>
/// <param name="Name">The topic's name.</param>
/// <param name="SubjectId">The subject it sits under.</param>
/// <param name="SubjectName">The subject's name.</param>
public sealed record TopicDirectoryEntry(Guid Id, string Name, Guid SubjectId, string SubjectName);
