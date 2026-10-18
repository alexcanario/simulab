using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Simulab.Email;
using Simulab.Identity.Infrastructure.Email;
using Simulab.Identity.Infrastructure.Resources;
using Simulab.Jobs;
using Simulab.Jobs.Email;

namespace Simulab.Identity.Tests;

/// <summary>
/// F-125 AC12 (BR12): the four Identity emails take the brand. The button is the primary dark blue with white text, the
/// text is ink, the font stack starts with Bricolage Grotesque and falls back to the system (email clients load no web
/// font), and each email opens with "Simulab" and "Control Alt Systems" as text, never as an image.
/// </summary>
public sealed class EmailIdentityTests
{
    private static readonly DateTimeOffset When = new(2026, 10, 18, 9, 30, 0, TimeSpan.Zero);

    public static TheoryData<string> Emails() => new("verification", "password-reset", "password-changed", "account-erased", "data-exported");

    [Theory]
    [MemberData(nameof(Emails))]
    public async Task Email_OpensWithTheBrandAsTextInInkAndTheBrandFont(string email)
    {
        var html = (await SendAsync(email)).HtmlBody;

        html.Should().Contain("font-family: 'Bricolage Grotesque', system-ui, -apple-system, 'Segoe UI', Roboto, sans-serif;");
        html.Should().Contain("color: #0B1B2E;");
        var brand = html.IndexOf(">Simulab<", StringComparison.Ordinal);
        var company = html.IndexOf(">Control Alt Systems<", StringComparison.Ordinal);
        var heading = html.IndexOf("<h1", StringComparison.Ordinal);
        brand.Should().BePositive("the email names the app as text");
        company.Should().BeGreaterThan(brand, "the company sits under the app name");
        heading.Should().BeGreaterThan(company, "the brand opens the email, before its heading");
        html.Should().NotContain("<img", "the brand is text, never an image");
        html.Should().NotContainAny("#19243E", "#4A5A7A", "#216DB5");
    }

    [Theory]
    [InlineData("verification")]
    [InlineData("password-reset")]
    public async Task Email_ButtonIsThePrimaryDarkBlueWithWhiteText(string email)
    {
        var html = (await SendAsync(email)).HtmlBody;

        html.Should().Contain("background: #064F80; color: #FFFFFF;");
        EmailHtml.ButtonColour.Should().Be("#064F80");
    }

    private static async Task<EmailMessage> SendAsync(string email)
    {
        var queue = new CapturingQueue();
        var localizer = new StringLocalizer<IdentityEmails>(
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()), NullLoggerFactory.Instance));
        var send = email switch
        {
            "verification" => new VerificationMailer(queue, localizer, Options.Create(new VerificationEmailOptions())).SendAsync("ana@example.com", "token", "en"),
            "password-reset" => new PasswordMailer(queue, localizer, Options.Create(new PasswordEmailOptions())).SendResetLinkAsync("ana@example.com", "token", "en"),
            "password-changed" => new PasswordMailer(queue, localizer, Options.Create(new PasswordEmailOptions())).SendPasswordChangedAsync("ana@example.com", When, "en"),
            "account-erased" => new ErasureMailer(queue, localizer, Options.Create(new ErasureEmailOptions())).SendAccountErasedAsync("ana@example.com", When, "en"),
            "data-exported" => new DataExportMailer(queue, localizer, Options.Create(new PasswordEmailOptions())).SendDataExportedAsync("ana@example.com", When, "en"),
            _ => throw new ArgumentOutOfRangeException(nameof(email)),
        };

        await send;
        return EmailJob.Deserialize(queue.Payloads.Single());
    }

    private sealed class CapturingQueue : IJobQueue
    {
        public List<string> Payloads { get; } = [];

        public void Enqueue(string type, string payload) => Payloads.Add(payload);

        public Task FlushAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
