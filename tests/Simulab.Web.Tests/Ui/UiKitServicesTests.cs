using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.Ui;

public class UiKitServicesTests : KitTestContext
{
    [Fact]
    public void AddUiKit_Snackbar_BottomRightFourSecondsClosable()
    {
        var configuration = Services.GetRequiredService<ISnackbar>().Configuration;

        configuration.PositionClass.Should().Be(Defaults.Classes.Position.BottomRight);
        configuration.VisibleStateDuration.Should().Be(4000);
        configuration.ShowCloseIcon.Should().BeTrue();
    }

    [Fact]
    public void AddUiKit_KitServices_AreRegistered()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLocalization();
        services.AddUiKit();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IConfirmService>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<ErrorText>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<ThemeState>().Should().NotBeNull();
    }

    [Fact]
    public void ThemeState_SetDarkMode_RaisesChangedOnlyOnChange()
    {
        var state = new ThemeState();
        var raised = 0;
        state.Changed += () => raised++;

        state.SetDarkMode(true);
        state.SetDarkMode(true);

        state.IsDarkMode.Should().BeTrue();
        raised.Should().Be(1);
    }
}
