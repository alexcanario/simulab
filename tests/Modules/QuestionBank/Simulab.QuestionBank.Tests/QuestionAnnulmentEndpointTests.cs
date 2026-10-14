using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Simulab.Catalog.Contracts;
using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Serialization;

namespace Simulab.QuestionBank.Tests;

/// <summary>
/// F-101 through HTTP, on a real PostgreSQL: annulling and reinstating (BR1 to BR4, BR6), the frozen state
/// (BR5), the history (BR2), the list filter (BR11), what an annulled question keeps (BR8) and the permission
/// (BR10).
/// </summary>
public sealed class QuestionAnnulmentEndpointTests : OriginApiTests
{
    private static Task<HttpResponseMessage> AnnulAsync(HttpClient client, Guid id, string? reason) =>
        reason is null
            ? client.PostAsync($"{Questions}/{id}/annul", null)
            : client.PostAsJsonAsync($"{Questions}/{id}/annul", new AnnulQuestionRequest(reason), AppJson.Options);

    private static Task<HttpResponseMessage> ReinstateAsync(HttpClient client, Guid id, string? reason) =>
        reason is null
            ? client.PostAsync($"{Questions}/{id}/reinstate", null)
            : client.PostAsJsonAsync($"{Questions}/{id}/reinstate", new ReinstateQuestionRequest(reason), AppJson.Options);

    private static async Task<QuestionResponse> AnnulledAsync(HttpClient client, Guid id, string reason = "A banca anulou o item.")
    {
        var response = await AnnulAsync(client, id, reason);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<QuestionResponse>(AppJson.Options))!;
    }

    private static async Task<QuestionResponse> ReinstatedAsync(HttpClient client, Guid id, string reason = "A banca reverteu.")
    {
        var response = await ReinstateAsync(client, id, reason);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<QuestionResponse>(AppJson.Options))!;
    }

    private static async Task<IReadOnlyList<QuestionVersionListItemResponse>> VersionsAsync(HttpClient client, Guid id)
    {
        var response = await client.GetAsync($"{Questions}/{id}/versions");
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<List<QuestionVersionListItemResponse>>(AppJson.Options))!;
    }

    private static async Task<QuestionVersionResponse> VersionAsync(HttpClient client, Guid id, int number) =>
        (await client.GetFromJsonAsync<QuestionVersionResponse>($"{Questions}/{id}/versions/{number}", AppJson.Options))!;

    private static async Task AssertRefusedAsync(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(status, body);
        CodeOf(body).Should().Be(code);
    }

    private async Task<(HttpClient Admin, TopicResponse Topic, QuestionResponse Question)> PublishedAtVersionOneAsync()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var published = await PublishedAsync(admin, topic.Id);

        return (admin, topic, published);
    }

    // AC1, UC1, BR2, BR4: the status, the response, the version and the earlier ones.
    [Fact]
    public async Task Annul_APublishedQuestion_BecomesAnnulled_WithVersionTwoThatSaysSo()
    {
        var (admin, _, question) = await PublishedAtVersionOneAsync();

        var annulled = await AnnulledAsync(admin, question.Id, "  A banca anulou o item 12.  ");

        annulled.Status.Should().Be(QuestionStatus.Annulled);
        annulled.CurrentVersion.Should().Be(2);
        annulled.Annulment.Should().NotBeNull();
        annulled.Annulment!.Reason.Should().Be("A banca anulou o item 12.");
        annulled.Annulment.AnnulledAt.Should().BeCloseTo(Factory.Clock.GetUtcNow(), TimeSpan.FromMinutes(5));
        annulled.StatementHtml.Should().Be(question.StatementHtml);
        var read = await GetAsync(admin, question.Id);
        read.Status.Should().Be(QuestionStatus.Annulled);
        read.Annulment!.Reason.Should().Be("A banca anulou o item 12.");
        var versions = await VersionsAsync(admin, question.Id);
        versions.Select(version => version.Number).Should().Equal(2, 1);
        versions[0].Annulled.Should().BeTrue();
        versions[0].AnswerKeyChanged.Should().BeFalse();
        versions[0].Reason.Should().Be("A banca anulou o item 12.");
        versions[0].IsCurrent.Should().BeTrue();
        versions[1].Annulled.Should().BeFalse();
        versions[1].Reason.Should().BeNull();
        var snapshot = await VersionAsync(admin, question.Id, 2);
        snapshot.Annulled.Should().BeTrue();
        snapshot.StatementHtml.Should().Be(question.StatementHtml);
        snapshot.Options.Select(option => (option.Position, option.TextHtml, option.IsCorrect))
            .Should().Equal(question.Options.Select(option => (option.Position, option.TextHtml, option.IsCorrect)));
        (await VersionAsync(admin, question.Id, 1)).Annulled.Should().BeFalse();
    }

    // AC2, BR1: a draft that was never published, one that has versions and one already annulled.
    [Fact]
    public async Task Annul_ADraftOrADraftWithVersions_IsRefusedWithNotPublished_AndAnAnnulledOneWithAlreadyAnnulled()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var neverPublished = await CreatedAsync(admin, Complete(topic.Id));
        var withVersions = await PublishedAsync(admin, topic.Id);
        (await admin.PostAsync($"{Questions}/{withVersions.Id}/unpublish", null)).StatusCode.Should().Be(HttpStatusCode.OK);

        await AssertRefusedAsync(await AnnulAsync(admin, neverPublished.Id, "Anulada."), HttpStatusCode.Conflict, QuestionBankErrorCodes.QuestionNotPublished);
        await AssertRefusedAsync(await AnnulAsync(admin, withVersions.Id, "Anulada."), HttpStatusCode.Conflict, QuestionBankErrorCodes.QuestionNotPublished);
        (await GetAsync(admin, neverPublished.Id)).Status.Should().Be(QuestionStatus.Draft);
        (await GetAsync(admin, withVersions.Id)).Status.Should().Be(QuestionStatus.Draft);
        (await VersionsAsync(admin, withVersions.Id)).Should().ContainSingle();

        var published = await PublishedAsync(admin, topic.Id);
        await AnnulledAsync(admin, published.Id);
        await AssertRefusedAsync(await AnnulAsync(admin, published.Id, "Outra vez."), HttpStatusCode.Conflict, QuestionBankErrorCodes.QuestionAlreadyAnnulled);
        (await VersionsAsync(admin, published.Id)).Should().HaveCount(2);

        await AssertRefusedAsync(await AnnulAsync(admin, Guid.CreateVersion7(), "Anulada."), HttpStatusCode.NotFound, QuestionBankErrorCodes.QuestionNotFound);
    }

    // AC3, BR3: a missing body, a blank reason and 501 characters change nothing; 500 is accepted.
    [Fact]
    public async Task Annul_WithAMissingBlankOrOverLongReason_IsRefused_AndNothingIsStored()
    {
        var (admin, _, question) = await PublishedAtVersionOneAsync();

        await AssertRefusedAsync(await AnnulAsync(admin, question.Id, null), HttpStatusCode.BadRequest, QuestionBankErrorCodes.QuestionStatusReasonRequired);
        await AssertRefusedAsync(await AnnulAsync(admin, question.Id, "   "), HttpStatusCode.BadRequest, QuestionBankErrorCodes.QuestionStatusReasonRequired);
        await AssertRefusedAsync(
            await AnnulAsync(admin, question.Id, new string('a', QuestionLimits.ChangeReasonMaxLength + 1)),
            HttpStatusCode.BadRequest,
            QuestionBankErrorCodes.QuestionStatusReasonTooLong);

        var read = await GetAsync(admin, question.Id);
        read.Status.Should().Be(QuestionStatus.Published);
        read.CurrentVersion.Should().Be(1);
        read.Annulment.Should().BeNull();
        (await VersionsAsync(admin, question.Id)).Should().ContainSingle();

        var accepted = await AnnulledAsync(admin, question.Id, new string('a', QuestionLimits.ChangeReasonMaxLength));
        accepted.Annulment!.Reason!.Length.Should().Be(QuestionLimits.ChangeReasonMaxLength);
    }

    // AC4, BR5: every write is a 409 question.annulled, even a save that would be a 404 for its topic; reads work.
    [Fact]
    public async Task AnAnnulledQuestion_RefusesSavePublishUnpublishAndDelete_AndStillReads()
    {
        var (admin, topic, question) = await PublishedAtVersionOneAsync();
        await AnnulledAsync(admin, question.Id);

        await AssertRefusedAsync(
            await UpdateAsync(admin, question.Id, Complete(topic.Id, statement: "<p>Outro enunciado.</p>")),
            HttpStatusCode.Conflict,
            QuestionBankErrorCodes.QuestionAnnulled);
        await AssertRefusedAsync(
            await UpdateAsync(admin, question.Id, Complete(Guid.CreateVersion7(), statement: "<p>Outro enunciado.</p>")),
            HttpStatusCode.Conflict,
            QuestionBankErrorCodes.QuestionAnnulled);
        await AssertRefusedAsync(await admin.PostAsync($"{Questions}/{question.Id}/publish", null), HttpStatusCode.Conflict, QuestionBankErrorCodes.QuestionAnnulled);
        await AssertRefusedAsync(await admin.PostAsync($"{Questions}/{question.Id}/unpublish", null), HttpStatusCode.Conflict, QuestionBankErrorCodes.QuestionAnnulled);
        await AssertRefusedAsync(await admin.DeleteAsync($"{Questions}/{question.Id}"), HttpStatusCode.Conflict, QuestionBankErrorCodes.QuestionAnnulled);

        var read = await GetAsync(admin, question.Id);
        read.Status.Should().Be(QuestionStatus.Annulled);
        read.StatementHtml.Should().Be(question.StatementHtml);
        (await VersionsAsync(admin, question.Id)).Should().HaveCount(2);
        (await ListAsync(admin, "?status=Annulled")).Items.Should().Contain(item => item.Id == question.Id);
    }

    // AC5, UC2, BR6: published again, the annulment gone from the response, editable, edits make versions.
    [Fact]
    public async Task Reinstate_AnAnnulledQuestion_IsPublishedAgain_AndCanBeEdited()
    {
        var (admin, topic, question) = await PublishedAtVersionOneAsync();
        await AnnulledAsync(admin, question.Id);

        var reinstated = await ReinstatedAsync(admin, question.Id, "  A banca reverteu.  ");

        reinstated.Status.Should().Be(QuestionStatus.Published);
        reinstated.CurrentVersion.Should().Be(3);
        reinstated.Annulment.Should().BeNull();
        var version = await VersionAsync(admin, question.Id, 3);
        version.Annulled.Should().BeFalse();
        version.Reason.Should().Be("A banca reverteu.");
        version.AnswerKeyChanged.Should().BeFalse();
        version.StatementHtml.Should().Be(question.StatementHtml);

        var edited = await UpdatedAsync(admin, question.Id, Complete(topic.Id, statement: "<p>Qual é a capital da Espanha?</p>"));

        edited.Status.Should().Be(QuestionStatus.Published);
        edited.CurrentVersion.Should().Be(4);
    }

    // AC6, BR6, BR3: a question that is not annulled, and the same reason rules.
    [Fact]
    public async Task Reinstate_AQuestionThatIsNotAnnulled_IsRefused_AndTheReasonRulesAreTheSame()
    {
        var (admin, _, published) = await PublishedAtVersionOneAsync();
        var draft = await CreatedAsync(admin, Complete((await TopicAsync(admin)).Id));
        await AssertRefusedAsync(await ReinstateAsync(admin, published.Id, "Revertida."), HttpStatusCode.Conflict, QuestionBankErrorCodes.QuestionNotAnnulled);
        await AssertRefusedAsync(await ReinstateAsync(admin, draft.Id, "Revertida."), HttpStatusCode.Conflict, QuestionBankErrorCodes.QuestionNotAnnulled);
        await AssertRefusedAsync(await ReinstateAsync(admin, Guid.CreateVersion7(), "Revertida."), HttpStatusCode.NotFound, QuestionBankErrorCodes.QuestionNotFound);

        var annulled = await PublishedAsync(admin, (await TopicAsync(admin)).Id);
        await AnnulledAsync(admin, annulled.Id);
        await AssertRefusedAsync(await ReinstateAsync(admin, annulled.Id, null), HttpStatusCode.BadRequest, QuestionBankErrorCodes.QuestionStatusReasonRequired);
        await AssertRefusedAsync(await ReinstateAsync(admin, annulled.Id, "   "), HttpStatusCode.BadRequest, QuestionBankErrorCodes.QuestionStatusReasonRequired);
        await AssertRefusedAsync(
            await ReinstateAsync(admin, annulled.Id, new string('a', QuestionLimits.ChangeReasonMaxLength + 1)),
            HttpStatusCode.BadRequest,
            QuestionBankErrorCodes.QuestionStatusReasonTooLong);

        (await GetAsync(admin, annulled.Id)).Status.Should().Be(QuestionStatus.Annulled);
        (await VersionsAsync(admin, annulled.Id)).Should().HaveCount(2);
    }

    // AC7, UC4, BR2, BR6: the annulment and the reinstatement are told apart in the history.
    [Fact]
    public async Task Versions_TellTheAnnulmentFromTheReinstatement()
    {
        var (admin, _, question) = await PublishedAtVersionOneAsync();
        await AnnulledAsync(admin, question.Id, "Anulada.");
        await ReinstatedAsync(admin, question.Id, "Revertida.");

        var versions = await VersionsAsync(admin, question.Id);

        versions.Select(version => (version.Number, version.Annulled, version.Reinstated))
            .Should().Equal((3, false, true), (2, true, false), (1, false, false));
        versions.Select(version => version.Reason).Should().Equal("Revertida.", "Anulada.", null);
        versions[0].IsCurrent.Should().BeTrue();
        (await VersionAsync(admin, question.Id, 2)).Annulled.Should().BeTrue();
        (await VersionAsync(admin, question.Id, 3)).Annulled.Should().BeFalse();
    }

    // AC9, BR11: the filter finds it, the published filter does not, an unknown value is ignored.
    [Fact]
    public async Task List_FilteredByStatus_FindsTheAnnulledQuestionOnlyUnderAnnulled()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var marker = Marker();
        var annulled = await CreatedAsync(admin, Complete(topic.Id, statement: $"<p>Anulada {marker}</p>"));
        (await admin.PostAsync($"{Questions}/{annulled.Id}/publish", null)).StatusCode.Should().Be(HttpStatusCode.OK);
        var live = await CreatedAsync(admin, Complete(topic.Id, statement: $"<p>Viva {marker}</p>"));
        (await admin.PostAsync($"{Questions}/{live.Id}/publish", null)).StatusCode.Should().Be(HttpStatusCode.OK);
        await AnnulledAsync(admin, annulled.Id);

        var onlyAnnulled = await ListAsync(admin, $"?search={marker}&status=Annulled");
        var onlyPublished = await ListAsync(admin, $"?search={marker}&status=Published");
        var ignored = await ListAsync(admin, $"?search={marker}&status=Bogus");

        onlyAnnulled.Items.Select(item => item.Id).Should().Equal(annulled.Id);
        onlyAnnulled.Items[0].Status.Should().Be(QuestionStatus.Annulled);
        onlyPublished.Items.Select(item => item.Id).Should().Equal(live.Id);
        ignored.Items.Select(item => item.Id).Should().BeEquivalentTo([annulled.Id, live.Id]);
    }

    // AC10, BR8: the annulled question still holds its paper number, its topic and its place in the edition.
    [Fact]
    public async Task AnAnnulledQuestion_KeepsItsPaperNumber_ItsTopicAndItsPlaceInTheEdition()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var edition = await EditionAsync(admin);
        var created = await CreatedAsync(admin, FromEdition(topic.Id, edition.Id, 12));
        (await admin.PostAsync($"{Questions}/{created.Id}/publish", null)).StatusCode.Should().Be(HttpStatusCode.OK);
        await AnnulledAsync(admin, created.Id);

        var taken = await CreateAsync(admin, FromEdition(topic.Id, edition.Id, 12, statement: "<p>Outra.</p>"));
        var topicDelete = await admin.DeleteAsync($"/api/v1/catalog/topics/{topic.Id}");
        var inEdition = await EditionQuestionsAsync(admin, edition.Id);

        await AssertRefusedAsync(taken, HttpStatusCode.Conflict, QuestionBankErrorCodes.QuestionPaperNumberTaken);
        await AssertRefusedAsync(topicDelete, HttpStatusCode.Conflict, CatalogErrorCodes.TopicInUse);
        inEdition.Items.Should().ContainSingle(item => item.Id == created.Id && item.Status == QuestionStatus.Annulled && item.PaperNumber == 12);
        (await GetAsync(admin, created.Id)).Origin!.PaperNumber.Should().Be(12);
    }

    // AC11, BR10: both routes need questions.manage.
    [Fact]
    public async Task BothRoutes_WithoutThePermission_AnswerForbidden_AndACuratorUsesThem()
    {
        var (admin, _, question) = await PublishedAtVersionOneAsync();
        var student = await StudentAsync();
        var curator = await CuratorAsync();

        var denied = new[]
        {
            await AnnulAsync(student, question.Id, "Anulada."),
            await ReinstateAsync(student, question.Id, "Revertida.")
        };

        denied.Select(answer => answer.StatusCode).Should().OnlyContain(status => status == HttpStatusCode.Forbidden);
        (await GetAsync(admin, question.Id)).Status.Should().Be(QuestionStatus.Published);
        (await AnnulledAsync(curator, question.Id)).Status.Should().Be(QuestionStatus.Annulled);
        (await ReinstatedAsync(curator, question.Id)).Status.Should().Be(QuestionStatus.Published);
    }

    // BR2: a true/false question keeps its key in the annulment version.
    [Fact]
    public async Task Annul_ATrueFalseQuestion_KeepsItsKeyInTheVersion()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var created = await CreatedAsync(
            admin,
            new SaveQuestionRequest(topic.Id, "pt-BR", "<p>Paris é a capital da França.</p>", Type: QuestionType.TrueFalse, TrueFalseKey: true));
        (await admin.PostAsync($"{Questions}/{created.Id}/publish", null)).StatusCode.Should().Be(HttpStatusCode.OK);

        var annulled = await AnnulledAsync(admin, created.Id);

        annulled.Status.Should().Be(QuestionStatus.Annulled);
        annulled.TrueFalseKey.Should().BeTrue();
        var version = await VersionAsync(admin, created.Id, 2);
        version.TrueFalseKey.Should().BeTrue();
        version.Options.Should().BeEmpty();
    }

    // The column is real: the annulled flag is stored on the version row.
    [Fact]
    public async Task TheAnnulledFlag_IsStoredOnTheVersionRow()
    {
        var (admin, _, question) = await PublishedAtVersionOneAsync();
        await AnnulledAsync(admin, question.Id);
        await ReinstatedAsync(admin, question.Id);

        var flags = await QueryAsync(context => context.Database
            .SqlQuery<string>($"SELECT string_agg(annulled::text, ',' ORDER BY number) AS \"Value\" FROM question_bank.question_versions WHERE question_id = {question.Id}")
            .SingleAsync());

        flags.Should().Be("false,true,false");
    }
}
