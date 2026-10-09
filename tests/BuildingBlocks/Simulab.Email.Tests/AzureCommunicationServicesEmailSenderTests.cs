using Azure;
using Azure.Communication.Email;
using Azure.Identity;
using Microsoft.Extensions.Options;

namespace Simulab.Email.Tests;

/// <summary>F-66 AC1 and AC3: the ACS sender sends one message and throws, without leaking, when the service refuses it.</summary>
public class AzureCommunicationServicesEmailSenderTests
{
    private const string Endpoint = "https://simulab-staging.communication.azure.com";

    private static AzureCommunicationServicesEmailSender SenderFor(FakeEmailClient client) => new(client, Options.Create(new EmailOptions
    {
        Provider = EmailProvider.AzureCommunicationServices,
        FromAddress = "DoNotReply@abc.azurecomm.net",
        AzureCommunicationServices = { Endpoint = Endpoint },
    }));

    [Fact]
    public async Task Send_Message_SendsOneMessageWithSenderRecipientSubjectAndBothBodies()
    {
        var client = new FakeEmailClient();

        await SenderFor(client).SendAsync(
            new EmailMessage("student@example.com", "Verify your email", "<p>Welcome</p>", "Welcome"),
            CancellationToken.None);

        client.Calls.Should().Be(1);
        var sent = client.Sent!;
        sent.SenderAddress.Should().Be("DoNotReply@abc.azurecomm.net");
        sent.Recipients.To.Should().ContainSingle().Which.Address.Should().Be("student@example.com");
        sent.Content.Subject.Should().Be("Verify your email");
        sent.Content.Html.Should().Be("<p>Welcome</p>");
        sent.Content.PlainText.Should().Be("Welcome");
    }

    [Fact]
    public async Task Send_Message_WaitsUntilTheServiceAcceptsOrRejectsIt()
    {
        var client = new FakeEmailClient();

        await SenderFor(client).SendAsync(new EmailMessage("student@example.com", "Subject", "<p>Body</p>"), CancellationToken.None);

        client.Waited.Should().Be(WaitUntil.Completed);
    }

    [Fact]
    public async Task Send_ServiceRefusesTheMessage_ThrowsWithTheErrorCodeAndTheEndpointAndNothingElse()
    {
        var refusal = new RequestFailedException(400, "Recipient student@example.com is not valid.", "InvalidRecipientEmailAddress", innerException: null);
        var sender = SenderFor(new FakeEmailClient { Throws = refusal });

        var send = async () => await sender.SendAsync(
            new EmailMessage("student@example.com", "Subject", "<p>Body</p>"), CancellationToken.None);

        var thrown = (await send.Should().ThrowAsync<InvalidOperationException>()).Which;
        thrown.Message.Should().Contain("InvalidRecipientEmailAddress").And.Contain(Endpoint).And.Contain("400");
        thrown.Message.Should().NotContain("student@example.com");
        thrown.InnerException.Should().BeNull();
    }

    [Fact]
    public async Task Send_IdentityCannotSignIn_ThrowsWithTheEndpointAndNoInnerException()
    {
        var sender = SenderFor(new FakeEmailClient { Throws = new CredentialUnavailableException("no identity endpoint") });

        var send = async () => await sender.SendAsync(
            new EmailMessage("student@example.com", "Subject", "<p>Body</p>"), CancellationToken.None);

        var thrown = (await send.Should().ThrowAsync<InvalidOperationException>()).Which;
        thrown.Message.Should().Contain(Endpoint).And.Contain("managed identity");
        thrown.InnerException.Should().BeNull();
    }

    [Fact]
    public async Task Send_OperationEndsAsFailed_Throws()
    {
        var sender = SenderFor(new FakeEmailClient { Status = EmailSendStatus.Failed });

        var send = async () => await sender.SendAsync(
            new EmailMessage("student@example.com", "Subject", "<p>Body</p>"), CancellationToken.None);

        (await send.Should().ThrowAsync<InvalidOperationException>()).Which.Message
            .Should().Contain(Endpoint).And.Contain("Failed");
    }
}
