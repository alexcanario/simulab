using System.Net;
using Simulab.Identity.Contracts;

namespace Simulab.Identity.Tests;

/// <summary>F-7 AC3 and AC11: both password emails are written in the account's own language.</summary>
public sealed class PasswordEmailLanguageTests : IdentityApiTests
{
    [Theory]
    [InlineData("en", "Reset your password", "Choose a new password", "Your password was changed")]
    [InlineData("pt-BR", "Redefina sua senha", "Escolher uma nova senha", "Sua senha foi alterada")]
    [InlineData("pt-PT", "Redefina a sua palavra-passe", "Escolher uma nova palavra-passe", "A sua palavra-passe foi alterada")]
    public async Task ResetAndChangedEmails_UseTheAccountLanguage(string locale, string resetSubject, string button, string changedSubject)
    {
        var client = Client(locale);
        var email = await ActiveUser.CreateAsync(client, Emails);
        Emails.Clear();

        // Asked from an English page: the account's language wins, not the request's.
        await PostAsync(Client("en"), "/api/v1/identity/password-reset-requests", new RequestPasswordResetRequest(email), HttpStatusCode.Accepted);
        var reset = Emails.Last!;
        await PostAsync(client, "/api/v1/identity/password-resets", new ResetPasswordRequest(ResetLink.TokenOf(reset.HtmlBody), "Revisar#2026!x"), HttpStatusCode.NoContent);
        var changed = Emails.Last!;

        reset.Subject.Should().StartWith(resetSubject);
        reset.HtmlBody.Should().Contain(button).And.Contain("reset-password?token=");
        reset.TextBody.Should().NotBeNullOrWhiteSpace();
        changed.Subject.Should().StartWith(changedSubject);
    }
}
