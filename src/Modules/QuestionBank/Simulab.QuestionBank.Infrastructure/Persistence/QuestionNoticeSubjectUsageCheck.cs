using Microsoft.EntityFrameworkCore;
using Simulab.Catalog.Contracts;

namespace Simulab.QuestionBank.Infrastructure.Persistence;

/// <summary>
/// The question bank's answer to the catalog's notice subject delete guard (F-96, BR8): a notice subject is in
/// use while at least one live question of any status points at it.
/// </summary>
public sealed class QuestionNoticeSubjectUsageCheck(QuestionBankModuleDbContext context) : INoticeSubjectUsageCheck
{
    public async Task<IReadOnlySet<Guid>> InUseAsync(
        IReadOnlyCollection<Guid> noticeSubjectIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(noticeSubjectIds);

        if (noticeSubjectIds.Count == 0)
        {
            return new HashSet<Guid>();
        }

        var ids = noticeSubjectIds.Cast<Guid?>().ToList();
        var used = await context.Questions
            .AsNoTracking()
            .Where(question => ids.Contains(question.NoticeSubjectId))
            .Select(question => question.NoticeSubjectId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        return used.ToHashSet();
    }
}
