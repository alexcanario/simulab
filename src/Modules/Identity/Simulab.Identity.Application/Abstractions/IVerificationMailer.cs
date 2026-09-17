namespace Simulab.Identity.Application.Abstractions;

/// <summary>
/// Renders and sends the verification email in the recipient's own language (BR13). The port keeps the
/// templates and the localizer out of the handlers.
/// </summary>
public interface IVerificationMailer
{
    Task SendAsync(string email, string rawToken, string locale, CancellationToken cancellationToken = default);
}
