using System.Globalization;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Simulab.Identity.Contracts;
using Simulab.Web.Resources;
using Simulab.Web.Tests.Ui;

namespace Simulab.Web.Tests.Admin;

/// <summary>
/// F-21, AC14: everything the trail can show — every event, way and reason, the page's own texts and the error
/// codes the endpoint returns — has a text in the three languages. An event added later without its text fails here.
/// </summary>
public sealed class AccountEventResourcesTests : KitTestContext
{
    public static TheoryData<string> Cultures() => new("en", "pt-BR", "pt-PT");

    [Theory]
    [MemberData(nameof(Cultures))]
    public void EveryEventMethodReasonAndErrorCode_HasAText(string culture)
    {
        CultureInfo.CurrentUICulture = new CultureInfo(culture);
        var l = Services.GetRequiredService<IStringLocalizer<SharedResources>>();

        var keys = AccountEventTypes.All.Select(name => $"AccountEvents.Event.{name}")
            .Concat(AccountEventMethods.All.Select(name => $"AccountEvents.Method.{name}"))
            .Concat(AccountEventReasons.All.Select(name => $"AccountEvents.Reason.{name}"))
            .Concat(
            [
                "Nav.AccountEvents",
                "AccountEvents.Title",
                "AccountEvents.Column.When",
                "AccountEvents.Column.Account",
                "AccountEvents.Column.Event",
                "AccountEvents.Column.Details",
                "AccountEvents.Column.Address",
                "AccountEvents.Filter.Event",
                "AccountEvents.Filter.AllEvents",
                "AccountEvents.Filter.Period",
                "AccountEvents.Filter.Period.All",
                "AccountEvents.Filter.User",
                "AccountEvents.Filter.Address",
                "AccountEvents.FilterByAddress",
                "AccountEvents.Search.Placeholder",
                "AccountEvents.ErasedAccount",
                "AccountEvents.UnknownAccount",
                "AccountEvents.Empty",
                "AccountEvents.NoMatch",
                "AccountEvents.Action.SecurityEvents"
            ])
            .Concat(AccountEventListQuery.Periods.Select(days => $"AccountEvents.Filter.Period.Days{days}"))
            .Concat(typeof(IdentityErrorCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Select(field => (string)field.GetValue(null)!)
                .Where(code => code.StartsWith("account_event.", StringComparison.Ordinal)));

        keys.Where(key => l[key].ResourceNotFound).Should().BeEmpty();
    }
}
