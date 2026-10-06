namespace Simulab.Catalog.Contracts;

/// <summary>A subject as the list and the subject page show it (F-79).</summary>
/// <param name="Id">The subject.</param>
/// <param name="Name">The name as typed.</param>
/// <param name="AreaId">The area it belongs to, null when it has none.</param>
/// <param name="AreaCode">The area's stable code, null when it has none.</param>
/// <param name="TopicCount">How many topics it has, deleted ones excluded; the screen blocks the delete on it.</param>
public sealed record SubjectResponse(Guid Id, string Name, Guid? AreaId, string? AreaCode, int TopicCount);
