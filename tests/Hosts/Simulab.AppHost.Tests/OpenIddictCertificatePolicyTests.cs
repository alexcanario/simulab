using System.Text.Json;

namespace Simulab.AppHost.Tests;

/// <summary>
/// F-73 AC8 (BR5): the policies Key Vault creates the OpenIddict certificates from are committed, and each asks for what the
/// Api checks at start (F-73 BR3) for its role, so a certificate made with them is never refused.
/// </summary>
public class OpenIddictCertificatePolicyTests
{
    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Directory.Packages.props")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Directory.Packages.props not found above the test output.");
    }

    private static string PolicyPath(string role) =>
        Path.Combine(RepositoryRoot(), "src", "Hosts", "Simulab.AppHost", "keyvault", $"openiddict-{role}-policy.json");

    [Theory]
    [InlineData("signing", "digitalSignature")]
    [InlineData("encryption", "keyEncipherment")]
    public void Policy_AsksForASelfSignedExportableRsaCertificateOfTwoYearsWithItsRoleUsage(string role, string usage)
    {
        using var policy = JsonDocument.Parse(File.ReadAllText(PolicyPath(role)));
        var root = policy.RootElement;

        root.GetProperty("issuerParameters").GetProperty("name").GetString().Should().Be("Self");

        var key = root.GetProperty("keyProperties");
        key.GetProperty("keyType").GetString().Should().Be("RSA");
        key.GetProperty("keySize").GetInt32().Should().BeGreaterThanOrEqualTo(2048);
        key.GetProperty("exportable").GetBoolean().Should().BeTrue("the Api reads the PFX with its private key as a secret");

        var properties = root.GetProperty("x509CertificateProperties");
        properties.GetProperty("validityInMonths").GetInt32().Should().Be(24);
        properties.GetProperty("keyUsage").EnumerateArray().Select(item => item.GetString()).Should().Equal(usage);

        root.GetProperty("secretProperties").GetProperty("contentType").GetString().Should().Be("application/x-pkcs12");
        root.GetProperty("lifetimeActions").GetArrayLength().Should().Be(0, "renewal is manual (BR7): an automatic one would sign everybody out unannounced");
    }

    /// <summary>AC9 (BR5, BR7): the first-deploy step creates the certificates from those files, and the renewal is written down.</summary>
    [Fact]
    public void Infra_CreatesTheCertificatesFromTheCommittedPoliciesAndDocumentsTheRenewal()
    {
        var infra = File.ReadAllText(Path.Combine(RepositoryRoot(), "docs", "infra.md"));

        infra.Should().Contain("openiddict-signing-policy.json")
            .And.Contain("openiddict-encryption-policy.json")
            .And.NotContain("get-default-policy")
            .And.Contain("Renewing the OpenIddict certificates");
    }

    /// <summary>
    /// F-84 AC12 (BR7, BR8, UC2, UC3): the renewal paragraph says a renewal is one command per certificate and a restart, that
    /// nobody is signed out en masse, what the overlap costs, and how to cut an old certificate off; the F-73 sentence that
    /// said every user is signed out once is gone.
    /// </summary>
    [Fact]
    public void Infra_RenewalKeepsSessionsAndDocumentsTheOverlapAndTheCutOff()
    {
        var infra = File.ReadAllText(Path.Combine(RepositoryRoot(), "docs", "infra.md"));
        var start = infra.IndexOf("Renewing the OpenIddict certificates", StringComparison.Ordinal);
        start.Should().BeGreaterThan(0);
        var paragraph = infra[start..infra.IndexOf('\n', start)];

        paragraph.Should().NotContain("signs every user out once")
            .And.Contain("new version")
            .And.Contain("committed policy")
            .And.Contain("restart")
            .And.Contain("no mass sign-out")
            .And.Contain("a few users can be signed out")
            .And.Contain("every replica has restarted")
            .And.Contain("disabling the previous version")
            .And.Contain("disabling the current");
        infra.Should().NotContain("Changing them signs everybody out");
    }
}
