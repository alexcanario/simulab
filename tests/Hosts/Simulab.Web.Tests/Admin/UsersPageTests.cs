using System.Net;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Simulab.Identity.Contracts;
using Simulab.Web.Components.Pages.Admin;

namespace Simulab.Web.Tests.Admin;

/// <summary>F-9 `/admin/users`: the list, the role filter and the edit-roles dialog (AC11-AC13, AC16).</summary>
public sealed class UsersPageTests : AdminPageTestContext
{
    private IRenderedComponent<Users> RenderPage(string? query = null)
    {
        if (query is not null)
        {
            Services.GetRequiredService<NavigationManager>().NavigateTo($"/admin/users{query}");
        }

        return Render<Users>();
    }

    private static AngleSharp.Dom.IElement RowOf(IRenderedComponent<Users> page, string email) =>
        page.FindAll("tbody tr").Single(row => row.TextContent.Contains(email, StringComparison.Ordinal));

    /// <summary>
    /// F-43 AC6: the first real use of the kit's status chip. Each state carries the tone that says what it
    /// means, and the same state reads the same on every screen from here on.
    /// </summary>
    [Fact]
    public void Load_EachStatusIsTheKitChipWithTheToneOfItsMeaning()
    {
        var page = RenderPage();

        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(3));
        RowOf(page, "diego.alves").QuerySelector(".app-status-chip")!.ClassList
            .Should().Contain("app-status-chip-warning", "a pending account is waiting, not healthy");
        RowOf(page, "ana.souza").QuerySelector(".app-status-chip")!.ClassList
            .Should().Contain("app-status-chip-success");
    }

    [Fact]
    public void Load_ShowsEmailNameStatusAndRoleChips()
    {
        var page = RenderPage();

        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(3));
        RowOf(page, "bruno.lima").QuerySelectorAll(".app-chip").Select(chip => chip.TextContent.Trim())
            .Should().Equal("Curator", "Content reviewer");
        // F-43 AC6: the status is the kit's chip now. The assertion is the same — the state is read as words.
        RowOf(page, "diego.alves").QuerySelector(".app-status-chip")!.TextContent.Trim().Should().Be("Pending");
        RowOf(page, "diego.alves").TextContent.Should().Contain("No roles").And.Contain("—");
        RowOf(page, "ana.souza").QuerySelector("button.app-row-action")!.GetAttribute("aria-label")
            .Should().Be("Edit roles: ana.souza@exemplo.com.br");
        page.Find(".app-table-search input").GetAttribute("placeholder").Should().Be("Search by email or name");
    }

    [Fact]
    public void RoleInTheAddress_IsPreselectedAndSentAsTheFilter()
    {
        var page = RenderPage($"?role={Curator.Id}");

        page.WaitForAssertion(() => Api.Received.Should().Contain(call => call.Path.EndsWith("/users", StringComparison.Ordinal)));
        Api.Received.Last(call => call.Path.EndsWith("/users", StringComparison.Ordinal)).Query.Should().Contain($"roleId={Curator.Id}");
        page.WaitForAssertion(() => page.Find("#users-role-filter").GetAttribute("value").Should().Be("Curator"));
    }

    [Fact]
    public void RoleFilterChanged_OnALaterPage_ReloadsFromTheFirstPage()
    {
        Api.Users = [.. Enumerable.Range(1, 30).Select(n => new UserSummaryResponse(Guid.NewGuid(), $"user{n}@exemplo.com", null, "Active", []))];
        var page = RenderPage();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().NotBeEmpty());
        var grid = page.FindComponent<MudBlazor.MudDataGrid<UserSummaryResponse>>();
        page.InvokeAsync(() => grid.Instance.NavigateTo(MudBlazor.Page.Next));
        page.WaitForAssertion(() => Api.Received.Last(call => call.Path.EndsWith("/users", StringComparison.Ordinal)).Query.Should().Contain("page=1&"));

        var filter = page.FindComponent<Simulab.Web.Components.Ui.AppSelectField<Guid?>>();
        page.InvokeAsync(() => filter.Instance.ValueChanged.InvokeAsync(Admin.Id));

        page.WaitForAssertion(() => Api.Received.Last(call => call.Path.EndsWith("/users", StringComparison.Ordinal)).Query
            .Should().Contain("page=0&").And.Contain($"roleId={Admin.Id}"));
    }

    [Fact]
    public void Load_ApiFails_ShowsTheErrorStateWithTryAgain()
    {
        Api.UsersFail = true;

        var page = RenderPage();

        page.WaitForAssertion(() => page.Markup.Should().Contain("We could not load this list."));
        page.FindAll("button.app-retry").Should().ContainSingle();
    }

    [Fact]
    public void Search_WithoutMatches_SaysNoUserMatchesTheTerm()
    {
        var page = RenderPage();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(3));
        Api.Users = [];

        // As the kit's own table test does: the debounced field's value, without waiting for its timer.
        var search = page.FindComponents<MudBlazor.MudTextField<string>>().Single(field => field.Instance.Class == "app-table-search");
        page.InvokeAsync(() => search.Instance.ValueChanged.InvokeAsync("zeca"));

        page.WaitForAssertion(() => page.Find(".app-state-empty").TextContent.Should().Contain("No user matches \"zeca\"."));
        Api.Received.Last(call => call.Path.EndsWith("/users", StringComparison.Ordinal)).Query.Should().Contain("search=zeca");
    }

    [Fact]
    public void EditRoles_ShowsEveryRoleWithItsPermissions_SavesTheSetAndShowsTheSnackbar()
    {
        var (dialogs, snackbars) = RenderProviders();
        var page = RenderPage();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(3));

        RowOf(page, "bruno.lima").QuerySelector("button.app-row-action")!.Click();
        dialogs.WaitForAssertion(() => dialogs.FindAll($"#role-{Support.Id}").Should().ContainSingle());
        dialogs.Markup.Should().Contain("A user can hold several roles; their permissions add up.")
            .And.Contain("Manage roles and user roles");
        dialogs.Find($"#role-{Curator.Id}").HasAttribute("checked").Should().BeTrue();
        dialogs.Find($"#role-{Support.Id}").Change(true);
        dialogs.Find("button.app-form-save").Click();

        dialogs.WaitForAssertion(() => Api.Received.Should().Contain(call => call.Method == HttpMethod.Put));
        FakeAdminApi.Read<SetUserRolesRequest>(Api.Received.Single(call => call.Method == HttpMethod.Put).Body).RoleIds
            .Should().BeEquivalentTo([Curator.Id, Reviewer.Id, Support.Id]);
        snackbars.WaitForAssertion(() => snackbars.Markup.Should().Contain("Roles of bruno.lima@exemplo.com.br saved."));
    }

    [Fact]
    public void EditRoles_LastManager_ShowsTheErrorAndKeepsTheDialogOpen()
    {
        Api.WriteFailure = (HttpStatusCode.UnprocessableEntity, IdentityErrorCodes.RoleAssignmentLastManager);
        var (dialogs, _) = RenderProviders();
        var page = RenderPage();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(3));

        RowOf(page, "ana.souza").QuerySelector("button.app-row-action")!.Click();
        dialogs.WaitForAssertion(() => dialogs.FindAll($"#role-{Admin.Id}").Should().ContainSingle());
        dialogs.Find($"#role-{Admin.Id}").Change(false);
        dialogs.Find("button.app-form-save").Click();

        dialogs.WaitForAssertion(() => dialogs.Markup.Should().Contain("This would leave nobody able to manage roles."));
        dialogs.FindAll("button.app-form-save").Should().ContainSingle("the dialog stays open");
    }
}
