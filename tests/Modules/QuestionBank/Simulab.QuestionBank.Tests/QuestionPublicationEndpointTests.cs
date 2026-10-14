using System.Net;
using System.Net.Http.Json;
using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Serialization;

namespace Simulab.QuestionBank.Tests;

/// <summary>F-95, publication through HTTP: the checks of BR4 on a publish, and the round trip of BR12.</summary>
public sealed class QuestionPublicationEndpointTests : QuestionBankApiTests
{
    private static Task<HttpResponseMessage> PublishAsync(HttpClient client, Guid id) =>
        client.PostAsync($"{Questions}/{id}/publish", null);

    private static Task<HttpResponseMessage> UnpublishAsync(HttpClient client, Guid id) =>
        client.PostAsync($"{Questions}/{id}/unpublish", null);

    private async Task<(HttpClient Admin, QuestionResponse Question)> DraftWithAsync(
        IReadOnlyList<SaveQuestionOptionRequest> options)
    {
        var admin = await AdminAsync();
        var topic = await TopicAsync(admin);
        var question = await CreatedAsync(admin, Complete(topic.Id) with { Options = options });

        return (admin, question);
    }

    // AC4: one option, or an empty option text, refuses the publish with its own code.
    [Fact]
    public async Task Publish_OneOption_IsRefusedWithOptionsCount()
    {
        var (admin, question) = await DraftWithAsync([new SaveQuestionOptionRequest("<p>Paris</p>", true)]);

        var response = await PublishAsync(admin, question.Id);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(QuestionBankErrorCodes.QuestionOptionsCount);
        (await GetAsync(admin, question.Id)).Status.Should().Be(QuestionStatus.Draft);
    }

    [Fact]
    public async Task Publish_EmptyOptionText_IsRefusedWithOptionTextRequired()
    {
        var (admin, question) = await DraftWithAsync(
            [new SaveQuestionOptionRequest("<p><br></p>", true), new SaveQuestionOptionRequest("<p>Lyon</p>")]);

        var response = await PublishAsync(admin, question.Id);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(QuestionBankErrorCodes.QuestionOptionTextRequired);
    }

    // AC5: exactly one correct option.
    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public async Task Publish_NoKeyOrTwoKeys_IsRefusedWithKeyInvalid(int correct)
    {
        var (admin, question) = await DraftWithAsync(
        [
            new SaveQuestionOptionRequest("<p>Paris</p>", correct == 2),
            new SaveQuestionOptionRequest("<p>Lyon</p>", correct == 2)
        ]);

        var response = await PublishAsync(admin, question.Id);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(QuestionBankErrorCodes.QuestionKeyInvalid);
    }

    [Fact]
    public async Task Publish_CompleteDraft_BecomesPublished()
    {
        var (admin, question) = await DraftWithAsync(Complete(Guid.Empty).Options!);

        var response = await PublishAsync(admin, question.Id);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var published = (await response.Content.ReadFromJsonAsync<QuestionResponse>(AppJson.Options))!;
        published.Status.Should().Be(QuestionStatus.Published);
        (await GetAsync(admin, question.Id)).Status.Should().Be(QuestionStatus.Published);
    }

    // AC6, BR12: unpublish returns to draft with the same content; the wrong state is a 409 with its own code.
    [Fact]
    public async Task Unpublish_ReturnsToDraft_WithTheSameContent()
    {
        var admin = await AdminAsync();
        var published = await PublishedAsync(admin, (await TopicAsync(admin)).Id);

        var response = await UnpublishAsync(admin, published.Id);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var draft = await GetAsync(admin, published.Id);
        draft.Status.Should().Be(QuestionStatus.Draft);
        draft.StatementHtml.Should().Be(published.StatementHtml);
        draft.Options.Should().Equal(published.Options);
    }

    [Fact]
    public async Task Publish_AlreadyPublished_Is409AlreadyPublished()
    {
        var admin = await AdminAsync();
        var published = await PublishedAsync(admin, (await TopicAsync(admin)).Id);

        var response = await PublishAsync(admin, published.Id);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(QuestionBankErrorCodes.QuestionAlreadyPublished);
    }

    [Fact]
    public async Task Unpublish_Draft_Is409NotPublished()
    {
        var (admin, question) = await DraftWithAsync(Complete(Guid.Empty).Options!);

        var response = await UnpublishAsync(admin, question.Id);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(QuestionBankErrorCodes.QuestionNotPublished);
    }

    [Fact]
    public async Task PublishAndUnpublish_UnknownId_Is404NotFound()
    {
        var admin = await AdminAsync();
        var missing = Guid.CreateVersion7();

        foreach (var response in new[] { await PublishAsync(admin, missing), await UnpublishAsync(admin, missing) })
        {
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            CodeOf(await response.Content.ReadAsStringAsync()).Should().Be(QuestionBankErrorCodes.QuestionNotFound);
        }
    }

    // After unpublishing, the draft can be deleted (BR11).
    [Fact]
    public async Task AnUnpublishedQuestion_CanBeDeleted()
    {
        var admin = await AdminAsync();
        var published = await PublishedAsync(admin, (await TopicAsync(admin)).Id);
        await UnpublishAsync(admin, published.Id);

        var response = await admin.DeleteAsync($"{Questions}/{published.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
