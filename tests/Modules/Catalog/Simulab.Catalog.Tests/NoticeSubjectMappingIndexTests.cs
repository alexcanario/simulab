using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Simulab.Catalog.Contracts;
using Simulab.Catalog.Domain.Entities;
using Simulab.Catalog.Infrastructure.Persistence;
using Simulab.Catalog.Infrastructure.Persistence.Configurations;

namespace Simulab.Catalog.Tests;

/// <summary>
/// F-75 AC15: the guarantees are the database's, not the handler's. Two live global rows (TenantId null) for the
/// same notice subject and target collide only because the index is NULLS NOT DISTINCT (ADR-0001, decision 8);
/// a dropped row does not hold its place because the index is filtered to live rows; a row with both targets or
/// neither is refused by the check constraint.
/// </summary>
public sealed class NoticeSubjectMappingIndexTests : CatalogApiTests
{
    // AC15
    [Fact]
    public async Task TwoGlobalRowsForTheSameTopic_AreRejectedByPostgres()
    {
        var (row, _, topic) = await ArrangeAsync();
        await SaveAsync(NoticeSubjectMapping.ForTopic(row, topic));

        var save = async () => await SaveAsync(NoticeSubjectMapping.ForTopic(row, topic));

        var failure = await save.Should().ThrowAsync<DbUpdateException>();
        failure.WithInnerExceptionExactly<PostgresException>()
            .Which.ConstraintName.Should().Be(NoticeSubjectMappingConfiguration.UniqueIndex);
    }

    [Fact]
    public async Task TwoGlobalRowsForTheSameWholeSubject_AreRejectedByPostgres()
    {
        var (row, subject, _) = await ArrangeAsync();
        await SaveAsync(NoticeSubjectMapping.ForSubject(row, subject));

        var save = async () => await SaveAsync(NoticeSubjectMapping.ForSubject(row, subject));

        var failure = await save.Should().ThrowAsync<DbUpdateException>();
        failure.WithInnerExceptionExactly<PostgresException>()
            .Which.ConstraintName.Should().Be(NoticeSubjectMappingConfiguration.UniqueIndex);
    }

    // BR6: the same target under another notice subject is fine.
    [Fact]
    public async Task TheSameTopicUnderAnotherNoticeSubject_IsAccepted()
    {
        var (first, _, topic) = await ArrangeAsync();
        var second = await AddNoticeSubjectAsync(await EditionOfAsync(first));
        await SaveAsync(NoticeSubjectMapping.ForTopic(first, topic));

        var save = async () => await SaveAsync(NoticeSubjectMapping.ForTopic(second, topic));

        await save.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ADeletedRow_DoesNotHoldItsTarget()
    {
        var (row, _, topic) = await ArrangeAsync();
        var first = NoticeSubjectMapping.ForTopic(row, topic);
        await SaveAsync(first);
        await QueryAsync(context => context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE catalog.notice_subject_mappings SET is_deleted = true WHERE id = {first.Id}"));

        var again = async () => await SaveAsync(NoticeSubjectMapping.ForTopic(row, topic));

        await again.Should().NotThrowAsync();
    }

    // AC15: both targets, or neither, are refused by the check constraint.
    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public async Task ARowWithBothTargetsOrNeither_IsRejectedByPostgres(bool subject, bool topic)
    {
        var (row, subjectId, topicId) = await ArrangeAsync();
        var id = Guid.CreateVersion7();
        var subjectValue = subject ? subjectId : (Guid?)null;
        var topicValue = topic ? topicId : (Guid?)null;

        var save = async () => await QueryAsync(context => context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO catalog.notice_subject_mappings
                (id, notice_subject_id, subject_id, topic_id, created_at, is_deleted)
            VALUES ({id}, {row}, {subjectValue}, {topicValue}, now(), false)
            """));

        var failure = await save.Should().ThrowAsync<PostgresException>();
        failure.Which.ConstraintName.Should().Be(NoticeSubjectMappingConfiguration.OneTargetCheck);
    }

    [Fact]
    public async Task TheUniqueIndex_TreatsNullTenantsAsEqual_AndHoldsOnlyLiveRows()
    {
        var definition = await QueryAsync(context => context.Database
            .SqlQuery<string>($"""
                SELECT indexdef AS "Value" FROM pg_indexes
                WHERE schemaname = 'catalog' AND tablename = 'notice_subject_mappings'
                  AND indexname = {NoticeSubjectMappingConfiguration.UniqueIndex}
                """)
            .ToListAsync());

        definition.Should().ContainSingle();
        definition[0].Should().Contain("UNIQUE", Exactly.Once());
        definition[0].Should().Contain("NULLS NOT DISTINCT", Exactly.Once());
        definition[0].Should().Contain("is_deleted = false", Exactly.Once());
    }

    [Fact]
    public async Task TheForeignKeys_AreRestrictedExceptTheNoticeSubjectOneWhichCascades()
    {
        var rules = await QueryAsync(context => context.Database
            .SqlQuery<string>($"""
                SELECT constraint_name || '=' || delete_rule AS "Value" FROM information_schema.referential_constraints
                WHERE constraint_name LIKE 'fk_notice_subject_mappings_%'
                """)
            .ToListAsync());

        rules.Should().BeEquivalentTo(
            "fk_notice_subject_mappings_notice_subject=CASCADE",
            "fk_notice_subject_mappings_subject=RESTRICT",
            "fk_notice_subject_mappings_topic=RESTRICT");
    }

    private async Task<(Guid NoticeSubject, Guid Subject, Guid Topic)> ArrangeAsync()
    {
        var edition = await AddEditionAsync();
        var noticeSubject = await AddNoticeSubjectAsync(edition);
        var subject = Subject.Create($"Disciplina {Guid.CreateVersion7():N}"[..30], null).Value;
        var topic = Topic.Create(subject.Id, $"Topico {Guid.CreateVersion7():N}"[..30]).Value;

        await using var scope = Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<CatalogModuleDbContext>();
        context.Subjects.Add(subject);
        context.Topics.Add(topic);
        await context.SaveChangesAsync();

        return (noticeSubject, subject.Id, topic.Id);
    }

    private Task<Guid> EditionOfAsync(Guid noticeSubject) =>
        QueryAsync(context => Task.FromResult(
            context.NoticeSubjects.Where(row => row.Id == noticeSubject).Select(row => row.ExamEditionId).Single()));

    private async Task SaveAsync(NoticeSubjectMapping mapping)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<CatalogModuleDbContext>();
        context.NoticeSubjectMappings.Add(mapping);
        await context.SaveChangesAsync();
    }

    private async Task<Guid> AddNoticeSubjectAsync(Guid edition)
    {
        var subject = NoticeSubject.Create(edition, null, $"Linha {Guid.CreateVersion7():N}"[..30], null).Value;

        await using var scope = Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<CatalogModuleDbContext>();
        context.NoticeSubjects.Add(subject);
        await context.SaveChangesAsync();

        return subject.Id;
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
}
