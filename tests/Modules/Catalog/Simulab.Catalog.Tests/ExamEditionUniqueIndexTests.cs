using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Simulab.Catalog.Contracts;
using Simulab.Catalog.Domain.Entities;
using Simulab.Catalog.Infrastructure.Persistence.Configurations;

namespace Simulab.Catalog.Tests;

/// <summary>
/// F-35 AC1, AC9, BR10 and BR12: the guarantee is the database's, not the handler's. Two global rows (TenantId
/// null) with the same exam, year, board and no position must collide, which only happens because the unique
/// index is NULLS NOT DISTINCT (ADR-0001, decision 8).
/// </summary>
public sealed class ExamEditionUniqueIndexTests : CatalogApiTests
{
    // AC1: the migration created the table with the three indexes.
    [Fact]
    public async Task Migration_CreatesTheTableWithTheUniqueIndexAndTheBoardYearIndex()
    {
        var indexes = await QueryAsync(context => context.Database
            .SqlQuery<string>($"""
                SELECT indexname || ' ' || indexdef AS "Value" FROM pg_indexes
                WHERE schemaname = 'catalog' AND tablename = 'exam_editions'
                """)
            .ToListAsync());

        var unique = indexes.Should().ContainSingle(index => index.StartsWith(ExamEditionConfiguration.UniqueIndex)).Subject;
        unique.Should().Contain("UNIQUE", Exactly.Once());
        unique.Should().Contain("NULLS NOT DISTINCT", Exactly.Once());
        unique.Should().Contain("tenant_id, exam_id, notice_year, normalized_position, organizer_id");

        indexes.Should().ContainSingle(index =>
            index.StartsWith("ix_exam_editions_organizer_year") && index.Contains("(organizer_id, notice_year)"));
        indexes.Should().Contain(index => index.StartsWith("ix_exam_editions_exam "));
    }

    // AC1 and BR12: both foreign keys are restricted, and they point at the exams and at the boards.
    [Theory]
    [InlineData("fk_exam_editions_exam", "exams")]
    [InlineData("fk_exam_editions_organizer", "organizers")]
    public async Task TheForeignKeys_AreRestrictedAndPointAtTheirParents(string constraint, string parentTable)
    {
        var rule = await QueryAsync(context => context.Database
            .SqlQuery<string>($"""
                SELECT rc.delete_rule AS "Value"
                FROM information_schema.referential_constraints rc
                WHERE rc.constraint_name = {constraint}
                """)
            .ToListAsync());

        rule.Should().ContainSingle().Which.Should().Be("RESTRICT");

        var parent = await QueryAsync(context => context.Database
            .SqlQuery<string>($"""
                SELECT ccu.table_name AS "Value"
                FROM information_schema.constraint_column_usage ccu
                WHERE ccu.constraint_name = {constraint}
                """)
            .ToListAsync());
        parent.Should().AllBe(parentTable);
    }

    // AC9: the same exam, year, board and no position twice is refused by PostgreSQL.
    [Fact]
    public async Task TwoGlobalRowsWithTheSameExamYearBoardAndNoPosition_AreRejectedByPostgres()
    {
        var (exam, board) = await AddParentsAsync();
        await AddAsync(exam, board, 2026, null);

        var save = async () => await AddAsync(exam, board, 2026, null);

        var failure = await save.Should().ThrowAsync<DbUpdateException>();
        failure.WithInnerExceptionExactly<PostgresException>()
            .Which.ConstraintName.Should().Be(ExamEditionConfiguration.UniqueIndex);
    }

    // BR10: the position counts through its normalized form, case and accents ignored.
    [Fact]
    public async Task TwoRowsWhosePositionsDifferOnlyInCaseAndAccents_AreRejectedByPostgres()
    {
        var (exam, board) = await AddParentsAsync();
        await AddAsync(exam, board, 2026, "Guarda Municipal");

        var save = async () => await AddAsync(exam, board, 2026, "GUARDA MUNICIPAL");

        await save.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task ADifferentYearOrABoardOrAPosition_IsAccepted()
    {
        var (exam, board) = await AddParentsAsync();
        var (_, otherBoard) = await AddParentsAsync();
        await AddAsync(exam, board, 2026, "Guarda");

        var save = async () =>
        {
            await AddAsync(exam, board, 2025, "Guarda");
            await AddAsync(exam, otherBoard, 2026, "Guarda");
            await AddAsync(exam, board, 2026, "Inspetor");
            await AddAsync(exam, board, 2026, null);
        };

        await save.Should().NotThrowAsync();
    }

    // BR1: a soft-deleted edition still holds its key.
    [Fact]
    public async Task ADeletedRow_KeepsItsKeyTaken()
    {
        var (exam, board) = await AddParentsAsync();
        var id = await AddAsync(exam, board, 2026, "Guarda");

        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<Simulab.Catalog.Infrastructure.Persistence.CatalogModuleDbContext>();
            var edition = await context.ExamEditions.FirstAsync(row => row.Id == id);
            context.ExamEditions.Remove(edition);
            await context.SaveChangesAsync();
        }

        var save = async () => await AddAsync(exam, board, 2026, "guarda");

        await save.Should().ThrowAsync<DbUpdateException>();
    }

    private async Task<(Guid Exam, Guid Board)> AddParentsAsync()
    {
        var authority = IssuingAuthority.Create(
            $"Orgao {Guid.CreateVersion7():N}"[..30],
            Guid.CreateVersion7().ToString("N")[..12],
            null,
            null).Value;
        var exam = Exam.Create(authority.Id, $"Exame {Guid.CreateVersion7():N}"[..30], AssessmentType.PublicServiceExam, ExamScope.National, null, "pt-BR").Value;
        var board = Organizer.Create(
            $"Banca {Guid.CreateVersion7():N}"[..30],
            Guid.CreateVersion7().ToString("N")[..12],
            OrganizerKind.ExamBoard,
            null,
            null).Value;

        await using var scope = Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<Simulab.Catalog.Infrastructure.Persistence.CatalogModuleDbContext>();
        context.IssuingAuthorities.Add(authority);
        context.Organizers.Add(board);
        context.Exams.Add(exam);
        await context.SaveChangesAsync();

        return (exam.Id, board.Id);
    }

    private async Task<Guid> AddAsync(Guid exam, Guid board, int year, string? position)
    {
        var edition = ExamEdition.Create(exam, board, year, position, null, null, null, ExamEditionStatus.Draft, 2100).Value;

        await using var scope = Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<Simulab.Catalog.Infrastructure.Persistence.CatalogModuleDbContext>();
        context.ExamEditions.Add(edition);
        await context.SaveChangesAsync();

        return edition.Id;
    }
}
