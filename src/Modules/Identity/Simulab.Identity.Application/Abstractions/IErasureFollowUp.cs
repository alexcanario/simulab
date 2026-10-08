namespace Simulab.Identity.Application.Abstractions;

/// <summary>
/// What an erasure still owes after it commits (F-59): revoking every session again and telling the rest of
/// the system (<c>UserErased</c>). It is staged on the erasure's own transaction, so a crash right after the
/// commit leaves a job the worker finishes, and a rollback leaves nothing.
/// </summary>
public interface IErasureFollowUp
{
    /// <summary>Stages the follow-up job. It is written by the <c>SaveChanges</c> that saves the erasure.</summary>
    void StageAccountErased(Guid userId, DateTimeOffset erasedAt);
}
