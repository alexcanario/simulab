using Microsoft.EntityFrameworkCore;
using Simulab.Catalog.Application.NoticeSubjects;
using Simulab.Catalog.Contracts;

namespace Simulab.Catalog.Infrastructure.Persistence;

/// <summary>An edition's notice subjects in display order (F-74, UC1), all in one call.</summary>
public sealed class NoticeSubjectQueries(CatalogModuleDbContext context) : INoticeSubjectQueries
{
    public async Task<IReadOnlyList<NoticeSubjectResponse>> ListAsync(
        Guid examEditionId,
        CancellationToken cancellationToken) =>
        await context.NoticeSubjects
            .AsNoTracking()
            .Where(subject => subject.ExamEditionId == examEditionId)
            .OrderBy(subject => subject.DisplayOrder)
            .ThenBy(subject => subject.CreatedAt)
            .ThenBy(subject => subject.Id)
            .Select(subject => new NoticeSubjectResponse(
                subject.Id,
                subject.ExamEditionId,
                subject.Group,
                subject.Label,
                subject.QuestionCount))
            .ToListAsync(cancellationToken);
}
