namespace Simulab.Catalog.Contracts;

/// <summary>A live subject as another module sees it (B-28): its id and name, nothing of the taxonomy around it.</summary>
/// <param name="Id">The subject.</param>
/// <param name="Name">The subject's name as typed.</param>
public sealed record SubjectDirectoryEntry(Guid Id, string Name);
