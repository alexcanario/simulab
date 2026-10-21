using System.Net;
using System.Net.Http.Json;
using Simulab.QuestionBank.Contracts;
using Simulab.SharedKernel.Serialization;

namespace Simulab.QuestionBank.Tests;

/// <summary>
/// F-103 through HTTP: when an image that left a text is deleted, and when it stays because a published version keeps
/// pointing at it (BR9, BR10) — for a draft (AC13), a published question (AC13) and a base text used by one (AC14).
/// </summary>
public sealed class QuestionImageReleaseTests : QuestionImageApiTests
{
    // AC13 (UC2, BR10): a draft that drops its image loses the blob and the row.
    [Fact]
    public async Task Save_ADraftWithoutItsImageAnymore_DeletesTheFileAndTheRow()
    {
        var curator = await CuratorAsync();
        var topic = await TopicAsync(await AdminAsync());
        var image = await UploadedAsync(curator);
        var draft = await CreatedAsync(curator, WithImages(topic.Id, $"<p>S</p>{Img(image.Id)}", "<p>A</p>", "<p>B</p>"));
        Files.Names.Should().ContainSingle();

        var response = await PutQuestionAsync(curator, draft.Id, WithImages(topic.Id, "<p>S</p>", "<p>A</p>", "<p>B</p>"));

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        Files.Names.Should().BeEmpty();
        (await RowAsync(image.Id))!.IsDeleted.Should().BeTrue();
        using var gone = await curator.GetAsync($"{Images}/{image.Id}");
        gone.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // AC13 (BR9): a published question whose latest version holds the image keeps the file when the text drops it.
    [Fact]
    public async Task Save_APublishedQuestionWithoutItsImageAnymore_KeepsTheFileBecauseAVersionHoldsIt()
    {
        var curator = await CuratorAsync();
        var topic = await TopicAsync(await AdminAsync());
        var image = await UploadedAsync(curator);
        var draft = await CreatedAsync(curator, WithImages(topic.Id, $"<p>S</p>{Img(image.Id)}", "<p>A</p>", "<p>B</p>"));
        (await curator.PostAsync($"{Questions}/{draft.Id}/publish", null)).StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await PutQuestionAsync(
            curator, draft.Id, WithImages(topic.Id, "<p>S, without the picture</p>", "<p>A</p>", "<p>B</p>") with { ChangeReason = "The picture was wrong." });

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        Files.Names.Should().ContainSingle();
        (await RowAsync(image.Id))!.IsDeleted.Should().BeFalse();
        using var still = await curator.GetAsync($"{Images}/{image.Id}");
        still.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // AC13 (BR9): an image only the live draft held, no version ever did, goes when the text drops it.
    [Fact]
    public async Task Save_AnUnpublishedQuestionThatAddedAnImageAfterItsVersionsAndDropsIt_DeletesTheFile()
    {
        var curator = await CuratorAsync();
        var topic = await TopicAsync(await AdminAsync());
        var draft = await CreatedAsync(curator, WithImages(topic.Id, "<p>S</p>", "<p>A</p>", "<p>B</p>"));
        (await curator.PostAsync($"{Questions}/{draft.Id}/publish", null)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await curator.PostAsync($"{Questions}/{draft.Id}/unpublish", null)).StatusCode.Should().Be(HttpStatusCode.OK);
        var image = await UploadedAsync(curator);
        (await PutQuestionAsync(curator, draft.Id, WithImages(topic.Id, $"<p>S</p>{Img(image.Id)}", "<p>A</p>", "<p>B</p>"))).StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await PutQuestionAsync(curator, draft.Id, WithImages(topic.Id, "<p>S</p>", "<p>A</p>", "<p>B</p>"));

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        Files.Names.Should().BeEmpty();
        (await RowAsync(image.Id))!.IsDeleted.Should().BeTrue();
    }

    // BR10: deleting the owner keeps its files.
    [Fact]
    public async Task Delete_ADraftQuestionWithAnImage_KeepsTheFile()
    {
        var curator = await CuratorAsync();
        var topic = await TopicAsync(await AdminAsync());
        var image = await UploadedAsync(curator);
        var draft = await CreatedAsync(curator, WithImages(topic.Id, $"<p>S</p>{Img(image.Id)}", "<p>A</p>", "<p>B</p>"));

        var response = await curator.DeleteAsync($"{Questions}/{draft.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent, await response.Content.ReadAsStringAsync());
        Files.Names.Should().ContainSingle();
        (await RowAsync(image.Id))!.IsDeleted.Should().BeFalse();
    }

    // AC14 (BR9): a base text image used by a published question stays when the text is edited without it.
    [Fact]
    public async Task Save_ABaseTextWithoutItsImageAnymore_KeepsTheFileWhileAPublishedQuestionVersionHoldsIt()
    {
        var curator = await CuratorAsync();
        var topic = await TopicAsync(await AdminAsync());
        var image = await UploadedAsync(curator);
        var text = await CreatedTextAsync(curator, $"<p>Read:</p>{Img(image.Id, "A chart")}");
        var question = await CreatedAsync(curator, WithImages(topic.Id, "<p>What does the chart show?</p>", "<p>A</p>", "<p>B</p>") with { BaseTextId = text.Id });
        (await curator.PostAsync($"{Questions}/{question.Id}/publish", null)).StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await PutTextAsync(curator, text.Id, text.Title, "<p>Read the passage:</p>");

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        Files.Names.Should().ContainSingle();
        (await RowAsync(image.Id))!.IsDeleted.Should().BeFalse();
        using var still = await curator.GetAsync($"{Images}/{image.Id}");
        still.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // BR10: a base text no version holds loses the file when its text drops the image.
    [Fact]
    public async Task Save_ABaseTextNoQuestionVersionHolds_DeletesTheFileWhenTheTextDropsTheImage()
    {
        var curator = await CuratorAsync();
        var image = await UploadedAsync(curator);
        var text = await CreatedTextAsync(curator, $"<p>Read:</p>{Img(image.Id, "A chart")}");

        var response = await PutTextAsync(curator, text.Id, text.Title, "<p>Read the passage:</p>");

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        Files.Names.Should().BeEmpty();
        (await RowAsync(image.Id))!.IsDeleted.Should().BeTrue();
    }

    // AC13: the version keeps pointing at the image it was published with.
    [Fact]
    public async Task Version_OfAPublishedQuestion_KeepsTheImageAddressAfterTheTextDroppedIt()
    {
        var curator = await CuratorAsync();
        var topic = await TopicAsync(await AdminAsync());
        var image = await UploadedAsync(curator);
        var draft = await CreatedAsync(curator, WithImages(topic.Id, $"<p>S</p>{Img(image.Id)}", "<p>A</p>", "<p>B</p>"));
        (await curator.PostAsync($"{Questions}/{draft.Id}/publish", null)).StatusCode.Should().Be(HttpStatusCode.OK);
        await PutQuestionAsync(
            curator, draft.Id, WithImages(topic.Id, "<p>S</p>", "<p>A</p>", "<p>B</p>") with { ChangeReason = "Dropped the picture." });

        var versions = (await curator.GetFromJsonAsync<List<QuestionVersionListItemResponse>>($"{Questions}/{draft.Id}/versions", AppJson.Options))!;
        var first = versions.Single(version => version.Number == 1);
        var detail = (await curator.GetFromJsonAsync<QuestionVersionResponse>($"{Questions}/{draft.Id}/versions/{first.Number}", AppJson.Options))!;

        detail.StatementHtml.Should().Contain($"/question-images/{image.Id}");
    }
}
