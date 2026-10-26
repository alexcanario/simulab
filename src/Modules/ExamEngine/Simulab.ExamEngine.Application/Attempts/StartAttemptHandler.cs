using Simulab.ExamEngine.Contracts;
using Simulab.SharedKernel.Results;

namespace Simulab.ExamEngine.Application.Attempts;

/// <summary>Starts an attempt, or returns the one already in progress (F-166, UC1, BR1 to BR3).</summary>
public sealed class StartAttemptHandler
{
    public Task<Result<StartedAttempt>> HandleAsync(
        Guid studentId,
        StartAttemptRequest request,
        CancellationToken cancellationToken) =>
        throw new NotImplementedException();
}

/// <summary>An attempt and whether this call created it: 201 when it did, 200 when the student already had it.</summary>
public sealed record StartedAttempt(AttemptResponse Attempt, bool Created);
