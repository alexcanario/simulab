using Simulab.Catalog.Application.ExamEditions;
using Simulab.Catalog.Contracts;
using Simulab.SharedKernel.Results;

namespace Simulab.Catalog.Application.NoticeSubjects;

/// <summary>
/// Copies the live notice subjects of another edition of the same exam into an edition that has none (F-86).
/// </summary>
public sealed class CopyNoticeSubjectsHandler(INoticeSubjectStore store, IExamEditionStore editions)
{
    public Task<Result<CopyNoticeSubjectsResponse>> HandleAsync(
        Guid examId,
        Guid targetEditionId,
        CopyNoticeSubjectsRequest request,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();
}
