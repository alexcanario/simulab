using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Simulab.Catalog.Contracts;
using Simulab.Catalog.Domain.Entities;
using Simulab.Catalog.Infrastructure.Persistence;

namespace Simulab.Catalog.Tests;

/// <summary>
/// F-34 AC8, BR1 and BR10: the guarantee is the database's, not the handler's. Two global rows (TenantId
/// null) with the same issuing authority and the same normalized name must collide, which only happens
/// because the unique index is NULLS NOT DISTINCT (ADR-0001, decision 8; Simulae bug #671).
/// </summary>
public sealed class ExamUniqueIndexTests : CatalogApiTests
{
    [Fact]
    public async Task TwoGlobalRowsWithTheSameAuthorityAndName_AreRejectedByPostgres()
    {
        var authority = await AddAuthorityAsync();
        var name = $"Exame {Guid.CreateVersion7():N}"[..30];

        await AddAsync(authority, name);

        var save = async () => await AddAsync(authority, name.ToUpperInvariant());

        var failure = await save.Should().ThrowAsync<DbUpdateException>();
        failure.WithInnerExceptionExactly<PostgresException>()
            .Which.ConstraintName.Should().Be(ExamUniqueViolations.NameIndex);
    }

    // BR10: the name is free again under a different issuing authority.
    [Fact]
    public async Task TheSameNameUnderAnotherAuthority_IsAccepted()
    {
        var first = await AddAuthorityAsync();
        var second = await AddAuthorityAsync();
        var name = $"Exame {Guid.CreateVersion7():N}"[..30];

        await AddAsync(first, name);

        var save = async () => await AddAsync(second, name);

        await save.Should().NotThrowAsync();
    }

    [Fact]
    public async Task TheIndexIsUniqueAndTreatsNullTenantsAsEqual()
    {
        var indexes = await QueryAsync(context => context.Database
            .SqlQuery<string>($"""
                SELECT indexdef AS "Value" FROM pg_indexes
                WHERE schemaname = 'catalog' AND tablename = 'exams' AND indexname LIKE 'ux_%'
                """)
            .ToListAsync());

        indexes.Should().ContainSingle();
        indexes[0].Should().Contain("UNIQUE", Exactly.Once());
        indexes[0].Should().Contain("NULLS NOT DISTINCT", Exactly.Once());
    }

    // BR12, at the database level: the foreign key refuses to leave an exam without its authority.
    [Fact]
    public async Task TheForeignKeyToTheIssuingAuthority_IsRestricted()
    {
        var rule = await QueryAsync(context => context.Database
            .SqlQuery<string>($"""
                SELECT rc.delete_rule AS "Value"
                FROM information_schema.referential_constraints rc
                WHERE rc.constraint_name = 'fk_exams_issuing_authority'
                """)
            .ToListAsync());

        rule.Should().ContainSingle();
        rule[0].Should().Be("RESTRICT", "the database refuses to delete an organizer an exam points at");
    }

    private async Task<Guid> AddAuthorityAsync()
    {
        var organizer = Organizer.Create(
            $"Orgao {Guid.CreateVersion7():N}"[..30],
            Guid.CreateVersion7().ToString("N")[..12],
            OrganizerKind.PublicBody,
            null,
            null).Value;

        await using var scope = Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<CatalogModuleDbContext>();
        context.Organizers.Add(organizer);
        await context.SaveChangesAsync();

        return organizer.Id;
    }

    private async Task AddAsync(Guid authority, string name)
    {
        var exam = Exam.Create(authority, name, AssessmentType.PublicServiceExam, ExamScope.National, null, "pt-BR").Value;

        await using var scope = Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<CatalogModuleDbContext>();
        context.Exams.Add(exam);
        await context.SaveChangesAsync();
    }
}
