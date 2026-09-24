using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Simulab.Identity.Contracts;
using Simulab.Web.Resources;
using Simulab.Web.Services;
using Simulab.Web.Tests.Ui;

namespace Simulab.Web.Tests.Admin;

/// <summary>
/// F-9, AC17: whatever the back office can show - every permission of the catalog, every system role, every
/// action name - has a text in the three languages. A permission added by a later feature without its text
/// fails here.
/// F-33, BR2: the roles screen lists every permission Identity seeds, from every module, so the list checked
/// here is <see cref="WebPermissions.All"/> and not one module's.
/// </summary>
public sealed class RoleResourcesTests : KitTestContext
{
    public static TheoryData<string> Cultures() => new("en", "pt-BR", "pt-PT");

    [Theory]
    [MemberData(nameof(Cultures))]
    public void EveryPermissionSystemRoleAndActionName_HasAText(string culture)
    {
        CultureInfo.CurrentUICulture = new CultureInfo(culture);
        var l = Services.GetRequiredService<IStringLocalizer<SharedResources>>();

        Keys().Where(key => l[key].ResourceNotFound).Should().BeEmpty();
    }

    /// <summary>
    /// The named keys this test owns. F-22 took the error codes away from here: they are checked by reflection,
    /// over every module, by <c>ErrorCodeTextTests</c>, and a list kept by hand was what let F-14 ship a code
    /// with no text at all.
    /// </summary>
    public static IEnumerable<string> Keys() =>
        WebPermissions.All.SelectMany(permission => new[]
            {
                $"Permission.{permission}.Name",
                $"Permission.{permission}.Description",
                $"PermissionGroup.{IdentityPermissions.GroupOf(permission)}"
            })
            .Concat(IdentityRoles.All.Select(role => $"Role.System.{role}"))
            .Concat(RoleChangeActions.All.Select(action => $"RoleHistory.Action.{action}"));
}
