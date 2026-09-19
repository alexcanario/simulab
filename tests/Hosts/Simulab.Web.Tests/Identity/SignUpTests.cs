using System.Net;
using Bunit;
using Simulab.Identity.Contracts;
using Simulab.Web.Components.Pages.Identity;

namespace Simulab.Web.Tests.Identity;

/// <summary>The sign-up screen: what the visitor sees in each state the feature file lists.</summary>
public class SignUpTests : IdentityPageTestContext
{
    private IRenderedComponent<SignUp> Render() => Render<SignUp>();

    private static void Fill(IRenderedComponent<SignUp> page, string password = "Estudar#2026!", string confirm = "Estudar#2026!")
    {
        page.Find("#sign-up-email").Change("ana@exemplo.com");
        page.Find("#sign-up-password").Change(password);
        page.Find("#sign-up-confirm").Change(confirm);
        foreach (var box in page.FindAll("input[type=checkbox]"))
        {
            box.Change(true);
        }
    }

    private static AngleSharp.Html.Dom.IHtmlInputElement Checkbox(IRenderedComponent<SignUp> page, string id) =>
        (AngleSharp.Html.Dom.IHtmlInputElement)page.Find($"#{id}");

    [Fact]
    public void Ready_ShowsEveryFieldAndTheThreeAcceptances()
    {
        var page = Render();

        page.Find("#sign-up-email").Should().NotBeNull();
        page.Find("#sign-up-password").Should().NotBeNull();
        page.Find("#sign-up-confirm").Should().NotBeNull();
        page.Find("#sign-up-full-name").Should().NotBeNull();
        page.FindAll("input[type=checkbox]").Should().HaveCount(3);
    }

    /// <summary>B-7 AC4: the comfort limits are the Api's own, read from the contracts.</summary>
    [Fact]
    public void Fields_AreLimitedToTheAccountLimits()
    {
        var page = Render();

        page.Find("#sign-up-full-name").GetAttribute("maxlength").Should().Be("120");
        page.Find("#sign-up-email").GetAttribute("maxlength").Should().Be("254");
    }

    [Fact]
    public void Submit_EmptyForm_ShowsOneMessagePerRule()
    {
        var page = Render();

        page.Find("button.app-sign-up-submit").Click();

        var text = page.Markup;
        text.Should().Contain("Enter your email.");
        text.Should().Contain("Enter a password.");
        text.Should().Contain("You must be 18 or older");
        text.Should().Contain("Accept the terms of use");
        Api.CountOf("/registrations").Should().Be(0, "nothing is sent while the form is invalid");
    }

    [Fact]
    public void Submit_PasswordsThatDoNotMatch_ShowsTheMismatch()
    {
        var page = Render();
        Fill(page, confirm: "Outra#Senha2026!");

        page.Find("button.app-sign-up-submit").Click();

        page.Markup.Should().Contain("The two passwords are not the same.");
        Api.CountOf("/registrations").Should().Be(0);
    }

    [Fact]
    public void Submit_WeakPassword_IsRefusedBeforeReachingTheApi()
    {
        var page = Render();
        Fill(page, "fraca", "fraca");

        page.Find("button.app-sign-up-submit").Click();

        page.Markup.Should().Contain("does not follow the rules above");
        Api.CountOf("/registrations").Should().Be(0);
    }

    [Fact]
    public void Submit_ValidForm_SendsTheVersionsThePageShowed()
    {
        var page = Render();
        Fill(page);

        page.Find("button.app-sign-up-submit").Click();

        Api.CountOf("/registrations").Should().Be(1);
    }

    [Fact]
    public void DraftLegalText_ShowsTheDraftNotice()
    {
        Api.Terms = Document(LegalTopic.Terms, placeholder: true);

        var page = Render();

        page.Markup.Should().Contain("Draft text");
    }

    [Fact]
    public void LegalDocumentsUnavailable_DisablesSubmit()
    {
        Api.Terms = null;

        var page = Render();

        page.Markup.Should().Contain("sign-up is unavailable right now");
        page.Find("button.app-sign-up-submit").HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public void TermsChangedWhileTheFormWasOpen_ClearsTheAcceptancesAndSaysWhy()
    {
        var page = Render();
        Fill(page);
        Api.RegisterFailure = (HttpStatusCode.Conflict, IdentityErrorCodes.TermsVersionOutdated);

        page.Find("button.app-sign-up-submit").Click();

        page.Markup.Should().Contain("The legal texts changed while you were filling the form");

        // Only the two acceptances are asked again: the age declaration is not tied to a document version.
        Checkbox(page, "sign-up-terms").IsChecked.Should().BeFalse();
        Checkbox(page, "sign-up-privacy").IsChecked.Should().BeFalse();
        Checkbox(page, "sign-up-adult").IsChecked.Should().BeTrue();
    }

    [Fact]
    public void ServerError_ShowsTheTranslatedCodeAndNeverTheCodeItself()
    {
        var page = Render();
        Fill(page);
        Api.RegisterFailure = (HttpStatusCode.TooManyRequests, IdentityErrorCodes.RegistrationRateLimited);

        page.Find("button.app-sign-up-submit").Click();

        page.Markup.Should().Contain("Too many attempts from this device");
        page.Markup.Should().NotContain(IdentityErrorCodes.RegistrationRateLimited);
    }
}
