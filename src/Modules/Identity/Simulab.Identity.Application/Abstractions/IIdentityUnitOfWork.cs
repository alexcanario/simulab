namespace Simulab.Identity.Application.Abstractions;

/// <summary>
/// Writes what the module has staged for this request (F-13 BR2). A mailer only stages its job, so a
/// handler whose work is already saved — a password that <c>UserManager</c> has just changed — asks for
/// this one write, and the job leaves with it.
/// </summary>
public interface IIdentityUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
