namespace Simulab.Catalog.Contracts;

/// <summary>
/// The same inversion as <see cref="IExamEditionUsageCheck"/> for notice subjects (F-96, BR8): the catalog
/// asks every registered check before it deletes one.
/// </summary>
public interface INoticeSubjectUsageCheck
{
    /// <summary>The ids among <paramref name="noticeSubjectIds"/> that this module still uses with a live row.</summary>
    Task<IReadOnlySet<Guid>> InUseAsync(IReadOnlyCollection<Guid> noticeSubjectIds, CancellationToken cancellationToken);
}
