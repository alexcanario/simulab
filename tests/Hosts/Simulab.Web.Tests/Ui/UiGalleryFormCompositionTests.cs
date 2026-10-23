using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Simulab.Web.Components.Pages.Dev;

namespace Simulab.Web.Tests.Ui;

/// <summary>
/// F-126 AC2 (BR2, BR3, BR6): the dev gallery shows the three kit changes of the question form — a numbered section
/// card, an aside whose content slot holds actions, and the action key with another icon, with an id, busy and
/// disabled. A screen mockup may use only what the gallery shows (rule: ui).
/// </summary>
public sealed class UiGalleryFormCompositionTests : KitTestContext
{
    private IRenderedComponent<UiGallery> RenderGallery()
    {
        Services.AddSingleton<IHostEnvironment>(new FakeHostEnvironment(Environments.Development));
        Render<MudBlazor.MudPopoverProvider>();
        return Render<UiGallery>(parameters => parameters.Add(p => p.SourceDelay, TimeSpan.Zero));
    }

    [Fact]
    public void Gallery_ShowsANumberedSectionCard()
    {
        var gallery = RenderGallery();

        var tag = gallery.Find("#gallery-section-card .app-section-card-number");
        tag.TextContent.Trim().Should().NotBeEmpty();
        tag.GetAttribute("aria-hidden").Should().Be("true");
    }

    [Fact]
    public void Gallery_ShowsAnAsideWhoseContentSlotHoldsActions()
    {
        var gallery = RenderGallery();

        var block = gallery.Find("#gallery-form-aside aside.app-form-aside section");
        block.QuerySelector("h2")!.TextContent.Trim().Should().NotBeEmpty();
        block.QuerySelectorAll("button").Should().NotBeEmpty("the slot holds actions, so the aside is no longer buttonless (F-126 BR3)");
    }

    [Fact]
    public void Gallery_ShowsTheActionKeyWithAnotherIconAnIdBusyAndDisabled()
    {
        var gallery = RenderGallery();

        var keys = gallery.FindAll("#gallery-buttons .app-action-key");
        keys.Should().HaveCountGreaterThanOrEqualTo(4);
        keys.Where(key => key.HasAttribute("id")).Should().NotBeEmpty("a key with an id");
        keys.Where(key => key.QuerySelector(".mud-progress-circular") is not null).Should().NotBeEmpty("a busy key");
        keys.Where(key => key.HasAttribute("disabled") && key.QuerySelector(".mud-progress-circular") is null).Should().NotBeEmpty("a disabled key");
        keys.Select(key => key.QuerySelector(".app-action-icon svg")?.InnerHtml).Where(icon => icon is not null).Distinct()
            .Should().HaveCountGreaterThanOrEqualTo(2, "the key shows more than one icon");
    }
}
