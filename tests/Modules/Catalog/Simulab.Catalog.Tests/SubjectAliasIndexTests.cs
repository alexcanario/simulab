using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Simulab.Catalog.Contracts;
using Simulab.Catalog.Infrastructure.Persistence;
using Simulab.SharedKernel.Serialization;

namespace Simulab.Catalog.Tests;

/// <summary>
/// F-77 AC8 and BR8: the guarantee is the database's. Two global alias rows (TenantId null) with the same
/// normalized form collide only because the unique indexes are NULLS NOT DISTINCT, and the index holds live rows
/// only, so a removed alias frees its text. Rows are inserted directly, not through a handler.
/// </summary>
public sealed class SubjectAliasIndexTests : CatalogApiTests
{
    private const string Subjects = "/api/v1/catalog/subjects";

    private static string Marker() => Guid.CreateVersion7().ToString("N")[..12];

    private async Task<Guid> SubjectIdAsync(string name)
    {
        var admin = await AdminAsync();
        var response = await admin.PostAsJsonAsync(Subjects, new SaveSubjectRequest(name), AppJson.Options);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<SubjectResponse>(AppJson.Options))!.Id;
    }

    private async Task<Guid> TopicIdAsync(Guid subjectId, string name)
    {
        var admin = await AdminAsync();
        var response = await admin.PostAsJsonAsync(
            $"{Subjects}/{subjectId}/topics", new SaveTopicRequest(name), AppJson.Options);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<TopicResponse>(AppJson.Options))!.Id;
    }

    private async Task<Guid> InsertSubjectAliasAsync(Guid subjectId, string normalized, bool deleted = false)
    {
        var id = Guid.CreateVersion7();
        await QueryAsync(context => context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO catalog.subject_aliases (id, subject_id, name, normalized_name, created_at, is_deleted)
            VALUES ({id}, {subjectId}, {normalized}, {normalized}, now(), {deleted})
            """));

        return id;
    }

    private async Task<Guid> InsertTopicAliasAsync(Guid topicId, string normalized, bool deleted = false)
    {
        var id = Guid.CreateVersion7();
        await QueryAsync(context => context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO catalog.topic_aliases (id, topic_id, name, normalized_name, created_at, is_deleted)
            VALUES ({id}, {topicId}, {normalized}, {normalized}, now(), {deleted})
            """));

        return id;
    }

    // AC8: the same subject, the same normalized form, both live: the second is rejected; with the first
    // deleted, it is accepted.
    [Fact]
    public async Task TwoLiveAliasesOfOneSubjectWithTheSameForm_AreRejected_UnlessTheFirstIsDeleted()
    {
        var subject = await SubjectIdAsync($"Língua {Marker()}");
        var form = $"PORTUGUES {Marker()}";
        await InsertSubjectAliasAsync(subject, form);

        var second = async () => await InsertSubjectAliasAsync(subject, form);

        var failure = await second.Should().ThrowAsync<PostgresException>();
        failure.Which.ConstraintName.Should().Be(SubjectUniqueViolations.AliasIndex);

        var other = $"CRASE {Marker()}";
        await InsertSubjectAliasAsync(subject, other, deleted: true);
        var afterDeleted = async () => await InsertSubjectAliasAsync(subject, other);
        await afterDeleted.Should().NotThrowAsync();
    }

    // BR6 in the database: a subject alias is unique across subjects, so a second subject cannot take it.
    [Fact]
    public async Task TheSameFormOnAnotherSubject_IsRejected()
    {
        var first = await SubjectIdAsync($"Primeira {Marker()}");
        var second = await SubjectIdAsync($"Segunda {Marker()}");
        var form = $"PORTUGUES {Marker()}";
        await InsertSubjectAliasAsync(first, form);

        var save = async () => await InsertSubjectAliasAsync(second, form);

        var failure = await save.Should().ThrowAsync<PostgresException>();
        failure.Which.ConstraintName.Should().Be(SubjectUniqueViolations.AliasIndex);
    }

    // AC8 (topic side): inside one topic the form collides; with the first deleted it is accepted; another
    // topic may hold the same form (BR7 compares inside a subject, in the handler).
    [Fact]
    public async Task TwoLiveAliasesOfOneTopicWithTheSameForm_AreRejected_UnlessTheFirstIsDeleted_AndAnotherTopicMayHoldIt()
    {
        var subject = await SubjectIdAsync($"Português {Marker()}");
        var topic = await TopicIdAsync(subject, "Crase");
        var otherTopic = await TopicIdAsync(subject, "Acentuação");
        var form = $"LEITURA {Marker()}";
        await InsertTopicAliasAsync(topic, form);

        var second = async () => await InsertTopicAliasAsync(topic, form);
        var elsewhere = async () => await InsertTopicAliasAsync(otherTopic, form);

        var failure = await second.Should().ThrowAsync<PostgresException>();
        failure.Which.ConstraintName.Should().Be(TopicUniqueViolations.AliasIndex);
        await elsewhere.Should().NotThrowAsync();

        var other = $"INTERPRETACAO {Marker()}";
        await InsertTopicAliasAsync(topic, other, deleted: true);
        var afterDeleted = async () => await InsertTopicAliasAsync(topic, other);
        await afterDeleted.Should().NotThrowAsync();
    }

    // Every unique index over a nullable tenant column is NULLS NOT DISTINCT and holds live rows only (BR8).
    [Theory]
    [InlineData("subject_aliases", "ux_subject_aliases_tenant_normalized_name")]
    [InlineData("topic_aliases", "ux_topic_aliases_tenant_topic_normalized_name")]
    public async Task TheUniqueIndexes_TreatNullTenantsAsEqual_AndHoldLiveRowsOnly(string table, string index)
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
        definition[0].Should().Contain("is_deleted = false", Exactly.Once());
    }
}
