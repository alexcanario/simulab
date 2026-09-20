using Simulab.Email;

namespace Simulab.Jobs.Email;

/// <summary>Hands the message the request already wrote to the mail server (F-13 BR4).</summary>
public sealed class SendEmailJobHandler(IEmailSender sender) : IJobHandler
{
    public string Type => EmailJob.Type;

    public Task HandleAsync(string payload, CancellationToken cancellationToken = default) =>
        sender.SendAsync(EmailJob.Deserialize(payload), cancellationToken);
}
