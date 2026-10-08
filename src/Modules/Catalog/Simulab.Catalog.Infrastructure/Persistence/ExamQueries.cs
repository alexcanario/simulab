using Microsoft.EntityFrameworkCore;
using Simulab.Catalog.Application.Exams;
using Simulab.Catalog.Contracts;
using Simulab.Catalog.Domain;
using Simulab.Catalog.Domain.Entities;

namespace Simulab.Catalog.Infrastructure.Persistence;

/// <summary>
/// The exam list and the form page's read (F-34, UC1 and UC3). The search runs over the normalized name so
/// it ignores case and accents without a PostgreSQL extension (BR14); the three filters combine with AND;
/// the two columns whose label is translated are ranked by the order the caller sends (B-15), because only
/// the caller knows the reader's culture.
/// </summary>
public sealed class ExamQueries(CatalogModuleDbContext context) : IExamQueries
{
    public async Task<ExamPageResponse> ListAsync(ExamListQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var sanitized = query.Sanitized();
        var exams = context.Exams.AsNoTracking();

        var search = CatalogText.Normalize(sanitized.Search);
        if (search.Length > 0)
        {
            exams = exams.Where(exam => exam.NormalizedName.Contains(search));
        }

        if (sanitized.IssuingAuthorityId is { } authority)
        {
            exams = exams.Where(exam => exam.IssuingAuthorityId == authority);
        }

        if (sanitized.AssessmentType is { } assessmentType)
        {
            exams = exams.Where(exam => exam.AssessmentType == assessmentType);
        }

        if (sanitized.Scope is { } scope)
        {
            exams = exams.Where(exam => exam.Scope == scope);
        }

        // F-57 BR2: a state filter lists the State exams that store this acronym, never a National or Municipal one.
        if (BrazilianStates.FindByAcronym(sanitized.State) is { } state)
        {
            var acronym = state.Acronym;
            exams = exams.Where(exam => exam.Scope == ExamScope.State && exam.ScopeDetail == acronym);
        }

        var total = await exams.CountAsync(cancellationToken);

        var items = await Select(Sort(exams, sanitized))
            .Skip(sanitized.Page * sanitized.PageSize)
            .Take(sanitized.PageSize)
            .ToListAsync(cancellationToken);

        return new ExamPageResponse(items, total);
    }

    public Task<ExamResponse?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        Select(context.Exams.AsNoTracking().Where(exam => exam.Id == id)).FirstOrDefaultAsync(cancellationToken);

    // The issuing authority's name comes from the same query: the list draws it, and the form
    // page fills its picker with it, so a second round trip would buy nothing.
    private IQueryable<ExamResponse> Select(IQueryable<Exam> exams) =>
        from exam in exams
        join authority in context.IssuingAuthorities on exam.IssuingAuthorityId equals authority.Id
        select new ExamResponse(
            exam.Id,
            exam.Name,
            exam.IssuingAuthorityId,
            authority.Name,
            exam.AssessmentType,
            exam.Scope,
            exam.ScopeDetail,
            exam.ContentLanguage);

    // The name sort runs over the normalized name so it matches what the reader sees: "Ábaco" sorts with the
    // A's, not after Z as a raw byte comparison would put it (the F-33 lesson).
    private IQueryable<Exam> Sort(IQueryable<Exam> exams, ExamListQuery query) =>
        (query.SortBy, query.Descending) switch
        {
            (ExamSort.IssuingAuthority, var descending) => SortByAuthority(exams, descending),
            (ExamSort.AssessmentType, var descending) => SortByAssessmentType(exams, query.AssessmentTypeOrder, descending),
            (ExamSort.Scope, var descending) => SortByScope(exams, query.ScopeOrder, descending),
            (_, true) => exams.OrderByDescending(exam => exam.NormalizedName),
            _ => exams.OrderBy(exam => exam.NormalizedName)
        };

    private IQueryable<Exam> SortByAuthority(IQueryable<Exam> exams, bool descending)
    {
        var withAuthority =
            from exam in exams
            join authority in context.IssuingAuthorities on exam.IssuingAuthorityId equals authority.Id
            select new { Exam = exam, authority.NormalizedName };

        return (descending
                ? withAuthority.OrderByDescending(row => row.NormalizedName)
                : withAuthority.OrderBy(row => row.NormalizedName))
            .ThenBy(row => row.Exam.NormalizedName)
            .Select(row => row.Exam);
    }

    private static IQueryable<Exam> SortByAssessmentType(
        IQueryable<Exam> exams,
        IReadOnlyList<AssessmentType>? order,
        bool descending)
    {
        if (order is not { Count: > 0 })
        {
            return Fallback(exams, exam => exam.AssessmentType, descending);
        }

        // Ranking by the caller's order is a chain of "is it this one?" terms, first to last: PostgreSQL gets
        // one CASE per ORDER BY term, which it can index-scan or sort, unlike a dictionary lookup that would
        // have to run in memory. Descending is the same chain over the reversed order.
        var ranked = descending ? [.. order.Reverse()] : order;
        var first = ranked[0];
        var ordered = exams.OrderBy(exam => exam.AssessmentType == first ? 0 : 1);
        for (var index = 1; index < ranked.Count; index++)
        {
            var next = ranked[index];
            ordered = ordered.ThenBy(exam => exam.AssessmentType == next ? 0 : 1);
        }

        // The name tie-break inside a value always stays ascending, whichever way the column sorts.
        return ordered.ThenBy(exam => exam.NormalizedName);
    }

    private static IQueryable<Exam> SortByScope(IQueryable<Exam> exams, IReadOnlyList<ExamScope>? order, bool descending)
    {
        if (order is not { Count: > 0 })
        {
            return Fallback(exams, exam => exam.Scope, descending);
        }

        var ranked = descending ? [.. order.Reverse()] : order;
        var first = ranked[0];
        var ordered = exams.OrderBy(exam => exam.Scope == first ? 0 : 1);
        for (var index = 1; index < ranked.Count; index++)
        {
            var next = ranked[index];
            ordered = ordered.ThenBy(exam => exam.Scope == next ? 0 : 1);
        }

        return ordered.ThenBy(exam => exam.NormalizedName);
    }

    // Without an order, the stored name is the only thing left to sort by (B-15).
    private static IQueryable<Exam> Fallback<TKey>(
        IQueryable<Exam> exams,
        System.Linq.Expressions.Expression<Func<Exam, TKey>> key,
        bool descending) =>
        (descending ? exams.OrderByDescending(key) : exams.OrderBy(key)).ThenBy(exam => exam.NormalizedName);
}
