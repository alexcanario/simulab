using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.Ui;

public class AppFormActionsTests : KitTestContext
{
    private sealed class FakeConfirm(bool answer) : IConfirmService
    {
        public int Calls { get; private set; }

        public Task<bool> ConfirmAsync(ConfirmRequest request)
        {
            Calls++;
            request.Destructive.Should().BeTrue();
            return Task.FromResult(answer);
        }
    }

    private FakeConfirm UseConfirm(bool answer)
    {
        var fake = new FakeConfirm(answer);
        Services.AddSingleton<IConfirmService>(fake);
        return fake;
    }

    [Theory]
    [InlineData(false, NavigationState.Prevented)]
    [InlineData(true, NavigationState.Succeeded)]
    public void Navigate_WithChanges_AsksAndFollowsTheAnswer(bool answer, NavigationState expected)
    {
        var confirm = UseConfirm(answer);
        var navigation = Services.GetRequiredService<BunitNavigationManager>();
        Render<AppFormActions>(parameters => parameters.Add(p => p.HasChanges, true));

        navigation.NavigateTo("/elsewhere");

        confirm.Calls.Should().Be(1);
        navigation.History.First().State.Should().Be(expected);
    }

    [Fact]
    public void Navigate_WithoutChanges_DoesNotAsk()
    {
        var confirm = UseConfirm(false);
        var navigation = Services.GetRequiredService<BunitNavigationManager>();
        Render<AppFormActions>(parameters => parameters.Add(p => p.HasChanges, false));

        navigation.NavigateTo("/elsewhere");

        confirm.Calls.Should().Be(0);
        navigation.History.First().State.Should().Be(NavigationState.Succeeded);
    }

    [Fact]
    public void BrowserTabGuard_FollowsHasChanges()
    {
        UseConfirm(true);
        var form = Render<AppFormActions>(parameters => parameters.Add(p => p.HasChanges, true));
        form.FindComponent<NavigationLock>().Instance.ConfirmExternalNavigation.Should().BeTrue();

        form.Render(parameters => parameters.Add(p => p.HasChanges, false));

        form.FindComponent<NavigationLock>().Instance.ConfirmExternalNavigation.Should().BeFalse();
    }

    [Fact]
    public void Render_Saving_SaveDisabledWithProgressAndCancelIsTextOnItsLeft()
    {
        UseConfirm(true);
        var form = Render<AppFormActions>(parameters => parameters
            .Add(p => p.Saving, true)
            .Add(p => p.OnSave, () => { }));

        var buttons = form.FindComponents<MudButton>();
        buttons[0].Instance.Class.Should().Be("app-form-cancel");
        buttons[0].Instance.Variant.Should().Be(Variant.Text);
        buttons[1].Instance.Class.Should().Be("app-form-save");
        buttons[1].Instance.Variant.Should().Be(Variant.Filled);
        form.Find(".app-form-save").HasAttribute("disabled").Should().BeTrue();
        form.FindComponents<MudProgressCircular>().Should().ContainSingle();
        form.Find(".app-form-save").TextContent.Should().Contain("Saving...");
    }

    [Fact]
    public void Render_Idle_SaveEnabledAndInvokesHandler()
    {
        UseConfirm(true);
        var saved = false;
        var form = Render<AppFormActions>(parameters => parameters.Add(p => p.OnSave, () => saved = true));

        form.Find(".app-form-save").HasAttribute("disabled").Should().BeFalse();
        form.FindComponents<MudProgressCircular>().Should().BeEmpty();
        form.Find(".app-form-save").Click();

        saved.Should().BeTrue();
    }

    [Fact]
    public void Render_WithoutSaveHandler_SaveSubmitsTheForm()
    {
        UseConfirm(true);
        var form = Render<AppFormActions>();

        form.Find(".app-form-save").GetAttribute("type").Should().Be("submit");
    }
}
