using System.Globalization;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Simulab.Testing;

/// <summary>
/// One PostgreSQL container per test project (profile: test strategy), started on first use and
/// reaped when the process ends. A test class asks for its own database, so classes never collide.
/// </summary>
public static class PostgresServer
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static PostgreSqlContainer? _container;

    /// <summary>Creates a new empty database on the shared container and returns its connection string.</summary>
    public static async Task<string> CreateDatabaseAsync(string name, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var container = await StartAsync(cancellationToken);
        var database = Sanitize(name);

        await using (var connection = new NpgsqlConnection(container.GetConnectionString()))
        {
            await connection.OpenAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = $"CREATE DATABASE \"{database}\"";
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        return new NpgsqlConnectionStringBuilder(container.GetConnectionString()) { Database = database }.ConnectionString;
    }

    /// <summary>The connection string of the container's default database. Use it only to inspect the server.</summary>
    public static async Task<string> ServerConnectionStringAsync(CancellationToken cancellationToken = default) =>
        (await StartAsync(cancellationToken)).GetConnectionString();

    private static async Task<PostgreSqlContainer> StartAsync(CancellationToken cancellationToken)
    {
        if (_container is not null)
        {
            return _container;
        }

        await Gate.WaitAsync(cancellationToken);
        try
        {
            if (_container is null)
            {
                var container = new PostgreSqlBuilder("postgres:18-alpine").Build();
                await container.StartAsync(cancellationToken);
                _container = container;
            }
        }
        finally
        {
            Gate.Release();
        }

        return _container;
    }

    // A database name is at most 63 bytes and is quoted, so only length and uniqueness matter.
    private static string Sanitize(string name) =>
        string.Concat(
            name.Length > 40 ? name[^40..] : name,
            "_",
            DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture)[^8..]);
}
