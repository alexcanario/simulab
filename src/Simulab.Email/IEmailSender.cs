namespace Simulab.Email;

/// <summary>
/// Sends one message. Every email of the app goes through it: Mailpit in development, a provider in the cloud.
/// A failure throws; the caller decides whether to retry or to fail the request.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
