using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Simulab.Web.Components.Pages.Admin;

namespace Simulab.Web.Tests.Admin;

/// <summary>
/// F-185 AC8 (review finding): a real click on a period button reaches the Api as <c>days</c>. The page tests of the
/// item send the range from code; this goes through the button, the range with its dates and the page's reading of
/// <c>Days</c>. Added after the lock, in a new file.
/// </summary>
public sealed class PeriodButtonClickTests : AdminPageTestContext
{
    [Fact]
    public void AccountEvents_ClickingThe90DaysButton_SendsDays90AndPressesIt()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("/admin/account-events");
        var page = Render<AccountEvents>();
        page.WaitForAssertion(() => page.FindAll("#account-events-period-filter-90").Should().ContainSingle());

        page.Find("#account-events-period-filter-90").Click();

        page.WaitForAssertion(() =>
            Api.Received.Last(call => call.Path.EndsWith("/account-events", StringComparison.Ordinal)).Query.Should().Contain("days=90"));
        page.WaitForAssertion(() => page.Find("#account-events-period-filter-90").GetAttribute("aria-pressed").Should().Be("true"));
    }

    [Fact]
    public void RoleHistory_ClickingTheAllButton_ClearsTheDays()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("/admin/role-history?days=30");
        var page = Render<RoleHistory>();
        page.WaitForAssertion(() => page.Find("#history-period-filter-30").GetAttribute("aria-pressed").Should().Be("true"));

        page.Find("#history-period-filter-all").Click();

        page.WaitForAssertion(() =>
            Api.Received.Last(call => call.Path.EndsWith("/role-changes", StringComparison.Ordinal)).Query.Should().NotContain("days="));
        page.WaitForAssertion(() => page.Find("#history-period-filter-all").GetAttribute("aria-pressed").Should().Be("true"));
    }
}
