using System.Net;

namespace Simulab.QuestionBank.Tests;

/// <summary>
/// F-103 AC13 (BR9), review finding 1: the version check reads the options of a version as well as its statement. A
/// published question whose option showed a picture keeps the file when the text drops it.
/// </summary>
public sealed class QuestionImageOptionReleaseTests : QuestionImageApiTests
{
    [Fact]
    public async Task Save_APublishedQuestionWithoutThePictureOfAnOption_KeepsTheFileBecauseTheVersionHoldsIt()
    {
        var curator = await CuratorAsync();
        var topic = await TopicAsync(await AdminAsync());
        var image = await UploadedAsync(curator);
        var draft = await CreatedAsync(curator, WithImages(topic.Id, "<p>Which one is a circle?</p>", $"<p>A</p>{Img(image.Id, "A circle")}", "<p>B</p>"));
        (await curator.PostAsync($"{Questions}/{draft.Id}/publish", null)).StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await PutQuestionAsync(
            curator, draft.Id, WithImages(topic.Id, "<p>Which one is a circle?</p>", "<p>A</p>", "<p>B</p>") with { ChangeReason = "The picture was wrong." });

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        Files.Names.Should().ContainSingle();
        (await RowAsync(image.Id))!.IsDeleted.Should().BeFalse();
    }

    // The same draft without any version loses the file: the check does not keep what nothing holds.
    [Fact]
    public async Task Save_ADraftWithoutThePictureOfAnOption_DeletesTheFile()
    {
        var curator = await CuratorAsync();
        var topic = await TopicAsync(await AdminAsync());
        var image = await UploadedAsync(curator);
        var draft = await CreatedAsync(curator, WithImages(topic.Id, "<p>S</p>", $"<p>A</p>{Img(image.Id, "A circle")}", "<p>B</p>"));

        var response = await PutQuestionAsync(curator, draft.Id, WithImages(topic.Id, "<p>S</p>", "<p>A</p>", "<p>B</p>"));

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        Files.Names.Should().BeEmpty();
        (await RowAsync(image.Id))!.IsDeleted.Should().BeTrue();
    }
}
