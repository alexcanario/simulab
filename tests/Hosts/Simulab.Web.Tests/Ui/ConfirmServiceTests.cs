using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using MudBlazor;
using Simulab.Web.Components.Ui;
using Simulab.Web.Resources;

namespace Simulab.Web.Tests.Ui;

public class ConfirmServiceTests : KitTestContext
{
    private (IRenderedComponent<MudDialogProvider> Provider, Task<bool> Answer) OpenDelete()
    {
        var provider = Render<MudDialogProvider>();
        var confirm = Services.GetRequiredService<IConfirmService>();
        var l = Services.GetRequiredService<IStringLocalizer<SharedResources>>();

        Task<bool> answer = null!;
        provider.InvokeAsync(() => { answer = confirm.ConfirmDeleteAsync(l, "exam board ABC", "Its exams are kept."); });
        provider.WaitForAssertion(() => provider.FindAll(".app-confirm-ok").Should().ContainSingle());
        return (provider, answer);
    }

    [Fact]
    public void ConfirmDeleteAsync_Shown_ConfirmButtonIsErrorAndNamesActionAndObject()
    {
        var (provider, _) = OpenDelete();

        provider.Find(".app-confirm-ok").TextContent.Trim().Should().Be("Delete exam board ABC");
        provider.Markup.Should().Contain("Its exams are kept.");
        provider.FindComponents<MudButton>().Single(b => b.Instance.Class == "app-confirm-ok").Instance.Color.Should().Be(Color.Error);
        provider.Find(".app-confirm-cancel").TextContent.Trim().Should().Be("Cancel");
    }

    [Fact]
    public async Task ConfirmDeleteAsync_ConfirmClicked_ReturnsTrue()
    {
        var (provider, answer) = OpenDelete();

        provider.Find(".app-confirm-ok").Click();

        (await answer).Should().BeTrue();
    }

    [Fact]
    public async Task ConfirmDeleteAsync_CancelClicked_ReturnsFalse()
    {
        var (provider, answer) = OpenDelete();

        provider.Find(".app-confirm-cancel").Click();

        (await answer).Should().BeFalse();
    }

    [Fact]
    public void Options_Always_CloseOnEscapeAndNotOnBackdrop()
    {
        // Esc is handled by MudBlazor's key interceptor (JS); the kit guarantees it is switched on.
        ConfirmService.Options.CloseOnEscapeKey.Should().BeTrue();
        ConfirmService.Options.BackdropClick.Should().BeFalse();
    }

    [Fact]
    public async Task ConfirmAsync_DialogClosedWithEsc_ReturnsFalse()
    {
        var provider = Render<MudDialogProvider>();
        var confirm = Services.GetRequiredService<IConfirmService>();
        Task<bool> answer = null!;
        await provider.InvokeAsync(() => { answer = confirm.ConfirmAsync(new ConfirmRequest("T", "M", "Do it", Destructive: false)); });
        provider.WaitForAssertion(() => provider.FindAll(".app-confirm-ok").Should().ContainSingle());

        // Esc closes the dialog as a cancel: the same path as the dialog instance's Cancel().
        var dialog = provider.FindComponent<MudDialogContainer>();
        await provider.InvokeAsync(() => ((IMudDialogInstance)dialog.Instance).Cancel());

        (await answer).Should().BeFalse();
    }
}
