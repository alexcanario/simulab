using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace Simulab.Persistence;

public static class ModuleMigrationExtensions
{
    /// <summary>
    /// Applies a module context's migrations. The history table is created first (F-19): the Npgsql
    /// provider probes it with a plain <c>SELECT</c> and catches "table does not exist", but the command
    /// logger has already written that probe as a failure, so a first start on an empty database would
    /// show a <c>fail</c> line that is not one. A real command failure is still logged and still throws.
    /// </summary>
    public static async Task MigrateModuleAsync<TContext>(this IServiceProvider services, CancellationToken cancellationToken = default)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);

        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<TContext>();
        await context.GetService<IHistoryRepository>().CreateIfNotExistsAsync(cancellationToken);
        await context.Database.MigrateAsync(cancellationToken);
    }
}
