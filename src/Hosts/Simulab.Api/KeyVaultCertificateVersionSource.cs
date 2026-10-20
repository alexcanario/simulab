using Azure.Security.KeyVault.Secrets;
using Simulab.Identity.Infrastructure;

namespace Simulab.Api;

/// <summary>
/// F-84 BR1, BR7: reads the versions of an OpenIddict certificate from the vault with the Api's own identity (the role
/// Key Vault Secrets User). The private key never leaves the vault by any other path: this is the same read the
/// configuration provider does for the current version, for one more version.
/// </summary>
public sealed class KeyVaultCertificateVersionSource(SecretClient client) : ICertificateVersionSource
{
    public async Task<IReadOnlyList<CertificateVersion>> GetNewestAsync(string secretName, int count, CancellationToken cancellationToken)
    {
        var listed = new List<SecretProperties>();
        await foreach (var properties in client.GetPropertiesOfSecretVersionsAsync(secretName, cancellationToken))
        {
            listed.Add(properties);
        }

        var newest = listed
            .OrderByDescending(properties => properties.CreatedOn ?? DateTimeOffset.MinValue)
            .Take(count)
            .ToList();

        var versions = new List<CertificateVersion>(newest.Count);
        foreach (var properties in newest)
        {
            // A disabled version keeps no readable value: the vault refuses it, and the Api must not need it.
            var enabled = properties.Enabled ?? true;
            string? value = null;
            if (enabled)
            {
                var secret = await client.GetSecretAsync(secretName, properties.Version, cancellationToken);
                value = secret.Value.Value;
            }

            versions.Add(new CertificateVersion(properties.Version, properties.CreatedOn ?? DateTimeOffset.MinValue, enabled, value));
        }

        return versions;
    }
}
