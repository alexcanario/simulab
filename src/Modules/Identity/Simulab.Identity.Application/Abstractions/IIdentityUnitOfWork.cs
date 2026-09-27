namespace Simulab.Identity.Application.Abstractions;

/// <summary>
/// Writes what the module has staged for this request (F-13 BR2). A mailer only stages its job, so a
/// handler whose work is already saved — a password that <c>UserManager</c> has just changed — asks for
/// this one write, and the job leaves with it.
/// </summary>
public interface IIdentityUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens the transaction that holds every write of the sign-up handlers (F-30 BR1, BR2, BR8, BR9): open
    /// it only after every validation, lookup and outbound call, since a write is all it should ever hold.
    /// </summary>
    Task<IIdentityTransaction> BeginAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// The one of the sign-up transaction's two unique indexes <paramref name="exception"/> reports a
    /// collision with (F-30 BR6, BR8: the only door this module opens onto EF Core's own exception type),
    /// or null when it is not one of those two and must propagate unchanged (AC8c).
    /// </summary>
    IdentityUniqueViolation? TranslateWriteFailure(Exception exception);
}
