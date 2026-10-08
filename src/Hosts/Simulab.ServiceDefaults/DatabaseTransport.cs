using System.Data.Common;
using Microsoft.Extensions.Configuration;

namespace Simulab.ServiceDefaults;

/// <summary>
/// F-64: the cloud PostgreSQL server accepts only encrypted connections (<c>require_secure_transport</c> is on), and the
/// connection string the deployment stores in Key Vault carries no SSL setting, so the host connected in plain text and
/// the server answered "no pg_hba.conf entry ... no encryption". A cloud host adds the setting itself; a local run, which
/// has no vault, keeps its own connection string.
/// </summary>
public static class DatabaseTransport
{
    private const string ConnectionName = "simulab";

    /// <summary>
    /// With a Key Vault connection (the cloud marker) and a database connection string that says nothing about SSL, adds
    /// <c>SSL Mode=Require</c> to it. A string that already sets a mode is left as it is.
    /// </summary>
    public static void RequireDatabaseTls(this ConfigurationManager configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        if (string.IsNullOrWhiteSpace(configuration.GetConnectionString("keyvault")))
        {
            return;
        }

        var connectionString = configuration.GetConnectionString(ConnectionName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        var parsed = new DbConnectionStringBuilder { ConnectionString = connectionString };
        if (parsed.ContainsKey("SSL Mode") || parsed.ContainsKey("SslMode"))
        {
            return;
        }

        configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{ConnectionName}"] = $"{connectionString.TrimEnd(';', ' ')};SSL Mode=Require",
        });
    }
}
