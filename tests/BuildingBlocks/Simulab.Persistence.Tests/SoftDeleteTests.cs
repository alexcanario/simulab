using Microsoft.EntityFrameworkCore;

namespace Simulab.Persistence.Tests;

/// <summary>AC4: a delete keeps the row, marks it, and the row disappears from normal queries.</summary>
public class SoftDeleteTests : ModuleDatabaseTests
{
    [Fact]
    public async Task Remove_Entity_KeepsTheRowMarkedAndHiddenFromQueries()
    {
        var actor = Guid.CreateVersion7();
        User.UserId = actor;

        await using (var context = CreateContext())
        {
            context.Items.Add(new SampleItem { Name = "To delete" });
            await context.SaveChangesAsync(CancellationToken.None);
        }

        Clock.Advance(TimeSpan.FromMinutes(5));

        await using (var context = CreateContext())
        {
            context.Items.Remove(await context.Items.SingleAsync(CancellationToken.None));
            await context.SaveChangesAsync(CancellationToken.None);
        }

        await using var reader = CreateContext();
        (await reader.Items.CountAsync(CancellationToken.None)).Should().Be(0);

        var deleted = await reader.Items
            .IgnoreQueryFilters([ModuleDbContext.SoftDeleteFilter])
            .SingleAsync(CancellationToken.None);
        deleted.IsDeleted.Should().BeTrue();
        deleted.DeletedAt.Should().Be(Clock.GetUtcNow());
        deleted.DeletedBy.Should().Be(actor);
    }
}
