using Azure;
using Azure.Communication.Email;
using AcsEmailMessage = Azure.Communication.Email.EmailMessage;

namespace Simulab.Email.Tests;

/// <summary>
/// The ACS client faked through its public virtual members: it records the call and answers with the outcome the
/// test sets. No service is called and no package beyond the sender's own is needed.
/// </summary>
internal sealed class FakeEmailClient : EmailClient
{
    public AcsEmailMessage? Sent { get; private set; }

    public WaitUntil? Waited { get; private set; }

    public int Calls { get; private set; }

    /// <summary>The final status the fake operation reports.</summary>
    public EmailSendStatus Status { get; init; } = EmailSendStatus.Succeeded;

    /// <summary>When set, the call throws it, as the service does for a refused message.</summary>
    public Exception? Throws { get; init; }

    public override Task<EmailSendOperation> SendAsync(
        WaitUntil wait, AcsEmailMessage message, CancellationToken cancellationToken = default)
    {
        Calls++;
        Waited = wait;
        Sent = message;
        if (Throws is not null)
        {
            throw Throws;
        }

        return Task.FromResult<EmailSendOperation>(new FakeSendOperation(Status));
    }

    private sealed class FakeSendOperation(EmailSendStatus status) : EmailSendOperation("operation-id", new FakeEmailClient())
    {
        public override EmailSendResult Value => EmailModelFactory.EmailSendResult("operation-id", status);
    }
}
