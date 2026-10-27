using Simulab.ExamEngine.Contracts;
using Simulab.SharedKernel.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.ExamEngine.Domain.Entities;

/// <summary>
/// A student's attempt at a published edition (F-166): the questions it fixed at the start, each with the version the
/// student saw, and the answers saved so far. Two states, <see cref="AttemptStatus.InProgress"/> and
/// <see cref="AttemptStatus.Submitted"/>; a submitted attempt never changes (BR6). The rules answer with a
/// <see cref="Result"/> and never throw.
/// </summary>
public sealed class Attempt : TenantEntity
{
    private readonly List<AttemptAnswer> _answers = [];

    private Attempt()
    {
    }

    /// <summary>The student who owns the attempt; held by id, never by a foreign key.</summary>
    public Guid StudentId { get; private set; }

    /// <summary>The published edition taken; held by id, never by a foreign key.</summary>
    public Guid ExamEditionId { get; private set; }

    public AttemptStatus Status { get; private set; }

    public DateTimeOffset StartedAt { get; private set; }

    public DateTimeOffset? SubmittedAt { get; private set; }

    /// <summary>One row per question of the attempt, in paper-number order.</summary>
    public IReadOnlyList<AttemptAnswer> Answers => _answers;

    /// <summary>Starts an attempt on the questions given: one blank answer row for each, in paper-number order (BR2, BR10, BR11).</summary>
    public static Attempt Start(
        Guid studentId,
        Guid examEditionId,
        Guid? tenantId,
        DateTimeOffset now,
        IReadOnlyList<AttemptQuestion> questions)
    {
        ArgumentNullException.ThrowIfNull(questions);

        var attempt = new Attempt
        {
            StudentId = studentId,
            ExamEditionId = examEditionId,
            Status = AttemptStatus.InProgress,
            StartedAt = ToMicroseconds(now)
        };
        attempt.TenantId = tenantId;
        attempt._answers.AddRange(questions
            .OrderBy(question => question.PaperNumber)
            .Select(question => AttemptAnswer.Blank(attempt.Id, tenantId, question)));

        return attempt;
    }

    /// <summary>The row of a question of the attempt, or null when the question is not one of them.</summary>
    public AttemptAnswer? AnswerOf(Guid questionId) => _answers.Find(answer => answer.QuestionId == questionId);

    /// <summary>Saves or replaces the answer to one question; only an attempt in progress takes it (BR4, BR6).</summary>
    public Result Save(Guid questionId, AnswerPayload answer)
    {
        ArgumentNullException.ThrowIfNull(answer);

        var row = AnswerOf(questionId);
        if (row is null)
        {
            return Result.Failure(new Error(ExamAttemptErrorCodes.QuestionNotInAttempt, ErrorKind.NotFound));
        }

        if (Status == AttemptStatus.Submitted)
        {
            return Result.Failure(new Error(ExamAttemptErrorCodes.AlreadySubmitted, ErrorKind.Conflict));
        }

        row.Set(answer);

        return Result.Success();
    }

    /// <summary>Clears the answer to one question; clearing a blank one is no failure (BR4).</summary>
    public Result Clear(Guid questionId)
    {
        var row = AnswerOf(questionId);
        if (row is null)
        {
            return Result.Failure(new Error(ExamAttemptErrorCodes.QuestionNotInAttempt, ErrorKind.NotFound));
        }

        if (Status == AttemptStatus.Submitted)
        {
            return Result.Failure(new Error(ExamAttemptErrorCodes.AlreadySubmitted, ErrorKind.Conflict));
        }

        row.Set(null);

        return Result.Success();
    }

    /// <summary>
    /// Submits the attempt with the server's time. A second submit changes nothing and succeeds, so a retry of a lost
    /// response is safe (BR6).
    /// </summary>
    public Result Submit(DateTimeOffset now)
    {
        if (Status == AttemptStatus.Submitted)
        {
            return Result.Success();
        }

        Status = AttemptStatus.Submitted;
        SubmittedAt = ToMicroseconds(now);

        return Result.Success();
    }

    /// <summary>
    /// Erases the free texts of every answer when the student's account is erased (F-167 BR1-BR3): a system action, so
    /// it is allowed whatever the status and changes nothing else. A second call changes nothing.
    /// </summary>
    public Result EraseTexts()
    {
        foreach (var answer in _answers)
        {
            answer.EraseText();
        }

        return Result.Success();
    }

    // PostgreSQL keeps microseconds; a time with more digits would read back different from the one the first response showed.
    private static DateTimeOffset ToMicroseconds(DateTimeOffset value) =>
        new(value.Ticks - (value.Ticks % (TimeSpan.TicksPerMillisecond / 1000)), value.Offset);
}

/// <summary>One question an attempt fixes at its start: the question, its number in the paper, its version and its type.</summary>
public sealed record AttemptQuestion(Guid QuestionId, int PaperNumber, int VersionNumber, AttemptQuestionType Type);
