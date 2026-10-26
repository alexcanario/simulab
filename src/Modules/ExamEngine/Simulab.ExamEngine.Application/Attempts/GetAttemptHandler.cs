using Simulab.ExamEngine.Contracts;
using Simulab.SharedKernel.Results;

namespace Simulab.ExamEngine.Application.Attempts;

/// <summary>
/// Reads an attempt for its owner, or for a user holding <c>attempts.read</c> (F-166, UC4, UC5, BR7). Anyone else gets the
/// answer of an id that does not exist, so a caller cannot tell the two apart (T2).
/// </summary>
public sealed class GetAttemptHandler(IAttemptStore store, IAttemptAccess access)
{
    public async Task<Result<AttemptResponse>> HandleAsync(Guid userId, Guid attemptId, CancellationToken cancellationToken)
    {
        var attempt = await store.FindAsync(attemptId, cancellationToken);
        if (attempt is null || (attempt.StudentId != userId && !await access.CanReadAnyAsync(userId, cancellationToken)))
        {
            return Result.Failure<AttemptResponse>(new Error(ExamAttemptErrorCodes.NotFound, ErrorKind.NotFound));
        }

        return Result.Success(AttemptMapper.ToResponse(attempt));
    }
}
