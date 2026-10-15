using System.Net;
using System.Net.Http.Json;
using Simulab.Catalog.Contracts;
using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Serialization;

namespace Simulab.QuestionBank.Tests;

/// <summary>
/// F-104 through HTTP, on a real PostgreSQL: authoring a multiple-answer question, the publish rules over its
/// marks, the refusals of BR2 and BR5, the key change that needs a reason (BR7), the rules the types share and
/// the permission.
/// </summary>
public sealed class MultipleAnswerQuestionEndpointTests : OriginApiTests
{
    private static SaveQuestionOptionRequest Option(string text, bool correct) => new($"<p>{text}</p>", correct);

    private static IReadOnlyList<SaveQuestionOptionRequest> Marked(params bool[] correct) =>
        [.. correct.Select((isCorrect, index) => Option($"Opção {index}", isCorrect))];

    private static SaveQuestionRequest Multiple(
        Guid topicId,
        IReadOnlyList<SaveQuestionOptionRequest>? options = null,
        string statement = "<p>Quais são capitais europeias?</p>") =>
        new(topicId, "pt-BR", statement, Type: QuestionType.MultipleAnswer, Options: options ?? []);

    private static Task<HttpResponseMessage> PublishAsync(HttpClient client, Guid id) =>
        client.PostAsync($"{Questions}/{id}/publish", null);

    private static async Task<IReadOnlyList<QuestionVersionListItemResponse>> VersionsAsync(HttpClient client, Guid id) =>
        (await client.GetFromJsonAsync<IReadOnlyList<QuestionVersionListItemResponse>>(
            $"{Questions}/{id}/versions", AppJson.Options))!;

    private static async Task<QuestionResponse> PublishedAsync(HttpClient admin, Guid topicId, params bool[] marks)
    {
        var question = await CreatedAsync(admin, Multiple(topicId, Marked(marks)));
        (await PublishAsync(admin, question.Id)).StatusCode.Should().Be(HttpStatusCode.OK);

        return question;
    }

    // AC1: type chosen at creation, no options yet: a draft of that type, listed with its type.
    [Fact]
    public async Task Create_MultipleAnswerWithNoOptions_IsStoredAsADraftOfThatType_AndListedWithIt()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);

        var question = await CreatedAsync(admin, Multiple(topic.Id));

        question.Type.Should().Be(QuestionType.MultipleAnswer);
        question.Status.Should().Be(QuestionStatus.Draft);
        question.Options.Should().BeEmpty();
        question.TrueFalseKey.Should().BeNull();
        (await GetAsync(admin, question.Id)).Type.Should().Be(QuestionType.MultipleAnswer);
        (await ListAsync(admin, "?pageSize=100")).Items.Single(item => item.Id == question.Id)
            .Type.Should().Be(QuestionType.MultipleAnswer);
    }

    // AC2, BR3: any marking is a valid draft; a sixth option is not.
    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, true)]
    public async Task Create_MultipleAnswerDraft_WithAnyMarking_IsAccepted_AndKeepsTheMarks(bool a, bool b, bool c)
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);

        var question = await CreatedAsync(admin, Multiple(topic.Id, Marked(a, b, c)));

        question.Options.OrderBy(option => option.Position).Select(option => option.IsCorrect)
            .Should().Equal(a, b, c);
    }

    [Fact]
    public async Task Create_MultipleAnswerWithSixOptions_IsRefusedWithOptionsCount()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);

        var response = await CreateAsync(admin, Multiple(topic.Id, Marked(true, true, false, false, false, false)));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(QuestionBankErrorCodes.QuestionOptionsCount);
    }

    // AC3, BR4: three to five options with text to publish.
    [Theory]
    [InlineData(2)]
    [InlineData(1)]
    public async Task Publish_MultipleAnswerWithFewerThanThreeOptions_IsRefusedWithMultipleOptionsCount(int count)
    {
        var admin = await AdminAsync();
        var question = await CreatedAsync(
            admin, Multiple((await TopicAsync(admin)).Id, Marked([.. Enumerable.Repeat(true, count)])));

        var response = await PublishAsync(admin, question.Id);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await response.Content.ReadAsStringAsync())
            .Should().Be(QuestionBankErrorCodes.QuestionMultipleOptionsCount);
        (await GetAsync(admin, question.Id)).Status.Should().Be(QuestionStatus.Draft);
    }

    [Fact]
    public async Task Publish_MultipleAnswerWithAnEmptyOptionText_IsRefusedWithOptionTextRequired()
    {
        var admin = await AdminAsync();
        var question = await CreatedAsync(
            admin,
            Multiple((await TopicAsync(admin)).Id, [Option("Paris", true), Option("Roma", true), new(null, false)]));

        var response = await PublishAsync(admin, question.Id);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(QuestionBankErrorCodes.QuestionOptionTextRequired);
    }

    // AC4, BR4: two or more correct and at least one wrong.
    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, true)]
    public async Task Publish_MultipleAnswerWithAnInvalidMarking_IsRefusedWithMultipleKeyInvalid(bool a, bool b, bool c)
    {
        var admin = await AdminAsync();
        var question = await CreatedAsync(admin, Multiple((await TopicAsync(admin)).Id, Marked(a, b, c)));

        var response = await PublishAsync(admin, question.Id);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(QuestionBankErrorCodes.QuestionMultipleKeyInvalid);
    }

    [Fact]
    public async Task Publish_MultipleAnswerWithTwoCorrectAndOneWrong_BecomesPublished()
    {
        var admin = await AdminAsync();
        var question = await CreatedAsync(admin, Multiple((await TopicAsync(admin)).Id, Marked(true, true, false)));

        var response = await PublishAsync(admin, question.Id);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var published = (await response.Content.ReadFromJsonAsync<QuestionResponse>(AppJson.Options))!;
        published.Status.Should().Be(QuestionStatus.Published);
        published.Options.Count(option => option.IsCorrect).Should().Be(2);
    }

    // AC5, BR4: a published question stays complete; a refusal leaves it as it was.
    [Fact]
    public async Task Update_PublishedMultipleAnswer_LeavingOneCorrect_IsRefused_AndTheStoredQuestionIsUnchanged()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var question = await PublishedAsync(admin, topic.Id, true, true, false);

        var response = await UpdateAsync(
            admin, question.Id, Multiple(topic.Id, Marked(true, false, false), "<p>Outro enunciado.</p>"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(QuestionBankErrorCodes.QuestionMultipleKeyInvalid);
        var stored = await GetAsync(admin, question.Id);
        stored.Status.Should().Be(QuestionStatus.Published);
        stored.StatementHtml.Should().Be(question.StatementHtml);
        stored.Options.Count(option => option.IsCorrect).Should().Be(2);
    }

    [Fact]
    public async Task Update_PublishedMultipleAnswer_MarkingAnotherValidSet_WithAReason_StaysPublished()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var question = await PublishedAsync(admin, topic.Id, true, true, false);

        var updated = await UpdatedAsync(
            admin,
            question.Id,
            Multiple(topic.Id, Marked(true, false, true)) with { ChangeReason = "Gabarito retificado." });

        updated.Status.Should().Be(QuestionStatus.Published);
        updated.Options.OrderBy(option => option.Position).Select(option => option.IsCorrect)
            .Should().Equal(true, false, true);
    }

    // AC6, BR5: a true/false key is refused, never ignored; nothing is stored.
    [Fact]
    public async Task Create_MultipleAnswerWithATrueFalseKey_IsRefusedWithKeyNotAllowed_AndNothingIsStored()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var marker = Marker();
        var request = Multiple(topic.Id, Marked(true, true, false), $"<p>{marker}</p>") with { TrueFalseKey = true };

        var response = await CreateAsync(admin, request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(QuestionBankErrorCodes.QuestionKeyNotAllowed);
        (await ListAsync(admin, $"?search={marker}")).Items.Should().BeEmpty();
    }

    // AC7, BR2: the type never changes; naming the stored type or none is accepted.
    [Fact]
    public async Task Update_NamingAnotherType_Is409TypeChanged_AndTheStoredQuestionIsUnchanged()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var multiple = await CreatedAsync(admin, Multiple(topic.Id, Marked(true, true, false)));
        var single = await CreatedAsync(admin, Complete(topic.Id));

        var toSingle = await UpdateAsync(
            admin, multiple.Id, Complete(topic.Id, "<p>Mudou</p>") with { Type = QuestionType.SingleChoice });
        var toMultiple = await UpdateAsync(admin, single.Id, Multiple(topic.Id, Marked(true, true, false), "<p>Mudou</p>"));

        foreach (var response in new[] { toSingle, toMultiple })
        {
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
            CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(QuestionBankErrorCodes.QuestionTypeChanged);
        }

        (await GetAsync(admin, multiple.Id)).Type.Should().Be(QuestionType.MultipleAnswer);
        (await GetAsync(admin, single.Id)).Type.Should().Be(QuestionType.SingleChoice);
    }

    [Fact]
    public async Task Update_NamingItsOwnTypeOrNone_IsAccepted()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var question = await CreatedAsync(admin, Multiple(topic.Id, Marked(true, true, false)));

        var named = await UpdatedAsync(admin, question.Id, Multiple(topic.Id, Marked(true, false, true)));
        var omitted = await UpdatedAsync(
            admin, question.Id, Multiple(topic.Id, Marked(false, true, true)) with { Type = null });

        named.Type.Should().Be(QuestionType.MultipleAnswer);
        omitted.Type.Should().Be(QuestionType.MultipleAnswer);
    }

    // AC8, BR7: moving a correct option that is not the first one is a key change and needs its reason.
    [Fact]
    public async Task Update_PublishedMultipleAnswer_MovingASecondCorrectOption_NeedsAReason_AndThenRecordsAKeyChange()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var question = await PublishedAsync(admin, topic.Id, true, true, false);
        var moved = Multiple(topic.Id, Marked(true, false, true));

        var refused = await UpdateAsync(admin, question.Id, moved);

        refused.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await refused.Content.ReadAsStringAsync())
            .Should().Be(QuestionBankErrorCodes.QuestionKeyChangeReasonRequired);
        (await VersionsAsync(admin, question.Id)).Should().ContainSingle();

        await UpdatedAsync(admin, question.Id, moved with { ChangeReason = "Gabarito oficial retificado." });

        var versions = await VersionsAsync(admin, question.Id);
        versions.Should().HaveCount(2);
        versions[0].Number.Should().Be(2);
        versions[0].AnswerKeyChanged.Should().BeTrue();
        versions[0].Reason.Should().Be("Gabarito oficial retificado.");
        versions[1].AnswerKeyChanged.Should().BeFalse();
    }

    // AC9, BR7: a text edit with the same correct set is a version, never a key change.
    [Fact]
    public async Task Update_PublishedMultipleAnswer_ChangingOnlyAnOptionText_RecordsAVersionWithNoKeyChange()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var question = await PublishedAsync(admin, topic.Id, true, true, false);

        await UpdatedAsync(
            admin,
            question.Id,
            Multiple(topic.Id, [Option("Opção 0", true), Option("Opção 1", true), Option("Outro texto", false)]));

        var versions = await VersionsAsync(admin, question.Id);
        versions.Should().HaveCount(2);
        versions[0].AnswerKeyChanged.Should().BeFalse();
    }

    // AC10, BR8: the rules the types share are the same rules.
    [Fact]
    public async Task Create_MultipleAnswer_SanitizesTheHtmlOfTheTextFields()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        const string hostile = "<p onclick=\"x()\">ok <sup>2</sup></p><script>alert(1)</script>";

        var question = await CreatedAsync(
            admin,
            Multiple(topic.Id, Marked(true, true, false), hostile) with { ExplanationHtml = hostile });

        foreach (var field in new[] { question.StatementHtml, question.ExplanationHtml! })
        {
            field.Should().NotContain("script").And.NotContain("onclick");
            field.Should().Contain("<sup>2</sup>");
        }
    }

    [Fact]
    public async Task Create_MultipleAnswer_WithAPaperNumberAlreadyTaken_IsRefusedWithPaperNumberTaken()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var edition = await EditionAsync(admin);
        await CreatedAsync(
            admin, Multiple(topic.Id, Marked(true, true, false)) with { ExamEditionId = edition.Id, PaperNumber = 9 });

        var response = await CreateAsync(
            admin, Multiple(topic.Id, Marked(true, true, false)) with { ExamEditionId = edition.Id, PaperNumber = 9 });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(QuestionBankErrorCodes.QuestionPaperNumberTaken);
    }

    [Fact]
    public async Task Delete_PublishedMultipleAnswer_IsRefused_AndADraftIsDeleted()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var published = await PublishedAsync(admin, topic.Id, true, true, false);
        var draft = await CreatedAsync(admin, Multiple(topic.Id));

        var refused = await admin.DeleteAsync($"{Questions}/{published.Id}");
        var deleted = await admin.DeleteAsync($"{Questions}/{draft.Id}");

        refused.StatusCode.Should().Be(HttpStatusCode.Conflict);
        CodeOf(await refused.Content.ReadAsStringAsync()).Should().Be(QuestionBankErrorCodes.QuestionDeleteRequiresDraft);
        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteTopic_WithOnlyAMultipleAnswerQuestion_IsRefusedWithTopicInUse()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        await CreatedAsync(admin, Multiple(topic.Id, Marked(true, true, false)));

        var response = await admin.DeleteAsync($"/api/v1/catalog/topics/{topic.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.TopicInUse);
    }

    // AC11: the list names each row's type.
    [Fact]
    public async Task List_QuestionsOfTheThreeTypes_NameTheTypeOfEachRow()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var marker = Marker();
        var single = await CreatedAsync(admin, Complete(topic.Id, $"<p>{marker} escolha</p>"));
        var trueFalse = await CreatedAsync(
            admin, new(topic.Id, "pt-BR", $"<p>{marker} certo</p>", Type: QuestionType.TrueFalse, TrueFalseKey: true));
        var multiple = await CreatedAsync(
            admin, Multiple(topic.Id, Marked(true, true, false), $"<p>{marker} várias</p>"));

        var items = (await ListAsync(admin, $"?search={marker}")).Items;

        items.Should().HaveCount(3);
        items.Single(item => item.Id == single.Id).Type.Should().Be(QuestionType.SingleChoice);
        items.Single(item => item.Id == trueFalse.Id).Type.Should().Be(QuestionType.TrueFalse);
        items.Single(item => item.Id == multiple.Id).Type.Should().Be(QuestionType.MultipleAnswer);
    }

    // AC15, BR8: the same permission as every question endpoint.
    [Fact]
    public async Task CreateAndUpdate_OfAMultipleAnswerQuestion_WithoutThePermission_AnswerForbidden()
    {
        var student = await StudentAsync();
        var request = Multiple(Guid.CreateVersion7(), Marked(true, true, false));

        var create = await student.PostAsJsonAsync(Questions, request, AppJson.Options);
        var update = await student.PutAsJsonAsync($"{Questions}/{Guid.CreateVersion7()}", request, AppJson.Options);

        create.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        update.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // AC14, BR1: the database keeps the marks per option and has no count or scoring column.
    [Fact]
    public async Task TheQuestionsTable_HasNoCorrectCountOrScoringModeColumn()
    {
        var columns = await QueryAsync(context => Task.FromResult(
            context.Model.FindEntityType(typeof(Domain.Entities.Question))!
                .GetProperties()
                .Select(property => property.Name)
                .ToList()));

        columns.Should().NotContain(name =>
            name.Contains("CorrectCount", StringComparison.OrdinalIgnoreCase)
            || name.Contains("ScoringMode", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Partial", StringComparison.OrdinalIgnoreCase));
    }
}
