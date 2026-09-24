using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Simulab.Catalog.Contracts;
using Simulab.Catalog.Domain.Entities;
using Simulab.Catalog.Infrastructure.Persistence;

namespace Simulab.Catalog.Tests;

/// <summary>
/// F-33 AC8, BR5: the guarantee is the database's, not the handler's. Two global rows (TenantId null)
/// with the same normalized value must collide, which only happens because the unique indexes are
/// NULLS NOT DISTINCT (ADR-0001, decision 8; Simulae bug #671).
/// </summary>
public sealed class OrganizerUniqueIndexTests : CatalogApiTests
{
    [Theory]
    [InlineData("ux_organizers_tenant_normalized_name")]
    [InlineData("ux_organizers_tenant_normalized_acronym")]
    public async Task TwoGlobalRowsWithTheSameKey_AreRejectedByPostgres(string index)
    {
        var sameName = index.EndsWith("name", StringComparison.Ordinal);
        var name = $"Banca {Guid.CreateVersion7():N}"[..30];
        var acronym = Guid.CreateVersion7().ToString("N")[..12];

        await AddAsync(name, acronym);

        var second = sameName
            ? Add(name, Guid.CreateVersion7().ToString("N")[..12])
            : Add($"Other {Guid.CreateVersion7():N}"[..30], acronym);

        var save = async () => await AddAsync(second.Name, second.Acronym);

        var failure = await save.Should().ThrowAsync<DbUpdateException>();
        failure.WithInnerExceptionExactly<PostgresException>()
            .Which.ConstraintName.Should().Be(index);
    }

    [Fact]
    public async Task TheTwoIndexesAreUniqueAndTreatNullTenantsAsEqual()
    {
        var indexes = await QueryAsync(context => context.Database
            .SqlQuery<string>($"""
                SELECT indexdef AS "Value" FROM pg_indexes
                WHERE schemaname = 'catalog' AND tablename = 'organizers' AND indexname LIKE 'ux_%'
                """)
            .ToListAsync());

        indexes.Should().HaveCount(2);
        indexes.Should().OnlyContain(definition => definition.Contains("UNIQUE", StringComparison.Ordinal));
        indexes.Should().OnlyContain(definition => definition.Contains("NULLS NOT DISTINCT", StringComparison.Ordinal));
    }

    private static (string Name, string Acronym) Add(string name, string acronym) => (name, acronym);

    private async Task AddAsync(string name, string acronym)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<CatalogModuleDbContext>();
        var organizer = Organizer.Create(name, acronym, OrganizerKind.ExamBoard, null, null);
        organizer.IsSuccess.Should().BeTrue();

        context.Organizers.Add(organizer.Value);
        await context.SaveChangesAsync();
    }
}
