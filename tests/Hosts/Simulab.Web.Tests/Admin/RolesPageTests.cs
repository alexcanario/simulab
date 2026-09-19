using System.Globalization;
using System.Net;
using Bunit;
using MudBlazor;
using Simulab.Identity.Contracts;
using Simulab.Web.Components.Pages.Admin;

namespace Simulab.Web.Tests.Admin;

/// <summary>F-9 `/admin/roles`: the list, the role dialog and delete (AC2-AC4, AC8, AC10, AC16, AC17).</summary>
public sealed class RolesPageTests : AdminPageTestContext
{
    private IRenderedComponent<Roles> RenderPage() => Render<Roles>();

    private static AngleSharp.Dom.IElement RowOf(IRenderedComponent<Roles> page, string name) =>
        page.FindAll("tbody tr").Single(row => row.QuerySelector(".app-role-name")?.TextContent.Trim() == name);

    [Fact]
    public void Load_ShowsEveryRoleWithSystemBadgeCountsAndALinkToItsUsers()
    {
        var page = RenderPage();

        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(5));
        page.FindAll(".app-role-name").Select(name => name.TextContent.Trim())
            .Should().Equal("Admin", "Content reviewer", "Curator", "Student", "Support");
        RowOf(page, "Curator").QuerySelector(".app-badge")!.TextContent.Trim().Should().Be("System");
        RowOf(page, "Support").QuerySelector(".app-badge").Should().BeNull();
        var link = RowOf(page, "Student").QuerySelector("a.app-link")!;
        link.GetAttribute("href").Should().Be($"/admin/users?role={Student.Id}");
        link.TextContent.Trim().Should().Be("1,243");
        link.GetAttribute("aria-label").Should().Be("See the 1,243 users with the role Student");
        RowOf(page, "Support").QuerySelector("a.app-link").Should().BeNull("nobody holds it");
    }

    [Fact]
    public void RowActions_SystemRoleHasNoDelete_CustomRoleInUseHasItDisabledWithTheReason()
    {
        var page = RenderPage();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(5));

        RowOf(page, "Admin").QuerySelectorAll("button.app-row-action").Select(button => button.GetAttribute("aria-label"))
            .Should().Equal("Edit: Admin");
        var inUse = RowOf(page, "Content reviewer").QuerySelectorAll("button.app-row-action")[1];
        inUse.HasAttribute("disabled").Should().BeTrue();
        page.FindComponents<MudTooltip>().Select(tooltip => tooltip.Instance.Text).Should().Contain("Held by 2 users: remove it from them first");
        RowOf(page, "Support").QuerySelectorAll("button.app-row-action")[1].HasAttribute("disabled").Should().BeFalse();
    }

    [Fact]
    public void Load_ApiFails_ShowsTheErrorStateWithTryAgain()
    {
        Api.Roles = null;

        var page = RenderPage();

        page.WaitForAssertion(() => page.Markup.Should().Contain("We could not load this list."));
    }

    [Fact]
    public void Add_ValidNameAndPermission_SendsThemAndShowsTheSnackbar()
    {
        var (dialogs, snackbars) = RenderProviders();
        var page = RenderPage();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(5));

        page.Find("button.app-primary-action").Click();
        dialogs.WaitForAssertion(() => dialogs.FindAll("#role-name").Should().ContainSingle());
        dialogs.Markup.Should().Contain("Identity and access").And.Contain("0 of 1 selected")
            .And.Contain("Manage roles and user roles").And.Contain("identity.roles.manage");
        dialogs.Find("#role-name").Change("  Helpdesk  ");
        dialogs.Find("#permission-identity-roles-manage").Change(true);
        dialogs.Find("button.app-form-save").Click();

        dialogs.WaitForAssertion(() => Api.Received.Should().Contain(call => call.Method == HttpMethod.Post));
        var sent = FakeAdminApi.Read<SaveRoleRequest>(Api.Received.Single(call => call.Method == HttpMethod.Post).Body);
        sent.Name.Should().Be("Helpdesk");
        sent.Permissions.Should().Equal(IdentityPermissions.RolesManage);
        snackbars.WaitForAssertion(() => snackbars.Markup.Should().Contain("Role saved."));
    }

    [Fact]
    public void Add_NameTooShort_ShowsTheFieldErrorAndSendsNothing()
    {
        var (dialogs, _) = RenderProviders();
        var page = RenderPage();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(5));

        page.Find("button.app-primary-action").Click();
        dialogs.WaitForAssertion(() => dialogs.FindAll("#role-name").Should().ContainSingle());
        dialogs.Find("#role-name").Change(" R ");
        dialogs.Find("button.app-form-save").Click();

        dialogs.WaitForAssertion(() => dialogs.Markup.Should().Contain("Use 2 to 50 characters."));
        Api.Received.Should().NotContain(call => call.Method == HttpMethod.Post);
    }

    [Fact]
    public void Save_ApiRefuses_ShowsTheTranslatedErrorInTheDialog()
    {
        Api.WriteFailure = (HttpStatusCode.Conflict, IdentityErrorCodes.RoleNameTaken);
        var (dialogs, _) = RenderProviders();
        var page = RenderPage();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(5));

        page.Find("button.app-primary-action").Click();
        dialogs.WaitForAssertion(() => dialogs.FindAll("#role-name").Should().ContainSingle());
        dialogs.Find("#role-name").Change("support");
        dialogs.Find("button.app-form-save").Click();

        dialogs.WaitForAssertion(() => dialogs.Find(".app-alert").TextContent.Should().Contain("A role with this name already exists (deleted roles included)."));
        dialogs.FindAll("#role-name").Should().ContainSingle("the dialog stays open");
    }

    [Fact]
    public void EditSystemRole_NameIsReadOnlyAndTheStoredNameIsSent()
    {
        CultureInfo.CurrentUICulture = new CultureInfo("pt-BR");
        var (dialogs, _) = RenderProviders();
        var page = RenderPage();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(5));

        RowOf(page, "Administrador").QuerySelector("button.app-row-action")!.Click();
        dialogs.WaitForAssertion(() => dialogs.FindAll("#role-name").Should().ContainSingle());
        dialogs.Find("#role-name").HasAttribute("readonly").Should().BeTrue();
        dialogs.Find("#role-name").GetAttribute("value").Should().Be("Administrador");
        dialogs.Markup.Should().Contain("Papéis de sistema mantêm o nome.").And.Contain("Gerenciar papéis e atribuições");
        dialogs.Find("button.app-form-save").Click();

        dialogs.WaitForAssertion(() => Api.Received.Should().Contain(call => call.Method == HttpMethod.Put));
        FakeAdminApi.Read<SaveRoleRequest>(Api.Received.Single(call => call.Method == HttpMethod.Put).Body).Name.Should().Be(IdentityRoles.Admin);
    }

    [Fact]
    public void Delete_Confirmed_DeletesAndShowsTheSnackbar()
    {
        var (dialogs, snackbars) = RenderProviders();
        var page = RenderPage();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(5));

        RowOf(page, "Support").QuerySelectorAll("button.app-row-action")[1].Click();
        dialogs.WaitForAssertion(() => dialogs.Find(".app-confirm-ok").TextContent.Trim().Should().Be("Delete Support"));
        dialogs.Markup.Should().Contain("The role Support will be deleted. No user holds it, so nobody loses access.");
        dialogs.Find(".app-confirm-ok").Click();

        page.WaitForAssertion(() => Api.Received.Should().Contain(call => call.Method == HttpMethod.Delete && call.Path.EndsWith($"/roles/{Support.Id}", StringComparison.Ordinal)));
        snackbars.WaitForAssertion(() => snackbars.Markup.Should().Contain("Role deleted."));
    }

    [Fact]
    public void Delete_RefusedBecauseSomeoneGotTheRoleMeanwhile_ShowsTheAlert()
    {
        Api.WriteFailure = (HttpStatusCode.UnprocessableEntity, IdentityErrorCodes.RoleInUse);
        var (dialogs, _) = RenderProviders();
        var page = RenderPage();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(5));

        RowOf(page, "Support").QuerySelectorAll("button.app-row-action")[1].Click();
        dialogs.WaitForAssertion(() => dialogs.FindAll(".app-confirm-ok").Should().ContainSingle());
        dialogs.Find(".app-confirm-ok").Click();

        page.WaitForAssertion(() => page.Find(".app-alert").TextContent.Should().Contain("Remove this role from every user before deleting it."));
    }

    [Fact]
    public void Load_InPortuguesePortugal_TranslatesSystemRolesAndKeepsCustomNames()
    {
        CultureInfo.CurrentUICulture = new CultureInfo("pt-PT");

        var page = RenderPage();

        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(5));
        page.FindAll(".app-role-name").Select(name => name.TextContent.Trim())
            .Should().BeEquivalentTo("Administrador", "Content reviewer", "Curador", "Estudante", "Support");
        page.Markup.Should().Contain("Sistema");
    }
}
