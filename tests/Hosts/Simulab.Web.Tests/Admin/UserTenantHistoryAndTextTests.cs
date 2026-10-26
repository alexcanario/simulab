using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Simulab.Identity.Contracts;
using Simulab.Web.Components.Pages.Admin;
using Simulab.Web.Resources;

namespace Simulab.Web.Tests.Admin;

/// <summary>
/// F-150: the role history shows a tenant change with its translated action and the tenant ids (AC17), and
/// every new text of the item exists in the three languages (AC22). The role, permission and action names
/// are also covered by <see cref="RoleResourcesTests"/>, which reads the catalogs.
/// </summary>
public sealed class UserTenantHistoryAndTextTests : AdminPageTestContext
{
    private static readonly Guid Before = Guid.Parse("3f2a9c10-7b1e-4c55-9d2e-0a1b2c3d4e5f");
    private static readonly Guid After = Guid.Parse("8d1e0b77-2c3a-4f19-a6b5-112233445566");

    public static TheoryData<string> Cultures() => new("en", "pt-BR", "pt-PT");

    // AC17.
    [Fact]
    public void RoleHistory_TenantChange_ShowsTheTranslatedActionTheUserAndBothTenantIds()
    {
        Api.RoleChanges =
        [
            new(Guid.NewGuid(), new DateTimeOffset(2026, 10, 25, 10, 0, 0, TimeSpan.Zero),
                new RoleChangeUserResponse(Guid.Parse("0198f0a2-0000-7000-8000-0000000000a1"), "ana.souza@exemplo.com.br"),
                RoleChangeActions.UserTenantChanged, null,
                new RoleChangeUserResponse(Guid.Parse("0198f0a2-0000-7000-8000-0000000000a2"), "bruno.lima@exemplo.com.br"),
                null, null,
                [new(After.ToString(), After.ToString(), false)],
                [new(Before.ToString(), Before.ToString(), false)])
        ];
        Services.GetRequiredService<NavigationManager>().NavigateTo("/admin/role-history");

        var page = Render<RoleHistory>();

        page.WaitForAssertion(() => page.FindAll("tbody tr").Should().ContainSingle());
        page.Find("tbody tr").TextContent.Should().Contain("User's tenant changed")
            .And.Contain("bruno.lima@exemplo.com.br")
            .And.Contain($"Added: {After}")
            .And.Contain($"Removed: {Before}");
    }

    // AC22.
    [Theory]
    [MemberData(nameof(Cultures))]
    public void EveryNewTextOfTheItem_ExistsInEachLanguage(string culture)
    {
        CultureInfo.CurrentUICulture = new CultureInfo(culture);
        var l = Services.GetRequiredService<IStringLocalizer<SharedResources>>();
        string[] keys =
        [
            "Role.System.Grader",
            "Permission.questions.grade.Name",
            "Permission.questions.grade.Description",
            "Users.Column.Tenant",
            "Users.Field.Tenant",
            "Users.Field.TenantHelp",
            "Users.Tenant.Invalid",
            "Users.Dialog.RolesSaved",
            "RoleHistory.Action.UserTenantChanged",
            IdentityErrorCodes.UserTenantInvalid
        ];

        keys.Where(key => l[key].ResourceNotFound).Should().BeEmpty();
    }

    // AC22: the texts the item changes say "access", in every language.
    [Theory]
    [InlineData("en", "Access of {0}")]
    [InlineData("pt-BR", "Acesso de {0}")]
    [InlineData("pt-PT", "Acesso de {0}")]
    public void DialogTitle_NamesTheAccessOfTheUser(string culture, string expected)
    {
        CultureInfo.CurrentUICulture = new CultureInfo(culture);
        var l = Services.GetRequiredService<IStringLocalizer<SharedResources>>();

        l["Users.Dialog.Title"].Value.Should().Be(expected);
    }
}
