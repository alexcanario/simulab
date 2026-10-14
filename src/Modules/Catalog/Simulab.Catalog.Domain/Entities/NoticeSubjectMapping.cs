using Simulab.SharedKernel.Entities;

namespace Simulab.Catalog.Domain.Entities;

/// <summary>
/// One entry of a notice subject's mapping to the canonical taxonomy (F-75, BR1): either a whole
/// <see cref="Subject"/> or one <see cref="Topic"/>, never both and never neither — the table has a check
/// constraint for it. A topic entry stores the topic only, so it follows the topic when F-79 moves it to
/// another subject (BR8). A save replaces the mapping by a diff, so a dropped entry is soft-deleted and may be
/// added again (the unique index only holds live rows).
/// </summary>
public sealed class NoticeSubjectMapping : TenantEntity
{
    private NoticeSubjectMapping()
    {
    }

    /// <summary>The notice subject this entry belongs to.</summary>
    public Guid NoticeSubjectId { get; private set; }

    /// <summary>The canonical subject covered whole, or null when the entry is a topic.</summary>
    public Guid? SubjectId { get; private set; }

    /// <summary>The canonical topic covered, or null when the entry is a whole subject.</summary>
    public Guid? TopicId { get; private set; }

    /// <summary>An entry covering a whole subject.</summary>
    public static NoticeSubjectMapping ForSubject(Guid noticeSubjectId, Guid subjectId) =>
        new() { NoticeSubjectId = noticeSubjectId, SubjectId = subjectId };

    /// <summary>An entry covering one topic.</summary>
    public static NoticeSubjectMapping ForTopic(Guid noticeSubjectId, Guid topicId) =>
        new() { NoticeSubjectId = noticeSubjectId, TopicId = topicId };
}
