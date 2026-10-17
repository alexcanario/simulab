using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Results;

namespace Simulab.QuestionBank.Application.Questions;

/// <summary>What the publish and review handlers share: the answer they give and the check of the send a reviewer saw.</summary>
internal static class ReviewAnswer
{
    /// <summary>The stored question, or the not-found failure when it vanished between the save and the read.</summary>
    public static async Task<Result<QuestionResponse>> ReadAsync(
        IQuestionQueries queries,
        Guid id,
        CancellationToken cancellationToken)
    {
        var saved = await queries.FindAsync(id, cancellationToken);

        return saved is null
            ? Result.Failure<QuestionResponse>(new Error(QuestionBankErrorCodes.QuestionNotFound, ErrorKind.NotFound))
            : Result.Success(saved);
    }

    /// <summary>
    /// True when the reviewer named a send (change note v2 of F-105) and it is not the latest one: the question was
    /// withdrawn and sent again while they read it, so a decision would judge content they never saw.
    /// </summary>
    public static async Task<bool> SendDiffersAsync(
        IQuestionQueries queries,
        Guid id,
        DateTimeOffset? seen,
        CancellationToken cancellationToken)
    {
        if (seen is not { } expected)
        {
            return false;
        }

        var current = await queries.FindAsync(id, cancellationToken);

        return current?.SubmittedAt is { } latest && latest != expected;
    }

    public static Error VersionConflict() => new(QuestionBankErrorCodes.QuestionVersionConflict, ErrorKind.Conflict);
}
