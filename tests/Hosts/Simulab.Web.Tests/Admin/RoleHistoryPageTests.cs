using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Simulab.Identity.Contracts;
using Simulab.Web.Components.Pages.Admin;
using Simulab.Web.Components.Ui;
using Simulab.Web.Services;

namespace Simulab.Web.Tests.Admin;

/// <summary>F-14 `/admin/role-history`: the list, its filters in the address, the user chip and the states (AC7, AC8, AC11, AC13).</summary>
public sealed class RoleHistoryPageTests : AdminPageTestContext
{
    private static readonly Guid Bruno = Guid.Parse("0198f0a2-0000-7000-8000-0000000000a2");
    private static readonly RoleChangeUserResponse Ana = new(Guid.Parse("0198f0a2-0000-7000-8000-0000000000a1"), "ana.souza@exemplo.com.br");
    private static readonly DateTimeOffset At = new(2026, 9, 20, 15, 30, 0, TimeSpan.Zero);

    private static readonly RoleChangeResponse Renamed = new(
        Guid.NewGuid(), At, Ana, RoleChangeActions.RoleUpdated, new UserRoleResponse(Reviewer.Id, "Content reviewer", false), null,
        "Reviewer", "Content reviewer", [new(IdentityPermissions.RolesManage, IdentityPermissions.RolesManage, false)], []);

    private static readonly RoleChangeResponse Assigned = new(
        Guid.NewGuid(), At.AddMinutes(-5), new RoleChangeUserResponse(Guid.NewGuid(), null), RoleChangeActions.UserRolesChanged, null,
        new RoleChangeUserResponse(Bruno, "bruno.lima@exemplo.com.br"), null, null,
        [new(Curator.Id.ToString(), IdentityRoles.Curator, true)], [new(Student.Id.ToString(), IdentityRoles.Student, true)]);

    private IRenderedComponent<RoleHistory> RenderPage(string? query = null)
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo($"/admin/role-history{query}");
        return Render<RoleHistory>();
    }

    private IEnumerable<string> LastListQuery() =>
        [Api.Received.Last(call => call.Path.EndsWith("/role-changes", StringComparison.Ordinal)).Query ?? string.Empty];

    [Fact]
    public void Load_ShowsWhenAuthorActionTargetAndChanges()
    {
        Api.RoleChanges = [Renamed, Assigned];

        var page = RenderPage();

        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(2));
        var renamed = page.FindAll("tbody tr")[0].TextContent;
        renamed.Should().Contain(At.ToString("g", CultureInfo.CurrentCulture))
            .And.Contain("ana.souza@exemplo.com.br")
            .And.Contain("Role changed")
            .And.Contain("Name: Reviewer → Content reviewer")
            .And.Contain("Added: Manage roles and user roles");
        var assigned = page.FindAll("tbody tr")[1].TextContent;
        assigned.Should().Contain("Erased account")
            .And.Contain("User's roles changed")
            .And.Contain("bruno.lima@exemplo.com.br")
            .And.Contain("Added: Curator")
            .And.Contain("Removed: Student");
        LastListQuery().Single().Should().Contain("page=0&").And.Contain("ascending=false");
    }

    [Fact]
    public void Load_BrowserInSaoPaulo_ShowsTheLocalTime()
    {
        JSInterop.Setup<string?>(UserTimeZone.TimeZoneFunction).SetResult("America/Sao_Paulo");
        Api.RoleChanges = [Renamed];

        var page = RenderPage();

        var local = At.ToOffset(TimeSpan.FromHours(-3)).ToString("g", CultureInfo.CurrentCulture);
        page.WaitForAssertion(() => page.Find("tbody tr").TextContent.Should().Contain(local));
    }

    [Fact]
    public void Empty_WithoutFilters_SaysNothingWasRecorded_WithAFilter_SaysNothingMatches()
    {
        var page = RenderPage();
        page.WaitForAssertion(() => page.Find(".app-state-empty").TextContent.Should().Contain("No role change has been recorded yet."));

        var filtered = RenderPage("?days=7");
        filtered.WaitForAssertion(() => filtered.Find(".app-state-empty").TextContent.Should().Contain("No change matches these filters."));
    }

    [Fact]
    public void Load_ApiFails_ShowsTheErrorStateWithTryAgain()
    {
        Api.RoleChangesFail = true;

        var page = RenderPage();

        page.WaitForAssertion(() => page.Markup.Should().Contain("We could not load this list."));
        page.FindAll("button.app-retry").Should().ContainSingle();
    }

    [Fact]
    public void FiltersInTheAddress_AreSentAndOfferedWithDeletedRolesAndErasedAuthorsNamed()
    {
        var page = RenderPage($"?role={Reviewer.Id}&author={Ana.Id}&days=30");

        page.WaitForAssertion(() => LastListQuery().Single()
            .Should().Contain($"roleId={Reviewer.Id}").And.Contain($"authorId={Ana.Id}").And.Contain("days=30"));
        var selects = page.FindComponents<AppSelectField<Guid?>>();
        selects[0].Instance.Options.Select(option => option.Text).Should().Equal("All roles", "Admin", "Content reviewer", "Old team (deleted)");
        selects[1].Instance.Options.Select(option => option.Text).Should().Equal("Anyone", "ana.souza@exemplo.com.br", "Erased account");
        page.WaitForAssertion(() => page.Find("#history-role-filter").GetAttribute("value").Should().Be("Content reviewer"));
        page.Find("#history-period-filter").GetAttribute("value").Should().Be("Last 30 days");
    }

    [Fact]
    public void UserInTheAddress_ShowsARemovableChipInsteadOfTheSearch()
    {
        var page = RenderPage($"?user={Bruno}");

        page.WaitForAssertion(() => page.Find(".app-filter-chip").TextContent.Should().Contain("User: bruno.lima@exemplo.com.br"));
        page.FindAll(".app-table-search").Should().BeEmpty();
        LastListQuery().Single().Should().Contain($"userId={Bruno}");

        page.Find(".app-filter-chip-remove").Click();

        page.WaitForAssertion(() => LastListQuery().Single().Should().NotContain("userId="));
        page.FindAll(".app-filter-chip").Should().BeEmpty();
        page.FindAll(".app-table-search").Should().ContainSingle();
    }

    [Fact]
    public void FilterChanged_OnALaterPage_ReloadsFromTheFirstPageWithTheFilter()
    {
        Api.RoleChanges = [.. Enumerable.Range(1, 30).Select(_ => Renamed with { Id = Guid.NewGuid() })];
        var page = RenderPage();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().NotBeEmpty());
        var grid = page.FindComponent<MudBlazor.MudDataGrid<RoleChangeResponse>>();
        page.InvokeAsync(() => grid.Instance.NavigateTo(MudBlazor.Page.Next));
        page.WaitForAssertion(() => LastListQuery().Single().Should().Contain("page=1&"));

        var period = page.FindComponent<AppSelectField<int?>>();
        page.InvokeAsync(() => period.Instance.ValueChanged.InvokeAsync(90));

        page.WaitForAssertion(() => LastListQuery().Single().Should().Contain("page=0&").And.Contain("days=90"));
        Services.GetRequiredService<NavigationManager>().Uri.Should().EndWith("days=90");
    }

    [Fact]
    public void Load_InPortuguesePortugal_TranslatesTitleActionsAndErasedAccounts()
    {
        CultureInfo.CurrentUICulture = new CultureInfo("pt-PT");
        Api.RoleChanges = [Assigned];

        var page = RenderPage();

        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(1));
        page.Markup.Should().Contain("Histórico de perfis");
        page.Find("tbody tr").TextContent.Should().Contain("Perfis do utilizador alterados")
            .And.Contain("Conta eliminada")
            .And.Contain("Adicionado: Curador")
            .And.Contain("Retirado: Estudante");
    }

    [Fact]
    public void HistoryAction_OnRolesAndUsers_OpensTheHistoryFilteredToTheRow()
    {
        var navigation = Services.GetRequiredService<NavigationManager>();
        var roles = Render<Roles>();
        roles.WaitForAssertion(() => roles.FindAll("tbody tr").Should().HaveCount(5));

        roles.FindAll("tbody tr").Single(row => row.TextContent.Contains("Support", StringComparison.Ordinal))
            .QuerySelectorAll("button.app-row-action").Single(button => button.GetAttribute("aria-label") == "History: Support").Click();

        navigation.Uri.Should().EndWith($"/admin/role-history?role={Support.Id}");

        navigation.NavigateTo("/admin/users");
        var users = Render<Users>();
        users.WaitForAssertion(() => users.FindAll("tbody tr").Should().HaveCount(3));
        users.FindAll("tbody tr").Single(row => row.TextContent.Contains("bruno.lima", StringComparison.Ordinal))
            .QuerySelectorAll("button.app-row-action").Single(button => button.GetAttribute("aria-label") == "History: bruno.lima@exemplo.com.br").Click();

        navigation.Uri.Should().EndWith($"/admin/role-history?user={Bruno}");
    }
}
