using Microsoft.Extensions.Options;

namespace Simulab.Email.Tests;

/// <summary>AC8: a message sent through <see cref="IEmailSender"/> arrives; an unreachable server throws.</summary>
public class SmtpEmailSenderTests(MailpitServer mailpit) : IClassFixture<MailpitServer>
{
    private static SmtpEmailSender SenderFor(EmailOptions options) => new(Options.Create(options));

    [Fact]
    public async Task Send_Message_ArrivesAtTheServerWithSenderRecipientAndSubject()
    {
        var sender = SenderFor(new EmailOptions
        {
            Host = mailpit.Host,
            Port = mailpit.Smtp,
            FromAddress = "no-reply@simulab.app",
            FromName = "Simulab",
        });
        var subject = $"Verify your email {Guid.CreateVersion7()}";

        await sender.SendAsync(
            new EmailMessage("student@example.com", subject, "<p>Welcome</p>", "Welcome"),
            CancellationToken.None);

        var messages = await mailpit.MessagesAsync();
        var message = messages.Should().ContainSingle(candidate => candidate.Subject == subject).Which;
        message.From.Address.Should().Be("no-reply@simulab.app");
        message.From.Name.Should().Be("Simulab");
        message.To.Should().ContainSingle().Which.Address.Should().Be("student@example.com");
    }

    [Fact]
    public async Task Send_UnreachableServer_Throws()
    {
        var sender = SenderFor(new EmailOptions { Host = "localhost", Port = 1 });

        var send = async () => await sender.SendAsync(
            new EmailMessage("student@example.com", "Subject", "<p>Body</p>"),
            CancellationToken.None);

        await send.Should().ThrowAsync<Exception>();
    }
}
