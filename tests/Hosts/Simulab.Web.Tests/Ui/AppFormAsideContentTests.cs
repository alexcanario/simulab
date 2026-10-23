using Bunit;
using MudBlazor;
using Simulab.Web.Components.Ui;
using Simulab.Web.Theme;

namespace Simulab.Web.Tests.Ui;

/// <summary>
/// F-126 BR3, BR6 and AC2, AC10: the aside gets a content slot after the checklist (F-43 BR4, "no button", is changed
/// here). The kit draws the block's divider, heading and subtitle; the page only fills the slot.
/// </summary>
public sealed class AppFormAsideContentTests : KitTestContext
{
    private IRenderedComponent<AppFormAside> RenderAside(bool withContent = true) =>
        Render<AppFormAside>(parameters =>
        {
            parameters
                .Add(aside => aside.Title, "Summary")
                .Add(aside => aside.EmptyText, "Not filled")
                .Add(aside => aside.Rows, [new AppAsideRow("Subject", "Law")])
                .Add(aside => aside.ChecklistTitle, "Before publishing")
                .Add(aside => aside.DoneText, "done")
                .Add(aside => aside.PendingText, "still missing")
                .Add(aside => aside.Checklist, [new AppChecklistItem("Topic", true)]);
            if (withContent)
            {
                parameters
                    .Add(aside => aside.ContentId, "question-section-publication")
                    .Add(aside => aside.ContentTitle, "Publication")
                    .Add(aside => aside.ContentSubtitle, "A draft is seen only here.")
                    .Add(aside => aside.ChildContent, builder => builder.AddMarkupContent(0, "<button id=\"do-it\" type=\"button\">Do it</button>"));
            }
        });

    [Fact]
    public void Content_IsASectionNamedByItsOwnHeadingInsideTheAside()
    {
        var aside = RenderAside();

        var block = aside.Find("aside.app-form-aside section#question-section-publication");
        var heading = block.QuerySelector("h2")!;
        heading.Id.Should().Be("question-section-publication-title");
        heading.TextContent.Trim().Should().Be("Publication");
        block.GetAttribute("aria-labelledby").Should().Be(heading.Id);
        block.TextContent.Should().Contain("A draft is seen only here.");
    }

    [Fact]
    public void Content_ComesAfterTheChecklistAndHoldsWhatThePagePassed()
    {
        var aside = RenderAside();

        var parts = aside.FindAll(".app-form-aside-checklist, section#question-section-publication");
        parts.Select(part => part.TagName).Should().Equal("UL", "SECTION");
        aside.Find("section#question-section-publication #do-it").TextContent.Should().Be("Do it");
    }

    [Fact]
    public void WithoutContent_TheAsideStillHasNoBlockAndNoButton()
    {
        var aside = RenderAside(withContent: false);

        aside.FindAll("aside.app-form-aside section").Should().BeEmpty();
        aside.FindAll("aside.app-form-aside button").Should().BeEmpty();
    }

    /// <summary>BR6, F-148 BR8: a disabled amber key sits on the page or surface colour, never on a sunken or tint background.</summary>
    [Theory]
    [InlineData("light")]
    [InlineData("dark")]
    public void AsideBackground_IsThePageOrTheSurfaceColour(string theme)
    {
        var palette = theme == "light" ? (Palette)SimulabTheme.Create().PaletteLight : SimulabTheme.Create().PaletteDark;
        string? declared;
        try
        {
            declared = AppCssColours.Declaration(".app-form-aside", "background-color");
        }
        catch (Exception)
        {
            declared = null; // no background of its own: the page colour shows through
        }

        if (declared is not null)
        {
            var colour = AppCssColours.Resolve(declared, palette);
            new[] { AppCssColours.Resolve("var(--mud-palette-background)", palette), AppCssColours.Resolve("var(--mud-palette-surface)", palette) }
                .Should().Contain(colour, $"the aside background must be the page or the surface colour ({theme})");
        }
    }

    /// <summary>The fallback of the screen pass: beside the form the aside is capped at the viewport and scrolls inside itself.</summary>
    [Fact]
    public void Aside_BesideTheForm_IsCappedAtTheViewportAndScrollsInsideItself()
    {
        AppCssColours.Declaration(".app-form-aside", "max-height").Should().Contain("100dvh");
        AppCssColours.Declaration(".app-form-aside", "overflow-y").Should().Be("auto");
        AppCssColours.Declaration(".app-form-aside", "overscroll-behavior").Should().Be("contain");
        AppCssColours.Declaration(".app-form-aside", "position").Should().Be("sticky");
    }

    [Fact]
    public void Aside_StackedUnderTheForm_IsNeitherStickyNorCapped()
    {
        AppCssColours.MediaDeclaration("max-width: 1279.98px", ".app-form-aside", "position").Should().Be("static");
        AppCssColours.MediaDeclaration("max-width: 1279.98px", ".app-form-aside", "max-height").Should().Be("none");
    }
}
