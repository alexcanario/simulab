using System.Globalization;
using System.Net;
using Simulab.Identity.Contracts;
using Simulab.Identity.Infrastructure.Email;

namespace Simulab.Identity.Tests;

/// <summary>
/// B-9 AC3: the button of every email is the app's primary blue, written once. It used to be the old primary
/// `#2478C5`, copied into two mailers.
/// </summary>
public sealed class EmailButtonTests : IdentityApiTests
{
    [Fact]
    public void Button_UsesAColourThatIsReadableUnderWhiteText()
    {
        Contrast("#FFFFFF", EmailHtml.ButtonColour).Should().BeGreaterThanOrEqualTo(4.5);
    }

    [Fact]
    public async Task VerificationAndPasswordEmails_UseTheSameButtonColour()
    {
        await RunJobsAsync();
        Emails.Clear();
        var client = Client();

        var email = await ActiveUser.CreateAsync(client, Factory);
        await RunJobsAsync();
        var verification = Emails.Messages.Single(message => message.HtmlBody.Contains("verify-email?token=", StringComparison.Ordinal));

        await PostAsync(client, "/api/v1/identity/password-reset-requests", new RequestPasswordResetRequest(email), HttpStatusCode.Accepted);
        await RunJobsAsync();
        var reset = Emails.Last!;

        verification.HtmlBody.Should().Contain($"background: {EmailHtml.ButtonColour}");
        reset.HtmlBody.Should().Contain($"background: {EmailHtml.ButtonColour}");
        verification.HtmlBody.Should().NotContain("#2478C5");
        reset.HtmlBody.Should().NotContain("#2478C5");
    }

    /// <summary>WCAG 2.2 relative luminance, the same formula as the web host's <c>ThemeContrastTests</c>.</summary>
    private static double Contrast(string foreground, string background)
    {
        var first = Luminance(foreground);
        var second = Luminance(background);
        return (Math.Max(first, second) + 0.05) / (Math.Min(first, second) + 0.05);
    }

    private static double Luminance(string colour)
    {
        var hex = colour.TrimStart('#');
        var channels = Enumerable.Range(0, 3)
            .Select(index => int.Parse(hex.AsSpan(index * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255d)
            .Select(value => value <= 0.03928 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4))
            .ToArray();

        return (0.2126 * channels[0]) + (0.7152 * channels[1]) + (0.0722 * channels[2]);
    }
}
