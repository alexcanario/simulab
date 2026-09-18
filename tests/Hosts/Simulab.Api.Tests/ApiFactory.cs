using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Simulab.Api.Tests;

/// <summary>
/// The Api with the connection strings the host would inject. The database connection is only opened
/// by the health check, so a test that does not need a real database passes an unreachable one.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    /// <summary>A server that is not listening: the health check must report it as unhealthy.</summary>
    public const string UnreachableDatabase = "Host=localhost;Port=1;Database=simulab;Username=simulab;Password=simulab;Timeout=1";

    public string DatabaseConnectionString { get; set; } = UnreachableDatabase;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseSetting("ConnectionStrings:simulab", DatabaseConnectionString);
        // These tests check the host, not a module: no migration runs against the (unreachable) database.
        builder.UseSetting("Database:ApplyMigrationsOnStart", "false");
        builder.UseSetting("ConnectionStrings:mailpit", "smtp://localhost:1025");
        builder.ConfigureAppConfiguration(configuration =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Email:FromAddress"] = "no-reply@simulab.app",
                ["Email:FromName"] = "Simulab",
            }));
    }
}
