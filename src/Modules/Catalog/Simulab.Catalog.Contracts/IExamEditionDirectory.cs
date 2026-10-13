namespace Simulab.Catalog.Contracts;

/// <summary>
/// What another module may ask about exam editions and their notice subjects without reading the catalog's
/// tables (F-96, BR9). Implemented by the Catalog module over its own context; an edition or a notice subject
/// that does not exist or was deleted is simply absent, and so is an edition whose exam was deleted.
/// </summary>
public interface IExamEditionDirectory
{
    /// <summary>The edition of any status, or null when it does not exist or was deleted.</summary>
    Task<ExamEditionDirectoryEntry?> FindAsync(Guid examEditionId, CancellationToken cancellationToken);

    /// <summary>The live editions among <paramref name="examEditionIds"/>; unknown or deleted ids are left out.</summary>
    Task<IReadOnlyList<ExamEditionDirectoryEntry>> FindManyAsync(
        IReadOnlyCollection<Guid> examEditionIds,
        CancellationToken cancellationToken);

    /// <summary>
    /// Editions of any status whose exam name, organizer acronym or organizer name, or year, contain
    /// <paramref name="search"/> (case and accents ignored), by exam name then notice year descending, at most
    /// <paramref name="take"/>. An empty search lists the first ones.
    /// </summary>
    Task<IReadOnlyList<ExamEditionDirectoryEntry>> SearchAsync(
        string? search,
        int take,
        CancellationToken cancellationToken);

    /// <summary>The live notice subjects of the edition in display order.</summary>
    Task<IReadOnlyList<NoticeSubjectDirectoryEntry>> ListNoticeSubjectsAsync(
        Guid examEditionId,
        CancellationToken cancellationToken);

    /// <summary>The live notice subjects among <paramref name="noticeSubjectIds"/>; unknown or deleted ids are left out.</summary>
    Task<IReadOnlyList<NoticeSubjectDirectoryEntry>> FindNoticeSubjectsAsync(
        IReadOnlyCollection<Guid> noticeSubjectIds,
        CancellationToken cancellationToken);
}
