using System.Net;
using Microsoft.EntityFrameworkCore;

namespace Simulab.Identity.Tests;

/// <summary>
/// F-150 AC13: a tenant change and its audit entry are one transaction - when the entry cannot be written,
/// the tenant is not changed either. Its own class: a trigger refuses every tenant entry in its database.
/// </summary>
public sealed class UserTenantTransactionTests : UserTenantApiTests
{
    [Fact]
    public async Task EntryCannotBeSaved_TheTenantIsNotChangedEither()
    {
        var admin = await AdminAsync();
        var user = await Accounts.CreateAsync(Factory.Services);
        var tenant = Guid.CreateVersion7();
        await StoreTenantAsync(user.Id, tenant);
        await QueryAsync(context => context.Database.ExecuteSqlRawAsync("""
            CREATE FUNCTION identity.refuse_tenant_entry() RETURNS trigger LANGUAGE plpgsql AS $$
            BEGIN
                IF NEW.action = 'UserTenantChanged' THEN
                    RAISE EXCEPTION 'the audit entry cannot be saved';
                END IF;
                RETURN NEW;
            END $$;
            CREATE TRIGGER refuse_tenant_entry BEFORE INSERT ON identity.role_changes
                FOR EACH ROW EXECUTE FUNCTION identity.refuse_tenant_entry();
            """));

        var response = await PutTenantAsync(admin, user.Id, Guid.CreateVersion7());

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError, "the route reached the save and the trigger refused the entry");
        (await StoredTenantAsync(user.Id)).Should().Be(tenant);
        (await RoleChangeCountAsync(user.Id)).Should().Be(0);
    }
}
