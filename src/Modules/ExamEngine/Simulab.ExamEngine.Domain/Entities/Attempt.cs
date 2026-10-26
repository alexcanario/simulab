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

    /// <summary>Starts an attempt on the questions given, in the order given (BR2, BR10, BR11).</summary>
    public static Attempt Start(
        Guid studentId,
        Guid examEditionId,
        Guid? tenantId,
        DateTimeOffset now,
        IReadOnlyList<(Guid QuestionId, int PaperNumber, int VersionNumber, AttemptQuestionType Type)> questions) =>
        throw new NotImplementedException();

    /// <summary>Submits the attempt with the server's time; a second submit changes nothing (BR6).</summary>
    public Result Submit(DateTimeOffset now) => throw new NotImplementedException();
}
