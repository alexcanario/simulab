namespace Simulab.Catalog.Domain;

/// <summary>A mapping entry in its comparable form: exactly one of the two ids is set (F-75, BR1).</summary>
public readonly record struct MappingEntry(Guid? SubjectId, Guid? TopicId);
