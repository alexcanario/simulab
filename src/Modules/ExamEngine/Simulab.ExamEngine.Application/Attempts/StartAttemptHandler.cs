using Simulab.ExamEngine.Contracts;
using Simulab.ExamEngine.Domain.Entities;
using Simulab.SharedKernel.Results;
using Simulab.SharedKernel.Security;

namespace Simulab.ExamEngine.Application.Attempts;

/// <summary>Starts an attempt, or returns the one already in progress (F-166, UC1, BR1 to BR3, BR10).</summary>
public sealed class StartAttemptHandler(
    IAttemptStore store,
    IAttemptSource source,
    ICurrentTenant currentTenant,
    TimeProvider clock)
{
    public async Task<Result<StartedAttempt>> HandleAsync(
        Guid studentId,
        StartAttemptRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.ExamEditionId is not { } editionId)
        {
            return Result.Failure<StartedAttempt>(new Error(ExamAttemptErrorCodes.EditionNotFound, ErrorKind.Validation));
        }

        if (await store.FindInProgressAsync(studentId, editionId, cancellationToken) is { } inProgress)
        {
            return Result.Success(new StartedAttempt(AttemptMapper.ToResponse(inProgress), Created: false));
        }

        var edition = await source.FindEditionAsync(editionId, cancellationToken);
        if (edition is not { IsPublished: true })
        {
            return Result.Failure<StartedAttempt>(new Error(ExamAttemptErrorCodes.EditionNotFound, ErrorKind.NotFound));
        }

        var questions = await source.ListPublishedQuestionsAsync(editionId, cancellationToken);
        if (questions.Count == 0)
        {
            return Result.Failure<StartedAttempt>(new Error(ExamAttemptErrorCodes.EditionWithoutQuestions, ErrorKind.BusinessRule));
        }

        var attempt = Attempt.Start(studentId, editionId, currentTenant.TenantId, clock.GetUtcNow(), questions);
        if (await store.TryAddAsync(attempt, cancellationToken))
        {
            return Result.Success(new StartedAttempt(AttemptMapper.ToResponse(attempt), Created: true));
        }

        // A start that arrived at the same time won the unique rule: its attempt is the one in progress.
        var winner = await store.FindInProgressAsync(studentId, editionId, cancellationToken)
            ?? throw new InvalidOperationException("The unique rule of the attempt in progress refused a row and none was found.");

        return Result.Success(new StartedAttempt(AttemptMapper.ToResponse(winner), Created: false));
    }
}

/// <summary>An attempt and whether this call created it: 201 when it did, 200 when the student already had it.</summary>
public sealed record StartedAttempt(AttemptResponse Attempt, bool Created);
