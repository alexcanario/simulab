namespace Simulab.Identity.Application.Abstractions;

/// <summary>Renders and sends the two password emails in the recipient's own language (F-7 BR4, BR11).</summary>
public interface IPasswordMailer
{
    /// <summary>The link to choose a new password, carrying the raw token (BR4).</summary>
    Task SendResetLinkAsync(string email, string rawToken, string locale, CancellationToken cancellationToken = default);

    /// <summary>The notice that the password changed, with the instant and a way back if it was not the owner (BR11).</summary>
    Task SendPasswordChangedAsync(string email, DateTimeOffset changedAt, string locale, CancellationToken cancellationToken = default);
}
