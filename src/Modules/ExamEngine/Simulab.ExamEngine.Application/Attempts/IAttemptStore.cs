using Simulab.ExamEngine.Domain.Entities;

namespace Simulab.ExamEngine.Application.Attempts;

/// <summary>Where attempts and their answers are kept (F-166). Implemented in Infrastructure over the module's context.</summary>
public interface IAttemptStore
{
    /// <summary>The student's attempt in progress on the edition, with its answers, or null when there is none (BR3).</summary>
    Task<Attempt?> FindInProgressAsync(Guid studentId, Guid examEditionId, CancellationToken cancellationToken);
}
