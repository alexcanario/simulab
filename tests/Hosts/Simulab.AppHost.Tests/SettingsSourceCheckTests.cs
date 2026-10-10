namespace Simulab.AppHost.Tests;

/// <summary>F-94 BR4, BR5, BR9: the source rules on small fakes, so each failure is shown without the real model.</summary>
public class SettingsSourceCheckTests
{
    private static readonly Dictionary<string, string?> NoSettings = new();

    private static RequiredSetting Deployed(SettingsHost host, string key) => new(host, key, SettingOrigin.Deployed, "a test reason");

    /// <summary>AC1: no value in the publish and none in the committed settings.</summary>
    [Fact]
    public void Check_KeyWithNoSource_IsReportedWithHostEnvironmentAndKey()
    {
        var outcome = SettingsSourceCheck.Check(
            SettingsHost.Web,
            "Staging",
            [Deployed(SettingsHost.Web, "Authentication:OpenIddict:ClientId")],
            new Dictionary<string, string?> { ["Authentication__OpenIddict__ClientSecret"] = "x" },
            NoSettings);

        outcome.Problems.Should().ContainSingle()
            .Which.Should().Contain("Web Staging").And.Contain("'Authentication:OpenIddict:ClientId'");
    }

    /// <summary>BR5: every missing key of the run is named, not the first one.</summary>
    [Fact]
    public void Check_SeveralMissingKeys_AreAllReported()
    {
        var outcome = SettingsSourceCheck.Check(
            SettingsHost.Api,
            "Production",
            [Deployed(SettingsHost.Api, "A:One"), Deployed(SettingsHost.Api, "A:Two"), Deployed(SettingsHost.Api, "A:Three")],
            new Dictionary<string, string?> { ["A__Two"] = "set" },
            NoSettings);

        outcome.Problems.Should().HaveCount(2);
        outcome.Problems.Should().Contain(problem => problem.Contains("'A:One'")).And.Contain(problem => problem.Contains("'A:Three'"));
    }

    [Fact]
    public void Check_KeyInThePublishModel_HasASource()
    {
        var outcome = SettingsSourceCheck.Check(
            SettingsHost.Web,
            "Staging",
            [Deployed(SettingsHost.Web, "Authentication:OpenIddict:ClientId")],
            new Dictionary<string, string?> { ["Authentication__OpenIddict__ClientId"] = "simulab-web" },
            NoSettings);

        outcome.Problems.Should().BeEmpty();
    }

    [Fact]
    public void Check_KeyInTheCommittedSettings_HasASource()
    {
        var outcome = SettingsSourceCheck.Check(
            SettingsHost.Api,
            "Staging",
            [Deployed(SettingsHost.Api, "Email:FromName")],
            new Dictionary<string, string?>(),
            new Dictionary<string, string?> { ["Email:FromName"] = "Simulab" });

        outcome.Problems.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Check_KeyWithAnEmptyValue_HasNoSource(string? value)
    {
        var outcome = SettingsSourceCheck.Check(
            SettingsHost.Web,
            "Staging",
            [Deployed(SettingsHost.Web, "Authentication:OpenIddict:ClientId")],
            new Dictionary<string, string?> { ["Authentication__OpenIddict__ClientId"] = value },
            NoSettings);

        outcome.Problems.Should().ContainSingle();
    }

    /// <summary>A key the other host needs is not asked of this one.</summary>
    [Fact]
    public void Check_KeyOfTheOtherHost_IsNotChecked()
    {
        var outcome = SettingsSourceCheck.Check(
            SettingsHost.Api,
            "Staging",
            [Deployed(SettingsHost.Web, "Authentication:OpenIddict:ClientId")],
            new Dictionary<string, string?>(),
            NoSettings);

        outcome.Problems.Should().BeEmpty();
    }

    /// <summary>AC7: a vault secret is reported as not verified and never fails the run.</summary>
    [Fact]
    public void Check_KeyVaultKey_IsNotVerifiedAndDoesNotFail()
    {
        var outcome = SettingsSourceCheck.Check(
            SettingsHost.Api,
            "Staging",
            [new RequiredSetting(SettingsHost.Api, "OpenIddict:SigningCertificate", SettingOrigin.KeyVault, "the owner keeps it in the vault")],
            new Dictionary<string, string?>(),
            NoSettings);

        outcome.Problems.Should().BeEmpty();
        outcome.NotVerified.Should().ContainSingle().Which.Should().Contain("OpenIddict:SigningCertificate");
    }

    [Fact]
    public void Check_KeysAreComparedWithoutCase()
    {
        var outcome = SettingsSourceCheck.Check(
            SettingsHost.Api,
            "Staging",
            [Deployed(SettingsHost.Api, "ConnectionStrings:simulab")],
            new Dictionary<string, string?> { ["CONNECTIONSTRINGS__SIMULAB"] = "Host=x" },
            NoSettings);

        outcome.Problems.Should().BeEmpty();
    }
}
