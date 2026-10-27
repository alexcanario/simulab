using Microsoft.EntityFrameworkCore;
using Npgsql;
using Simulab.ExamEngine.Application.Attempts;
using Simulab.ExamEngine.Contracts;
using Simulab.ExamEngine.Domain.Entities;
using Simulab.ExamEngine.Infrastructure.Persistence.Configurations;
using Simulab.Persistence;

namespace Simulab.ExamEngine.Infrastructure.Persistence;

/// <summary>
/// The attempts over the module's context (F-166). Reads go through the tenant and soft-delete filters like every query of
/// the module; nothing here ignores them. A lock read takes the attempt's row first, in its own statement, and then loads the
/// attempt: PostgreSQL reads every statement of a read-committed transaction afresh, so what the load sees is what the
/// transaction that held the row before it committed.
/// </summary>
public sealed class AttemptStore(ExamEngineModuleDbContext context) : IAttemptStore
{
    public Task<Attempt?> FindInProgressAsync(Guid studentId, Guid examEditionId, CancellationToken cancellationToken) =>
        context.Attempts
            .AsNoTracking()
            .Include(attempt => attempt.Answers)
            .AsSplitQuery()
            .FirstOrDefaultAsync(
                attempt => attempt.StudentId == studentId
                    && attempt.ExamEditionId == examEditionId
                    && attempt.Status == AttemptStatus.InProgress,
                cancellationToken);

    public Task<Attempt?> FindAsync(Guid attemptId, CancellationToken cancellationToken) =>
        context.Attempts
            .AsNoTracking()
            .Include(attempt => attempt.Answers)
            .AsSplitQuery()
            .FirstOrDefaultAsync(attempt => attempt.Id == attemptId, cancellationToken);

    public async Task<bool> TryAddAsync(Attempt attempt, CancellationToken cancellationToken)
    {
        context.Attempts.Add(attempt);
        try
        {
            await context.SaveChangesAsync(cancellationToken);

            return true;
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } violation
            && violation.ConstraintName == AttemptConfiguration.InProgressIndex)
        {
            // A failed save keeps the entries Added; the winner's attempt is read next, so nothing of ours stays tracked.
            context.ChangeTracker.Clear();

            return false;
        }
    }

    public Task<Attempt?> FindForAnswerAsync(Guid studentId, Guid attemptId, CancellationToken cancellationToken) =>
        LockAndLoadAsync(
            attemptId,
            $"SELECT id AS \"Value\" FROM exam_engine.attempts WHERE id = {attemptId} AND student_id = {studentId} AND is_deleted = false FOR SHARE",
            cancellationToken);

    public Task<Attempt?> FindForSubmitAsync(Guid studentId, Guid attemptId, CancellationToken cancellationToken) =>
        LockAndLoadAsync(
            attemptId,
            $"SELECT id AS \"Value\" FROM exam_engine.attempts WHERE id = {attemptId} AND student_id = {studentId} AND is_deleted = false FOR UPDATE",
            cancellationToken);

    public Task<IReadOnlyList<Attempt>> FindAllOfStudentForErasureAsync(Guid studentId, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    // The lock is taken only on the student's own row, so ownership is decided before any side effect on another row.
    private async Task<Attempt?> LockAndLoadAsync(Guid attemptId, FormattableString lockSql, CancellationToken cancellationToken)
    {
        await context.Database.BeginTransactionIfNoneAsync(cancellationToken);
        var locked = await context.Database.SqlQuery<Guid>(lockSql).ToListAsync(cancellationToken);

        return locked.Count == 0 ? null : await LoadAsync(attemptId, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await context.SaveChangesAsync(cancellationToken);
        await context.Database.CommitTransactionIfAnyAsync(cancellationToken);
    }

    private Task<Attempt?> LoadAsync(Guid attemptId, CancellationToken cancellationToken) =>
        context.Attempts
            .Include(attempt => attempt.Answers)
            .AsSplitQuery()
            .FirstOrDefaultAsync(attempt => attempt.Id == attemptId, cancellationToken);
}
