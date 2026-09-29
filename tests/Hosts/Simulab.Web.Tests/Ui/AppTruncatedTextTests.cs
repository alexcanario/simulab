using Bunit;
using MudBlazor;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.Ui;

/// <summary>
/// F-36 BR13 and AC17: the truncated text can be a link (one tab stop) and can carry the language of its content.
/// Without the new parameters it is the focusable span it always was.
/// </summary>
public sealed class AppTruncatedTextTests : KitTestContext
{
    private const string LongName = "Analista Judiciario - Area Administrativa - Especialidade em Gestao de Pessoas";

    private IRenderedComponent<AppTruncatedText> RenderText(string? href = null, string? lang = null)
    {
        Render<MudPopoverProvider>();
        return Render<AppTruncatedText>(parameters => parameters
            .Add(p => p.Text, LongName)
            .Add(p => p.MaxWidth, "10rem")
            .Add(p => p.Href, href)
            .Add(p => p.Lang, lang));
    }

    [Fact]
    public void Render_WithoutHref_IsTheFocusableSpanWithNoLanguage()
    {
        var text = RenderText();

        var span = text.Find("span.app-truncate");
        span.GetAttribute("tabindex").Should().Be("0");
        span.GetAttribute("lang").Should().BeNull();
        span.TextContent.Should().Be(LongName);
        text.FindAll("a").Should().BeEmpty();
    }

    [Fact]
    public void Render_WithHref_IsOneLinkWithTheTruncationAndNoOtherTabStop()
    {
        var text = RenderText(href: "/catalog/exams/1?scope=State", lang: "pt-BR");

        var link = text.Find("a.app-link");
        link.GetAttribute("href").Should().Be("/catalog/exams/1?scope=State");
        link.ClassList.Should().Contain("app-truncate");
        link.GetAttribute("style").Should().Contain("max-width: 10rem");
        link.GetAttribute("lang").Should().Be("pt-BR");
        link.TextContent.Should().Be(LongName);
        text.FindAll("a").Should().ContainSingle();
        text.FindAll("[tabindex]").Should().BeEmpty();
    }

    [Fact]
    public void Render_WithHref_KeepsTheFullTextAsTheTooltip()
    {
        var text = RenderText(href: "/catalog/exams/1");

        text.FindComponent<MudTooltip>().Instance.Text.Should().Be(LongName);
    }

    [Fact]
    public void Render_WithLangOnly_MarksTheSpanAndStaysFocusable()
    {
        var text = RenderText(lang: "en");

        var span = text.Find("span.app-truncate");
        span.GetAttribute("lang").Should().Be("en");
        span.GetAttribute("tabindex").Should().Be("0");
    }
}
