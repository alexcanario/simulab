using Microsoft.EntityFrameworkCore;
using Simulab.Catalog.Contracts;
using Simulab.Catalog.Domain;
using Simulab.Catalog.Domain.Entities;

namespace Simulab.Catalog.Infrastructure.Persistence;

/// <summary>
/// The student side of the catalog (F-36). An exam exists here only when at least one of its editions is
/// published (BR1); the soft-delete filter runs inside every correlated subquery, so a deleted edition, exam
/// or board never counts. The search is word by word over the normalized exam name, issuing authority name
/// and scope detail (BR3), and the board and year filters match on the same edition (BR5).
/// </summary>
public sealed class PublishedExamQueries(CatalogModuleDbContext context) : IPublishedExamQueries
{
    public async Task<PublishedExamPageResponse> ListAsync(PublishedExamListQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var sanitized = query.Sanitized();
        var rows = Published(sanitized.OrganizerId, sanitized.NoticeYear);

        foreach (var word in CatalogText.Normalize(sanitized.Search).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
        {
            var term = word;
            rows = rows.Where(row =>
                row.Exam.NormalizedName.Contains(term)
                || row.Authority.NormalizedName.Contains(term)
                || row.Exam.NormalizedScopeDetail.Contains(term));
        }

        if (sanitized.AssessmentType is { } assessmentType)
        {
            rows = rows.Where(row => row.Exam.AssessmentType == assessmentType);
        }

        if (sanitized.Scope is { } scope)
        {
            rows = rows.Where(row => row.Exam.Scope == scope);
        }

        var total = await rows.CountAsync(cancellationToken);

        var items = await Select(rows.OrderBy(row => row.Exam.NormalizedName).ThenBy(row => row.Exam.Id))
            .Skip(sanitized.Page * sanitized.PageSize)
            .Take(sanitized.PageSize)
            .ToListAsync(cancellationToken);

        return new PublishedExamPageResponse(items, total);
    }

    public async Task<PublishedExamDetailResponse?> FindAsync(Guid id, CancellationToken cancellationToken)
    {
        var exam = await Select(Published(null, null).Where(row => row.Exam.Id == id)).FirstOrDefaultAsync(cancellationToken);
        if (exam is null)
        {
            return null;
        }

        // BR8: all the published editions, whatever filters led here, in the back office order (F-35 BR14).
        var editions = await (
                from edition in context.ExamEditions.AsNoTracking()
                join organizer in context.Organizers on edition.OrganizerId equals organizer.Id
                where edition.ExamId == id && edition.Status == ExamEditionStatus.Published
                orderby edition.NoticeYear descending, edition.NormalizedPosition, organizer.NormalizedName
                select new PublishedExamEditionResponse(
                    edition.Id,
                    edition.NoticeYear,
                    edition.Position,
                    organizer.Name,
                    organizer.Acronym,
                    edition.NoticeReference,
                    edition.NoticeUrl,
                    edition.AppliedOn))
            .ToListAsync(cancellationToken);

        return new PublishedExamDetailResponse(
            exam.Id,
            exam.Name,
            exam.IssuingAuthorityName,
            exam.AssessmentType,
            exam.Scope,
            exam.ScopeDetail,
            exam.ContentLanguage,
            exam.PublishedEditionCount,
            exam.LatestNoticeYear,
            editions);
    }

    public async Task<PublishedExamFiltersResponse> FiltersAsync(CancellationToken cancellationToken)
    {
        // Joining the exams drops the editions of a deleted exam, and joining the boards a deleted board:
        // BR7 offers only what a student can really reach.
        var published =
            from edition in context.ExamEditions.AsNoTracking()
            join exam in context.Exams on edition.ExamId equals exam.Id
            where edition.Status == ExamEditionStatus.Published
            select edition;

        var organizers = await (
                from edition in published
                join organizer in context.Organizers on edition.OrganizerId equals organizer.Id
                select new { organizer.Id, organizer.Name, organizer.Acronym, organizer.NormalizedAcronym })
            .Distinct()
            .OrderBy(organizer => organizer.NormalizedAcronym)
            .ThenBy(organizer => organizer.Id)
            .ToListAsync(cancellationToken);

        var years = await published
            .Select(edition => edition.NoticeYear)
            .Distinct()
            .OrderByDescending(year => year)
            .ToListAsync(cancellationToken);

        return new PublishedExamFiltersResponse(
            [.. organizers.Select(organizer => new PublishedExamOrganizerResponse(organizer.Id, organizer.Name, organizer.Acronym))],
            years);
    }

    // The published exams with their issuing authority. The board and the year, when given, are terms of the
    // same edition test (BR5); with neither it is the plain "has a published edition" rule (BR1).
    private IQueryable<ExamRow> Published(Guid? organizerId, int? noticeYear) =>
        from exam in context.Exams.AsNoTracking()
        join authority in context.IssuingAuthorities on exam.IssuingAuthorityId equals authority.Id
        where context.ExamEditions.Any(edition =>
            edition.ExamId == exam.Id
            && edition.Status == ExamEditionStatus.Published
            && (organizerId == null || edition.OrganizerId == organizerId)
            && (noticeYear == null || edition.NoticeYear == noticeYear))
        select new ExamRow { Exam = exam, Authority = authority };

    // The count and the latest year always look at every published edition of the exam, not only the ones
    // the filters matched: the row says what the exam has, not why it was found.
    private IQueryable<PublishedExamResponse> Select(IQueryable<ExamRow> rows) =>
        rows.Select(row => new PublishedExamResponse(
            row.Exam.Id,
            row.Exam.Name,
            row.Authority.Name,
            row.Exam.AssessmentType,
            row.Exam.Scope,
            row.Exam.ScopeDetail,
            row.Exam.ContentLanguage,
            context.ExamEditions.Count(edition => edition.ExamId == row.Exam.Id && edition.Status == ExamEditionStatus.Published),
            context.ExamEditions
                .Where(edition => edition.ExamId == row.Exam.Id && edition.Status == ExamEditionStatus.Published)
                .Max(edition => edition.NoticeYear)));

    // Member-initialised (not a positional record) so EF Core can keep composing the query over it.
    private sealed class ExamRow
    {
        public required Exam Exam { get; init; }

        public required IssuingAuthority Authority { get; init; }
    }
}
