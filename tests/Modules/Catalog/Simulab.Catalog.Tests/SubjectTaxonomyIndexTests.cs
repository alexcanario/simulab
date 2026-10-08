using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Simulab.Catalog.Domain.Entities;
using Simulab.Catalog.Infrastructure.Persistence;

namespace Simulab.Catalog.Tests;

/// <summary>
/// F-79 AC1, AC5, BR5 and BR7: the guarantee is the database's, not the handler's. Two global rows
/// (TenantId null) with the same normalized name must collide, which only happens because the unique
/// indexes are NULLS NOT DISTINCT (ADR-0001, decision 8). A topic's name is unique only inside its subject.
/// </summary>
public sealed class SubjectTaxonomyIndexTests : CatalogApiTests
{
    // AC5: two global subjects with the same normalized name collide in PostgreSQL.
    [Fact]
    public async Task TwoGlobalSubjectsWithTheSameName_AreRejectedByPostgres()
    {
        var name = $"Subject {Guid.CreateVersion7():N}"[..30];
        await AddSubjectAsync(name);

        var save = async () => await AddSubjectAsync(name.ToUpperInvariant());

        var failure = await save.Should().ThrowAsync<DbUpdateException>();
        failure.WithInnerExceptionExactly<PostgresException>()
            .Which.ConstraintName.Should().Be(SubjectUniqueViolations.NameIndex);
    }

    // BR7: two global topics with the same normalized name in one subject collide in PostgreSQL.
    [Fact]
    public async Task TwoGlobalTopicsWithTheSameNameInOneSubject_AreRejectedByPostgres()
    {
        var subject = await AddSubjectAsync($"Subject {Guid.CreateVersion7():N}"[..30]);
        await AddTopicAsync(subject, "Crase");

        var save = async () => await AddTopicAsync(subject, "CRASE");

        var failure = await save.Should().ThrowAsync<DbUpdateException>();
        failure.WithInnerExceptionExactly<PostgresException>()
            .Which.ConstraintName.Should().Be(TopicUniqueViolations.NameIndex);
    }

    // BR7: the same name under two subjects is allowed.
    [Fact]
    public async Task TheSameTopicNameUnderAnotherSubject_IsAccepted()
    {
        var first = await AddSubjectAsync($"First {Guid.CreateVersion7():N}"[..30]);
        var second = await AddSubjectAsync($"Second {Guid.CreateVersion7():N}"[..30]);
        await AddTopicAsync(first, "Crase");

        var save = async () => await AddTopicAsync(second, "Crase");

        await save.Should().NotThrowAsync();
    }

    // Every unique index over a nullable tenant column is NULLS NOT DISTINCT (rule project.md).
    [Theory]
    [InlineData("areas", "ux_areas_tenant_code")]
    [InlineData("subjects", "ux_subjects_tenant_normalized_name")]
    [InlineData("topics", "ux_topics_tenant_subject_normalized_name")]
    public async Task TheUniqueIndexes_TreatNullTenantsAsEqual(string table, string index)
    {
        var definition = await QueryAsync(context => context.Database
            .SqlQuery<string>($"""
                SELECT indexdef AS "Value" FROM pg_indexes
                WHERE schemaname = 'catalog' AND tablename = {table} AND indexname = {index}
                """)
            .ToListAsync());

        definition.Should().ContainSingle();
        definition[0].Should().Contain("UNIQUE", Exactly.Once());
        definition[0].Should().Contain("NULLS NOT DISTINCT", Exactly.Once());
    }

    // The area code collides on a second global row too.
    [Fact]
    public async Task TwoGlobalAreasWithTheSameCode_AreRejectedByPostgres()
    {
        var save = async () => await QueryAsync(context => context.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO catalog.areas (id, code, display_order, created_at, is_deleted) VALUES ({Guid.CreateVersion7()}, 'Law', 99, now(), false)"));

        var failure = await save.Should().ThrowAsync<PostgresException>();
        failure.Which.ConstraintName.Should().Be("ux_areas_tenant_code");
    }

    // AC1 (BR6): the foreign keys of the taxonomy are restricted.
    [Theory]
    [InlineData("fk_topics_subject", "subjects")]
    [InlineData("fk_subjects_area", "areas")]
    public async Task TheForeignKeys_AreRestrictedAndPointAtTheirParent(string constraint, string parent)
    {
        var rule = await QueryAsync(context => context.Database
            .SqlQuery<string>($"""
                SELECT delete_rule AS "Value" FROM information_schema.referential_constraints
                WHERE constraint_name = {constraint}
                """)
            .ToListAsync());
        var parents = await QueryAsync(context => context.Database
            .SqlQuery<string>($"""
                SELECT table_name AS "Value" FROM information_schema.constraint_column_usage
                WHERE constraint_name = {constraint}
                """)
            .ToListAsync());

        rule.Should().ContainSingle().Which.Should().Be("RESTRICT");
        parents.Should().AllBe(parent);
    }

    private async Task<Guid> AddSubjectAsync(string name)
    {
        var subject = Subject.Create(name, null).Value;

        await using var scope = Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<CatalogModuleDbContext>();
        context.Subjects.Add(subject);
        await context.SaveChangesAsync();

        return subject.Id;
    }

    private async Task AddTopicAsync(Guid subject, string name)
    {
        var topic = Topic.Create(subject, name).Value;

        await using var scope = Factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<CatalogModuleDbContext>();
        context.Topics.Add(topic);
        await context.SaveChangesAsync();
    }
}
