namespace Simulab.ExamEngine.Application.Attempts;

/// <summary>Who may read an attempt that is not their own (F-166, BR7). Implemented over Identity's permission query.</summary>
public interface IAttemptAccess
{
    /// <summary>True when the user holds <c>attempts.read</c> right now, read from the database and not from the token.</summary>
    Task<bool> CanReadAnyAsync(Guid userId, CancellationToken cancellationToken);
}
