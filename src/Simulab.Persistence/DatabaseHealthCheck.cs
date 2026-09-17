using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace Simulab.Persistence;

/// <summary>
/// Reports the database in the host's health endpoint: opens a connection and runs one statement.
/// A module's own context is not needed, so the check exists before the first module (F-3).
/// </summary>
public sealed class DatabaseHealthCheck(NpgsqlDataSource dataSource) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var command = dataSource.CreateCommand("SELECT 1");
            await command.ExecuteScalarAsync(cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (NpgsqlException exception)
        {
            return HealthCheckResult.Unhealthy("The database is not reachable.", exception);
        }
    }
}
