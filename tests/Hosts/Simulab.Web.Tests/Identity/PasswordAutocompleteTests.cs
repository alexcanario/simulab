using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Simulab.Web.Components.Pages.Identity;
using Simulab.Web.Services.Auth;
using Simulab.Web.Tests.Auth;

namespace Simulab.Web.Tests.Identity;

/// <summary>
/// B-5 AC1-AC2: each password input tells a password manager which password it asks for: the one the user
/// has (fill it) or a new one (offer to generate one).
/// </summary>
public sealed class PasswordAutocompleteTests : IdentityPageTestContext
{
    public PasswordAutocompleteTests()
    {
        Services.AddSingleton(_ => new FakeAuthApi());
        Services.AddSingleton(provider => new AuthClient(provider.GetRequiredService<FakeAuthApi>().Client(), Options.Create(new OpenIddictClientOptions())));
        Services.AddSingleton<SignInTicketStore>();
    }

    private static string? AutocompleteOf<TPage>(IRenderedComponent<TPage> page, string id)
        where TPage : IComponent =>
        page.Find($"#{id}").GetAttribute("autocomplete");

    [Fact]
    public void SignIn_AsksForTheCurrentPassword()
    {
        var page = Render<SignIn>();

        AutocompleteOf(page, "sign-in-password").Should().Be("current-password");
    }

    [Fact]
    public void SignUp_AsksForANewPasswordTwice()
    {
        var page = Render<SignUp>();

        AutocompleteOf(page, "sign-up-password").Should().Be("new-password");
        AutocompleteOf(page, "sign-up-confirm").Should().Be("new-password");
    }

    [Fact]
    public void ChangePassword_AsksForTheCurrentOneThenANewOne()
    {
        var store = new InMemoryWebSessionStore();
        Services.AddSingleton(provider => new WebSessionTokenAccessor(
            store, provider.GetRequiredService<AuthClient>(), new SessionRefreshGate(), TimeProvider.System));
        Authorization.SetAuthorized("ana@exemplo.com");

        var page = Render<ChangePassword>();

        AutocompleteOf(page, "change-password-current").Should().Be("current-password");
        AutocompleteOf(page, "change-password-new").Should().Be("new-password");
        AutocompleteOf(page, "change-password-confirm").Should().Be("new-password");
    }

    [Fact]
    public void ResetPassword_AsksForANewPasswordTwice()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("/reset-password?token=abc");

        var page = Render<ResetPassword>();

        page.WaitForAssertion(() => page.FindAll("#reset-password-new").Should().ContainSingle());
        AutocompleteOf(page, "reset-password-new").Should().Be("new-password");
        AutocompleteOf(page, "reset-password-confirm").Should().Be("new-password");
    }

    /// <summary>
    /// AC3: the kit field has no default. A page that leaves it out gets the compiler's RZ2012 warning, which the
    /// gate refuses (checked by hand on 2026-09-19 by removing it from the sign-in page).
    /// </summary>
    [Fact]
    public void KitField_RequiresTheAutocomplete()
    {
        var property = typeof(Simulab.Web.Components.Ui.AppPasswordField).GetProperty(nameof(Simulab.Web.Components.Ui.AppPasswordField.Autocomplete))!;

        property.PropertyType.Should().Be<Simulab.Web.Components.Ui.PasswordAutocomplete>();
        property.GetCustomAttributes(typeof(EditorRequiredAttribute), inherit: false).Should().ContainSingle();
    }
}
