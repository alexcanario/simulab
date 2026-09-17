using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Simulab.Persistence.Tests;

/// <summary>AC7: two global rows with the same key must not both fit (NULLS NOT DISTINCT).</summary>
public class TenantUniqueIndexTests : ModuleDatabaseTests
{
    [Fact]
    public async Task Insert_TwoGlobalRowsWithTheSameName_IsRejected()
    {
        await using var context = CreateContext();
        context.Items.Add(new SampleItem { Name = "Same" });
        await context.SaveChangesAsync(CancellationToken.None);

        await using var second = CreateContext();
        second.Items.Add(new SampleItem { Name = "Same" });

        var save = async () => await second.SaveChangesAsync(CancellationToken.None);

        var thrown = await save.Should().ThrowAsync<DbUpdateException>();
        thrown.Which.InnerException.Should().BeOfType<PostgresException>()
            .Which.SqlState.Should().Be(PostgresErrorCodes.UniqueViolation);
    }

    [Fact]
    public async Task Insert_SameNameInTwoTenants_IsAllowed()
    {
        await using var context = CreateContext();
        var first = new SampleItem { Name = "Same" };
        first.PlaceInTenant(Guid.CreateVersion7());
        var second = new SampleItem { Name = "Same" };
        second.PlaceInTenant(Guid.CreateVersion7());
        context.Items.AddRange(first, second);

        await context.SaveChangesAsync(CancellationToken.None);

        (await context.Items.IgnoreQueryFilters().CountAsync(CancellationToken.None)).Should().Be(2);
    }
}
