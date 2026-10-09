namespace Simulab.Catalog.Application.NoticeSubjects;

/// <summary>
/// The live taxonomy items a mapping request points at (F-75): the subject ids that exist, and for each topic
/// that exists its current subject (BR8: a topic entry follows its topic).
/// </summary>
/// <param name="SubjectIds">The requested subject ids that exist and are not deleted.</param>
/// <param name="TopicSubjects">Each requested topic that exists and is not deleted, with its current subject.</param>
public sealed record MappingTargets(IReadOnlySet<Guid> SubjectIds, IReadOnlyDictionary<Guid, Guid> TopicSubjects);
