using Simulab.Web.Tests.Admin;

namespace Simulab.Web.Tests.Localization;

/// <summary>
/// F-22 AC8: the tests that keep a list of keys by hand own named keys only. An error code checked here again
/// would be the hand-kept list this item removed, back by another door — and a list kept by hand is what let
/// F-14 ship `role_change.period_invalid` with no text in any language.
/// </summary>
public sealed class NamedKeyTestsOwnNoErrorCodeTests
{
    public static TheoryData<string, IEnumerable<string>> Lists() => new()
    {
        { nameof(RoleResourcesTests), RoleResourcesTests.Keys() },
        { nameof(AccountEventResourcesTests), AccountEventResourcesTests.Keys() },
    };

    [Theory]
    [MemberData(nameof(Lists))]
    public void NoHandKeptList_HoldsAnErrorCode(string owner, IEnumerable<string> keys)
    {
        var codeShaped = keys.Where(ErrorCodeTextCheck.IsCodeShaped).ToList();

        codeShaped.Should().BeEmpty($"{owner} must leave the error codes to ErrorCodeTextTests");
    }

    /// <summary>The rule of presence: an empty list would pass the rule above for the wrong reason.</summary>
    [Theory]
    [MemberData(nameof(Lists))]
    public void EveryHandKeptList_StillCoversItsOwnKeys(string owner, IEnumerable<string> keys) =>
        keys.Should().NotBeEmpty($"{owner} still owns the named keys nothing else checks");
}
