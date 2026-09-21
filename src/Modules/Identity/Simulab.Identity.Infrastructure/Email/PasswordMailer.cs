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
/// Writes the two password emails in the recipient's own language (F-7 BR4, BR11) and sends them through
/// the shared <see cref="IEmailSender"/>, the way <see cref="VerificationMailer"/> does for verification.
/// </summary>
public sealed class PasswordMailer(
    IJobQueue queue,
    IStringLocalizer<IdentityEmails> localizer,
    IOptions<PasswordEmailOptions> options) : IPasswordMailer
{
    public Task SendResetLinkAsync(string email, string rawToken, string locale, CancellationToken cancellationToken = default)
    {
        var link = $"{options.Value.PasswordResetUrl.TrimEnd('/')}?token={Uri.EscapeDataString(rawToken)}";
        var safeLink = WebUtility.HtmlEncode(link);

        using var culture = EmailCulture.Use(locale);
        var html = Page(
            localizer["Email.PasswordReset.Heading"],
            $"""
            <p>{Encode("Email.PasswordReset.Body")}</p>
            {EmailHtml.Button(link, localizer["Email.PasswordReset.Button"])}
            <p>{Encode("Email.PasswordReset.Expiry")}</p>
            <p style="color: #4A5A7A; font-size: 13px;">{Encode("Email.Verification.LinkFallback")}<br><a href="{safeLink}">{safeLink}</a></p>
            <p style="color: #4A5A7A; font-size: 13px;">{Encode("Email.PasswordReset.Ignore")}</p>
            """);
        var text = $"""
            {localizer["Email.PasswordReset.Heading"]}

            {localizer["Email.PasswordReset.Body"]}

            {link}

            {localizer["Email.PasswordReset.Expiry"]}
            {localizer["Email.PasswordReset.Ignore"]}
            """;

        // F-13 BR2: staged on the caller's unit of work; the save that writes the token writes it too.
        queue.EnqueueEmail(new EmailMessage(email, localizer["Email.PasswordReset.Subject"], html, text));
        return Task.CompletedTask;
    }

    public Task SendPasswordChangedAsync(string email, DateTimeOffset changedAt, string locale, CancellationToken cancellationToken = default)
    {
        var forgotLink = options.Value.ForgotPasswordUrl;
        var safeForgotLink = WebUtility.HtmlEncode(forgotLink);

        using var culture = EmailCulture.Use(locale);

        // The user's time zone is not known yet (F-8): the instant says plainly that it is UTC.
        var when = $"{changedAt.UtcDateTime.ToString("f", culture.Culture)} UTC";
        string body = localizer["Email.PasswordChanged.Body", when];
        var html = Page(
            localizer["Email.PasswordChanged.Heading"],
            $"""
            <p>{WebUtility.HtmlEncode(body)}</p>
            <p>{Encode("Email.PasswordChanged.NotYou")}<br><a href="{safeForgotLink}">{safeForgotLink}</a></p>
            """);
        var text = $"""
            {localizer["Email.PasswordChanged.Heading"]}

            {body}

            {localizer["Email.PasswordChanged.NotYou"]}
            {forgotLink}
            """;

        // F-13 BR2: staged; the caller saves it (PasswordNotice), since the password itself is already written.
        queue.EnqueueEmail(new EmailMessage(email, localizer["Email.PasswordChanged.Subject"], html, text));
        return Task.CompletedTask;
    }

    private string Encode(string key) => WebUtility.HtmlEncode(localizer[key]);

    private static string Page(string heading, string innerHtml) =>
        $"""
        <!doctype html>
        <html>
          <body style="font-family: -apple-system, 'Segoe UI', Roboto, sans-serif; color: #19243E; line-height: 1.6;">
            <h1 style="font-size: 20px;">{WebUtility.HtmlEncode(heading)}</h1>
            {innerHtml}
          </body>
        </html>
        """;
}
