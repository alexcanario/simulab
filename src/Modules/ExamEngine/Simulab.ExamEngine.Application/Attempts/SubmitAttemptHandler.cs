using Simulab.ExamEngine.Contracts;
using Simulab.SharedKernel.Results;

namespace Simulab.ExamEngine.Application.Attempts;

/// <summary>
/// Submits an attempt with the server's clock, whatever the caller sent (F-166, UC3, BR6, T8). A second submit answers
/// with the attempt as it was, so a retry of a lost response is safe.
/// </summary>
public sealed class SubmitAttemptHandler(IAttemptStore store, TimeProvider clock)
{
    public async Task<Result<AttemptResponse>> HandleAsync(Guid studentId, Guid attemptId, CancellationToken cancellationToken)
    {
        var attempt = await store.FindForSubmitAsync(studentId, attemptId, cancellationToken);
        if (attempt is null || attempt.StudentId != studentId)
        {
            return Result.Failure<AttemptResponse>(new Error(ExamAttemptErrorCodes.NotFound, ErrorKind.NotFound));
        }

        var submitted = attempt.Submit(clock.GetUtcNow());
        if (submitted.IsFailure)
        {
            return Result.Failure<AttemptResponse>(submitted.Error!);
        }

        await store.SaveChangesAsync(cancellationToken);

        return Result.Success(AttemptMapper.ToResponse(attempt));
    }
}
