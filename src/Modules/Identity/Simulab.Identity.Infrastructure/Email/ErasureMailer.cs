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
/// The farewell email (F-10, BR12): what was erased, what was kept, and that the address is free again.
/// Same shape as <see cref="PasswordMailer"/> — the recipient's language, HTML and plain text.
/// </summary>
public sealed class ErasureMailer(
    IJobQueue queue,
    IStringLocalizer<IdentityEmails> localizer,
    IOptions<ErasureEmailOptions> options) : IErasureMailer
{
    public Task SendAccountErasedAsync(string email, DateTimeOffset erasedAt, string locale, CancellationToken cancellationToken = default)
    {
        var signUpLink = options.Value.SignUpUrl;
        var safeSignUpLink = WebUtility.HtmlEncode(signUpLink);

        using var culture = EmailCulture.Use(locale);

        // The user's time zone is not known (F-8), so the instant says plainly that it is UTC.
        var when = $"{erasedAt.UtcDateTime.ToString("f", culture.Culture)} UTC";
        string body = localizer["Email.AccountErased.Body", when];
        var html = Page(
            localizer["Email.AccountErased.Heading"],
            $"""
            <p>{WebUtility.HtmlEncode(body)}</p>
            <p>{Encode("Email.AccountErased.Kept")}</p>
            <p>{Encode("Email.AccountErased.ComeBack")}<br><a href="{safeSignUpLink}">{safeSignUpLink}</a></p>
            """);
        var text = $"""
            {localizer["Email.AccountErased.Heading"]}

            {body}

            {localizer["Email.AccountErased.Kept"]}

            {localizer["Email.AccountErased.ComeBack"]}
            {signUpLink}
            """;

        // F-13 BR2: staged on the erasure's own transaction, so an erasure that rolls back sends nothing.
        queue.EnqueueEmail(new EmailMessage(email, localizer["Email.AccountErased.Subject"], html, text));
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
