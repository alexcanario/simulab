namespace Simulab.Catalog.Contracts;

/// <summary>What the add and the edit dialog of a subject send (F-79).</summary>
/// <param name="Name">The subject's name, 2 to 150 characters.</param>
/// <param name="AreaId">The area it belongs to, optional.</param>
public sealed record SaveSubjectRequest(string? Name, Guid? AreaId = null);
