using Bunit;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.Ui;

/// <summary>F-169: the kit's save status, a polite live region that is in the page from the start and says one thing at a time.</summary>
public sealed class AppSaveStatusTests : KitTestContext
{
    private IRenderedComponent<AppSaveStatus> RenderStatus(AppSaveState state) =>
        Render<AppSaveStatus>(parameters => parameters.Add(status => status.State, state));

    [Fact]
    public void None_IsAnEmptyLiveRegion()
    {
        var status = RenderStatus(AppSaveState.None);

        status.Find("[role=status]").TextContent.Trim().Should().BeEmpty();
    }

    [Fact]
    public void Saving_SaysSo_AndHidesTheProgressFromAssistiveTechnology()
    {
        var status = RenderStatus(AppSaveState.Saving);

        status.Find("[role=status]").TextContent.Trim().Should().Be("Saving…");
        status.Find("[role=progressbar]").ParentElement!.GetAttribute("aria-hidden").Should().Be("true");
    }

    [Theory]
    [InlineData(AppSaveState.Saved, "Answer saved")]
    [InlineData(AppSaveState.Cleared, "Answer cleared")]
    public void SavedAndCleared_SayTheirWords_WithADecorativeCheck(AppSaveState state, string text)
    {
        var status = RenderStatus(state);

        status.Find("[role=status]").TextContent.Trim().Should().Be(text);
        status.Find("[role=status] svg").GetAttribute("aria-hidden").Should().Be("true");
    }

    // The live region is the same element across states, so a screen reader hears the change and not a new region.
    [Fact]
    public void ChangingTheState_KeepsTheSameLiveRegion()
    {
        var status = RenderStatus(AppSaveState.None);

        status.Render(parameters => parameters.Add(s => s.State, AppSaveState.Saved));

        status.FindAll("[role=status]").Should().ContainSingle().Which.TextContent.Trim().Should().Be("Answer saved");
    }
}
