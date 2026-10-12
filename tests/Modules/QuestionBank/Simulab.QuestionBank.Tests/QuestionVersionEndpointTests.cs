using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Simulab.Catalog.Contracts;
using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Serialization;

namespace Simulab.QuestionBank.Tests;

/// <summary>
/// F-98 through HTTP, on a real PostgreSQL: the versions a publish and a save record (BR2, BR3), the answer key
/// change and its reason (BR4, BR5), reading the history (BR7, BR8), the permission and what a delete leaves.
/// </summary>
public sealed class QuestionVersionEndpointTests : OriginApiTests
{
    private static SaveQuestionRequest Request(
        Guid topicId,
        string statement = "<p>Qual é a capital da França?</p>",
        bool parisIsCorrect = true,
        bool swap = false,
        string? reason = null,
        int? difficulty = null)
    {
        var paris = new SaveQuestionOptionRequest("<p>Paris</p>", parisIsCorrect);
        var lyon = new SaveQuestionOptionRequest("<p>Lyon</p>", !parisIsCorrect);

        return new SaveQuestionRequest(
            topicId,
            "pt-BR",
            statement,
            Options: swap ? [lyon, paris] : [paris, lyon],
            Difficulty: difficulty,
            ChangeReason: reason);
    }

    private static Task<HttpResponseMessage> PublishAsync(HttpClient client, Guid id, string? reason = null) =>
        reason is null
            ? client.PostAsync($"{Questions}/{id}/publish", null)
            : client.PostAsJsonAsync($"{Questions}/{id}/publish", new PublishQuestionRequest(reason), AppJson.Options);

    private static async Task<QuestionResponse> PublishedAgainAsync(HttpClient client, Guid id, string? reason = null)
    {
        var response = await PublishAsync(client, id, reason);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<QuestionResponse>(AppJson.Options))!;
    }

    private static async Task UnpublishAsync(HttpClient client, Guid id)
    {
        var response = await client.PostAsync($"{Questions}/{id}/unpublish", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
    }

    private static async Task<IReadOnlyList<QuestionVersionListItemResponse>> VersionsAsync(HttpClient client, Guid id)
    {
        var response = await client.GetAsync($"{Questions}/{id}/versions");
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<List<QuestionVersionListItemResponse>>(AppJson.Options))!;
    }

    private static async Task<QuestionVersionResponse> VersionAsync(HttpClient client, Guid id, int number)
    {
        var response = await client.GetAsync($"{Questions}/{id}/versions/{number}");
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<QuestionVersionResponse>(AppJson.Options))!;
    }

    private async Task<(HttpClient Admin, TopicResponse Topic, QuestionResponse Question)> PublishedAtVersionOneAsync()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var draft = await CreatedAsync(admin, Request(topic.Id));
        var published = await PublishedAgainAsync(admin, draft.Id);

        return (admin, topic, published);
    }

    private Task<int> VersionRowsAsync(Guid questionId) =>
        QueryAsync(context => context.Database
            .SqlQuery<int>($"SELECT count(*)::int AS \"Value\" FROM question_bank.question_versions WHERE question_id = {questionId}")
            .SingleAsync());

    // AC1, BR2, BR11: saving a draft makes no version; the first publication makes version 1 with the content.
    [Fact]
    public async Task Publish_ACompleteDraft_RecordsVersionOne_AndSavingTheDraftBeforeRecordedNone()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var draft = await CreatedAsync(admin, Request(topic.Id));
        await UpdatedAsync(admin, draft.Id, Request(topic.Id, statement: "<p>Qual é a capital da França? (rascunho)</p>"));
        (await VersionsAsync(admin, draft.Id)).Should().BeEmpty();
        (await GetAsync(admin, draft.Id)).CurrentVersion.Should().BeNull();

        var published = await PublishedAgainAsync(admin, draft.Id);

        published.CurrentVersion.Should().Be(1);
        (await GetAsync(admin, draft.Id)).CurrentVersion.Should().Be(1);
        var versions = await VersionsAsync(admin, draft.Id);
        versions.Should().ContainSingle();
        versions[0].Number.Should().Be(1);
        versions[0].AnswerKeyChanged.Should().BeFalse();
        versions[0].Reason.Should().BeNull();
        versions[0].IsCurrent.Should().BeTrue();
        var version = await VersionAsync(admin, draft.Id, 1);
        version.QuestionId.Should().Be(draft.Id);
        version.Type.Should().Be(QuestionType.SingleChoice);
        version.ContentLanguage.Should().Be("pt-BR");
        version.StatementHtml.Should().Be("<p>Qual é a capital da França? (rascunho)</p>");
        version.Options.Select(option => (option.Position, option.IsCorrect)).Should().Equal((0, true), (1, false));
        version.Options.Select(option => option.TextHtml).Should().Equal("<p>Paris</p>", "<p>Lyon</p>");
        version.CreatedAt.Should().BeAfter(DateTimeOffset.UtcNow.AddMinutes(-5));
    }

    // BR7: the options are stored in the version's JSON column under the names the mapping reads back.
    [Fact]
    public async Task AVersion_StoresItsOptionsAsJsonWithPositionTextHtmlAndIsCorrect()
    {
        var (_, _, question) = await PublishedAtVersionOneAsync();

        var members = await QueryAsync(context => context.Database
            .SqlQuery<string>($"SELECT string_agg(k, ',' ORDER BY k) AS \"Value\" FROM (SELECT DISTINCT jsonb_object_keys(e) AS k FROM question_bank.question_versions v, jsonb_array_elements(v.options) e WHERE v.question_id = {question.Id}) keys")
            .SingleAsync());

        members.Should().Be("IsCorrect,Position,TextHtml");
    }

    // AC2, BR1, BR3, BR6: a new statement makes version 2; version 1 still holds the old one.
    [Fact]
    public async Task Save_AChangedStatementOnAPublishedQuestion_RecordsVersionTwo_AndVersionOneKeepsTheOldText()
    {
        var (admin, topic, question) = await PublishedAtVersionOneAsync();

        var saved = await UpdatedAsync(admin, question.Id, Request(topic.Id, statement: "<p>Qual é a capital da Espanha?</p>"));

        saved.CurrentVersion.Should().Be(2);
        var versions = await VersionsAsync(admin, question.Id);
        versions.Select(version => version.Number).Should().Equal(2, 1);
        (await VersionAsync(admin, question.Id, 2)).StatementHtml.Should().Be("<p>Qual é a capital da Espanha?</p>");
        (await VersionAsync(admin, question.Id, 1)).StatementHtml.Should().Be("<p>Qual é a capital da França?</p>");
        (await GetAsync(admin, question.Id)).StatementHtml.Should().Be("<p>Qual é a capital da Espanha?</p>");
    }

    // AC3, BR1, BR3: the same content, or only the catalog data, makes no version.
    [Fact]
    public async Task Save_SameContent_OrOnlyTopicDifficultyAndOrigin_RecordsNoVersion()
    {
        var (admin, topic, question) = await PublishedAtVersionOneAsync();
        var otherTopic = await TopicAsync(admin);
        var edition = await EditionAsync(admin);

        await UpdatedAsync(admin, question.Id, Request(topic.Id));
        (await VersionsAsync(admin, question.Id)).Should().ContainSingle();

        await UpdatedAsync(admin, question.Id, Request(otherTopic.Id, difficulty: 4) with
        {
            ExamEditionId = edition.Id,
            PaperNumber = 7
        });

        (await VersionsAsync(admin, question.Id)).Should().ContainSingle();
        (await GetAsync(admin, question.Id)).CurrentVersion.Should().Be(1);
    }

    // AC4, BR4, BR5: the key moves with no reason: 400, and neither the question nor the versions change.
    [Fact]
    public async Task Save_AKeyChangeWithNoReason_IsRefused_AndNothingIsStored()
    {
        var (admin, topic, question) = await PublishedAtVersionOneAsync();

        var response = await UpdateAsync(admin, question.Id, Request(topic.Id, parisIsCorrect: false));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await response.Content.ReadAsStringAsync())
            .Should().Be(QuestionBankErrorCodes.QuestionKeyChangeReasonRequired);
        (await VersionsAsync(admin, question.Id)).Should().ContainSingle();
        var stored = await GetAsync(admin, question.Id);
        stored.Options.Single(option => option.IsCorrect).TextHtml.Should().Be("<p>Paris</p>");
        stored.CurrentVersion.Should().Be(1);
    }

    [Fact]
    public async Task Save_AKeyChangeWithAReason_RecordsAVersionMarkedAsKeyChanged_WithTheReasonAndTheDate()
    {
        var (admin, topic, question) = await PublishedAtVersionOneAsync();

        var saved = await UpdatedAsync(
            admin, question.Id, Request(topic.Id, parisIsCorrect: false, reason: "  Recurso deferido pela banca.  "));

        saved.CurrentVersion.Should().Be(2);
        var list = await VersionsAsync(admin, question.Id);
        list[0].Number.Should().Be(2);
        list[0].AnswerKeyChanged.Should().BeTrue();
        list[0].Reason.Should().Be("Recurso deferido pela banca.");
        list[0].IsCurrent.Should().BeTrue();
        list[1].IsCurrent.Should().BeFalse();
        var version = await VersionAsync(admin, question.Id, 2);
        version.AnswerKeyChanged.Should().BeTrue();
        version.CreatedAt.Should().BeAfter(DateTimeOffset.UtcNow.AddMinutes(-5));
        version.Options.Single(option => option.IsCorrect).TextHtml.Should().Be("<p>Lyon</p>");
    }

    // AC5, BR5: spaces only count as missing; 501 characters is refused with its own code; 500 is fine.
    [Fact]
    public async Task Save_AKeyChangeWithABlankOrOverLongReason_IsRefused_AndNothingIsStored()
    {
        var (admin, topic, question) = await PublishedAtVersionOneAsync();

        var blank = await UpdateAsync(admin, question.Id, Request(topic.Id, parisIsCorrect: false, reason: "     "));
        var tooLong = await UpdateAsync(
            admin, question.Id, Request(topic.Id, parisIsCorrect: false, reason: new string('a', 501)));

        blank.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await blank.Content.ReadAsStringAsync())
            .Should().Be(QuestionBankErrorCodes.QuestionKeyChangeReasonRequired);
        tooLong.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await tooLong.Content.ReadAsStringAsync())
            .Should().Be(QuestionBankErrorCodes.QuestionChangeReasonTooLong);
        (await VersionsAsync(admin, question.Id)).Should().ContainSingle();

        var exact = await UpdatedAsync(
            admin, question.Id, Request(topic.Id, parisIsCorrect: false, reason: new string('a', 500)));
        exact.CurrentVersion.Should().Be(2);
    }

    // AC6, BR4: reordering so that the correct option changes letter is a key change.
    [Fact]
    public async Task Save_AReorderThatMovesTheCorrectOption_IsAKeyChangeAndNeedsTheReason()
    {
        var (admin, topic, question) = await PublishedAtVersionOneAsync();

        var refused = await UpdateAsync(admin, question.Id, Request(topic.Id, swap: true));
        var saved = await UpdatedAsync(
            admin, question.Id, Request(topic.Id, swap: true, reason: "A banca reordenou as alternativas."));

        refused.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await refused.Content.ReadAsStringAsync())
            .Should().Be(QuestionBankErrorCodes.QuestionKeyChangeReasonRequired);
        saved.CurrentVersion.Should().Be(2);
        (await VersionAsync(admin, question.Id, 2)).AnswerKeyChanged.Should().BeTrue();
    }

    // AC7, BR4: a published true/false question whose key flips.
    [Fact]
    public async Task Save_ATrueFalseKeyThatFlips_IsAKeyChangeAndNeedsTheReason()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var trueFalse = new SaveQuestionRequest(
            topic.Id, "pt-BR", "<p>Paris é a capital da França.</p>", Type: QuestionType.TrueFalse, TrueFalseKey: true);
        var draft = await CreatedAsync(admin, trueFalse);
        await PublishedAgainAsync(admin, draft.Id);
        (await VersionAsync(admin, draft.Id, 1)).TrueFalseKey.Should().BeTrue();

        var refused = await UpdateAsync(admin, draft.Id, trueFalse with { TrueFalseKey = false });
        var saved = await UpdatedAsync(
            admin, draft.Id, trueFalse with { TrueFalseKey = false, ChangeReason = "Gabarito oficial retificado." });

        refused.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await refused.Content.ReadAsStringAsync())
            .Should().Be(QuestionBankErrorCodes.QuestionKeyChangeReasonRequired);
        saved.CurrentVersion.Should().Be(2);
        var version = await VersionAsync(admin, draft.Id, 2);
        version.AnswerKeyChanged.Should().BeTrue();
        version.TrueFalseKey.Should().BeFalse();
        version.Options.Should().BeEmpty();
        version.Type.Should().Be(QuestionType.TrueFalse);
    }

    // AC8, BR5: a reason on a version that did not change the key is its note; on a save that makes none it is ignored.
    [Fact]
    public async Task Save_AChangedStatementWithAReason_KeepsItAsANote_AndAnIdenticalSaveWithAReasonRecordsNothing()
    {
        var (admin, topic, question) = await PublishedAtVersionOneAsync();

        await UpdatedAsync(
            admin, question.Id, Request(topic.Id, statement: "<p>Nova redação.</p>", reason: "Correção de redação."));
        await UpdatedAsync(
            admin, question.Id, Request(topic.Id, statement: "<p>Nova redação.</p>", reason: "Outra nota."));

        var versions = await VersionsAsync(admin, question.Id);
        versions.Select(version => version.Number).Should().Equal(2, 1);
        versions[0].AnswerKeyChanged.Should().BeFalse();
        versions[0].Reason.Should().Be("Correção de redação.");
    }

    // AC9, UC4, BR3: unpublish, edit as a draft, publish: the publish records the version, when the content differs.
    [Fact]
    public async Task Publish_AfterAnEditOfTheDraft_RecordsTheNextVersion_AndWithNoEditRecordsNone()
    {
        var (admin, topic, question) = await PublishedAtVersionOneAsync();
        await UnpublishAsync(admin, question.Id);
        await UpdatedAsync(admin, question.Id, Request(topic.Id, statement: "<p>Outro enunciado.</p>"));
        (await VersionsAsync(admin, question.Id)).Should().ContainSingle();

        var published = await PublishedAgainAsync(admin, question.Id);

        published.CurrentVersion.Should().Be(2);
        (await VersionAsync(admin, question.Id, 2)).StatementHtml.Should().Be("<p>Outro enunciado.</p>");

        await UnpublishAsync(admin, question.Id);
        var again = await PublishedAgainAsync(admin, question.Id);

        again.CurrentVersion.Should().Be(2);
        (await VersionsAsync(admin, question.Id)).Select(version => version.Number).Should().Equal(2, 1);
    }

    // AC9, BR5: a key that changed while it was a draft needs the reason at the publish.
    [Fact]
    public async Task Publish_AfterTheKeyChangedInTheDraft_NeedsTheReason_AndTheSecondTryWithItPublishes()
    {
        var (admin, topic, question) = await PublishedAtVersionOneAsync();
        await UnpublishAsync(admin, question.Id);
        await UpdatedAsync(admin, question.Id, Request(topic.Id, parisIsCorrect: false));

        var refused = await PublishAsync(admin, question.Id);

        refused.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await refused.Content.ReadAsStringAsync())
            .Should().Be(QuestionBankErrorCodes.QuestionKeyChangeReasonRequired);
        (await GetAsync(admin, question.Id)).Status.Should().Be(QuestionStatus.Draft);
        (await VersionsAsync(admin, question.Id)).Should().ContainSingle();

        var tooLong = await PublishAsync(admin, question.Id, new string('a', 501));
        tooLong.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await tooLong.Content.ReadAsStringAsync())
            .Should().Be(QuestionBankErrorCodes.QuestionChangeReasonTooLong);
        (await GetAsync(admin, question.Id)).Status.Should().Be(QuestionStatus.Draft);

        var published = await PublishedAgainAsync(admin, question.Id, "Recurso deferido.");

        published.Status.Should().Be(QuestionStatus.Published);
        published.CurrentVersion.Should().Be(2);
        var version = await VersionAsync(admin, question.Id, 2);
        version.AnswerKeyChanged.Should().BeTrue();
        version.Reason.Should().Be("Recurso deferido.");
    }

    // BR5: publishing for the first time with a reason keeps it as the note of version 1, which is never a key change.
    [Fact]
    public async Task Publish_ForTheFirstTimeWithAReason_KeepsItAsTheNoteOfVersionOne()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var draft = await CreatedAsync(admin, Request(topic.Id));

        await PublishedAgainAsync(admin, draft.Id, "Primeira publicação.");

        var version = await VersionAsync(admin, draft.Id, 1);
        version.AnswerKeyChanged.Should().BeFalse();
        version.Reason.Should().Be("Primeira publicação.");
    }

    // AC10, BR7, BR8: newest first with the current one marked; one snapshot by number; a missing number is 404.
    [Fact]
    public async Task Versions_ListNewestFirstWithTheCurrentOneMarked_AndAMissingNumberIsNotFound()
    {
        var (admin, topic, question) = await PublishedAtVersionOneAsync();
        await UpdatedAsync(admin, question.Id, Request(topic.Id, statement: "<p>Dois.</p>"));
        await UpdatedAsync(admin, question.Id, Request(topic.Id, statement: "<p>Três.</p>"));

        var versions = await VersionsAsync(admin, question.Id);

        versions.Select(version => version.Number).Should().Equal(3, 2, 1);
        versions.Select(version => version.IsCurrent).Should().Equal(true, false, false);
        versions.Select(version => version.Id).Distinct().Should().HaveCount(3);
        (await VersionAsync(admin, question.Id, 2)).Id.Should().Be(versions[1].Id);
        var missing = await admin.GetAsync($"{Questions}/{question.Id}/versions/4");
        missing.StatusCode.Should().Be(HttpStatusCode.NotFound);
        CodeOf(await missing.Content.ReadAsStringAsync())
            .Should().Be(QuestionBankErrorCodes.QuestionVersionNotFound);
    }

    [Fact]
    public async Task Versions_OfAnotherQuestion_AreNotFound_AndAnUnknownQuestionAnswersQuestionNotFound()
    {
        var (admin, topic, first) = await PublishedAtVersionOneAsync();
        await UpdatedAsync(admin, first.Id, Request(topic.Id, statement: "<p>Dois.</p>"));
        var second = await PublishedAsync(admin, topic.Id);

        var ofAnother = await admin.GetAsync($"{Questions}/{second.Id}/versions/2");
        var unknownList = await admin.GetAsync($"{Questions}/{Guid.CreateVersion7()}/versions");
        var unknownOne = await admin.GetAsync($"{Questions}/{Guid.CreateVersion7()}/versions/1");

        ofAnother.StatusCode.Should().Be(HttpStatusCode.NotFound);
        CodeOf(await ofAnother.Content.ReadAsStringAsync())
            .Should().Be(QuestionBankErrorCodes.QuestionVersionNotFound);
        unknownList.StatusCode.Should().Be(HttpStatusCode.NotFound);
        CodeOf(await unknownList.Content.ReadAsStringAsync()).Should().Be(QuestionBankErrorCodes.QuestionNotFound);
        unknownOne.StatusCode.Should().Be(HttpStatusCode.NotFound);
        CodeOf(await unknownOne.Content.ReadAsStringAsync()).Should().Be(QuestionBankErrorCodes.QuestionNotFound);
    }

    // AC11, BR8: both reads need questions.manage; a curator holds it.
    [Fact]
    public async Task BothVersionReads_WithoutThePermission_AnswerForbidden_AndACuratorReadsThem()
    {
        var (_, _, question) = await PublishedAtVersionOneAsync();
        var student = await StudentAsync();
        var curator = await CuratorAsync();

        var denied = new[]
        {
            await student.GetAsync($"{Questions}/{question.Id}/versions"),
            await student.GetAsync($"{Questions}/{question.Id}/versions/1")
        };

        denied.Select(answer => answer.StatusCode).Should().OnlyContain(status => status == HttpStatusCode.Forbidden);
        (await VersionsAsync(curator, question.Id)).Should().ContainSingle();
        (await VersionAsync(curator, question.Id, 1)).Number.Should().Be(1);
    }

    // AC12, BR6: deleting a draft that has versions leaves the version rows; a published question is still refused.
    [Fact]
    public async Task Delete_ADraftThatHasVersions_LeavesTheVersionRowsWithTheirContent()
    {
        var (admin, topic, question) = await PublishedAtVersionOneAsync();
        await UpdatedAsync(admin, question.Id, Request(topic.Id, statement: "<p>Dois.</p>"));
        var refused = await admin.DeleteAsync($"{Questions}/{question.Id}");
        refused.StatusCode.Should().Be(HttpStatusCode.Conflict);
        CodeOf(await refused.Content.ReadAsStringAsync())
            .Should().Be(QuestionBankErrorCodes.QuestionDeleteRequiresDraft);
        await UnpublishAsync(admin, question.Id);

        var deleted = await admin.DeleteAsync($"{Questions}/{question.Id}");

        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await admin.GetAsync($"{Questions}/{question.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await VersionRowsAsync(question.Id)).Should().Be(2);
        var statements = await QueryAsync(context => context.Database
            .SqlQuery<string>($"SELECT statement_html AS \"Value\" FROM question_bank.question_versions WHERE question_id = {question.Id} AND is_deleted = false ORDER BY number")
            .ToListAsync());
        statements.Should().Equal("<p>Qual é a capital da França?</p>", "<p>Dois.</p>");
    }

    // BR8, F-95 BR14: a deleted question's history is not read through the API.
    [Fact]
    public async Task Versions_OfADeletedQuestion_AreNotFound()
    {
        var (admin, _, question) = await PublishedAtVersionOneAsync();
        await UnpublishAsync(admin, question.Id);
        (await admin.DeleteAsync($"{Questions}/{question.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var response = await admin.GetAsync($"{Questions}/{question.Id}/versions");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(QuestionBankErrorCodes.QuestionNotFound);
    }

    // BR10 (the unique index itself): two rows with the same number for one question are refused by the database,
    // also when the tenant is null on both.
    [Fact]
    public async Task TheDatabase_RefusesTwoVersionsWithTheSameNumberForOneQuestion()
    {
        var (_, _, question) = await PublishedAtVersionOneAsync();

        var insertTwice = () => QueryAsync(context => context.Database.ExecuteSqlInterpolatedAsync(
            $@"INSERT INTO question_bank.question_versions
                (id, tenant_id, question_id, number, type, content_language, statement_html, options, answer_key_changed, created_at, is_deleted)
               VALUES ({Guid.CreateVersion7()}, NULL, {question.Id}, 1, 'SingleChoice', 'pt-BR', '<p>x</p>', '[]'::jsonb, false, now(), false)"));

        var thrown = await insertTwice.Should().ThrowAsync<Npgsql.PostgresException>();
        thrown.Which.SqlState.Should().Be("23505");
    }
}
