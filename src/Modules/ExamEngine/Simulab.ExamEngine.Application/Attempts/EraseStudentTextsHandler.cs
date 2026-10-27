namespace Simulab.ExamEngine.Application.Attempts;

/// <summary>
/// Erases the free texts of every attempt of a student whose account was erased (F-167 UC1, BR1-BR5), in one
/// transaction. Repeatable: a second run, or a student with no attempt, changes nothing and succeeds.
/// </summary>
public sealed class EraseStudentTextsHandler(IAttemptStore store)
{
    public Task HandleAsync(Guid studentId, CancellationToken cancellationToken) => throw new NotImplementedException();
}
