using Microsoft.EntityFrameworkCore;

namespace Simulab.Persistence.Tests;

/// <summary>AC5, AC6: global rows are visible to everyone, another tenant's rows never are.</summary>
public class TenantFilterTests : ModuleDatabaseTests
{
    private static readonly Guid TenantA = Guid.CreateVersion7();
    private static readonly Guid TenantB = Guid.CreateVersion7();

    private async Task GivenOneRowPerTenantAsync()
    {
        await using var context = CreateContext();
        foreach (var (name, tenantId) in new (string Name, Guid? TenantId)[] { ("global", null), ("a", TenantA), ("b", TenantB) })
        {
            var item = new SampleItem { Name = name };
            item.PlaceInTenant(tenantId);
            context.Items.Add(item);

            var note = new SampleNote { Text = name };
            note.PlaceInTenant(tenantId);
            context.Notes.Add(note);
        }

        await context.SaveChangesAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Query_NoTenant_ReturnsOnlyGlobalRows()
    {
        await GivenOneRowPerTenantAsync();
        Tenant.TenantId = null;

        await using var context = CreateContext();

        (await context.Items.Select(item => item.Name).ToListAsync(CancellationToken.None)).Should().Equal("global");
    }

    [Fact]
    public async Task Query_TenantA_ReturnsGlobalAndItsOwnRows()
    {
        await GivenOneRowPerTenantAsync();
        Tenant.TenantId = TenantA;

        await using var context = CreateContext();

        (await context.Items.Select(item => item.Name).OrderBy(name => name).ToListAsync(CancellationToken.None))
            .Should().Equal("a", "global");
    }

    [Fact]
    public async Task Query_IgnoringTheTenantFilter_ReturnsEveryNonDeletedRow()
    {
        await GivenOneRowPerTenantAsync();
        Tenant.TenantId = null;

        await using var context = CreateContext();

        (await context.Items.IgnoreQueryFilters([ModuleDbContext.TenantFilter]).CountAsync(CancellationToken.None))
            .Should().Be(3);
    }

    [Fact]
    public async Task Query_SecondEntityWithoutFilterCode_IsFilteredToo()
    {
        await GivenOneRowPerTenantAsync();
        Tenant.TenantId = TenantA;

        await using var context = CreateContext();

        (await context.Notes.Select(note => note.Text).OrderBy(text => text).ToListAsync(CancellationToken.None))
            .Should().Equal("a", "global");
        (await context.Notes.IgnoreQueryFilters([ModuleDbContext.TenantFilter]).CountAsync(CancellationToken.None))
            .Should().Be(3);
    }

    [Fact]
    public async Task Query_AfterAnotherTenantUsedTheContext_DoesNotLeakRows()
    {
        await GivenOneRowPerTenantAsync();

        Tenant.TenantId = TenantA;
        await using (var first = CreateContext())
        {
            (await first.Items.CountAsync(CancellationToken.None)).Should().Be(2);
        }

        Tenant.TenantId = TenantB;
        await using var second = CreateContext();
        (await second.Items.Select(item => item.Name).OrderBy(name => name).ToListAsync(CancellationToken.None))
            .Should().Equal("b", "global");
    }
}
