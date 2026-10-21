using Azure;
using Azure.Security.KeyVault.Secrets;

namespace Simulab.Api.Tests;

/// <summary>
/// F-84 BR1, BR6, BR7: the Api's own reading of the vault's versions: the newest ones by creation date, a value only for an
/// enabled version, nothing else asked of the vault.
/// </summary>
public class KeyVaultCertificateVersionSourceTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);

    private sealed class FakeSecretClient(IReadOnlyList<SecretProperties> versions) : SecretClient
    {
        public List<string> ValuesRead { get; } = [];

        public override AsyncPageable<SecretProperties> GetPropertiesOfSecretVersionsAsync(string name, CancellationToken cancellationToken = default) =>
            AsyncPageable<SecretProperties>.FromPages([Page<SecretProperties>.FromValues(versions, null, null!)]);

        public override Task<Response<KeyVaultSecret>> GetSecretAsync(string name, string? version = null, CancellationToken cancellationToken = default)
        {
            ValuesRead.Add(version!);
            return Task.FromResult(Response.FromValue(SecretModelFactory.KeyVaultSecret(SecretModelFactory.SecretProperties(version: version), $"value-{version}"), null!));
        }
    }

    private static SecretProperties Properties(string version, int day, bool enabled)
    {
        var properties = SecretModelFactory.SecretProperties(version: version, createdOn: Start.AddDays(day));
        properties.Enabled = enabled;
        return properties;
    }

    [Fact]
    public async Task GetNewest_FourVersions_ReturnsTheNewestTwoByCreationDateWithTheirValues()
    {
        var client = new FakeSecretClient([Properties("v2", 2, true), Properties("v4", 4, true), Properties("v1", 1, true), Properties("v3", 3, true)]);
        var source = new KeyVaultCertificateVersionSource(client);

        var versions = await source.GetNewestAsync("OpenIddict--SigningCertificate", 2, CancellationToken.None);

        versions.Select(version => version.Id).Should().Equal("v4", "v3");
        versions.Select(version => version.Value).Should().Equal("value-v4", "value-v3");
        client.ValuesRead.Should().BeEquivalentTo(["v4", "v3"], "only the versions that are returned are read");
    }

    [Fact]
    public async Task GetNewest_DisabledVersion_IsReturnedWithoutAValueAndNeverRead()
    {
        var client = new FakeSecretClient([Properties("v2", 2, true), Properties("v1", 1, false)]);
        var source = new KeyVaultCertificateVersionSource(client);

        var versions = await source.GetNewestAsync("OpenIddict--SigningCertificate", 2, CancellationToken.None);

        versions.Should().HaveCount(2);
        versions[1].Enabled.Should().BeFalse();
        versions[1].Value.Should().BeNull();
        client.ValuesRead.Should().Equal("v2");
    }
}
