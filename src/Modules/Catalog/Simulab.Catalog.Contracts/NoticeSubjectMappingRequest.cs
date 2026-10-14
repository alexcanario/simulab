namespace Simulab.Catalog.Contracts;

/// <summary>
/// One entry of a notice subject's mapping (F-75, BR1): a whole canonical subject or one canonical topic,
/// exactly one of the two. Both ids are nullable in the contract so a missing one is a validation error with
/// a code, never a silent default.
/// </summary>
/// <param name="SubjectId">The canonical subject covered whole, or null when the entry is a topic.</param>
/// <param name="TopicId">The canonical topic covered, or null when the entry is a whole subject.</param>
public sealed record NoticeSubjectMappingRequest(Guid? SubjectId = null, Guid? TopicId = null);
