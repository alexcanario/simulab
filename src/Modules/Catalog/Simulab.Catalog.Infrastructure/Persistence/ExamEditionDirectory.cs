using Microsoft.EntityFrameworkCore;
using Simulab.Catalog.Contracts;
using Simulab.SharedKernel.Text;

namespace Simulab.Catalog.Infrastructure.Persistence;

/// <summary>
/// What another module may ask about exam editions and their notice subjects (F-96, BR9): live rows only, an
/// edition with its exam and organizer, never a deleted one. The global soft-delete filters leave out a
/// deleted edition, exam, organizer or notice subject, so an edition whose exam was deleted is absent too.
/// </summary>
public sealed class ExamEditionDirectory(CatalogModuleDbContext context) : IExamEditionDirectory
{
    public async Task<ExamEditionDirectoryEntry?> FindAsync(Guid examEditionId, CancellationToken cancellationToken)
    {
        var found = await FindManyAsync([examEditionId], cancellationToken);

        return found.Count > 0 ? found[0] : null;
    }

    public async Task<IReadOnlyList<ExamEditionDirectoryEntry>> FindManyAsync(
        IReadOnlyCollection<Guid> examEditionIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(examEditionIds);

        if (examEditionIds.Count == 0)
        {
            return [];
        }

        var ids = examEditionIds.ToList();

        return await Entries(context.ExamEditions.Where(edition => ids.Contains(edition.Id)))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ExamEditionDirectoryEntry>> SearchAsync(
        string? search,
        int take,
        CancellationToken cancellationToken)
    {
        var text = ComparableText.Normalize(search);

        // A year typed in full or in part ("2024", "202") matches the years that begin with it: a range,
        // because the number is never converted to text in the query.
        var (yearFrom, yearTo) = YearRange(text);

        var rows = from edition in context.ExamEditions
                   join exam in context.Exams on edition.ExamId equals exam.Id
                   join organizer in context.Organizers on edition.OrganizerId equals organizer.Id
                   where text.Length == 0
                       || exam.NormalizedName.Contains(text)
                       || organizer.NormalizedAcronym.Contains(text)
                       || organizer.NormalizedName.Contains(text)
                       || (edition.NoticeYear >= yearFrom && edition.NoticeYear <= yearTo)
                   orderby exam.NormalizedName, edition.NoticeYear descending, edition.Id
                   select new ExamEditionDirectoryEntry(
                       edition.Id,
                       exam.Id,
                       exam.Name,
                       organizer.Id,
                       organizer.Acronym,
                       organizer.Name,
                       edition.NoticeYear,
                       edition.Position,
                       edition.Status);

        return await rows.AsNoTracking().Take(Math.Max(take, 0)).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NoticeSubjectDirectoryEntry>> ListNoticeSubjectsAsync(
        Guid examEditionId,
        CancellationToken cancellationToken) =>
        await context.NoticeSubjects
            .AsNoTracking()
            .Where(subject => subject.ExamEditionId == examEditionId)
            .OrderBy(subject => subject.DisplayOrder)
            .ThenBy(subject => subject.CreatedAt)
            .ThenBy(subject => subject.Id)
            .Select(subject => new NoticeSubjectDirectoryEntry(
                subject.Id, subject.ExamEditionId, subject.Group, subject.Label, subject.QuestionCount))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<NoticeSubjectDirectoryEntry>> FindNoticeSubjectsAsync(
        IReadOnlyCollection<Guid> noticeSubjectIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(noticeSubjectIds);

        if (noticeSubjectIds.Count == 0)
        {
            return [];
        }

        var ids = noticeSubjectIds.ToList();

        return await context.NoticeSubjects
            .AsNoTracking()
            .Where(subject => ids.Contains(subject.Id))
            .Select(subject => new NoticeSubjectDirectoryEntry(
                subject.Id, subject.ExamEditionId, subject.Group, subject.Label, subject.QuestionCount))
            .ToListAsync(cancellationToken);
    }

    // The years that begin with the digits typed; an empty range (1, 0) when the text is not a number of one to
    // four digits.
    private static (int From, int To) YearRange(string text)
    {
        if (text.Length is < 1 or > 4 || !text.All(char.IsAsciiDigit))
        {
            return (1, 0);
        }

        var scale = (int)Math.Pow(10, 4 - text.Length);
        var from = int.Parse(text, System.Globalization.CultureInfo.InvariantCulture) * scale;

        return (from, from + scale - 1);
    }

    private IQueryable<ExamEditionDirectoryEntry> Entries(IQueryable<Domain.Entities.ExamEdition> editions) =>
        from edition in editions.AsNoTracking()
        join exam in context.Exams on edition.ExamId equals exam.Id
        join organizer in context.Organizers on edition.OrganizerId equals organizer.Id
        select new ExamEditionDirectoryEntry(
            edition.Id,
            exam.Id,
            exam.Name,
            organizer.Id,
            organizer.Acronym,
            organizer.Name,
            edition.NoticeYear,
            edition.Position,
            edition.Status);
}
