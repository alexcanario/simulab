namespace Simulab.Catalog.Contracts;

/// <summary>What the add and the edit dialog of a topic send (F-79).</summary>
/// <param name="Name">The topic's name, 2 to 200 characters.</param>
/// <param name="SubjectId">
/// Where the topic moves to (BR10). A create takes its subject from the route and ignores this; an update
/// with null keeps the current subject.
/// </param>
public sealed record SaveTopicRequest(string? Name, Guid? SubjectId = null);
