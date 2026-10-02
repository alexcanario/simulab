using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Simulab.Catalog.Infrastructure.Persistence;
using Simulab.Identity.Contracts;
using Simulab.Testing.ApiHost;

namespace Simulab.Catalog.Tests;

/// <summary>
/// The shared Api host (Simulab.Testing) plus what only this module's tests need: its own context and
/// a signed-in Admin, who is the only role that holds <c>catalog.manage</c> (F-33, BR3).
/// </summary>
public abstract class CatalogApiTests : ApiHostTests
{
    /// <summary>Runs a query on the module's own context, as the module itself would see the data.</summary>
    protected async Task<T> QueryAsync<T>(Func<CatalogModuleDbContext, Task<T>> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        await using var scope = Factory.Services.CreateAsyncScope();
        return await query(scope.ServiceProvider.GetRequiredService<CatalogModuleDbContext>());
    }

    /// <summary>
    /// Stores an acronym on an issuing authority, as the rows typed before F-44 hid it hold one: no screen or
    /// request sets it any more, so only the table can.
    /// </summary>
    protected Task StoreAcronymAsync(Guid authorityId, string acronym) =>
        QueryAsync(context => context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE catalog.issuing_authorities SET acronym = {acronym.ToUpperInvariant()}, normalized_acronym = {acronym.ToUpperInvariant()} WHERE id = {authorityId}"));

    /// <summary>A client signed in as a fresh account holding the Admin role.</summary>
    protected async Task<HttpClient> AdminAsync()
    {
        var admin = await TestAccounts.CreateAsync(Factory.Services, roles: IdentityRoles.Admin);
        return await TestAccounts.SignedInAsync(Client(), admin.Email!);
    }

    /// <summary>A client signed in as a fresh account with no role at all: a plain student.</summary>
    protected async Task<HttpClient> StudentAsync()
    {
        var student = await TestAccounts.CreateAsync(Factory.Services, roles: IdentityRoles.Student);
        return await TestAccounts.SignedInAsync(Client(), student.Email!);
    }
}
