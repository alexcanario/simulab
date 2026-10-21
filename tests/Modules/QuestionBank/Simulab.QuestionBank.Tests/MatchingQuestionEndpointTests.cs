using System.Net;
using System.Net.Http.Json;
using Simulab.Catalog.Contracts;
using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Serialization;

namespace Simulab.QuestionBank.Tests;

/// <summary>
/// F-106 through HTTP, on a real PostgreSQL: authoring a matching question, the draft and publish rules over its two
/// columns, the refusals of BR2, BR5 and BR6, the key change that needs a reason (BR8), the rules the types share,
/// the base-text fan-out, the responses and the permission.
/// </summary>
public sealed class MatchingQuestionEndpointTests : OriginApiTests
{
    private const string BaseTexts = "/api/v1/question-bank/base-texts";

    private static SaveQuestionOptionRequest Right(string text) => new($"<p>{text}</p>");

    private static IReadOnlyList<SaveQuestionOptionRequest> Rights(int count) =>
        [.. Enumerable.Range(0, count).Select(index => Right($"Direita {index}"))];

    private static IReadOnlyList<SaveMatchItemRequest> Lefts(params int?[] matches) =>
        [.. matches.Select((match, index) => new SaveMatchItemRequest($"<p>Esquerda {index}</p>", match))];

    private static SaveQuestionRequest Matching(
        Guid topicId,
        IReadOnlyList<SaveQuestionOptionRequest>? rights = null,
        IReadOnlyList<SaveMatchItemRequest>? lefts = null,
        string statement = "<p>Associe cada país à sua capital.</p>") =>
        new(topicId, "pt-BR", statement, Type: QuestionType.Matching, Options: rights ?? [], MatchItems: lefts);

    private static Task<HttpResponseMessage> PublishAsync(HttpClient client, Guid id) =>
        client.PostAsync($"{Questions}/{id}/publish", null);

    private static async Task<IReadOnlyList<QuestionVersionListItemResponse>> VersionsAsync(HttpClient client, Guid id) =>
        (await client.GetFromJsonAsync<IReadOnlyList<QuestionVersionListItemResponse>>(
            $"{Questions}/{id}/versions", AppJson.Options))!;

    private static async Task<QuestionVersionResponse> VersionAsync(HttpClient client, Guid id, int number) =>
        (await client.GetFromJsonAsync<QuestionVersionResponse>($"{Questions}/{id}/versions/{number}", AppJson.Options))!;

    // 1→C, 2→A, 3→B.
    private static async Task<QuestionResponse> PublishedMatchingAsync(HttpClient admin, Guid topicId, Guid? baseTextId = null)
    {
        var question = await CreatedAsync(admin, Matching(topicId, Rights(3), Lefts(2, 0, 1)) with { BaseTextId = baseTextId });
        var response = await PublishAsync(admin, question.Id);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        return question;
    }

    private static async Task<string> RefusalAsync(HttpResponseMessage response, HttpStatusCode status = HttpStatusCode.BadRequest)
    {
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(status, body);

        return CodeOf(body)!;
    }

    // AC1: type chosen at creation, no items yet: a draft of that type, listed with its type.
    [Fact]
    public async Task Create_MatchingWithNoItems_IsStoredAsADraftOfThatType_AndListedWithIt()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);

        var question = await CreatedAsync(admin, Matching(topic.Id));

        question.Type.Should().Be(QuestionType.Matching);
        question.Status.Should().Be(QuestionStatus.Draft);
        question.Options.Should().BeEmpty();
        question.MatchItems.Should().NotBeNull().And.BeEmpty();
        (await ListAsync(admin, "?pageSize=100")).Items.Single(item => item.Id == question.Id)
            .Type.Should().Be(QuestionType.Matching);
    }

    // AC2, BR3: up to eight on each side, matches set or not, come back as sent; a ninth on either side does not.
    [Fact]
    public async Task Create_MatchingDraft_WithEightItemsEachSide_ReturnsTheItemsAndMatchesAsSent()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);

        var question = await CreatedAsync(admin, Matching(topic.Id, Rights(8), Lefts(7, null, 0, 1, null, 2, 3, 4)));

        question.Options.Should().HaveCount(8).And.OnlyContain(option => !option.IsCorrect);
        question.MatchItems!.OrderBy(item => item.Position).Select(item => item.MatchPosition)
            .Should().Equal(7, null, 0, 1, null, 2, 3, 4);
        question.MatchItems!.OrderBy(item => item.Position).Select(item => item.Position)
            .Should().Equal(0, 1, 2, 3, 4, 5, 6, 7);
        question.MatchItems!.Single(item => item.Position == 0).TextHtml.Should().Be("<p>Esquerda 0</p>");
        (await GetAsync(admin, question.Id)).MatchItems.Should().BeEquivalentTo(question.MatchItems);
    }

    [Fact]
    public async Task Create_MatchingWithANinthItemOnEitherSide_IsRefusedWithMatchingItemsCount()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);

        var ninthLeft = await CreateAsync(admin, Matching(topic.Id, Rights(2), Lefts([.. Enumerable.Repeat<int?>(null, 9)])));
        var ninthRight = await CreateAsync(admin, Matching(topic.Id, Rights(9), Lefts(0)));

        (await RefusalAsync(ninthLeft)).Should().Be(QuestionBankErrorCodes.QuestionMatchingItemsCount);
        (await RefusalAsync(ninthRight)).Should().Be(QuestionBankErrorCodes.QuestionMatchingItemsCount);
    }

    // AC3, BR3: broken data is refused even on a draft, and nothing is stored.
    [Fact]
    public async Task Create_MatchingDraft_WithAMissingOrARepeatedRightItem_IsRefused_AndNothingIsStored()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var marker = Marker();

        var missing = await CreateAsync(admin, Matching(topic.Id, Rights(3), Lefts(0, 5), $"<p>{marker}</p>"));
        var repeated = await CreateAsync(admin, Matching(topic.Id, Rights(3), Lefts(1, 1), $"<p>{marker}</p>"));

        (await RefusalAsync(missing)).Should().Be(QuestionBankErrorCodes.QuestionMatchingKeyInvalid);
        (await RefusalAsync(repeated)).Should().Be(QuestionBankErrorCodes.QuestionMatchingKeyRepeated);
        (await ListAsync(admin, $"?search={marker}")).Items.Should().BeEmpty();
    }

    // AC4, BR4: two to eight on the left, at least as many on the right, every text written.
    [Fact]
    public async Task Publish_Matching_WithOneLeftItemOrTooFewRightItems_IsRefusedWithMatchingItemsCount()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var oneLeft = await CreatedAsync(admin, Matching(topic.Id, Rights(2), Lefts(0)));
        var fewRight = await CreatedAsync(admin, Matching(topic.Id, Rights(2), Lefts(0, 1, null)));

        (await RefusalAsync(await PublishAsync(admin, oneLeft.Id))).Should().Be(QuestionBankErrorCodes.QuestionMatchingItemsCount);
        (await RefusalAsync(await PublishAsync(admin, fewRight.Id))).Should().Be(QuestionBankErrorCodes.QuestionMatchingItemsCount);
        (await GetAsync(admin, oneLeft.Id)).Status.Should().Be(QuestionStatus.Draft);
    }

    [Fact]
    public async Task Publish_Matching_WithAnEmptyRightOrLeftText_IsRefusedWithTheTextCode()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var emptyRight = await CreatedAsync(admin, Matching(topic.Id, [Right("Paris"), new(null)], Lefts(0, 1)));
        var emptyLeft = await CreatedAsync(
            admin, Matching(topic.Id, Rights(2), [new("<p>França</p>", 0), new(null, 1)]));

        (await RefusalAsync(await PublishAsync(admin, emptyRight.Id))).Should().Be(QuestionBankErrorCodes.QuestionOptionTextRequired);
        (await RefusalAsync(await PublishAsync(admin, emptyLeft.Id))).Should().Be(QuestionBankErrorCodes.QuestionMatchItemTextRequired);
    }

    // AC5, BR4: every left item matched; extra right items are allowed.
    [Fact]
    public async Task Publish_Matching_WithAnItemWithNoMatch_IsRefused_AndWithEveryItemMatchedAndExtras_IsPublished()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var unmatched = await CreatedAsync(admin, Matching(topic.Id, Rights(3), Lefts(0, null)));
        var complete = await CreatedAsync(admin, Matching(topic.Id, Rights(5), Lefts(4, 0, 2)));

        (await RefusalAsync(await PublishAsync(admin, unmatched.Id))).Should().Be(QuestionBankErrorCodes.QuestionMatchingKeyInvalid);
        var response = await PublishAsync(admin, complete.Id);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        (await response.Content.ReadFromJsonAsync<QuestionResponse>(AppJson.Options))!.Status.Should().Be(QuestionStatus.Published);
    }

    // AC6, BR4: a published question stays complete; a refusal leaves it as it was.
    [Fact]
    public async Task Update_PublishedMatching_BreakingTheKey_IsRefused_AndTheStoredQuestionIsUnchanged()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var question = await PublishedMatchingAsync(admin, topic.Id);

        var unmatched = await UpdateAsync(admin, question.Id, Matching(topic.Id, Rights(3), Lefts(2, 0, null), "<p>Outro.</p>"));
        var oneLeft = await UpdateAsync(admin, question.Id, Matching(topic.Id, Rights(3), Lefts(2), "<p>Outro.</p>"));

        (await RefusalAsync(unmatched)).Should().Be(QuestionBankErrorCodes.QuestionMatchingKeyInvalid);
        (await RefusalAsync(oneLeft)).Should().Be(QuestionBankErrorCodes.QuestionMatchingItemsCount);
        var stored = await GetAsync(admin, question.Id);
        stored.Status.Should().Be(QuestionStatus.Published);
        stored.StatementHtml.Should().Be(question.StatementHtml);
        stored.MatchItems!.OrderBy(item => item.Position).Select(item => item.MatchPosition).Should().Equal(2, 0, 1);
    }

    [Fact]
    public async Task Update_PublishedMatching_WithAnotherValidSetAndAReason_StaysPublished()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var question = await PublishedMatchingAsync(admin, topic.Id);

        var updated = await UpdatedAsync(
            admin, question.Id, Matching(topic.Id, Rights(3), Lefts(0, 2, 1)) with { ChangeReason = "Gabarito retificado." });

        updated.Status.Should().Be(QuestionStatus.Published);
        updated.MatchItems!.OrderBy(item => item.Position).Select(item => item.MatchPosition).Should().Equal(0, 2, 1);
    }

    // AC7, BR5, BR6: what the type does not have is refused, never ignored; nothing is stored.
    [Fact]
    public async Task Create_MatchingWithATrueFalseKeyOrAMarkedRightItem_IsRefusedWithKeyNotAllowed()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var marker = Marker();

        var withKey = await CreateAsync(
            admin, Matching(topic.Id, Rights(2), Lefts(0, 1), $"<p>{marker}</p>") with { TrueFalseKey = true });
        var marked = await CreateAsync(
            admin, Matching(topic.Id, [Right("A"), new("<p>B</p>", true)], Lefts(0, 1), $"<p>{marker}</p>"));

        (await RefusalAsync(withKey)).Should().Be(QuestionBankErrorCodes.QuestionKeyNotAllowed);
        (await RefusalAsync(marked)).Should().Be(QuestionBankErrorCodes.QuestionKeyNotAllowed);
        (await ListAsync(admin, $"?search={marker}")).Items.Should().BeEmpty();
    }

    [Theory]
    [InlineData(QuestionType.SingleChoice)]
    [InlineData(QuestionType.TrueFalse)]
    [InlineData(QuestionType.MultipleAnswer)]
    public async Task Create_AnotherTypeWithMatchItems_IsRefusedWithMatchItemsNotAllowed(QuestionType type)
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);

        var response = await CreateAsync(
            admin, new SaveQuestionRequest(topic.Id, "pt-BR", "<p>Enunciado</p>", Type: type, MatchItems: Lefts(null)));

        (await RefusalAsync(response)).Should().Be(QuestionBankErrorCodes.QuestionMatchItemsNotAllowed);
    }

    // AC8, BR2: the type never changes; naming its own type is accepted.
    [Fact]
    public async Task Update_NamingAnotherType_Is409TypeChanged_AndNamingItsOwnIsAccepted()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var question = await CreatedAsync(admin, Matching(topic.Id, Rights(2), Lefts(0, 1)));

        var other = await UpdateAsync(admin, question.Id, Complete(topic.Id, "<p>Mudou</p>") with { Type = QuestionType.SingleChoice });
        var own = await UpdatedAsync(admin, question.Id, Matching(topic.Id, Rights(2), Lefts(1, 0)));

        (await RefusalAsync(other, HttpStatusCode.Conflict)).Should().Be(QuestionBankErrorCodes.QuestionTypeChanged);
        own.Type.Should().Be(QuestionType.Matching);
    }

    // AC9, BR8: changing what a number matches needs a reason and records a key change with the items.
    [Fact]
    public async Task Update_PublishedMatching_ChangingTheMatches_NeedsAReason_AndThenRecordsAKeyChange()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var question = await PublishedMatchingAsync(admin, topic.Id);
        var swapped = Matching(topic.Id, Rights(3), Lefts(0, 2, 1));

        var refused = await UpdateAsync(admin, question.Id, swapped);

        (await RefusalAsync(refused)).Should().Be(QuestionBankErrorCodes.QuestionKeyChangeReasonRequired);
        (await VersionsAsync(admin, question.Id)).Should().ContainSingle();

        await UpdatedAsync(admin, question.Id, swapped with { ChangeReason = "Gabarito oficial retificado." });

        var version = await VersionAsync(admin, question.Id, 2);
        version.AnswerKeyChanged.Should().BeTrue();
        version.Reason.Should().Be("Gabarito oficial retificado.");
        version.MatchItems!.OrderBy(item => item.Position).Select(item => item.MatchPosition).Should().Equal(0, 2, 1);
        (await VersionAsync(admin, question.Id, 1)).MatchItems!.Select(item => item.MatchPosition).Should().Equal(2, 0, 1);
    }

    // AC10, BR8: a text edit is a version with no key change; adding a left item needs a reason.
    [Fact]
    public async Task Update_PublishedMatching_ChangingOnlyTexts_RecordsAVersionWithNoKeyChange()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var question = await PublishedMatchingAsync(admin, topic.Id);

        await UpdatedAsync(
            admin,
            question.Id,
            Matching(
                topic.Id,
                [Right("Outra"), Right("Direita 1"), Right("Direita 2")],
                [new("<p>Outro item</p>", 2), new("<p>Esquerda 1</p>", 0), new("<p>Esquerda 2</p>", 1)]));

        var versions = await VersionsAsync(admin, question.Id);
        versions.Should().HaveCount(2);
        versions[0].AnswerKeyChanged.Should().BeFalse();
    }

    [Fact]
    public async Task Update_PublishedMatching_AddingALeftItem_NeedsAReason()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var question = await PublishedMatchingAsync(admin, topic.Id);

        var response = await UpdateAsync(admin, question.Id, Matching(topic.Id, Rights(4), Lefts(2, 0, 1, 3)));

        (await RefusalAsync(response)).Should().Be(QuestionBankErrorCodes.QuestionKeyChangeReasonRequired);
    }

    // AC10, AC11 (architect #1): editing the linked base text records a version that is not a key change.
    [Fact]
    public async Task UpdateBaseText_LinkedToAPublishedMatching_RecordsAVersionWithNoKeyChange()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var created = await admin.PostAsJsonAsync(
            BaseTexts, new SaveBaseTextRequest($"Texto {Marker()}", "pt-BR", "<p>O rio corria.</p>"), AppJson.Options);
        created.StatusCode.Should().Be(HttpStatusCode.Created, await created.Content.ReadAsStringAsync());
        var text = (await created.Content.ReadFromJsonAsync<BaseTextResponse>(AppJson.Options))!;
        var question = await PublishedMatchingAsync(admin, topic.Id, text.Id);

        var updated = await admin.PutAsJsonAsync(
            $"{BaseTexts}/{text.Id}", new SaveBaseTextRequest(text.Title, "pt-BR", "<p>O rio corria depressa.</p>"), AppJson.Options);

        updated.StatusCode.Should().Be(HttpStatusCode.OK, await updated.Content.ReadAsStringAsync());
        var versions = await VersionsAsync(admin, question.Id);
        versions.Should().HaveCount(2);
        versions[0].AnswerKeyChanged.Should().BeFalse();
        (await VersionAsync(admin, question.Id, 2)).MatchItems!.Select(item => item.MatchPosition).Should().Equal(2, 0, 1);
    }

    // AC11, BR7, BR9: the rules the types share hold for this type.
    [Fact]
    public async Task Create_Matching_SanitizesTheLeftAndRightItems()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        const string hostile = "<p onclick=\"x()\">ok <sup>2</sup></p><script>alert(1)</script>";

        var question = await CreatedAsync(admin, Matching(topic.Id, [new(hostile), Right("B")], [new(hostile, 0)]));

        foreach (var field in new[] { question.Options[0].TextHtml, question.MatchItems![0].TextHtml })
        {
            field.Should().NotContain("script").And.NotContain("onclick");
            field.Should().Contain("<sup>2</sup>");
        }
    }

    [Fact]
    public async Task Create_Matching_WithALeftItemOverTheLimit_IsRefusedWithMatchItemTooLong()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var longText = $"<p>{new string('a', QuestionLimits.OptionMaxLength)}</p>";

        var response = await CreateAsync(admin, Matching(topic.Id, Rights(2), [new(longText, 0)]));

        (await RefusalAsync(response)).Should().Be(QuestionBankErrorCodes.QuestionMatchItemTooLong);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public async Task Create_Matching_WithADifficultyOutOfRange_IsRefused(int difficulty)
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);

        var response = await CreateAsync(admin, Matching(topic.Id) with { Difficulty = difficulty });

        (await RefusalAsync(response)).Should().Be(QuestionBankErrorCodes.QuestionDifficultyInvalid);
    }

    [Fact]
    public async Task Create_Matching_WithAPaperNumberAlreadyTaken_IsRefusedWithPaperNumberTaken()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var edition = await EditionAsync(admin);
        await CreatedAsync(admin, Matching(topic.Id) with { ExamEditionId = edition.Id, PaperNumber = 9 });

        var response = await CreateAsync(admin, Matching(topic.Id) with { ExamEditionId = edition.Id, PaperNumber = 9 });

        (await RefusalAsync(response, HttpStatusCode.Conflict)).Should().Be(QuestionBankErrorCodes.QuestionPaperNumberTaken);
    }

    [Fact]
    public async Task Delete_PublishedMatching_IsRefused_AndADraftIsDeleted()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var published = await PublishedMatchingAsync(admin, topic.Id);
        var draft = await CreatedAsync(admin, Matching(topic.Id, Rights(2), Lefts(0)));

        var refused = await admin.DeleteAsync($"{Questions}/{published.Id}");
        var deleted = await admin.DeleteAsync($"{Questions}/{draft.Id}");

        (await RefusalAsync(refused, HttpStatusCode.Conflict)).Should().Be(QuestionBankErrorCodes.QuestionDeleteRequiresDraft);
        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteTopic_WithOnlyAMatchingQuestion_IsRefusedWithTopicInUse()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        await CreatedAsync(admin, Matching(topic.Id, Rights(2), Lefts(0, 1)));

        var response = await admin.DeleteAsync($"/api/v1/catalog/topics/{topic.Id}");

        (await RefusalAsync(response, HttpStatusCode.Conflict)).Should().Be(CatalogErrorCodes.TopicInUse);
    }

    [Fact]
    public async Task AnnulAndReinstate_Matching_KeepTheMatchItemsInTheirVersions()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var question = await PublishedMatchingAsync(admin, topic.Id);

        var annul = await admin.PostAsJsonAsync($"{Questions}/{question.Id}/annul", new AnnulQuestionRequest("Anulada."), AppJson.Options);
        var reinstate = await admin.PostAsJsonAsync(
            $"{Questions}/{question.Id}/reinstate", new ReinstateQuestionRequest("Recurso indeferido."), AppJson.Options);

        annul.StatusCode.Should().Be(HttpStatusCode.OK, await annul.Content.ReadAsStringAsync());
        reinstate.StatusCode.Should().Be(HttpStatusCode.OK, await reinstate.Content.ReadAsStringAsync());
        (await VersionAsync(admin, question.Id, 2)).MatchItems!.Select(item => item.MatchPosition).Should().Equal(2, 0, 1);
        (await VersionAsync(admin, question.Id, 3)).MatchItems!.Select(item => item.MatchPosition).Should().Equal(2, 0, 1);
        (await GetAsync(admin, question.Id)).Status.Should().Be(QuestionStatus.Published);
    }

    [Fact]
    public async Task Submit_CompleteMatchingDraft_GoesToReview_AndAnIncompleteOneIsRefused()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var complete = await CreatedAsync(admin, Matching(topic.Id, Rights(3), Lefts(2, 0)));
        var incomplete = await CreatedAsync(admin, Matching(topic.Id, Rights(3), Lefts(2, null)));

        var sent = await admin.PostAsync($"{Questions}/{complete.Id}/submit", null);
        var refused = await admin.PostAsync($"{Questions}/{incomplete.Id}/submit", null);

        sent.StatusCode.Should().Be(HttpStatusCode.OK, await sent.Content.ReadAsStringAsync());
        (await GetAsync(admin, complete.Id)).Status.Should().Be(QuestionStatus.InReview);
        (await RefusalAsync(refused)).Should().Be(QuestionBankErrorCodes.QuestionMatchingKeyInvalid);
    }

    // AC12, BR11: every type is named in the list and the responses carry the left items, empty for the others.
    [Fact]
    public async Task List_QuestionsOfTheFourTypes_NameTheTypeOfEachRow_AndOtherTypesHaveNoMatchItems()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var marker = Marker();
        var single = await CreatedAsync(admin, Complete(topic.Id, $"<p>{marker} escolha</p>"));
        var trueFalse = await CreatedAsync(
            admin, new(topic.Id, "pt-BR", $"<p>{marker} certo</p>", Type: QuestionType.TrueFalse, TrueFalseKey: true));
        var multiple = await CreatedAsync(
            admin, new(topic.Id, "pt-BR", $"<p>{marker} várias</p>", Type: QuestionType.MultipleAnswer));
        var matching = await CreatedAsync(admin, Matching(topic.Id, Rights(2), Lefts(0, 1), $"<p>{marker} associação</p>"));

        var items = (await ListAsync(admin, $"?search={marker}")).Items;

        items.Should().HaveCount(4);
        items.Single(item => item.Id == single.Id).Type.Should().Be(QuestionType.SingleChoice);
        items.Single(item => item.Id == trueFalse.Id).Type.Should().Be(QuestionType.TrueFalse);
        items.Single(item => item.Id == multiple.Id).Type.Should().Be(QuestionType.MultipleAnswer);
        items.Single(item => item.Id == matching.Id).Type.Should().Be(QuestionType.Matching);
        foreach (var other in new[] { single, trueFalse, multiple })
        {
            (await GetAsync(admin, other.Id)).MatchItems.Should().NotBeNull().And.BeEmpty();
        }
    }

    [Fact]
    public async Task FindVersion_OfAnotherType_HasAnEmptyMatchItemsList()
    {
        var admin = await AdminAsync();
        var question = await PublishedAsync(admin, (await TopicAsync(admin)).Id);

        (await VersionAsync(admin, question.Id, 1)).MatchItems.Should().NotBeNull().And.BeEmpty();
    }

    // AC16, BR9: the same permission as every question endpoint.
    [Fact]
    public async Task CreateAndUpdate_OfAMatchingQuestion_WithoutThePermission_AnswerForbidden()
    {
        var student = await StudentAsync();
        var request = Matching(Guid.CreateVersion7(), Rights(2), Lefts(0, 1));

        var create = await student.PostAsJsonAsync(Questions, request, AppJson.Options);
        var update = await student.PutAsJsonAsync($"{Questions}/{Guid.CreateVersion7()}", request, AppJson.Options);

        create.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        update.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
