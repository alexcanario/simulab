using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Simulab.ServiceDefaults;

/// <summary>
/// F-64 BR4, D9 (change note v2): where a deployed host keeps its Data Protection key ring. A container's file system is lost
/// when it restarts, and the keys encrypt the Web's Redis sessions (B-3) and the Api's Identity tokens (email verification,
/// password reset), so every restart would sign testers out and kill the links already sent.
/// </summary>
public static class DataProtectionExtensions
{
    /// <summary>The connection the app host passes when it gives the host a blob container for its keys (publish mode only).</summary>
    public const string KeysConnectionName = "keys";

    /// <summary>Key Vault key that encrypts the key ring at rest, e.g. <c>https://vault.vault.azure.net/keys/dataprotection</c>.</summary>
    public const string KeyVaultKeyIdSetting = "DataProtection:KeyVaultKeyId";

    /// <summary>
    /// With <c>ConnectionStrings:keys</c> the keys go to the blob <c>&lt;hostName&gt;.xml</c> of that container, under the
    /// application name <paramref name="hostName"/>, and with <see cref="KeyVaultKeyIdSetting"/> they are encrypted with that
    /// Key Vault key. Without the connection nothing changes: the framework's own store, as in a local run.
    /// </summary>
    public static IHostApplicationBuilder AddAppDataProtection(this IHostApplicationBuilder builder, string hostName)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(hostName);

        var dataProtection = builder.Services.AddDataProtection();
        if (string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString(KeysConnectionName)))
        {
            return builder;
        }

        builder.AddAzureBlobContainerClient(KeysConnectionName);
        dataProtection
            .SetApplicationName(hostName)
            .PersistKeysToAzureBlobStorage(services =>
                services.GetRequiredService<BlobContainerClient>().GetBlobClient($"{hostName}.xml"));

        var keyId = builder.Configuration[KeyVaultKeyIdSetting];
        if (!string.IsNullOrWhiteSpace(keyId))
        {
            dataProtection.ProtectKeysWithAzureKeyVault(
                Uri.TryCreate(keyId, UriKind.Absolute, out var uri)
                    ? uri
                    : throw new InvalidOperationException($"The configuration '{KeyVaultKeyIdSetting}' has '{keyId}', which is not a Key Vault key address."),
                new DefaultAzureCredential());
        }

        return builder;
    }
}
