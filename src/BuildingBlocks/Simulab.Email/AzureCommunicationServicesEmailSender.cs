using Azure;
using Azure.Communication.Email;
using Azure.Identity;
using Microsoft.Extensions.Options;
using AcsEmailMessage = Azure.Communication.Email.EmailMessage;

namespace Simulab.Email;

/// <summary>
/// Sends through Azure Communication Services Email over HTTP (F-66). It waits until the service accepts or rejects
/// the message, so a rejection throws and the job worker retries it; delivery to the mailbox is not followed (F-88).
/// </summary>
public sealed class AzureCommunicationServicesEmailSender(EmailClient client, IOptions<EmailOptions> options) : IEmailSender
{
    private readonly EmailOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

    public async Task SendAsync(Simulab.Email.EmailMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        // The display name of the sender lives on the domain's sender username (the Bicep sets it): the call
        // takes only the address.
        var mail = new AcsEmailMessage(
            _options.FromAddress,
            message.To,
            new EmailContent(message.Subject) { Html = message.HtmlBody, PlainText = message.TextBody });

        try
        {
            var operation = await client.SendAsync(WaitUntil.Completed, mail, cancellationToken);
            var status = operation.Value.Status;
            if (status != EmailSendStatus.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Azure Communication Services at {Endpoint} did not send the message: status {status}.");
            }
        }
        catch (RequestFailedException exception)
        {
            // No inner exception: its message carries the response body, which may echo the recipient, and the job
            // runner logs the whole chain and stores the message (BR7).
            throw new InvalidOperationException(
                $"Azure Communication Services at {Endpoint} refused the message: {exception.ErrorCode ?? "unknown"} (HTTP {exception.Status}).");
        }
        catch (AuthenticationFailedException)
        {
            throw new InvalidOperationException(
                $"Could not sign in to Azure Communication Services at {Endpoint} with the managed identity.");
        }
    }

    private string Endpoint => _options.AzureCommunicationServices.Endpoint ?? "(no endpoint)";
}
