namespace Simulab.ExamEngine.Application.Attempts;

/// <summary>
/// Erases the free texts of every attempt of a student whose account was erased (F-167 UC1, BR1-BR5), in one
/// transaction. Repeatable: a second run, or a student with no attempt, changes nothing and succeeds.
/// </summary>
public sealed class EraseStudentTextsHandler(IAttemptStore store)
{
    public async Task HandleAsync(Guid studentId, CancellationToken cancellationToken)
    {
        var attempts = await store.FindAllOfStudentForErasureAsync(studentId, cancellationToken);
        foreach (var attempt in attempts)
        {
            // A system action: allowed on any status, so it never fails (BR2).
            attempt.EraseTexts();
        }

        // Never an answer text in an exception: a failure's message is kept on the job row (jobs.last_error).
        await store.SaveChangesAsync(cancellationToken);
    }
}
