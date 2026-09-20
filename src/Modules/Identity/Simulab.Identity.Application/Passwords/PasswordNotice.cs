using Simulab.Identity.Application.Abstractions;
using Simulab.Identity.Domain.Entities;

namespace Simulab.Identity.Application.Passwords;

/// <summary>
/// F-7 BR11, through the queue (F-13). The password is already written by <c>UserManager</c> when this
/// runs, so there is no later save to carry the job: the notice is staged and written here, in one
/// statement of its own. A mail server that is down no longer reaches this code at all, which is why
/// the best-effort <c>try/catch</c> that used to hide a lost email is gone (F-13 BR3).
/// </summary>
internal static class PasswordNotice
{
    public static async Task EnqueueAsync(
        IPasswordMailer mailer,
        IIdentityUnitOfWork unitOfWork,
        User user,
        DateTimeOffset changedAt,
        CancellationToken cancellationToken)
    {
        await mailer.SendPasswordChangedAsync(user.Email!, changedAt, user.PreferredLanguage, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
