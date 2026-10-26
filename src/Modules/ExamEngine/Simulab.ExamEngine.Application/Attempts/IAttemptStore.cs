using Simulab.ExamEngine.Domain.Entities;

namespace Simulab.ExamEngine.Application.Attempts;

/// <summary>Where attempts and their answers are kept (F-166). Implemented in Infrastructure over the module's context.</summary>
public interface IAttemptStore
{
    /// <summary>The student's attempt in progress on the edition, with its answers, or null when there is none (BR3).</summary>
    Task<Attempt?> FindInProgressAsync(Guid studentId, Guid examEditionId, CancellationToken cancellationToken);

    /// <summary>An attempt with its answers, read only; null when it does not exist or is not visible to the tenant (BR7, BR10).</summary>
    Task<Attempt?> FindAsync(Guid attemptId, CancellationToken cancellationToken);

    /// <summary>
    /// Adds a new attempt. False when the student already has one in progress on the edition (the unique rule of BR3 met
    /// a racing start): nothing was added and the caller reads the one in progress.
    /// </summary>
    Task<bool> TryAddAsync(Attempt attempt, CancellationToken cancellationToken);

    /// <summary>
    /// An attempt with its answers, to save or clear one of them (BR9): its row is locked in a shared way until
    /// <see cref="SaveChangesAsync"/>, so a submit waits for the saves in flight and a save that arrives after it sees the
    /// attempt submitted (BR6). Null as <see cref="FindAsync"/>.
    /// </summary>
    Task<Attempt?> FindForAnswerAsync(Guid attemptId, CancellationToken cancellationToken);

    /// <summary>An attempt with its answers, to submit: its row is locked until <see cref="SaveChangesAsync"/>. Null as <see cref="FindAsync"/>.</summary>
    Task<Attempt?> FindForSubmitAsync(Guid attemptId, CancellationToken cancellationToken);

    /// <summary>Saves what the rules changed and ends the transaction the lock opened.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
