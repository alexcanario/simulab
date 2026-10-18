using System.Net;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Simulab.Email;
using Simulab.Jobs;
using Simulab.Jobs.Email;
using Simulab.Identity.Application.Abstractions;
using Simulab.Identity.Infrastructure.Resources;

namespace Simulab.Identity.Infrastructure.Email;

/// <summary>
/// Writes the verification email in the recipient's own language (BR13) and sends it through the
/// shared <see cref="IEmailSender"/>. The email leaves the request, so the language comes from the
/// user's stored locale and not from the current thread.
/// </summary>
public sealed class VerificationMailer(
    IJobQueue queue,
    IStringLocalizer<IdentityEmails> localizer,
    IOptions<VerificationEmailOptions> options) : IVerificationMailer
{
    public Task SendAsync(string email, string rawToken, string locale, CancellationToken cancellationToken = default)
    {
        var link = $"{options.Value.VerificationUrl.TrimEnd('/')}?token={Uri.EscapeDataString(rawToken)}";

        // IStringLocalizer reads CurrentUICulture; the recipient's language is the one that matters here.
        using var culture = EmailCulture.Use(locale);
        var message = new EmailMessage(
            email,
            localizer["Email.Verification.Subject"],
            RenderHtml(link),
            RenderText(link));

        // F-13 BR2: staged on the caller's unit of work; the save that writes the token writes it too.
        queue.EnqueueEmail(message);
        return Task.CompletedTask;
    }

    private string RenderHtml(string link)
    {
        var safeLink = WebUtility.HtmlEncode(link);

        return EmailHtml.Page(
            localizer["Email.Verification.Heading"],
            $"""
            <p>{WebUtility.HtmlEncode(localizer["Email.Verification.Body"])}</p>
            {EmailHtml.Button(link, localizer["Email.Verification.Button"])}
            <p>{WebUtility.HtmlEncode(localizer["Email.Verification.Expiry"])}</p>
            {EmailHtml.SmallPrint($"{WebUtility.HtmlEncode(localizer["Email.Verification.LinkFallback"])}<br><a href=\"{safeLink}\">{safeLink}</a>")}
            {EmailHtml.SmallPrint(WebUtility.HtmlEncode(localizer["Email.Verification.Ignore"]))}
            """);
    }

    private string RenderText(string link) =>
        $"""
        {localizer["Email.Verification.Heading"]}

        {localizer["Email.Verification.Body"]}

        {link}

        {localizer["Email.Verification.Expiry"]}
        {localizer["Email.Verification.Ignore"]}
        """;
}
