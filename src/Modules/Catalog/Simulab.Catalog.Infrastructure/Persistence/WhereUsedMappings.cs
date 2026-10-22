using Microsoft.EntityFrameworkCore;
using Simulab.Catalog.Contracts;
using Simulab.Catalog.Domain.Entities;

namespace Simulab.Catalog.Infrastructure.Persistence;

/// <summary>
/// The rows of a "where used" dialog (F-90, BR5), shared by the subject and the topic reads. It starts from
/// <see cref="CatalogModuleDbContext.LiveNoticeSubjectMappings"/>, so "live" (F-75 BR9) is the one rule that also
/// fills <c>InUse</c>, and orders by exam name (normalized), notice year descending, position, then the notice
/// subject's own order. The join to the exam is inner: an exam with live editions cannot be deleted.
/// </summary>
internal static class WhereUsedMappings
{
    public static async Task<IReadOnlyList<WhereUsedMappingResponse>> ListAsync(
        CatalogModuleDbContext context,
        IQueryable<NoticeSubjectMapping> mappings,
        CancellationToken cancellationToken) =>
        await (
                from mapping in mappings
                join noticeSubject in context.NoticeSubjects on mapping.NoticeSubjectId equals noticeSubject.Id
                join edition in context.ExamEditions on noticeSubject.ExamEditionId equals edition.Id
                join exam in context.Exams on edition.ExamId equals exam.Id
                orderby exam.NormalizedName, edition.NoticeYear descending, edition.NormalizedPosition,
                    noticeSubject.DisplayOrder, noticeSubject.Id
                select new WhereUsedMappingResponse(
                    exam.Id,
                    exam.Name,
                    edition.Id,
                    edition.NoticeYear,
                    edition.Position,
                    noticeSubject.Id,
                    noticeSubject.Label))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
}
