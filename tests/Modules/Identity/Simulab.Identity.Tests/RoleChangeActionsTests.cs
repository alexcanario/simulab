using Simulab.Identity.Contracts;
using Simulab.Identity.Domain.Entities;

namespace Simulab.Identity.Tests;

/// <summary>F-14: the screen translates <see cref="RoleChangeResponse.Action"/> by the contract's names, so they must be the stored ones.</summary>
public sealed class RoleChangeActionsTests
{
    [Fact]
    public void All_MatchTheDomainActionsOneToOne() =>
        RoleChangeActions.All.Should().BeEquivalentTo(Enum.GetNames<RoleChangeAction>());
}
