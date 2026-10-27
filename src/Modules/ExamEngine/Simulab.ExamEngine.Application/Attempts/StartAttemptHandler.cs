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
    public async Task<StartAttemptOutcome> HandleAsync(
        Guid studentId,
        StartAttemptRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // B-35: no id and the empty id are a malformed body, told apart from an unknown edition before any lookup.
        if (request.ExamEditionId is not { } editionId || editionId == Guid.Empty)
        {
            return StartAttemptOutcome.EditionRequired;
        }

        if (await store.FindInProgressAsync(studentId, editionId, cancellationToken) is { } inProgress)
        {
            return StartAttemptOutcome.Started(new StartedAttempt(AttemptMapper.ToResponse(inProgress), Created: false));
        }

        var edition = await source.FindEditionAsync(editionId, cancellationToken);
        if (edition is not { IsPublished: true })
        {
            return StartAttemptOutcome.Failed(new Error(ExamAttemptErrorCodes.EditionNotFound, ErrorKind.NotFound));
        }

        var questions = await source.ListPublishedQuestionsAsync(editionId, cancellationToken);
        if (questions.Count == 0)
        {
            return StartAttemptOutcome.Failed(new Error(ExamAttemptErrorCodes.EditionWithoutQuestions, ErrorKind.BusinessRule));
        }

        var attempt = Attempt.Start(studentId, editionId, currentTenant.TenantId, clock.GetUtcNow(), questions);
        if (await store.TryAddAsync(attempt, cancellationToken))
        {
            return StartAttemptOutcome.Started(new StartedAttempt(AttemptMapper.ToResponse(attempt), Created: true));
        }

        // A start that arrived at the same time won the unique rule: its attempt is the one in progress.
        var winner = await store.FindInProgressAsync(studentId, editionId, cancellationToken)
            ?? throw new InvalidOperationException("The unique rule of the attempt in progress refused a row and none was found.");

        return StartAttemptOutcome.Started(new StartedAttempt(AttemptMapper.ToResponse(winner), Created: false));
    }
}

/// <summary>An attempt and whether this call created it: 201 when it did, 200 when the student already had it.</summary>
public sealed record StartedAttempt(AttemptResponse Attempt, bool Created);

/// <summary>
/// What a start answers: the attempt, or the failure; a body with no edition comes with its one field entry (B-35), in the
/// shape of <see cref="SaveAnswerOutcome"/>.
/// </summary>
public sealed record StartAttemptOutcome(
    StartedAttempt? Attempt,
    Error? Error,
    IReadOnlyList<AnswerFieldError> Fields)
{
    public bool IsSuccess => Error is null;

    public static StartAttemptOutcome EditionRequired { get; } = new(
        null,
        new Error(ExamAttemptErrorCodes.EditionRequired, ErrorKind.Validation),
        [new AnswerFieldError("examEditionId", ExamAttemptErrorCodes.EditionRequired)]);

    public static StartAttemptOutcome Started(StartedAttempt attempt) => new(attempt, null, []);

    public static StartAttemptOutcome Failed(Error error) => new(null, error, []);
}
