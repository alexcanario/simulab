using Microsoft.EntityFrameworkCore;
using Simulab.Catalog.Application.ExamEditions;
using Simulab.Catalog.Contracts;
using Simulab.Catalog.Domain.Entities;

namespace Simulab.Catalog.Infrastructure.Persistence;

/// <summary>
/// The editions section and the edition page's read (F-35, UC1 and UC3). The order runs over the normalized
/// position and the board's normalized name so it matches what the reader sees: an accented name sorts with
/// its letter, not after Z as a raw byte comparison would put it (the F-33 lesson).
/// </summary>
public sealed class ExamEditionQueries(CatalogModuleDbContext context) : IExamEditionQueries
{
    public async Task<IReadOnlyList<ExamEditionResponse>> ListAsync(Guid examId, CancellationToken cancellationToken)
    {
        var rows =
            from edition in context.ExamEditions.AsNoTracking()
            where edition.ExamId == examId
            join organizer in context.Organizers on edition.OrganizerId equals organizer.Id
            orderby edition.NoticeYear descending, edition.NormalizedPosition, organizer.NormalizedName
            select new { Edition = edition, Organizer = organizer };

        var items = await rows.ToListAsync(cancellationToken);
        var totals = await ObjectiveMaxScoreReads.TotalsAsync(context, [.. items.Select(row => row.Edition.Id)], cancellationToken);

        return [.. items.Select(row => ToResponse(row.Edition, row.Organizer, totals.Of(row.Edition.Id)))];
    }

    public async Task<ExamEditionResponse?> FindAsync(Guid examId, Guid id, CancellationToken cancellationToken)
    {
        var row = await (
                from edition in context.ExamEditions.AsNoTracking()
                where edition.ExamId == examId && edition.Id == id
                join organizer in context.Organizers on edition.OrganizerId equals organizer.Id
                select new { Edition = edition, Organizer = organizer })
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return null;
        }

        var totals = await ObjectiveMaxScoreReads.TotalsAsync(context, [row.Edition.Id], cancellationToken);

        return ToResponse(row.Edition, row.Organizer, totals.Of(row.Edition.Id));
    }

    // The board's name and acronym come from the same query: the section shows the acronym and the page fills
    // its picker with the name, so a second round trip would buy nothing.
    private static ExamEditionResponse ToResponse(ExamEdition edition, Organizer organizer, decimal? objectiveMaxScore) =>
        new(
            edition.Id,
            edition.ExamId,
            edition.OrganizerId,
            organizer.Name,
            organizer.Acronym,
            edition.NoticeYear,
            edition.Position,
            edition.NoticeReference,
            edition.NoticeUrl,
            edition.AppliedOn,
            edition.Status,
            edition.NoticeDocument is { } document ? new NoticeDocumentResponse(document.FileName, document.Size, document.UploadedAt) : null,
            edition.DurationMinutes,
            objectiveMaxScore,
            edition.ObjectiveCutOffScore,
            edition.EssayMaxScore,
            edition.EssayCutOffScore);
}
