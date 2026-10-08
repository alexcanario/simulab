using Microsoft.Extensions.Configuration;
using Simulab.ServiceDefaults;

namespace Simulab.Api.Tests;

/// <summary>
/// F-64 (validation): the cloud database refuses a plain-text connection, so a cloud host (one with a Key Vault connection)
/// asks for SSL; a local run and a string that already sets a mode stay as they are.
/// </summary>
public sealed class DatabaseTransportTests
{
    private const string Plain = "Host=postgres.example.net;Username=login;Password=secret;Database=simulab";

    private static string? ConnectionOf(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationManager();
        configuration.AddInMemoryCollection(settings);
        configuration.RequireDatabaseTls();
        return configuration.GetConnectionString("simulab");
    }

    [Fact]
    public void CloudHostWithPlainConnectionString_AsksForSsl()
    {
        var connection = ConnectionOf(new()
        {
            ["ConnectionStrings:keyvault"] = "https://simulab-test.vault.azure.net/",
            ["ConnectionStrings:simulab"] = Plain,
        });

        connection.Should().Contain("Host=postgres.example.net").And.Contain("Password=secret")
            .And.Contain("SSL Mode=Require");
    }

    [Theory]
    [InlineData("SSL Mode=VerifyFull")]
    [InlineData("SslMode=Disable")]
    public void CloudHostWhoseStringSetsAMode_IsLeftAsItIs(string mode)
    {
        var connection = ConnectionOf(new()
        {
            ["ConnectionStrings:keyvault"] = "https://simulab-test.vault.azure.net/",
            ["ConnectionStrings:simulab"] = $"{Plain};{mode}",
        });

        connection.Should().Be($"{Plain};{mode}");
    }

    /// <summary>The rule of presence for the tests above: a local run has no vault, so its string is not touched.</summary>
    [Fact]
    public void LocalHostWithoutAVault_KeepsItsConnectionString()
    {
        var connection = ConnectionOf(new() { ["ConnectionStrings:simulab"] = Plain });

        connection.Should().Be(Plain);
    }

    [Fact]
    public void CloudHostWithoutADatabaseString_ChangesNothing()
    {
        ConnectionOf(new() { ["ConnectionStrings:keyvault"] = "https://simulab-test.vault.azure.net/" }).Should().BeNull();
    }
}
