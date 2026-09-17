using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MudBlazor;
using Simulab.Web.Components.Layout;
using Simulab.Web.Tests.Ui;

namespace Simulab.Web.Tests.Layout;

/// <summary>Kit context plus the shell's environment: host environment, viewport, system theme and current URL.</summary>
public abstract class ShellTestContext : KitTestContext
{
    protected static readonly RenderFragment ProbeBody = builder =>
    {
        builder.OpenElement(0, "h1");
        builder.AddContent(1, "Probe page");
        builder.CloseElement();
    };

    protected void UseShellEnvironment(
        Breakpoint breakpoint = Breakpoint.Lg,
        bool systemDark = false,
        string environment = "Development",
        string url = "/")
    {
        Services.AddSingleton<IHostEnvironment>(new FakeHostEnvironment(environment));
        Services.AddSingleton<IBrowserViewportService>(new FakeViewport(breakpoint));
        JSInterop.Setup<bool>(_ => true).SetResult(systemDark);
        Services.GetRequiredService<Bunit.TestDoubles.BunitNavigationManager>().NavigateTo(url);
    }

    protected IEnumerable<string?> CookieWrites(string name) =>
        JSInterop.Invocations[PreferenceWriter.SetFunction]
            .Where(i => (string?)i.Arguments[0] == name)
            .Select(i => (string?)i.Arguments[1]);
}
