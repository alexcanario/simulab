using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Simulab.Web.Components.Pages.Dev;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.Ui;

/// <summary>
/// F-134 AC10 (BR6): the state of a whole page is one kit component (icon, title, message, an actions slot), shown in
/// the gallery section <c>gallery-states</c>. A message wider than the component wraps in the kit's element.
/// </summary>
public sealed class AppPageStateTests : KitTestContext
{
    private static readonly RenderFragment TwoActions = builder =>
    {
        builder.OpenElement(0, "a");
        builder.AddAttribute(1, "href", "/one");
        builder.AddContent(2, "First action");
        builder.CloseElement();
        builder.OpenElement(3, "a");
        builder.AddAttribute(4, "href", "/two");
        builder.AddContent(5, "Second action");
        builder.CloseElement();
    };

    private IRenderedComponent<AppPageState> RenderState(string message = "Something happened.", int titleLevel = 1) =>
        Render<AppPageState>(parameters => parameters
            .Add(p => p.Icon, AppIcons.Error)
            .Add(p => p.Title, "A title")
            .Add(p => p.Message, message)
            .Add(p => p.TitleLevel, titleLevel)
            .Add(p => p.Actions, TwoActions));

    [Fact]
    public void Render_ShowsTheIconTheTitleTheMessageAndTheActions()
    {
        var state = RenderState();

        state.Find(".app-page-state .mud-icon-root").GetAttribute("aria-hidden").Should().Be("true");
        state.Find(".app-page-state h1").TextContent.Trim().Should().Be("A title");
        state.Find(".app-page-state-message").TextContent.Trim().Should().Be("Something happened.");
        state.FindAll(".app-page-state-actions a").Select(a => a.TextContent.Trim())
            .Should().Equal("First action", "Second action");
    }

    [Fact]
    public void Render_WithTitleLevelTwo_UsesASecondLevelHeading()
    {
        var state = RenderState(titleLevel: 2);

        state.FindAll("h1").Should().BeEmpty();
        state.Find(".app-page-state h2").TextContent.Trim().Should().Be("A title");
    }

    [Fact]
    public void Render_WithoutActions_HasNoActionsRow()
    {
        var state = Render<AppPageState>(parameters => parameters
            .Add(p => p.Icon, AppIcons.Error)
            .Add(p => p.Title, "A title")
            .Add(p => p.Message, "Only a message."));

        state.Find(".app-page-state h1").TextContent.Trim().Should().Be("A title");
        state.FindAll(".app-page-state-actions").Should().BeEmpty();
    }

    [Fact]
    public void Render_WithAMessageWiderThanTheComponent_WrapsInTheKitElement()
    {
        var state = RenderState(new string('w', 400));

        var message = state.Find(".app-page-state-message");
        message.HasAttribute("style").Should().BeFalse("no fixed width and no nowrap on the page");
        state.Find(".app-page-state").HasAttribute("style").Should().BeFalse();
        AppCssColours.Declaration(".app-page-state-message", "overflow-wrap").Should().Be("anywhere");
    }
}

/// <summary>F-134 AC10: the gallery shows the page state.</summary>
public sealed class UiGalleryPageStateTests : KitTestContext
{
    [Fact]
    public void Gallery_StatesSection_ShowsAPageStateWithIconTitleMessageAndTwoActions()
    {
        Services.AddSingleton<IHostEnvironment>(new FakeHostEnvironment(Environments.Development));
        Render<MudBlazor.MudPopoverProvider>();
        var gallery = Render<UiGallery>(parameters => parameters.Add(p => p.SourceDelay, TimeSpan.Zero));

        var state = gallery.Find("#gallery-states .app-page-state");
        state.QuerySelector(".mud-icon-root").Should().NotBeNull();
        state.QuerySelector("h2")!.TextContent.Trim().Should().NotBeEmpty("the gallery page owns the one h1");
        state.QuerySelector(".app-page-state-message")!.TextContent.Trim().Should().NotBeEmpty();
        state.QuerySelectorAll(".app-page-state-actions a, .app-page-state-actions button").Should().HaveCountGreaterThanOrEqualTo(2);
    }
}
