using Microsoft.EntityFrameworkCore;

namespace Simulab.Persistence.Tests;

/// <summary>AC2, AC3: the interceptor fills the audit fields; hand-set values do not survive.</summary>
public class AuditTests : ModuleDatabaseTests
{
    [Fact]
    public async Task Save_NewEntity_FillsCreatedFromTheClockAndTheCurrentUser()
    {
        var actor = Guid.CreateVersion7();
        User.UserId = actor;
        var item = new SampleItem
        {
            Name = "First",
            CreatedAt = DateTimeOffset.MinValue,
            CreatedBy = Guid.Empty,
            UpdatedAt = DateTimeOffset.MaxValue,
        };

        await using (var context = CreateContext())
        {
            context.Items.Add(item);
            await context.SaveChangesAsync(CancellationToken.None);
        }

        await using var reader = CreateContext();
        var saved = await reader.Items.SingleAsync(CancellationToken.None);
        saved.CreatedAt.Should().Be(Clock.GetUtcNow());
        saved.CreatedBy.Should().Be(actor);
        saved.UpdatedAt.Should().BeNull();
        saved.UpdatedBy.Should().BeNull();
    }

    [Fact]
    public async Task Save_ChangedEntity_FillsUpdatedAndKeepsCreated()
    {
        var creator = Guid.CreateVersion7();
        var editor = Guid.CreateVersion7();
        User.UserId = creator;

        await using (var context = CreateContext())
        {
            context.Items.Add(new SampleItem { Name = "First" });
            await context.SaveChangesAsync(CancellationToken.None);
        }

        var created = Clock.GetUtcNow();
        Clock.Advance(TimeSpan.FromHours(2));
        User.UserId = editor;

        await using (var context = CreateContext())
        {
            var toChange = await context.Items.SingleAsync(CancellationToken.None);
            toChange.Name = "Second";
            await context.SaveChangesAsync(CancellationToken.None);
        }

        await using var reader = CreateContext();
        var saved = await reader.Items.SingleAsync(CancellationToken.None);
        saved.CreatedAt.Should().Be(created);
        saved.CreatedBy.Should().Be(creator);
        saved.UpdatedAt.Should().Be(Clock.GetUtcNow());
        saved.UpdatedBy.Should().Be(editor);
    }

    [Fact]
    public async Task Save_AnonymousUser_LeavesCreatedByNull()
    {
        User.UserId = null;

        await using (var context = CreateContext())
        {
            context.Items.Add(new SampleItem { Name = "Anonymous" });
            await context.SaveChangesAsync(CancellationToken.None);
        }

        await using var reader = CreateContext();
        (await reader.Items.SingleAsync(CancellationToken.None)).CreatedBy.Should().BeNull();
    }
}
