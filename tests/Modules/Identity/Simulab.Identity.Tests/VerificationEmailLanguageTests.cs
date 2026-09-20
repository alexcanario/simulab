using System.Net;

namespace Simulab.Identity.Tests;

/// <summary>AC10: the email is written in the language the account was created in.</summary>
public sealed class VerificationEmailLanguageTests : IdentityApiTests
{
    private const string RegisterRoute = "/api/v1/identity/registrations";

    [Theory]
    [InlineData("en", "Confirm your email", "Confirm email")]
    [InlineData("pt-BR", "Confirme seu e-mail", "Confirmar e-mail")]
    [InlineData("pt-PT", "Confirme o seu e-mail", "Confirmar e-mail")]
    public async Task Register_WritesTheEmailInTheRequestLanguage(string locale, string subject, string button)
    {
        await RunJobsAsync();
        Emails.Clear();
        var request = SignUpForm.Valid($"idioma.{locale}@exemplo.com");

        await PostAsync(Client(locale), RegisterRoute, request, HttpStatusCode.Accepted);

        await RunJobsAsync();
        var message = Emails.Last!;
        message.Subject.Should().StartWith(subject);
        message.HtmlBody.Should().Contain(button);
        message.HtmlBody.Should().Contain("verify-email?token=");
        message.TextBody.Should().NotBeNullOrWhiteSpace();

        var user = await QueryAsync(context => Task.FromResult(context.Users.Single(u => u.Email == request.Email)));
        user.PreferredLanguage.Should().Be(locale);
    }

    [Fact]
    public async Task Register_UnknownLanguage_FallsBackToEnglish()
    {
        await RunJobsAsync();
        Emails.Clear();
        var request = SignUpForm.Valid("idioma.desconhecido@exemplo.com");

        await PostAsync(Client("de-DE"), RegisterRoute, request, HttpStatusCode.Accepted);

        await RunJobsAsync();
        Emails.Last!.Subject.Should().StartWith("Confirm your email");
    }
}
