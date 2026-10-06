using Simulab.Catalog.Contracts;

namespace Simulab.Catalog.Application.NoticeSubjects;

/// <summary>The read side of an edition's notice subjects (F-74, UC1).</summary>
public interface INoticeSubjectQueries
{
    /// <summary>The edition's notice subjects in display order, all in one call.</summary>
    Task<IReadOnlyList<NoticeSubjectResponse>> ListAsync(Guid examEditionId, CancellationToken cancellationToken);
}
