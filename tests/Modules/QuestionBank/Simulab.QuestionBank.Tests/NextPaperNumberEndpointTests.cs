using System.Net;
using System.Net.Http.Json;
using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Serialization;

namespace Simulab.QuestionBank.Tests;

/// <summary>
/// F-99 BR4, BR7: the paper number transcription mode suggests next. It is a convenience read: the uniqueness
/// check of F-96 stays the rule.
/// </summary>
public sealed class NextPaperNumberEndpointTests : OriginApiTests
{
    private static async Task<int> NextAsync(HttpClient client, Guid editionId) =>
        (await client.GetFromJsonAsync<NextPaperNumberResponse>(
            $"{QuestionBank}/editions/{editionId}/next-paper-number", AppJson.Options))!.PaperNumber;

    // AC2
    [Fact]
    public async Task NextPaperNumber_OfAnEditionWithNoQuestions_IsOne()
    {
        var admin = await AdminAsync();
        var edition = await EditionAsync(admin);

        (await NextAsync(admin, edition.Id)).Should().Be(1);
    }

    // AC2
    [Fact]
    public async Task NextPaperNumber_IsTheHighestLiveNumberPlusOne_AndIgnoresDeletedQuestions()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var edition = await EditionAsync(admin);
        foreach (var number in new[] { 1, 2, 7 })
        {
            await CreatedAsync(admin, FromEdition(topic.Id, edition.Id, number, null, $"<p>Enunciado {number}</p>"));
        }

        var last = await CreatedAsync(admin, FromEdition(topic.Id, edition.Id, 9, null, "<p>Enunciado 9</p>"));
        (await admin.DeleteAsync($"{Questions}/{last.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await NextAsync(admin, edition.Id)).Should().Be(8);
    }

    // AC2: the number moves with every save, so the screen asks again after each one.
    [Fact]
    public async Task NextPaperNumber_FollowsASavedQuestion()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var edition = await EditionAsync(admin);

        await CreatedAsync(admin, FromEdition(topic.Id, edition.Id, 1, null, "<p>Primeira</p>"));

        (await NextAsync(admin, edition.Id)).Should().Be(2);
    }

    // AC10
    [Fact]
    public async Task NextPaperNumber_OfAnUnknownEdition_Is404()
    {
        var admin = await AdminAsync();

        var response = await admin.GetAsync($"{QuestionBank}/editions/{Guid.CreateVersion7()}/next-paper-number");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(QuestionBankErrorCodes.QuestionExamEditionNotFound);
    }

    // AC8
    [Fact]
    public async Task NextPaperNumber_WithoutTheManagePermission_IsForbidden()
    {
        var admin = await AdminAsync();
        var student = await StudentAsync();
        var edition = await EditionAsync(admin);

        var response = await student.GetAsync($"{QuestionBank}/editions/{edition.Id}/next-paper-number");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // AC8: a curator holds questions.manage and nothing from the catalog.
    [Fact]
    public async Task NextPaperNumber_ForACurator_Answers200()
    {
        var admin = await AdminAsync();
        var curator = await CuratorAsync();
        var edition = await EditionAsync(admin);

        (await NextAsync(curator, edition.Id)).Should().Be(1);
    }

    // AC3, AC4: transcription saves through the regular route, so the F-96 refusal of a repeated number stands.
    [Fact]
    public async Task SavingTheSuggestedNumberTwice_IsRefusedTheSecondTime()
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var edition = await EditionAsync(admin);
        var suggested = await NextAsync(admin, edition.Id);
        await CreatedAsync(admin, FromEdition(topic.Id, edition.Id, suggested, null, "<p>Primeira</p>"));

        var response = await admin.PostAsJsonAsync(
            Questions,
            FromEdition(topic.Id, edition.Id, suggested, null, "<p>Repetida</p>"),
            AppJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be("question.paper_number_taken");
    }
}
