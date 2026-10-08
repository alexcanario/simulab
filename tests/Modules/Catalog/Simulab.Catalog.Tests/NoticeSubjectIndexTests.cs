using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Simulab.Catalog.Contracts;
using Simulab.Catalog.Domain.Entities;
using Simulab.Catalog.Infrastructure.Persistence;
using Simulab.Catalog.Infrastructure.Persistence.Configurations;

namespace Simulab.Catalog.Tests;

/// <summary>
/// F-74 BR5 and AC7, AC8: the guarantee is the database's, not the handler's. Two global rows (TenantId null)
/// with the same normalized group and label in one edition must collide, which only happens because the index
/// is NULLS NOT DISTINCT (ADR-0001, decision 8); and a deleted row must not hold its place, because the index
/// is filtered to the rows not deleted.
/// </summary>
public sealed class NoticeSubjectIndexTests : CatalogApiTests
{
    // BR5: two global rows with the same normalized group and label collide in PostgreSQL.
    [Fact]
    public async Task TwoGlobalRowsWithTheSameGroupAndLabel_AreRejectedByPostgres()
    {
        var edition = await AddEditionAsync();
        await AddAsync(edition, "Básicos", "Português");

        var save = async () => await AddAsync(edition, "BASICOS", "PORTUGUES");

        var failure = await save.Should().ThrowAsync<DbUpdateException>();
        failure.WithInnerExceptionExactly<PostgresException>()
            .Which.ConstraintName.Should().Be(NoticeSubjectConfiguration.UniqueIndex);
    }

    // "No group" is one group: the empty normalized group collides with itself.
    [Fact]
    public async Task TwoGlobalRowsWithNoGroupAndTheSameLabel_AreRejectedByPostgres()
    {
        var edition = await AddEditionAsync();
        await AddAsync(edition, null, "Português");

        var save = async () => await AddAsync(edition, "   ", "Português");

        var failure = await save.Should().ThrowAsync<DbUpdateException>();
        failure.WithInnerExceptionExactly<PostgresException>()
            .Which.ConstraintName.Should().Be(NoticeSubjectConfiguration.UniqueIndex);
    }

    [Fact]
    public async Task TheSameLabelInAnotherGroupOrEdition_IsAccepted()
    {
        var first = await AddEditionAsync();
        var second = await AddEditionAsync();
        await AddAsync(first, "Básicos", "Português");

        var otherGroup = async () => await AddAsync(first, "Específicos", "Português");
        var otherEdition = async () => await AddAsync(second, "Básicos", "Português");

        await otherGroup.Should().NotThrowAsync();
        await otherEdition.Should().NotThrowAsync();
    }

    // AC8: the index is filtered, so a deleted row does not hold its label.
    [Fact]
    public async Task ADeletedRow_DoesNotHoldItsLabel()
    {
        var edition = await AddEditionAsync();
        var first = await AddAsync(edition, "Básicos", "Português");
        await QueryAsync(context => context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE catalog.notice_subjects SET is_deleted = true WHERE id = {first}"));

        var again = async () => await AddAsync(edition, "Básicos", "Português");

        await again.Should().NotThrowAsync();
    }

    // The index is NULLS NOT DISTINCT and filtered to live rows (rule project.md and BR5).
    [Fact]
    public async Task TheUniqueIndex_TreatsNullTenantsAsEqual_AndHoldsOnlyLiveRows()
    {
        var definition = await QueryAsync(context => context.Database
            .SqlQuery<string>($"""
                SELECT indexdef AS "Value" FROM pg_indexes
                WHERE schemaname = 'catalog' AND tablename = 'notice_subjects'
                  AND indexname = {NoticeSubjectConfiguration.UniqueIndex}
                """)
            .ToListAsync());

        definition.Should().ContainSingle();
        definition[0].Should().Contain("UNIQUE", Exactly.Once());
        definition[0].Should().Contain("NULLS NOT DISTINCT", Exactly.Once());
        definition[0].Should().Contain("is_deleted = false", Exactly.Once());
    }

    [Fact]
    public async Task TheEditionForeignKey_IsRestrictedAndPointsAtTheEditions()
    {
        var rule = await QueryAsync(context => context.Database
            .SqlQuery<string>($"""
                SELECT delete_rule AS "Value" FROM information_schema.referential_constraints
                WHERE constraint_name = 'fk_notice_subjects_exam_edition'
                """)
            .ToListAsync());
        var parents = await QueryAsync(context => context.Database
            .SqlQuery<string>($"""
                SELECT table_name AS "Value" FROM information_schema.constraint_column_usage
                WHERE constraint_name = 'fk_notice_subjects_exam_edition'
                """)
            .ToListAsync());

        rule.Should().ContainSingle().Which.Should().Be("RESTRICT");
        parents.Should().AllBe("exam_editions");
    }

    // A row needs an edition that exists: the foreign key refuses an orphan.
    [Fact]
    public async Task ARowWithoutAnEdition_IsRejectedByPostgres()
    {
        var save = async () => await AddAsync(Guid.CreateVersion7(), "Básicos", "Português");

        var failure = await save.Should().ThrowAsync<DbUpdateException>();
        failure.WithInnerExceptionExactly<PostgresException>()
            .Which.ConstraintName.Should().Be("fk_notice_subjects_exam_edition");
    }

    private async Task<Guid> AddEditionAsync()
    {
        var authority = IssuingAuthority.Create($"Orgao {Guid.CreateVersion7():N}"[..30], null, null).Value;
        var exam = Exam.Create(authority.Id, $"Exame {Guid.CreateVersion7():N}"[..30], AssessmentType.PublicServiceExam, ExamScope.National, null, "pt-BR").Value;
        var board = Organizer.Create(
            $"Banca {Guid.CreateVersion7():N}"[..30],
            Guid.CreateVersion7().ToString("N")[..12],
            OrganizerKind.ExamBoard,
            null,
            null).Value;
        var edition = ExamEdition.Create(exam.Id, board.Id, 2026, null, null, null, null, ExamEditionStatus.Draft, 2100).Value;

        await using var scope = Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<CatalogModuleDbContext>();
        context.IssuingAuthorities.Add(authority);
        context.Organizers.Add(board);
        context.Exams.Add(exam);
        context.ExamEditions.Add(edition);
        await context.SaveChangesAsync();

        return edition.Id;
    }

    private async Task<Guid> AddAsync(Guid edition, string? group, string label)
    {
        var subject = NoticeSubject.Create(edition, group, label, null).Value;

        await using var scope = Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<CatalogModuleDbContext>();
        context.NoticeSubjects.Add(subject);
        await context.SaveChangesAsync();

        return subject.Id;
    }
}
