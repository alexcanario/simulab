using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Serialization;

namespace Simulab.QuestionBank.Tests;

/// <summary>
/// F-102 through HTTP, on a real PostgreSQL: creating and validating a base text (AC1), linking a question
/// (AC2), the versions an edit makes (AC3, AC4), the locked language (AC5), the delete guard (AC6), the list and
/// the linked questions (AC8, AC9) and the permission (AC10).
/// </summary>
public sealed class BaseTextEndpointTests : OriginApiTests
{
    private const string BaseTexts = "/api/v1/question-bank/base-texts";
    private const string Passage = "<p>O rio corria devagar entre as pedras.</p>";

    private static SaveBaseTextRequest Text(
        string? title = null,
        string language = "pt-BR",
        string html = Passage) =>
        new(title ?? $"Texto {Marker()}", language, html);

    private static Task<HttpResponseMessage> PostTextAsync(HttpClient client, SaveBaseTextRequest request) =>
        client.PostAsJsonAsync(BaseTexts, request, AppJson.Options);

    private static async Task<BaseTextResponse> CreatedTextAsync(HttpClient client, SaveBaseTextRequest? request = null)
    {
        var response = await PostTextAsync(client, request ?? Text());
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<BaseTextResponse>(AppJson.Options))!;
    }

    private static async Task<BaseTextResponse> UpdatedTextAsync(HttpClient client, Guid id, SaveBaseTextRequest request)
    {
        var response = await client.PutAsJsonAsync($"{BaseTexts}/{id}", request, AppJson.Options);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<BaseTextResponse>(AppJson.Options))!;
    }

    private static async Task<BaseTextResponse> GetTextAsync(HttpClient client, Guid id) =>
        (await client.GetFromJsonAsync<BaseTextResponse>($"{BaseTexts}/{id}", AppJson.Options))!;

    private static SaveQuestionRequest Linked(
        Guid topicId,
        Guid? baseTextId,
        string language = "pt-BR",
        string statement = "<p>Como corria o rio?</p>") =>
        Complete(topicId, statement) with { BaseTextId = baseTextId, ContentLanguage = language };

    private static async Task<QuestionResponse> PublishedLinkedAsync(HttpClient client, Guid topicId, Guid baseTextId)
    {
        var question = await CreatedAsync(client, Linked(topicId, baseTextId));
        var response = await client.PostAsync($"{Questions}/{question.Id}/publish", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        return (await response.Content.ReadFromJsonAsync<QuestionResponse>(AppJson.Options))!;
    }

    private static async Task<IReadOnlyList<QuestionVersionListItemResponse>> VersionsAsync(HttpClient client, Guid id) =>
        (await client.GetFromJsonAsync<List<QuestionVersionListItemResponse>>($"{Questions}/{id}/versions", AppJson.Options))!;

    private static async Task<QuestionVersionResponse> VersionAsync(HttpClient client, Guid id, int number) =>
        (await client.GetFromJsonAsync<QuestionVersionResponse>($"{Questions}/{id}/versions/{number}", AppJson.Options))!;

    private static async Task<BaseTextPageResponse> ListTextsAsync(HttpClient client, string query = "") =>
        (await client.GetFromJsonAsync<BaseTextPageResponse>($"{BaseTexts}{query}", AppJson.Options))!;

    // AC1, BR1, BR8: stored sanitized, title trimmed, language canonical, no question yet.
    [Fact]
    public async Task Create_AValidBaseText_Answers201WithTheSanitizedHtml_AndAFreshCount()
    {
        var admin = await AdminAsync();
        const string hostile =
            "<p onclick=\"x()\">ok <sup>2</sup></p><script>alert(1)</script>" +
            "<a href=\"javascript:alert(1)\">bad</a><a href=\"https://example.com\">good</a>" +
            "<table><tbody><tr><td>cell</td></tr></tbody></table>";

        var response = await PostTextAsync(admin, new SaveBaseTextRequest($"  Texto {Marker()}  ", "pt-br", hostile));

        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        var created = (await response.Content.ReadFromJsonAsync<BaseTextResponse>(AppJson.Options))!;
        response.Headers.Location!.ToString().Should().EndWith($"{BaseTexts}/{created.Id}");
        created.Title.Should().NotStartWith(" ").And.NotEndWith(" ");
        created.ContentLanguage.Should().Be("pt-BR");
        created.QuestionCount.Should().Be(0);
        created.TextHtml.Should().NotContain("script").And.NotContain("onclick").And.NotContain("javascript:");
        created.TextHtml.Should().Contain("<sup>2</sup>").And.Contain("https://example.com").And.Contain("<td>cell</td>");
        (await GetTextAsync(admin, created.Id)).TextHtml.Should().Be(created.TextHtml);
    }

    // AC1, BR1: every refusal has its code, answers 400 and stores nothing.
    [Fact]
    public async Task Create_ABadBaseText_IsRefusedWithTheFieldsOwnCode_AndStoresNothing()
    {
        var admin = await AdminAsync();
        var marker = Marker();
        var longTitle = new string('a', QuestionLimits.BaseTextTitleMaxLength + 1);
        var longText = $"<p>{new string('a', QuestionLimits.BaseTextMaxLength)}</p>";

        (string Code, SaveBaseTextRequest Request)[] cases =
        [
            (QuestionBankErrorCodes.BaseTextTitleRequired, new SaveBaseTextRequest("   ", "pt-BR", Passage)),
            (QuestionBankErrorCodes.BaseTextTitleTooLong, new SaveBaseTextRequest(longTitle, "pt-BR", Passage)),
            (QuestionBankErrorCodes.BaseTextTextRequired, new SaveBaseTextRequest($"T {marker}", "pt-BR", "<p><br></p>")),
            (QuestionBankErrorCodes.BaseTextTextTooLong, new SaveBaseTextRequest($"T {marker}", "pt-BR", longText)),
            (QuestionBankErrorCodes.BaseTextContentLanguageInvalid, new SaveBaseTextRequest($"T {marker}", "xx", Passage))
        ];

        foreach (var (code, request) in cases)
        {
            var response = await PostTextAsync(admin, request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest, code);
            CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(code);
        }

        (await ListTextsAsync(admin, $"?search={marker}")).Total.Should().Be(0);
    }

    // AC2, BR2, BR7: the question carries the linked text; null removes the link.
    [Fact]
    public async Task Save_AQuestionWithABaseTextId_ReturnsTheLinkedText_AndNullRemovesIt()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var text = await CreatedTextAsync(admin, Text("Texto do rio"));

        var created = await CreatedAsync(admin, Linked(topic.Id, text.Id));

        created.BaseText.Should().NotBeNull();
        created.BaseText!.Id.Should().Be(text.Id);
        created.BaseText.Title.Should().Be("Texto do rio");
        created.BaseText.TextHtml.Should().Be(text.TextHtml);
        (await GetTextAsync(admin, text.Id)).QuestionCount.Should().Be(1);

        var unlinked = await UpdatedAsync(admin, created.Id, Linked(topic.Id, null));

        unlinked.BaseText.Should().BeNull();
        (await GetAsync(admin, created.Id)).BaseText.Should().BeNull();
        (await GetTextAsync(admin, text.Id)).QuestionCount.Should().Be(0);
    }

    [Fact]
    public async Task Save_ABaseTextOfAnotherLanguage_Answers409LanguageMismatch_AndStoresNothing()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var english = await CreatedTextAsync(admin, Text(language: "en", html: "<p>The river.</p>"));

        var response = await CreateAsync(admin, Linked(topic.Id, english.Id, language: "pt-BR"));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(QuestionBankErrorCodes.QuestionBaseTextLanguageMismatch);
        (await GetTextAsync(admin, english.Id)).QuestionCount.Should().Be(0);
    }

    // BR3: changing a linked question's language away from its text's is the same refusal.
    [Fact]
    public async Task Save_ChangingTheLanguageOfALinkedQuestion_Answers409LanguageMismatch_AndTheLinkStays()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var text = await CreatedTextAsync(admin);
        var question = await CreatedAsync(admin, Linked(topic.Id, text.Id));

        var response = await UpdateAsync(admin, question.Id, Linked(topic.Id, text.Id, language: "en"));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(QuestionBankErrorCodes.QuestionBaseTextLanguageMismatch);
        var stored = await GetAsync(admin, question.Id);
        stored.ContentLanguage.Should().Be("pt-BR");
        stored.BaseText!.Id.Should().Be(text.Id);
    }

    // AC2, BR7: unknown, deleted; and an invalid language still answers 400 first.
    [Fact]
    public async Task Save_AnUnknownOrDeletedBaseText_Answers404_AndAnInvalidLanguageStillAnswers400()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var deleted = await CreatedTextAsync(admin);
        (await admin.DeleteAsync($"{BaseTexts}/{deleted.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        foreach (var id in new[] { Guid.CreateVersion7(), deleted.Id })
        {
            var response = await CreateAsync(admin, Linked(topic.Id, id));

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(QuestionBankErrorCodes.QuestionBaseTextNotFound);
        }

        var text = await CreatedTextAsync(admin);
        var invalid = await CreateAsync(admin, Linked(topic.Id, text.Id, language: "xx"));

        invalid.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await invalid.Content.ReadAsStringAsync()).Should().Be(QuestionBankErrorCodes.QuestionContentLanguageInvalid);
    }

    // AC3, BR4, BR5: published questions get version N+1; drafts and annulled ones none; old versions untouched.
    [Fact]
    public async Task Edit_TheText_RecordsANewVersionOnEachPublishedLinkedQuestion_AndOnNoDraftNorAnnulled()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var text = await CreatedTextAsync(admin);
        var published = await PublishedLinkedAsync(admin, topic.Id, text.Id);
        var draft = await CreatedAsync(admin, Linked(topic.Id, text.Id, statement: "<p>Rascunho?</p>"));
        var toAnnul = await PublishedLinkedAsync(admin, topic.Id, text.Id);
        (await admin.PostAsJsonAsync(
            $"{Questions}/{toAnnul.Id}/annul", new AnnulQuestionRequest("A banca anulou."), AppJson.Options))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        var annulledVersionsBefore = (await VersionsAsync(admin, toAnnul.Id)).Count;

        const string edited = "<p>O rio corria depressa entre as pedras.</p>";
        var saved = await UpdatedTextAsync(admin, text.Id, Text(text.Title, html: edited));

        saved.TextHtml.Should().Be(edited);
        saved.QuestionCount.Should().Be(3);
        var versions = await VersionsAsync(admin, published.Id);
        versions.Select(version => version.Number).Should().Equal(2, 1);
        versions[0].AnswerKeyChanged.Should().BeFalse();
        versions[0].Reason.Should().BeNull();
        (await VersionAsync(admin, published.Id, 2)).BaseTextHtml.Should().Be(edited);
        (await VersionAsync(admin, published.Id, 1)).BaseTextHtml.Should().Be(Passage);
        (await GetAsync(admin, published.Id)).CurrentVersion.Should().Be(2);
        (await VersionsAsync(admin, draft.Id)).Should().BeEmpty();
        (await GetAsync(admin, draft.Id)).BaseText!.TextHtml.Should().Be(edited, "a draft follows the live text");
        (await VersionsAsync(admin, toAnnul.Id)).Should().HaveCount(annulledVersionsBefore);
    }

    // AC3, BR4: the title is not versioned content.
    [Fact]
    public async Task Edit_OnlyTheTitle_RecordsNoVersion()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var text = await CreatedTextAsync(admin);
        var published = await PublishedLinkedAsync(admin, topic.Id, text.Id);

        await UpdatedTextAsync(admin, text.Id, Text($"Outro título {Marker()}", html: text.TextHtml));

        (await VersionsAsync(admin, published.Id)).Should().ContainSingle();
    }

    // AC4, BR4: two questions, one save; the rows say so.
    [Fact]
    public async Task Edit_TheText_OfTwoPublishedQuestions_RecordsBothVersionsInOneSave()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var text = await CreatedTextAsync(admin);
        var first = await PublishedLinkedAsync(admin, topic.Id, text.Id);
        var second = await PublishedLinkedAsync(admin, topic.Id, text.Id);

        await UpdatedTextAsync(admin, text.Id, Text(text.Title, html: "<p>Novo texto.</p>"));

        var created = await QueryAsync(context => context.Database
            .SqlQuery<DateTimeOffset>(
                $"SELECT created_at AS \"Value\" FROM question_bank.question_versions WHERE question_id IN ({first.Id}, {second.Id}) AND number = 2")
            .ToListAsync());
        created.Should().HaveCount(2);
        (await VersionsAsync(admin, first.Id)).Select(version => version.Number).Should().Equal(2, 1);
        (await VersionsAsync(admin, second.Id)).Select(version => version.Number).Should().Equal(2, 1);
    }

    // AC5, BR3: locked while questions use it, free otherwise.
    [Fact]
    public async Task Edit_TheLanguage_Answers409WhileAQuestionLinksIt_AndChangesWhenNoneDoes()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var text = await CreatedTextAsync(admin);
        var question = await CreatedAsync(admin, Linked(topic.Id, text.Id));

        var refused = await admin.PutAsJsonAsync($"{BaseTexts}/{text.Id}", Text(text.Title, "en"), AppJson.Options);

        refused.StatusCode.Should().Be(HttpStatusCode.Conflict);
        CodeOf(await refused.Content.ReadAsStringAsync()).Should().Be(QuestionBankErrorCodes.BaseTextLanguageInUse);
        (await GetTextAsync(admin, text.Id)).ContentLanguage.Should().Be("pt-BR");

        await UpdatedAsync(admin, question.Id, Linked(topic.Id, null));
        (await UpdatedTextAsync(admin, text.Id, Text(text.Title, "en"))).ContentLanguage.Should().Be("en");
    }

    // AC6, BR6: a live question of any status blocks the delete; with none, it is soft-deleted.
    [Fact]
    public async Task Delete_WhileAQuestionLinksIt_Answers409InUse_AndWithNoneItDisappears()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var text = await CreatedTextAsync(admin);
        var draft = await CreatedAsync(admin, Linked(topic.Id, text.Id));

        var refused = await admin.DeleteAsync($"{BaseTexts}/{text.Id}");

        refused.StatusCode.Should().Be(HttpStatusCode.Conflict);
        CodeOf(await refused.Content.ReadAsStringAsync()).Should().Be(QuestionBankErrorCodes.BaseTextInUse);
        (await admin.GetAsync($"{BaseTexts}/{text.Id}")).StatusCode.Should().Be(HttpStatusCode.OK);

        await UpdatedAsync(admin, draft.Id, Linked(topic.Id, null));
        (await admin.DeleteAsync($"{BaseTexts}/{text.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await admin.GetAsync($"{BaseTexts}/{text.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await ListTextsAsync(admin, $"?search={Uri.EscapeDataString(text.Title)}")).Items.Should().BeEmpty();
        var again = await admin.DeleteAsync($"{BaseTexts}/{text.Id}");
        CodeOf(await again.Content.ReadAsStringAsync()).Should().Be(QuestionBankErrorCodes.BaseTextNotFound);
    }

    // AC8, BR11: sorted by title, search ignores case and accents, language filter, unknown language ignored.
    [Fact]
    public async Task List_SortsByTitle_SearchesWithoutCaseOrAccents_AndFiltersByLanguage()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var marker = Marker();
        var zeta = await CreatedTextAsync(admin, Text($"Zeta {marker}"));
        var alfa = await CreatedTextAsync(admin, Text($"Álfa {marker}"));
        var english = await CreatedTextAsync(admin, Text($"Mid {marker}", "en", "<p>The river.</p>"));
        await CreatedAsync(admin, Linked(topic.Id, alfa.Id));
        await CreatedAsync(admin, Linked(topic.Id, alfa.Id, statement: "<p>Outra?</p>"));

        var all = await ListTextsAsync(admin, $"?search={marker}");
        var portuguese = await ListTextsAsync(admin, $"?search={Uri.EscapeDataString($"ALFA {marker}")}&language=pt-BR");
        var englishOnly = await ListTextsAsync(admin, $"?search={marker}&language=en");
        var unknownLanguage = await ListTextsAsync(admin, $"?search={marker}&language=zz");

        all.Total.Should().Be(3);
        all.Items.Select(item => item.Id).Should().Equal(alfa.Id, english.Id, zeta.Id);
        all.Items[0].QuestionCount.Should().Be(2);
        all.Items[1].QuestionCount.Should().Be(0);
        portuguese.Items.Select(item => item.Id).Should().Equal(alfa.Id);
        englishOnly.Items.Select(item => item.Id).Should().Equal(english.Id);
        unknownLanguage.Total.Should().Be(3);
    }

    // AC9, BR11: the linked questions, with their excerpt and status, most recently changed first.
    [Fact]
    public async Task Questions_OfABaseText_ListsTheLiveLinkedQuestions_AndAnUnknownTextAnswers404()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var text = await CreatedTextAsync(admin);
        var older = await CreatedAsync(admin, Linked(topic.Id, text.Id, statement: "<p>Primeira?</p>"));
        var newer = await PublishedLinkedAsync(admin, topic.Id, text.Id);
        await CreatedAsync(admin, Linked(topic.Id, null, statement: "<p>Sem texto?</p>"));

        var page = (await admin.GetFromJsonAsync<QuestionPageResponse>($"{BaseTexts}/{text.Id}/questions", AppJson.Options))!;

        page.Total.Should().Be(2);
        page.Items.Select(item => item.Id).Should().Equal(newer.Id, older.Id);
        page.Items[0].Status.Should().Be(QuestionStatus.Published);
        page.Items[1].StatementExcerpt.Should().Be("Primeira?");
        var missing = await admin.GetAsync($"{BaseTexts}/{Guid.CreateVersion7()}/questions");
        missing.StatusCode.Should().Be(HttpStatusCode.NotFound);
        CodeOf(await missing.Content.ReadAsStringAsync()).Should().Be(QuestionBankErrorCodes.BaseTextNotFound);
    }

    // AC10, BR12: no permission, no endpoint; a curator holds it.
    [Fact]
    public async Task EveryBaseTextEndpoint_WithoutThePermission_AnswersForbidden_AndACuratorHasIt()
    {
        var student = await StudentAsync();
        var id = Guid.CreateVersion7();

        var answers = new[]
        {
            await student.GetAsync(BaseTexts),
            await student.GetAsync($"{BaseTexts}/{id}"),
            await student.PostAsJsonAsync(BaseTexts, new SaveBaseTextRequest(), AppJson.Options),
            await student.PutAsJsonAsync($"{BaseTexts}/{id}", new SaveBaseTextRequest(), AppJson.Options),
            await student.DeleteAsync($"{BaseTexts}/{id}"),
            await student.GetAsync($"{BaseTexts}/{id}/questions")
        };

        answers.Select(answer => answer.StatusCode).Should().OnlyContain(status => status == HttpStatusCode.Forbidden);
        var curator = await CuratorAsync();
        (await curator.GetAsync(BaseTexts)).StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
