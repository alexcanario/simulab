using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Simulab.ServiceDefaults;

namespace Simulab.Api.Tests;

/// <summary>
/// F-64 AC3 (BR4): a host the app host gave a blob container keeps its Data Protection keys there, under its own application
/// name and blob, encrypted with the configured Key Vault key; a host with neither behaves as before. Nothing here calls
/// Azure: the key ring really surviving a restart is AC8, on the staging itself.
/// </summary>
public sealed class DataProtectionWiringTests
{
    private const string Container = "Endpoint=https://simulabtest.blob.core.windows.net/;ContainerName=keys";
    private const string KeyId = "https://simulab-test.vault.azure.net/keys/dataprotection";

    private static KeyManagementOptions OptionsOf(string hostName, Dictionary<string, string?> settings)
    {
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());
        builder.Configuration.AddInMemoryCollection(settings);
        builder.AddAppDataProtection(hostName);

        using var services = builder.Services.BuildServiceProvider();
        return services.GetRequiredService<IOptions<KeyManagementOptions>>().Value;
    }

    private static string RepositoryOf(KeyManagementOptions options) => options.XmlRepository?.GetType().Name ?? "none";

    [Theory]
    [InlineData("simulab-api")]
    [InlineData("simulab-web")]
    public void BlobContainerGiven_TheKeysGoToTheBlob(string hostName)
    {
        var options = OptionsOf(hostName, new() { [$"ConnectionStrings:{DataProtectionExtensions.KeysConnectionName}"] = Container });

        RepositoryOf(options).Should().Be("AzureBlobXmlRepository");
        options.XmlEncryptor.Should().BeNull("without a Key Vault key the keys stay as the framework writes them");
    }

    [Fact]
    public void BlobContainerAndKeyVaultKeyGiven_TheKeysAreEncryptedWithThatKey()
    {
        var options = OptionsOf("simulab-api", new()
        {
            ["ConnectionStrings:keys"] = Container,
            [DataProtectionExtensions.KeyVaultKeyIdSetting] = KeyId,
        });

        RepositoryOf(options).Should().Be("AzureBlobXmlRepository");
        options.XmlEncryptor.Should().NotBeNull().And.Subject.GetType().Name.Should().Contain("KeyVault");
    }

    /// <summary>The rule of presence for the two tests above: a local run keeps the framework's own store.</summary>
    [Fact]
    public void NothingGiven_TheFrameworksOwnStoreStays()
    {
        var options = OptionsOf("simulab-api", []);

        RepositoryOf(options).Should().NotBe("AzureBlobXmlRepository");
        options.XmlEncryptor.Should().BeNull();
    }

    [Fact]
    public void KeyVaultKeyIsNotAnAddress_TheStartRefusesAndNamesTheSetting()
    {
        var configure = () => OptionsOf("simulab-api", new()
        {
            ["ConnectionStrings:keys"] = Container,
            [DataProtectionExtensions.KeyVaultKeyIdSetting] = "not an address",
        });

        configure.Should().Throw<InvalidOperationException>().WithMessage($"*{DataProtectionExtensions.KeyVaultKeyIdSetting}*");
    }

    [Theory]
    [InlineData("simulab-api")]
    [InlineData("simulab-web")]
    public void BlobContainerGiven_TheHostKeepsItsOwnApplicationName(string hostName)
    {
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:keys"] = Container });
        builder.AddAppDataProtection(hostName);

        using var services = builder.Services.BuildServiceProvider();

        services.GetRequiredService<IOptions<DataProtectionOptions>>().Value.ApplicationDiscriminator.Should().Be(hostName);
    }
}
