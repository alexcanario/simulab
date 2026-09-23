using System.Globalization;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Simulab.Catalog.Contracts;
using Simulab.Identity.Contracts;
using Simulab.Web.Resources;
using Simulab.Web.Services;
using Simulab.Web.Tests.Ui;

namespace Simulab.Web.Tests.Admin;

/// <summary>
/// F-9, AC17: whatever the back office can show - every permission of the catalog, every system role, every
/// error code the module returns - has a text in the three languages. A permission added by a later
/// feature without its text fails here.
/// F-33, BR2: the roles screen lists every permission Identity seeds, from every module, so the list
/// checked here is <see cref="WebPermissions.All"/> and not one module's.
/// </summary>
public sealed class RoleResourcesTests : KitTestContext
{
    public static TheoryData<string> Cultures() => new("en", "pt-BR", "pt-PT");

    [Theory]
    [MemberData(nameof(Cultures))]
    public void EveryPermissionSystemRoleAndErrorCode_HasAText(string culture)
    {
        CultureInfo.CurrentUICulture = new CultureInfo(culture);
        var l = Services.GetRequiredService<IStringLocalizer<SharedResources>>();

        var keys = WebPermissions.All.SelectMany(permission => new[]
            {
                $"Permission.{permission}.Name",
                $"Permission.{permission}.Description",
                $"PermissionGroup.{IdentityPermissions.GroupOf(permission)}"
            })
            .Concat(IdentityRoles.All.Select(role => $"Role.System.{role}"))
            .Concat(typeof(IdentityErrorCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Select(field => (string)field.GetValue(null)!)
                .Where(code => code.StartsWith("role.", StringComparison.Ordinal)
                    || code.StartsWith("role_assignment.", StringComparison.Ordinal)
                    || code.StartsWith("user.", StringComparison.Ordinal)
                    || code.StartsWith("role_change.", StringComparison.Ordinal)))
            .Concat(RoleChangeActions.All.Select(action => $"RoleHistory.Action.{action}"))
            // F-33, BR14: every error code the Catalog module can return is a message the dialog shows.
            .Concat(typeof(CatalogErrorCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Select(field => (string)field.GetValue(null)!));

        keys.Where(key => l[key].ResourceNotFound).Should().BeEmpty();
    }
}
