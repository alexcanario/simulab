using Microsoft.EntityFrameworkCore;
using Simulab.Catalog.Contracts;

namespace Simulab.Catalog.Infrastructure.Persistence;

/// <summary>
/// F-168 BR4 on the read side: one batched query over the live notice subjects of the editions asked for (the
/// soft-delete filter keeps the deleted ones out), then <see cref="ObjectiveMaxScores.Compute"/> per edition, so the
/// rule is written once (design trade-off 10). An edition with no live subject is absent: its total is unknown.
/// </summary>
internal static class ObjectiveMaxScoreReads
{
    public static async Task<IReadOnlyDictionary<Guid, decimal?>> TotalsAsync(
        CatalogModuleDbContext context,
        IReadOnlyCollection<Guid> examEditionIds,
        CancellationToken cancellationToken)
    {
        if (examEditionIds.Count == 0)
        {
            return new Dictionary<Guid, decimal?>();
        }

        var rows = await context.NoticeSubjects.AsNoTracking()
            .Where(subject => examEditionIds.Contains(subject.ExamEditionId))
            .Select(subject => new { subject.ExamEditionId, subject.QuestionCount, subject.PointsPerQuestion })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(row => row.ExamEditionId)
            .ToDictionary(
                group => group.Key,
                group => ObjectiveMaxScores.Compute([.. group.Select(row => (row.QuestionCount, row.PointsPerQuestion))]).Total);
    }

    /// <summary>The total of one edition from the dictionary above, null when it is unknown.</summary>
    public static decimal? Of(this IReadOnlyDictionary<Guid, decimal?> totals, Guid examEditionId) =>
        totals.TryGetValue(examEditionId, out var total) ? total : null;
}
