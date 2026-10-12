using Microsoft.EntityFrameworkCore;
using Simulab.Catalog.Contracts;

namespace Simulab.QuestionBank.Infrastructure.Persistence;

/// <summary>
/// The question bank's answer to the catalog's edition delete guard (F-96, BR8): an edition is in use while
/// at least one live question of any status points at it. Deleted questions are left out by the soft-delete filter.
/// </summary>
public sealed class QuestionExamEditionUsageCheck(QuestionBankModuleDbContext context) : IExamEditionUsageCheck
{
    public async Task<IReadOnlySet<Guid>> InUseAsync(
        IReadOnlyCollection<Guid> examEditionIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(examEditionIds);

        if (examEditionIds.Count == 0)
        {
            return new HashSet<Guid>();
        }

        var ids = examEditionIds.Cast<Guid?>().ToList();
        var used = await context.Questions
            .AsNoTracking()
            .Where(question => ids.Contains(question.ExamEditionId))
            .Select(question => question.ExamEditionId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        return used.ToHashSet();
    }
}
