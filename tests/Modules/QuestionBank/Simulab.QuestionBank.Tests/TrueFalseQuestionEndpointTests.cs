using System.Net;
using System.Net.Http.Json;
using Simulab.Catalog.Contracts;
using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Serialization;

namespace Simulab.QuestionBank.Tests;

/// <summary>
/// F-97 through HTTP, on a real PostgreSQL: authoring a true/false question, its key, the refusals of BR2 and
/// BR3, publishing, the rules both types share, the list and the permission.
/// </summary>
public sealed class TrueFalseQuestionEndpointTests : OriginApiTests
{
    private static SaveQuestionRequest TrueFalse(
        Guid topicId,
        bool? key = null,
        string statement = "<p>A capital da França é Paris.</p>") =>
        new(topicId, "pt-BR", statement, Type: QuestionType.TrueFalse, TrueFalseKey: key);

    private static Task<HttpResponseMessage> PublishAsync(HttpClient client, Guid id) =>
        client.PostAsync($"{Questions}/{id}/publish", null);

    // AC1: type chosen at creation, no key yet: a draft of that type, with no options, listed with its type.
    [Fact]
    public async Task Create_TrueFalseWithNoKey_IsStoredAsADraftOfThatType_AndListedWithIt()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);

        var question = await CreatedAsync(admin, TrueFalse(topic.Id));

        question.Type.Should().Be(QuestionType.TrueFalse);
        question.Status.Should().Be(QuestionStatus.Draft);
        question.Options.Should().BeEmpty();
        question.TrueFalseKey.Should().BeNull();
        var stored = await GetAsync(admin, question.Id);
        stored.Type.Should().Be(QuestionType.TrueFalse);
        stored.TrueFalseKey.Should().BeNull();
        (await ListAsync(admin, "?pageSize=100")).Items.Single(item => item.Id == question.Id)
            .Type.Should().Be(QuestionType.TrueFalse);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Create_TrueFalseWithAKey_StoresAndReturnsIt(bool key)
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);

        var question = await CreatedAsync(admin, TrueFalse(topic.Id, key));

        question.TrueFalseKey.Should().Be(key);
        (await GetAsync(admin, question.Id)).TrueFalseKey.Should().Be(key);
    }

    // AC2, BR3: options on a true/false question, a key on a single-choice one: 400, and nothing is stored.
    [Fact]
    public async Task Create_TrueFalseWithOptions_IsRefusedWithOptionsNotAllowed_AndNothingIsStored()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var marker = Marker();
        var request = TrueFalse(topic.Id, statement: $"<p>{marker}</p>") with
        {
            Options = [new SaveQuestionOptionRequest("<p>Paris</p>", true)]
        };

        var response = await CreateAsync(admin, request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(QuestionBankErrorCodes.QuestionOptionsNotAllowed);
        (await ListAsync(admin, $"?search={marker}")).Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Create_TrueFalseWithAnEmptyOptionList_IsAccepted()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);

        var question = await CreatedAsync(admin, TrueFalse(topic.Id, true) with { Options = [] });

        question.Options.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData(QuestionType.SingleChoice)]
    public async Task Create_SingleChoiceWithAKey_IsRefusedWithKeyNotAllowed_AndNothingIsStored(QuestionType? type)
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var marker = Marker();
        var request = Complete(topic.Id, $"<p>{marker}</p>") with { Type = type, TrueFalseKey = true };

        var response = await CreateAsync(admin, request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(QuestionBankErrorCodes.QuestionKeyNotAllowed);
        (await ListAsync(admin, $"?search={marker}")).Items.Should().BeEmpty();
    }

    // AC3, BR4: a draft with no key cannot be published; with a key it can, whichever the key.
    [Fact]
    public async Task Publish_TrueFalseWithNoKey_IsRefusedWithTrueFalseKeyRequired()
    {
        var admin = await AdminAsync();
        var question = await CreatedAsync(admin, TrueFalse((await TopicAsync(admin)).Id));

        var response = await PublishAsync(admin, question.Id);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(QuestionBankErrorCodes.QuestionTrueFalseKeyRequired);
        (await GetAsync(admin, question.Id)).Status.Should().Be(QuestionStatus.Draft);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Publish_TrueFalseWithAKey_BecomesPublished(bool key)
    {
        var admin = await AdminAsync();
        var question = await CreatedAsync(admin, TrueFalse((await TopicAsync(admin)).Id, key));

        var response = await PublishAsync(admin, question.Id);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var published = (await response.Content.ReadFromJsonAsync<QuestionResponse>(AppJson.Options))!;
        published.Status.Should().Be(QuestionStatus.Published);
        published.TrueFalseKey.Should().Be(key);
    }

    [Fact]
    public async Task Update_TrueFalseDraft_SettingTheKeyThenPublishing_Works()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var question = await CreatedAsync(admin, TrueFalse(topic.Id));

        var updated = await UpdatedAsync(admin, question.Id, TrueFalse(topic.Id, false));
        var response = await PublishAsync(admin, question.Id);

        updated.TrueFalseKey.Should().BeFalse();
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
    }

    // AC4, BR4: a published key may change, never be cleared; a refusal leaves the stored question as it was.
    [Fact]
    public async Task Update_PublishedTrueFalse_ChangingTheKey_StaysPublishedWithTheNewKey()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var question = await CreatedAsync(admin, TrueFalse(topic.Id, true));
        (await PublishAsync(admin, question.Id)).StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await UpdatedAsync(admin, question.Id, TrueFalse(topic.Id, false));

        updated.Status.Should().Be(QuestionStatus.Published);
        updated.TrueFalseKey.Should().BeFalse();
        (await GetAsync(admin, question.Id)).TrueFalseKey.Should().BeFalse();
    }

    [Fact]
    public async Task Update_PublishedTrueFalse_ClearingTheKey_IsRefused_AndTheStoredQuestionIsUnchanged()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var question = await CreatedAsync(admin, TrueFalse(topic.Id, true));
        (await PublishAsync(admin, question.Id)).StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await UpdateAsync(admin, question.Id, TrueFalse(topic.Id, null, "<p>Outro enunciado.</p>"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(QuestionBankErrorCodes.QuestionTrueFalseKeyRequired);
        var stored = await GetAsync(admin, question.Id);
        stored.TrueFalseKey.Should().BeTrue();
        stored.StatementHtml.Should().Be("<p>A capital da França é Paris.</p>");
        stored.Status.Should().Be(QuestionStatus.Published);
    }

    // AC5, BR2: a create that names no type is single choice and behaves as in F-95.
    [Fact]
    public async Task Create_WithNoType_IsSingleChoice_WithNoKey()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);

        var question = await CreatedAsync(admin, Complete(topic.Id));

        question.Type.Should().Be(QuestionType.SingleChoice);
        question.TrueFalseKey.Should().BeNull();
        question.Options.Should().HaveCount(2);
    }

    // AC6, BR2: the type never changes; naming the stored type is accepted.
    [Fact]
    public async Task Update_NamingAnotherType_Is409TypeChanged_AndTheStoredQuestionIsUnchanged()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var trueFalse = await CreatedAsync(admin, TrueFalse(topic.Id, true));
        var single = await CreatedAsync(admin, Complete(topic.Id));

        var toSingle = await UpdateAsync(
            admin, trueFalse.Id, Complete(topic.Id, "<p>Mudou</p>") with { Type = QuestionType.SingleChoice });
        var toTrueFalse = await UpdateAsync(
            admin, single.Id, TrueFalse(topic.Id, false, "<p>Mudou</p>"));

        foreach (var response in new[] { toSingle, toTrueFalse })
        {
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
            CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(QuestionBankErrorCodes.QuestionTypeChanged);
        }

        var storedTrueFalse = await GetAsync(admin, trueFalse.Id);
        storedTrueFalse.Type.Should().Be(QuestionType.TrueFalse);
        storedTrueFalse.TrueFalseKey.Should().BeTrue();
        storedTrueFalse.StatementHtml.Should().Be(trueFalse.StatementHtml);
        var storedSingle = await GetAsync(admin, single.Id);
        storedSingle.Type.Should().Be(QuestionType.SingleChoice);
        storedSingle.Options.Should().HaveCount(2);
        storedSingle.StatementHtml.Should().Be(single.StatementHtml);
    }

    [Fact]
    public async Task Update_NamingItsOwnType_IsAccepted()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var question = await CreatedAsync(admin, TrueFalse(topic.Id, true));

        var updated = await UpdatedAsync(admin, question.Id, TrueFalse(topic.Id, false));

        updated.Type.Should().Be(QuestionType.TrueFalse);
        updated.TrueFalseKey.Should().BeFalse();
    }

    [Fact]
    public async Task Update_NamingNoType_KeepsTheStoredType()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var question = await CreatedAsync(admin, TrueFalse(topic.Id, true));

        var updated = await UpdatedAsync(
            admin, question.Id, TrueFalse(topic.Id, false) with { Type = null });

        updated.Type.Should().Be(QuestionType.TrueFalse);
        updated.TrueFalseKey.Should().BeFalse();
    }

    // AC7, BR5: the rules both types share hold for a true/false question.
    [Fact]
    public async Task Create_TrueFalse_SanitizesTheHtmlOfTheTextFields()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        const string hostile = "<p onclick=\"x()\">ok <sup>2</sup></p><script>alert(1)</script>";

        var question = await CreatedAsync(
            admin,
            TrueFalse(topic.Id, true, hostile) with { BaseTextHtml = hostile, ExplanationHtml = hostile });

        foreach (var field in new[] { question.StatementHtml, question.BaseTextHtml!, question.ExplanationHtml! })
        {
            field.Should().NotContain("script").And.NotContain("onclick");
            field.Should().Contain("<sup>2</sup>");
        }
    }

    [Fact]
    public async Task Create_TrueFalse_StatementOverItsLimitDifficultyOutOfRangeAndEmptyStatement_AreRefused()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);

        var tooLong = await CreateAsync(admin, TrueFalse(topic.Id, true, new string('a', QuestionLimits.StatementMaxLength + 1)));
        var difficulty = await CreateAsync(admin, TrueFalse(topic.Id, true) with { Difficulty = 6 });
        var empty = await CreateAsync(admin, TrueFalse(topic.Id, true, "<p><br></p>"));

        tooLong.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await tooLong.Content.ReadAsStringAsync()).Should().Be(QuestionBankErrorCodes.QuestionStatementTooLong);
        difficulty.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await difficulty.Content.ReadAsStringAsync()).Should().Be(QuestionBankErrorCodes.QuestionDifficultyInvalid);
        empty.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await empty.Content.ReadAsStringAsync()).Should().Be(QuestionBankErrorCodes.QuestionStatementRequired);
    }

    [Fact]
    public async Task Create_TrueFalse_WithAPaperNumberAlreadyTaken_IsRefusedWithPaperNumberTaken()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var edition = await EditionAsync(admin);
        await CreatedAsync(admin, TrueFalse(topic.Id, true) with { ExamEditionId = edition.Id, PaperNumber = 7 });

        var response = await CreateAsync(
            admin, TrueFalse(topic.Id, false) with { ExamEditionId = edition.Id, PaperNumber = 7 });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(QuestionBankErrorCodes.QuestionPaperNumberTaken);
    }

    [Fact]
    public async Task Delete_PublishedTrueFalse_IsRefused_AndADraftIsDeleted()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var published = await CreatedAsync(admin, TrueFalse(topic.Id, true));
        (await PublishAsync(admin, published.Id)).StatusCode.Should().Be(HttpStatusCode.OK);
        var draft = await CreatedAsync(admin, TrueFalse(topic.Id));

        var refused = await admin.DeleteAsync($"{Questions}/{published.Id}");
        var deleted = await admin.DeleteAsync($"{Questions}/{draft.Id}");

        refused.StatusCode.Should().Be(HttpStatusCode.Conflict);
        CodeOf(await refused.Content.ReadAsStringAsync()).Should().Be(QuestionBankErrorCodes.QuestionDeleteRequiresDraft);
        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteTopic_WithOnlyATrueFalseQuestion_IsRefusedWithTopicInUse()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        await CreatedAsync(admin, TrueFalse(topic.Id, true));

        var response = await admin.DeleteAsync($"/api/v1/catalog/topics/{topic.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(CatalogErrorCodes.TopicInUse);
    }

    // AC9, BR8: questions of both types in one list, each row naming its type.
    [Fact]
    public async Task List_QuestionsOfBothTypes_NameTheTypeOfEachRow()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var marker = Marker();
        var single = await CreatedAsync(admin, Complete(topic.Id, $"<p>{marker} escolha</p>"));
        var trueFalse = await CreatedAsync(admin, TrueFalse(topic.Id, true, $"<p>{marker} certo ou errado</p>"));

        var items = (await ListAsync(admin, $"?search={marker}")).Items;

        items.Should().HaveCount(2);
        items.Single(item => item.Id == single.Id).Type.Should().Be(QuestionType.SingleChoice);
        items.Single(item => item.Id == trueFalse.Id).Type.Should().Be(QuestionType.TrueFalse);
    }

    // AC12, BR5: no permission, no true/false endpoint.
    [Fact]
    public async Task CreateAndUpdate_OfATrueFalseQuestion_WithoutThePermission_AnswerForbidden()
    {
        var student = await StudentAsync();
        var request = TrueFalse(Guid.CreateVersion7(), true);

        var create = await student.PostAsJsonAsync(Questions, request, AppJson.Options);
        var update = await student.PutAsJsonAsync($"{Questions}/{Guid.CreateVersion7()}", request, AppJson.Options);

        create.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        update.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ACurator_CanAuthorATrueFalseQuestion()
    {
        var curator = await CuratorAsync();
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);

        var question = await CreatedAsync(curator, TrueFalse(topic.Id, true));

        question.Type.Should().Be(QuestionType.TrueFalse);
    }

    // AC8, BR1: the stored row has the key and no penalty, weight or score column.
    [Fact]
    public async Task TheQuestionsTable_HasTheKeyAndNoPenaltyWeightOrScoreColumn()
    {
        var columns = await QueryAsync(context => Task.FromResult(
            context.Model.FindEntityType(typeof(Domain.Entities.Question))!
                .GetProperties()
                .Select(property => property.Name)
                .ToList()));

        columns.Should().Contain("TrueFalseKey");
        columns.Should().NotContain(name =>
            name.Contains("Penalt", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Weight", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Score", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Fraction", StringComparison.OrdinalIgnoreCase));
    }
}
