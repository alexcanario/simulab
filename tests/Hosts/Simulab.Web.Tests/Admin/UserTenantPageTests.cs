using System.Net;
using Bunit;
using Simulab.Identity.Contracts;
using Simulab.Web.Components.Pages.Admin;

namespace Simulab.Web.Tests.Admin;

/// <summary>
/// F-150 on `/admin/users`: the Tenant column (AC20) and the access dialog that saves roles and tenant
/// together, roles first (AC19, BR11).
/// </summary>
public sealed class UserTenantPageTests : AdminPageTestContext
{
    private static readonly Guid Tenant = Guid.Parse("3f2a9c10-7b1e-4c55-9d2e-0a1b2c3d4e5f");
    private const string Bruno = "bruno.lima@exemplo.com.br";

    private static AngleSharp.Dom.IElement RowOf(IRenderedComponent<Users> page, string email) =>
        page.FindAll("tbody tr").Single(row => row.TextContent.Contains(email, StringComparison.Ordinal));

    private IEnumerable<(HttpMethod Method, string Path, string? Query, string? Body)> Writes() =>
        Api.Received.Where(call => call.Method == HttpMethod.Put);

    private (IRenderedComponent<MudBlazor.MudDialogProvider> Dialogs, IRenderedComponent<MudBlazor.MudSnackbarProvider> Snackbars, IRenderedComponent<Users> Page) OpenBrunoDialog()
    {
        var (dialogs, snackbars) = RenderProviders();
        var page = Render<Users>();
        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(Api.Users.Count));
        RowOf(page, Bruno).QuerySelector("button.app-row-action")!.Click();
        dialogs.WaitForAssertion(() => dialogs.FindAll("#user-tenant").Should().ContainSingle());
        return (dialogs, snackbars, page);
    }

    // AC20.
    [Fact]
    public void List_HasATenantColumn_WithTheFirstEightCharactersOrADash()
    {
        Api.Users = [Api.Users[0] with { TenantId = Tenant }, Api.Users[1], Api.Users[2]];

        var page = Render<Users>();

        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().HaveCount(3));
        page.Find("thead").TextContent.Should().Contain("Tenant");
        var tenantCells = page.FindAll("tbody tr td.app-user-tenant");
        tenantCells.Select(cell => cell.TextContent.Trim()).Should().Equal("3f2a9c10", "—", "—");
        tenantCells[0].QuerySelector(".app-cell-nowrap").Should().NotBeNull("a short label in a list column does not wrap");
    }

    // AC19: both changed - the roles first, then the tenant, then one snackbar and the dialog closes.
    [Fact]
    public void Dialog_RolesAndTenantChanged_SavesRolesThenTenant()
    {
        var (dialogs, snackbars, _) = OpenBrunoDialog();
        dialogs.Markup.Should().Contain($"Access of {Bruno}");
        dialogs.Find("#user-tenant").GetAttribute("value").Should().BeNullOrEmpty();
        dialogs.Markup.Should().Contain("Paste the tenant id; leave empty for none.");

        dialogs.Find($"#role-{Support.Id}").Change(true);
        dialogs.Find("#user-tenant").Change($"  {Tenant}  ");
        dialogs.Find("button.app-form-save").Click();

        dialogs.WaitForAssertion(() => Writes().Should().HaveCount(2));
        Writes().Select(call => call.Path).Should().Equal(
            $"/api/v1/identity/users/{Api.Users[1].Id}/roles",
            $"/api/v1/identity/users/{Api.Users[1].Id}/tenant");
        FakeAdminApi.Read<SetUserTenantRequest>(Writes().Last().Body).TenantId.Should().Be(Tenant);
        snackbars.WaitForAssertion(() => snackbars.Markup.Should().Contain($"Access of {Bruno} saved."));
        dialogs.WaitForAssertion(() => dialogs.FindAll("#user-tenant").Should().BeEmpty());
    }

    // AC19: only the tenant changed - no roles request.
    [Fact]
    public void Dialog_OnlyTenantChanged_SendsOnlyTheTenant()
    {
        var (dialogs, _, _) = OpenBrunoDialog();

        dialogs.Find("#user-tenant").Change(Tenant.ToString());
        dialogs.Find("button.app-form-save").Click();

        dialogs.WaitForAssertion(() => Writes().Should().ContainSingle());
        Writes().Single().Path.Should().EndWith("/tenant");
    }

    // AC19: clearing the field sends null.
    [Fact]
    public void Dialog_TenantCleared_SendsNull()
    {
        Api.Users = [Api.Users[0], Api.Users[1] with { TenantId = Tenant }, Api.Users[2]];
        var (dialogs, _, _) = OpenBrunoDialog();
        dialogs.Find("#user-tenant").GetAttribute("value").Should().Be(Tenant.ToString());

        dialogs.Find("#user-tenant").Change(string.Empty);
        dialogs.Find("button.app-form-save").Click();

        dialogs.WaitForAssertion(() => Writes().Should().ContainSingle());
        FakeAdminApi.Read<SetUserTenantRequest>(Writes().Single().Body).TenantId.Should().BeNull();
    }

    // AC19: the second request fails - the dialog stays open with the error, and the roles show as saved.
    [Fact]
    public void Dialog_TenantRequestFails_StaysOpenWithTheError_AndTheRolesShowAsSaved()
    {
        Api.TenantWriteFailure = (HttpStatusCode.BadRequest, IdentityErrorCodes.UserTenantInvalid);
        var (dialogs, _, _) = OpenBrunoDialog();

        dialogs.Find($"#role-{Support.Id}").Change(true);
        dialogs.Find("#user-tenant").Change(Tenant.ToString());
        dialogs.Find("button.app-form-save").Click();

        dialogs.WaitForAssertion(() => dialogs.Markup.Should().Contain("The tenant id is not valid. Paste the id of a tenant, or leave the field empty."));
        dialogs.Markup.Should().Contain($"Roles of {Bruno} saved.");
        dialogs.FindAll("button.app-form-save").Should().ContainSingle("the dialog stays open");

        // A second try sends only what is still unsaved: the tenant.
        Api.TenantWriteFailure = null;
        dialogs.Find("button.app-form-save").Click();
        dialogs.WaitForAssertion(() => Writes().Should().HaveCount(3));
        Writes().Count(call => call.Path.EndsWith("/roles", StringComparison.Ordinal)).Should().Be(1);
    }

    // AC19: a value that is not a GUID is refused on the screen, before any request.
    [Fact]
    public void Dialog_TenantThatIsNotAGuid_ShowsTheFieldErrorAndSendsNothing()
    {
        var (dialogs, _, _) = OpenBrunoDialog();

        dialogs.Find("#user-tenant").Change("partner-a");
        dialogs.Find("button.app-form-save").Click();

        dialogs.WaitForAssertion(() => dialogs.Find(".app-field-error-text").TextContent.Should().Be("Paste a tenant id such as 3f2a9c10-7b1e-4c55-9d2e-0a1b2c3d4e5f."));
        Writes().Should().BeEmpty();
    }
}
