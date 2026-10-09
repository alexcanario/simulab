using Simulab.Identity.Application.Abstractions;
using Simulab.Jobs;

namespace Simulab.Identity.Infrastructure.Account;

/// <summary>Stages the <c>account.erased</c> job on the erasure's transaction (F-59 BR1).</summary>
public sealed class ErasureFollowUp(IJobQueue queue) : IErasureFollowUp
{
    public void StageAccountErased(Guid userId, DateTimeOffset erasedAt) =>
        queue.Enqueue(AccountErasedJob.Type, AccountErasedJob.Serialize(userId, erasedAt));
}
