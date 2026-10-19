using Simulab.Catalog.Contracts;

namespace Simulab.Catalog.Application.NoticeSubjects;

/// <summary>The read side of an edition's notice subjects (F-74, UC1).</summary>
public interface INoticeSubjectQueries
{
    /// <summary>The edition's notice subjects in display order, all in one call.</summary>
    Task<IReadOnlyList<NoticeSubjectResponse>> ListAsync(Guid examEditionId, CancellationToken cancellationToken);

    /// <summary>One notice subject of the edition with its mapping, or null when it does not exist or was deleted.</summary>
    Task<NoticeSubjectResponse?> FindAsync(Guid examEditionId, Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// The editions of the exam whose notice subjects may be copied into this one (F-86, BR1): not deleted, not the
    /// target itself, with at least one live notice subject, newest notice year first. Not paged.
    /// </summary>
    Task<IReadOnlyList<NoticeSubjectCopySourceResponse>> ListCopySourcesAsync(
        Guid examId,
        Guid targetEditionId,
        CancellationToken cancellationToken);
}
