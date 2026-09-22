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
/// The notice that the user's data was downloaded (F-16, BR7): when, and what to do if it was not them.
/// Same shape as <see cref="PasswordMailer"/> — the recipient's language, HTML and plain text.
/// </summary>
public sealed class DataExportMailer(
    IJobQueue queue,
    IStringLocalizer<IdentityEmails> localizer,
    IOptions<PasswordEmailOptions> options) : IDataExportMailer
{
    public Task SendDataExportedAsync(string email, DateTimeOffset exportedAt, string locale, CancellationToken cancellationToken = default)
    {
        var forgotLink = options.Value.ForgotPasswordUrl;
        var safeForgotLink = WebUtility.HtmlEncode(forgotLink);

        using var culture = EmailCulture.Use(locale);

        // The user's time zone is not known (F-8), so the instant says plainly that it is UTC.
        var when = $"{exportedAt.UtcDateTime.ToString("f", culture.Culture)} UTC";
        string body = localizer["Email.DataExported.Body", when];
        var html = Page(
            localizer["Email.DataExported.Heading"],
            $"""
            <p>{WebUtility.HtmlEncode(body)}</p>
            <p>{WebUtility.HtmlEncode(localizer["Email.DataExported.NotYou"])}<br><a href="{safeForgotLink}">{safeForgotLink}</a></p>
            """);
        var text = $"""
            {localizer["Email.DataExported.Heading"]}

            {body}

            {localizer["Email.DataExported.NotYou"]}
            {forgotLink}
            """;

        // F-13 BR2: staged; the export handler saves it.
        queue.EnqueueEmail(new EmailMessage(email, localizer["Email.DataExported.Subject"], html, text));
        return Task.CompletedTask;
    }

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
