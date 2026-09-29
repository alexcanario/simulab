using Bunit;
using MudBlazor;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.Ui;

public class AppPageHeaderTests : KitTestContext
{
    [Fact]
    public void Render_TitleBreadcrumbAndAction_ShowsAllWithOneFilledButton()
    {
        var clicked = false;
        var header = Render<AppPageHeader>(parameters => parameters
            .Add(p => p.Title, "Exam boards")
            .Add(p => p.Breadcrumbs, [new BreadcrumbItem("Catalog", "/catalog"), new BreadcrumbItem("Exam boards", null, disabled: true)])
            .Add(p => p.PrimaryActionText, "Add")
            .Add(p => p.PrimaryActionIcon, AppIcons.Add)
            .Add(p => p.OnPrimaryAction, () => clicked = true));

        header.Find("h1").TextContent.Should().Be("Exam boards");
        header.Find(".app-breadcrumbs").TextContent.Should().Contain("Catalog");
        header.FindComponents<MudButton>().Where(b => b.Instance.Variant == Variant.Filled).Should().ContainSingle();

        header.Find(".app-primary-action").Click();
        clicked.Should().BeTrue();
    }

    // F-36 AC17: a title that is content (an exam's name) carries its own language; the app's own titles carry none.
    [Fact]
    public void Render_TitleLang_SetsTheLanguageOfTheH1()
    {
        var header = Render<AppPageHeader>(parameters => parameters
            .Add(p => p.Title, "Guarda Municipal")
            .Add(p => p.TitleLang, "pt-BR"));

        header.Find("h1").GetAttribute("lang").Should().Be("pt-BR");
    }

    [Fact]
    public void Render_WithoutTitleLang_TheH1HasNoLang()
    {
        var header = Render<AppPageHeader>(parameters => parameters.Add(p => p.Title, "Home"));

        header.Find("h1").HasAttribute("lang").Should().BeFalse();
    }

    [Fact]
    public void Render_WithoutAction_ShowsNoButton()
    {
        var header = Render<AppPageHeader>(parameters => parameters.Add(p => p.Title, "Home"));

        header.FindAll("button").Should().BeEmpty();
        header.FindAll(".app-breadcrumbs").Should().BeEmpty();
    }
}
