using Microsoft.Extensions.Logging;
using Simulab.Identity.Application.Abstractions;
using Simulab.Identity.Domain.Entities;

namespace Simulab.Identity.Application.Passwords;

/// <summary>
/// F-7 BR11, best effort: the password has already changed when this runs. A mail server that fails is
/// logged, never reported as a failed change, or the user would retry with a password that is no longer theirs.
/// </summary>
internal static class PasswordNotice
{
    public static async Task SendAsync(IPasswordMailer mailer, ILogger logger, User user, DateTimeOffset changedAt, CancellationToken cancellationToken)
    {
        try
        {
            await mailer.SendPasswordChangedAsync(user.Email!, changedAt, user.PreferredLanguage, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Sending the password-changed email failed; the change itself stands.");
        }
    }
}
