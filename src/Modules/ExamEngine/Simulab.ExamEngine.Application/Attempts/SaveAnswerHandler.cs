using Simulab.ExamEngine.Contracts;
using Simulab.SharedKernel.Results;

namespace Simulab.ExamEngine.Application.Attempts;

/// <summary>
/// What a save answers: the question as the attempt now shows it, or the failure; a failure of the answer itself comes
/// with one entry per field (F-166, BR5).
/// </summary>
public sealed record SaveAnswerOutcome(
    AttemptQuestionResponse? Question,
    Error? Error,
    IReadOnlyList<AnswerFieldError> Fields)
{
    public bool IsSuccess => Error is null;

    public static SaveAnswerOutcome Saved(AttemptQuestionResponse question) => new(question, null, []);

    public static SaveAnswerOutcome Failed(Error error) => new(null, error, []);

    public static SaveAnswerOutcome Invalid(IReadOnlyList<AnswerFieldError> fields) =>
        new(null, new Error(ExamAttemptErrorCodes.AnswerInvalid, ErrorKind.Validation), fields);
}

/// <summary>
/// Saves or replaces the answer to one question of an attempt (F-166, UC2, BR4 to BR6, BR9). The attempt is read with a shared
/// row lock so a submit waits for the saves in flight; the checks run in the order a caller can tell apart: the attempt is
/// the caller's, the question is one of it, the attempt takes answers, the answer fits.
/// </summary>
public sealed class SaveAnswerHandler(IAttemptStore store, IAttemptSource source)
{
    // The answer is null when the body could not be read as one of the eight shapes.
    public async Task<SaveAnswerOutcome> HandleAsync(
        Guid studentId,
        Guid attemptId,
        Guid questionId,
        AnswerPayload? answer,
        CancellationToken cancellationToken)
    {
        var attempt = await store.FindForAnswerAsync(attemptId, cancellationToken);
        if (attempt is null || attempt.StudentId != studentId)
        {
            return SaveAnswerOutcome.Failed(new Error(ExamAttemptErrorCodes.NotFound, ErrorKind.NotFound));
        }

        var row = attempt.AnswerOf(questionId);
        if (row is null)
        {
            return SaveAnswerOutcome.Failed(new Error(ExamAttemptErrorCodes.QuestionNotInAttempt, ErrorKind.NotFound));
        }

        if (attempt.Status == AttemptStatus.Submitted)
        {
            return SaveAnswerOutcome.Failed(new Error(ExamAttemptErrorCodes.AlreadySubmitted, ErrorKind.Conflict));
        }

        if (answer is null)
        {
            return SaveAnswerOutcome.Invalid([new AnswerFieldError("type", ExamAttemptErrorCodes.AnswerTypeMismatch)]);
        }

        var shape = await source.FindShapeAsync(row.QuestionId, row.VersionNumber, cancellationToken);
        if (shape is null)
        {
            return SaveAnswerOutcome.Failed(new Error(ExamAttemptErrorCodes.QuestionNotInAttempt, ErrorKind.NotFound));
        }

        var check = AnswerChecker.Check(shape, answer);
        if (!check.IsValid)
        {
            return SaveAnswerOutcome.Invalid(check.Errors);
        }

        var saved = attempt.Save(questionId, check.Answer!);
        if (saved.IsFailure)
        {
            return SaveAnswerOutcome.Failed(saved.Error!);
        }

        await store.SaveChangesAsync(cancellationToken);

        return SaveAnswerOutcome.Saved(AttemptMapper.ToResponse(row));
    }
}

/// <summary>Clears the answer to one question of an attempt, so the question is blank again (F-166, UC2, BR4).</summary>
public sealed class ClearAnswerHandler(IAttemptStore store)
{
    public async Task<Result> HandleAsync(Guid studentId, Guid attemptId, Guid questionId, CancellationToken cancellationToken)
    {
        var attempt = await store.FindForAnswerAsync(attemptId, cancellationToken);
        if (attempt is null || attempt.StudentId != studentId)
        {
            return Result.Failure(new Error(ExamAttemptErrorCodes.NotFound, ErrorKind.NotFound));
        }

        var cleared = attempt.Clear(questionId);
        if (cleared.IsFailure)
        {
            return cleared;
        }

        await store.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
