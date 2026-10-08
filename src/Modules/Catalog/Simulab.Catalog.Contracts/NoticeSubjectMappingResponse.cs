namespace Simulab.Catalog.Contracts;

/// <summary>
/// One entry of a notice subject's mapping as the edition page shows it (F-75, BR13). The order is not part
/// of the contract: the screen sorts.
/// </summary>
/// <param name="SubjectId">The canonical subject: the one covered whole, or the topic's current subject (BR8).</param>
/// <param name="SubjectName">The subject's name as typed.</param>
/// <param name="TopicId">The canonical topic covered, or null when the entry is a whole subject.</param>
/// <param name="TopicName">The topic's name as typed, or null when the entry is a whole subject.</param>
public sealed record NoticeSubjectMappingResponse(Guid SubjectId, string SubjectName, Guid? TopicId, string? TopicName);
