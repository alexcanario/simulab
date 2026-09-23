using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Simulab.Identity.Contracts;
using Simulab.Web.Components.Pages.Admin;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Tests.Admin;

/// <summary>F-21 `/admin/account-events`: the list, its filters in the address, the two chips and the states (AC8, AC9, AC12, AC14).</summary>
public sealed class AccountEventsPageTests : AdminPageTestContext
{
    private static readonly Guid Bruno = Guid.Parse("0198f0a2-0000-7000-8000-0000000000a2");
    private static readonly AccountEventAccountResponse Ana = new(Guid.Parse("0198f0a2-0000-7000-8000-0000000000a1"), "ana.souza@exemplo.com.br");
    private static readonly DateTimeOffset At = new(2026, 9, 20, 15, 30, 0, TimeSpan.Zero);

    private static readonly AccountEventResponse SignedIn = new(
        Guid.NewGuid(), At, Ana, AccountEventTypes.SignInSucceeded, AccountEventMethods.Password, null, "203.0.113.10");

    private static readonly AccountEventResponse Unknown = new(
        Guid.NewGuid(), At.AddMinutes(-5), null, AccountEventTypes.SignInFailed, null, AccountEventReasons.UnknownAccount, "198.51.100.7");

    private static readonly AccountEventResponse Erased = new(
        Guid.NewGuid(), At.AddMinutes(-10), new AccountEventAccountResponse(Guid.NewGuid(), null), AccountEventTypes.AccountErased, null, null, null);

    private IRenderedComponent<AccountEvents> RenderPage(string? query = null)
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo($"/admin/account-events{query}");
        return Render<AccountEvents>();
    }

    private string LastListQuery() =>
        Api.Received.Last(call => call.Path.EndsWith("/account-events", StringComparison.Ordinal)).Query ?? string.Empty;

    // AC12, AC9: what each row says, including an account with no email and an attempt with no account.
    [Fact]
    public void Load_ShowsWhenAccountEventDetailsAndAddress()
    {
        Api.AccountEvents = [SignedIn, Unknown, Erased];

        var page = RenderPage();

        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(3));
        page.FindAll("tbody tr")[0].TextContent.Should()
            .Contain(At.ToString("g", CultureInfo.CurrentCulture))
            .And.Contain("ana.souza@exemplo.com.br")
            .And.Contain("Signed in")
            .And.Contain("with the password")
            .And.Contain("203.0.113.10");
        page.FindAll("tbody tr")[1].TextContent.Should()
            .Contain("Unknown account").And.Contain("Sign-in failed").And.Contain("unknown account");
        page.FindAll("tbody tr")[2].TextContent.Should().Contain("Erased account").And.Contain("Account erased");
        LastListQuery().Should().Contain("page=0&").And.Contain("ascending=false");
    }

    // AC12: the states.
    [Fact]
    public void Empty_WithoutFilters_SaysNothingWasRecorded_WithAFilter_SaysNothingMatches()
    {
        var page = RenderPage();
        page.WaitForAssertion(() => page.Find(".app-state-empty").TextContent.Should().Contain("No account event has been recorded yet."));

        var filtered = RenderPage("?days=7");
        filtered.WaitForAssertion(() => filtered.Find(".app-state-empty").TextContent.Should().Contain("No event matches these filters."));
    }

    [Fact]
    public void Load_ApiFails_ShowsTheErrorStateWithTryAgain()
    {
        Api.AccountEventsFail = true;

        var page = RenderPage();

        page.WaitForAssertion(() => page.Markup.Should().Contain("We could not load this list."));
        page.FindAll("button.app-retry").Should().ContainSingle();
    }

    // AC8, AC12: the filters travel in the address and are offered on the screen.
    [Fact]
    public void FiltersInTheAddress_AreSentAndShownOnTheFilters()
    {
        var page = RenderPage($"?event={AccountEventTypes.SignInFailed}&days=30");

        page.WaitForAssertion(() => LastListQuery()
            .Should().Contain($"event={AccountEventTypes.SignInFailed}").And.Contain("days=30"));
        page.FindComponent<AppSelectField<string>>().Instance.Options.Select(option => option.Text)
            .Should().Equal(
                "All events", "Signed in", "Sign-in failed", "Account locked", "Signed out", "Password changed",
                "Password reset requested", "Password reset", "Two-factor turned on", "Two-factor turned off",
                "Recovery codes regenerated", "Account erased");
        page.Find("#account-events-type-filter").GetAttribute("value").Should().Be("Sign-in failed");
        page.Find("#account-events-period-filter").GetAttribute("value").Should().Be("Last 30 days");
    }

    // AC12: an account in the address becomes a removable chip instead of the search box.
    [Fact]
    public void AccountInTheAddress_ShowsARemovableChipInsteadOfTheSearch()
    {
        var page = RenderPage($"?user={Bruno}");

        page.WaitForAssertion(() => page.Find(".app-filter-chip").TextContent.Should().Contain("Account: bruno.lima@exemplo.com.br"));
        page.FindAll(".app-table-search").Should().BeEmpty();
        LastListQuery().Should().Contain($"user={Bruno}");

        page.Find(".app-filter-chip-remove").Click();

        page.WaitForAssertion(() => LastListQuery().Should().NotContain("user="));
        page.FindAll(".app-filter-chip").Should().BeEmpty();
        page.FindAll(".app-table-search").Should().ContainSingle();
    }

    // AC12, UC4: clicking an address filters by it and shows its own chip.
    [Fact]
    public void ClickingAnAddress_FiltersByItAndShowsARemovableChip()
    {
        Api.AccountEvents = [SignedIn];
        var page = RenderPage();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().ContainSingle());

        page.Find("tbody tr button").Click();

        page.WaitForAssertion(() => LastListQuery().Should().Contain("ip=203.0.113.10"));
        page.Find(".app-filter-chip").TextContent.Should().Contain("From: 203.0.113.10");
        Services.GetRequiredService<NavigationManager>().Uri.Should().EndWith("ip=203.0.113.10");

        page.Find(".app-filter-chip-remove").Click();

        page.WaitForAssertion(() => LastListQuery().Should().NotContain("ip="));
    }

    // AC12: changing a filter goes back to the first page.
    [Fact]
    public void FilterChanged_OnALaterPage_ReloadsFromTheFirstPageWithTheFilter()
    {
        Api.AccountEvents = [.. Enumerable.Range(1, 30).Select(_ => SignedIn with { Id = Guid.NewGuid() })];
        var page = RenderPage();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().NotBeEmpty());
        var grid = page.FindComponent<MudBlazor.MudDataGrid<AccountEventResponse>>();
        page.InvokeAsync(() => grid.Instance.NavigateTo(MudBlazor.Page.Next));
        page.WaitForAssertion(() => LastListQuery().Should().Contain("page=1&"));

        var period = page.FindComponent<AppSelectField<int?>>();
        page.InvokeAsync(() => period.Instance.ValueChanged.InvokeAsync(90));

        page.WaitForAssertion(() => LastListQuery().Should().Contain("page=0&").And.Contain("days=90"));
        Services.GetRequiredService<NavigationManager>().Uri.Should().EndWith("days=90");
    }

    // AC12, UC3: the users page opens the trail filtered to that account.
    [Fact]
    public void SecurityEventsAction_OnUsers_OpensTheTrailFilteredToTheRow()
    {
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/admin/users");
        var users = Render<Users>();
        users.WaitForAssertion(() => users.FindAll("tbody tr").Should().HaveCount(3));

        users.FindAll("tbody tr").Single(row => row.TextContent.Contains("bruno.lima", StringComparison.Ordinal))
            .QuerySelectorAll("button.app-row-action")
            .Single(button => button.GetAttribute("aria-label") == "Security events: bruno.lima@exemplo.com.br").Click();

        navigation.Uri.Should().EndWith($"/admin/account-events?user={Bruno}");
    }

    // AC14: the page reads in the three languages; pt-PT is not a copy of pt-BR.
    [Fact]
    public void Load_InPortuguesePortugal_TranslatesTitleEventsAndAccounts()
    {
        CultureInfo.CurrentUICulture = new CultureInfo("pt-PT");
        Api.AccountEvents = [SignedIn, Erased];

        var page = RenderPage();

        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(2));
        page.Markup.Should().Contain("Eventos de conta");
        page.FindAll("tbody tr")[0].TextContent.Should().Contain("Sessão iniciada").And.Contain("com a palavra-passe");
        page.FindAll("tbody tr")[1].TextContent.Should().Contain("Conta eliminada");
    }

    // AC12: the address button says what it does, for a screen reader.
    [Fact]
    public void AddressButton_HasAnAccessibleName()
    {
        Api.AccountEvents = [SignedIn];

        var page = RenderPage();

        page.WaitForAssertion(() => page.Find("tbody tr button").GetAttribute("aria-label")
            .Should().Be("Show every event from 203.0.113.10"));
    }
}
