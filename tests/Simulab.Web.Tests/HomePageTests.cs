using System.Globalization;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Simulab.Web.Components.Pages;
using Simulab.Web.Localization;

namespace Simulab.Web.Tests;

public class HomePageTests : BunitContext
{
    public HomePageTests()
    {
        Services.AddLocalization();
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Theory]
    [InlineData("en", "Practice like it is exam day")]
    [InlineData("pt-BR", "Pratique como se fosse o dia da prova")]
    [InlineData("pt-PT", "Simulações realistas")]
    public void Home_is_shown_in_the_user_language(string cultureName, string expected)
    {
        var previous = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = new CultureInfo(cultureName);
        try
        {
            var page = Render<Home>();

            page.Markup.Should().Contain(expected);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Theory]
    [InlineData(null, "/")]
    [InlineData("", "/")]
    [InlineData("/exams/1", "/exams/1")]
    [InlineData("https://evil.example", "/")]
    [InlineData("//evil.example", "/")]
    [InlineData("/\\evil.example", "/")]
    public void Language_switch_only_returns_to_a_page_inside_the_app(string? redirectUri, string expected)
    {
        SupportedCultures.SafeLocalPath(redirectUri).Should().Be(expected);
    }
}
