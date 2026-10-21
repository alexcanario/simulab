using Simulab.Identity.Infrastructure;

namespace Simulab.Identity.Tests;

/// <summary>
/// F-84 AC3, AC4 (BR1, BR2, BR6): the previous certificate is the version immediately before the current one, only when
/// it is enabled, and only when the newest listed version is the one the Api configured as current.
/// </summary>
public class PreviousCertificateSelectorTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);

    private static CertificateVersion Version(string id, int day, bool enabled = true) =>
        new(id, Start.AddDays(day), enabled, enabled ? $"value-{id}" : null);

    [Fact]
    public void Select_OneVersionOnly_HasNoPreviousAndNothingToReport()
    {
        var selection = PreviousCertificateSelector.Select([Version("v1", 1)], "value-v1");

        selection.Value.Should().BeNull();
        selection.Problem.Should().BeNull();
    }

    [Fact]
    public void Select_NoVersionsListed_HasNoPreviousAndNothingToReport()
    {
        var selection = PreviousCertificateSelector.Select([], "value-v1");

        selection.Value.Should().BeNull();
    }

    [Fact]
    public void Select_ThreeEnabledVersions_ThePreviousIsTheOneBeforeTheNewest()
    {
        // Listed out of order on purpose: the order is by creation date, not by the order the vault answers in.
        var selection = PreviousCertificateSelector.Select([Version("v1", 1), Version("v3", 3), Version("v2", 2)], "value-v3");

        selection.Value.Should().Be("value-v2");
        selection.Problem.Should().BeNull();
    }

    [Fact]
    public void Select_PreviousVersionDisabled_HasNoPreviousAndAnOlderOneIsNotPromoted()
    {
        var selection = PreviousCertificateSelector.Select([Version("v3", 3), Version("v2", 2, enabled: false), Version("v1", 1)], "value-v3");

        selection.Value.Should().BeNull("v1 is enabled but it is not the version immediately before the current one");
        selection.Problem.Should().BeNull("a disabled previous version is the operator's cut-off, not a fault");
    }

    [Fact]
    public void Select_NewestVersionIsNotTheConfiguredCurrentValue_HasNoPreviousAndReportsTheDisagreement()
    {
        // A renewal landed between the two reads: the previous of the listing would be the current one of the configuration.
        var selection = PreviousCertificateSelector.Select([Version("v3", 3), Version("v2", 2)], "value-v2");

        selection.Value.Should().BeNull();
        selection.Problem.Should().NotBeNull().And.Contain("current");
    }

    [Fact]
    public void Select_NewestVersionDisabled_LooksNoPreviousUp()
    {
        // The provider leaves a disabled current version out and the start refuses (F-73); if the selector is asked anyway it gives nothing.
        var selection = PreviousCertificateSelector.Select([Version("v3", 3, enabled: false), Version("v2", 2)], currentValue: null);

        selection.Value.Should().BeNull();
    }
}
