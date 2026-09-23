using Simulab.Identity.Contracts;
using Simulab.Identity.Domain.Entities;

namespace Simulab.Identity.Tests;

/// <summary>
/// F-21: the screen translates the event, the method and the reason by the contract's names, and the endpoint
/// refuses an event outside that list — so all three must be exactly what the trail stores.
/// </summary>
public sealed class AccountEventNamesTests
{
    [Fact]
    public void Events_MatchTheDomainTypesOneToOne() =>
        AccountEventTypes.All.Should().BeEquivalentTo(Enum.GetNames<AccountEventType>());

    [Fact]
    public void Methods_MatchTheDomainMethodsOneToOne() =>
        AccountEventMethods.All.Should().BeEquivalentTo(Enum.GetNames<AccountEventMethod>());

    [Fact]
    public void Reasons_MatchTheDomainReasonsOneToOne() =>
        AccountEventReasons.All.Should().BeEquivalentTo(Enum.GetNames<AccountEventReason>());
}
